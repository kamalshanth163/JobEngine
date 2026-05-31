using JobEngine.Shared.Contracts.Events;
using MassTransit;
using StackExchange.Redis;
using WorkerService.Clients;
using WorkerService.Locking;
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
    IWorkerIdentity _identity,
    ILogger<JobSubmittedConsumer> _logger
) : IConsumer<JobSubmittedEvent>
{
    public async Task Consume(ConsumeContext<JobSubmittedEvent> ctx)
    {
        var msg = ctx.Message;
        var ct = ctx.CancellationToken;

        // -- STEP 1: Idempotency check ----------------------------------
        // Redis key expires after 24h. If this job was already processed
        // (duplicate delivery from RabbitMQ), skip silently — do NOT throw
        // (throwing causes requeue which makes the problem worse)
        var idempotencyKey = $"job:processed:{msg.JobId}";
        var isFirstAttempt = await _redis.StringSetAsync(
            idempotencyKey, _identity.WorkerId,
            TimeSpan.FromHours(24),
            When.NotExists);
 
        if (!isFirstAttempt)
        {
            _logger.LogWarning("Duplicate delivery of job {JobId} — skipping", msg.JobId);
            return;
        }

        // -- STEP 2: Distributed Redis lock ----------------------------
        // Even though idempotency key handles duplicates, the lock prevents
        // two workers racing on the same message in edge cases
        await using var redisLock = await _lockManager
            .TryAcquireAsync($"job:lock:{msg.JobId}", TimeSpan.FromMinutes(5), ct);

        if (redisLock is null)
        {
            _logger.LogWarning("Could not acquire lock for job {JobId}", msg.JobId);
            return;
        }

        // -- STEP 3: Claim job in database -----------------------------
        await _statusUpdater.MarkRunningAsync(msg.JobId, _identity.WorkerId, ct);

        ExecutionResult result;
        try
        {
            // -- STEP 4: Delegate to Execution Service via HTTP -----------
            result = await _executor.ExecuteAsync(
                new ExecuteJobRequest(msg.JobId, msg.JobType, msg.Payload), ct);
        }
        catch (Exception ex)
        {
            // Execution Service unreachable — fail the job
            result = ExecutionResult.Fail(ex.Message);
        }

        // -- STEP 5: Update status + publish outcome event -------------
        if (result.Success)
        {
            await _statusUpdater.MarkCompletedAsync(msg.JobId, result.Output, ct);
            await _bus.Publish(new JobCompletedEvent
            {
                JobId = msg.JobId,
                TenantId = msg.TenantId,
                Result = result.Output,
                CompletedAt = DateTime.UtcNow,
                WebhookUrl = msg.WebhookUrl,
                WebhookSecret = msg.WebhookSecret
            }, ct);
        }
        else
        {
            // Persist failed attempt first so emitted event reflects committed DB state.
            var failure = await _statusUpdater.MarkFailedAsync(msg.JobId, result.Error!, ct);

            // Emit a failure event for every attempt; downstream services can track retries
            // and react differently when IsFinal=true (terminal/dead-letter condition).
            await _bus.Publish(new JobFailedEvent
            {
                JobId = msg.JobId,
                TenantId = msg.TenantId,
                Error = result.Error!,
                // Attempt number comes from DB after MarkFailed increments/status transition.
                AttemptNum = failure.Attempt,
                // True when max attempts are exhausted and job moved to DeadLetter.
                IsFinal = failure.IsFinal,
                WebhookUrl = msg.WebhookUrl,
                WebhookSecret = msg.WebhookSecret
            }, ct);

            // We publish JobFailedEvent on every failed attempt for observability/alerts.
            // For non-final failures, throw so MassTransit keeps ownership of retry timing/backoff.
            // Final failures are acknowledged here and remain in DeadLetter state.
            if (!failure.IsFinal)
                throw new JobExecutionException(result.Error!);
        }
    }
}