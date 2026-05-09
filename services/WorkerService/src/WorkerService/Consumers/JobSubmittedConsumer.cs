using JobEngine.Shared.Contracts.Jobs;
using MassTransit;
using StackExchange.Redis;
using WorkerService.Clients;
using WorkerService.Services;
using ExecutionResult = WorkerService.Clients.ExecutionResult;

namespace WorkerService.Consumers;

// MassTransit IConsumer = RabbitMQ message handler
// MassTransit handles ACK automatically on success, NACK on exception
public sealed class JobSubmittedConsumer(
    IDistributedLockManager _lockManager,
    IExecutionServiceClient _executor,
    IJobStatusUpdater _statusUpdater,
    IPublishEndpoint _bus,
    IDatabase _redis,
    ILogger<JobSubmittedConsumer> _logger
) : IConsumer<JobSubmittedEvent>
{
    private readonly string _workerId =
        Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];

    public async Task Consume(ConsumeContext<JobSubmittedEvent> ctx)
    {
        var msg = ctx.Message;
        var ct = ctx.CancellationToken;

        // ── STEP 1: Idempotency check ──────────────────────────────────
        // Redis key expires after 24h. If this job was already processed
        // (duplicate delivery from RabbitMQ), skip silently — do NOT throw
        // (throwing causes requeue which makes the problem worse)
        var idempotencyKey = $"job:processed:{msg.JobId}";
        var alreadyDone = await _redis.StringSetAsync(
            idempotencyKey, _workerId,
            TimeSpan.FromHours(24),
            When.NotExists);

        if (!alreadyDone)
        {
            _logger.LogWarning("Duplicate delivery of job {JobId} — skipping", msg.JobId);
            return;
        }

        // ── STEP 2: Distributed Redis lock ────────────────────────────
        // Even though idempotency key handles duplicates, the lock prevents
        // two workers racing on the same message in edge cases
        await using var redisLock = await _lockManager
            .TryAcquireAsync($"job:lock:{msg.JobId}", TimeSpan.FromMinutes(5), ct);

        if (redisLock is null)
        {
            _logger.LogWarning("Could not acquire lock for job {JobId}", msg.JobId);
            return;
        }

        // ── STEP 3: Claim job in database ─────────────────────────────
        await _statusUpdater.MarkRunningAsync(msg.JobId, _workerId, ct);

        ExecutionResult result;
        try
        {
            // ── STEP 4: Delegate to Execution Service via HTTP ───────────
            result = await _executor.ExecuteAsync(new ExecuteJobRequest
            {
                JobId = msg.JobId,
                JobType = msg.JobType,
                Payload = msg.Payload
            }, ct);
        }
        catch (Exception ex)
        {
            // Execution Service unreachable — fail the job
            result = ExecutionResult.Failure(ex.Message);
        }

        // ── STEP 5: Update status + publish outcome event ─────────────
        if (result.Success)
        {
            await _statusUpdater.MarkCompletedAsync(msg.JobId, result.Output, ct);
            await _bus.Publish(new JobCompletedEvent
            {
                JobId = msg.JobId,
                TenantId = msg.TenantId,
                Result = result.Output,
                CompletedAt = DateTime.UtcNow
            }, ct);
        }
        else
        {
            var isFinal = msg.MaxAttempts <= 1;
            await _statusUpdater.MarkFailedAsync(msg.JobId, result.Error!, ct);
            await _bus.Publish(new JobFailedEvent
            {
                JobId = msg.JobId,
                TenantId = msg.TenantId,
                Error = result.Error!,
                AttemptNum = msg.MaxAttempts,
                IsFinal = isFinal
            }, ct);

            // If not final, throw so MassTransit requeues for retry
            // MassTransit + configured retry policy handles the backoff
            if (!isFinal)
                throw new JobExecutionException(result.Error!);
        }
    }
}