using MediatR;
using JobEngine.Shared.Contracts.Jobs;
using JobService.Application.Common.Interfaces;
using JobService.Domain.Entities;

namespace JobService.Application.Commands;

public sealed record SubmitJobCommand(
    Guid TenantId,
    string Type,
    string Payload,
    int Priority = 0,
    int MaxAttempts = 3,
    DateTime? ScheduledAt = null
) : IRequest<Guid>;

public sealed class SubmitJobHandler(
    IJobRepository _jobs,
    ITenantQuotaService _quota,
    IPublishEndpoint _bus,   // MassTransit abstracts RabbitMQ
    IUnitOfWork _uow
) : IRequestHandler<SubmitJobCommand, Guid>
{
    public async Task<Guid> Handle(
        SubmitJobCommand cmd, CancellationToken ct)
    {
        // 1. Tenant quota check — throws QuotaExceededException if over limit
        await _quota.EnforceAsync(cmd.TenantId, ct);

        // 2. Create job aggregate via factory — domain invariants enforced
        var job = Job.Create(
            cmd.TenantId, cmd.Type, cmd.Payload,
            cmd.Priority, cmd.MaxAttempts, cmd.ScheduledAt);

        await _jobs.AddAsync(job, ct);

        // 3. Persist first — if RabbitMQ publish fails we can retry
        //    This is the Outbox pattern — DB is source of truth
        await _uow.SaveChangesAsync(ct);

        // 4. Publish event to RabbitMQ via MassTransit
        //    Workers will consume this and execute the job
        job.MarkQueued();
        await _bus.Publish(new JobSubmittedEvent
        {
            JobId = job.Id,
            TenantId = job.TenantId,
            JobType = job.Type,
            Payload = job.Payload,
            Priority = job.Priority,
            MaxAttempts = job.MaxAttempts
        }, ct);

        await _uow.SaveChangesAsync(ct); // save Queued status

        return job.Id;
    }
}