using JobEngine.Shared.Contracts.Events;
using JobService.Application.Commands;
using JobService.Application.Common.Interfaces;
using JobService.Domain.Entities;

namespace JobService.Tests;

public sealed class SubmitJobHandlerTests
{
    [Fact]
    public async Task Handle_CreatesQueuesAndPublishesJob()
    {
        var repository = new InMemoryJobRepository();
        var publisher = new CapturingEventPublisher();
        var unitOfWork = new CountingUnitOfWork();
        var quota = new TrackingTenantQuotaService();
        var handler = new SubmitJobHandler(repository, quota, publisher, unitOfWork);
        var tenantId = Guid.NewGuid();

        var jobId = await handler.Handle(
            new SubmitJobCommand(tenantId, "send-email", "{\"to\":\"user@test\"}", Priority: 5, MaxAttempts: 4),
            CancellationToken.None);

        var job = Assert.Single(repository.AddedJobs);
        Assert.Equal(jobId, job.Id);
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(tenantId, quota.LastTenantId);
        Assert.Equal(2, unitOfWork.SaveCalls);

        var evt = Assert.IsType<JobSubmittedEvent>(Assert.Single(publisher.Events));
        Assert.Equal(job.Id, evt.JobId);
        Assert.Equal(tenantId, evt.TenantId);
        Assert.Equal("send-email", evt.JobType);
        Assert.Equal(job.Payload, evt.Payload);
        Assert.Equal(job.Priority, evt.Priority);
        Assert.Equal(job.MaxAttempts, evt.MaxAttempts);
    }

    [Fact]
    public async Task Handle_DoesNotPersistWhenQuotaCheckFails()
    {
        var repository = new InMemoryJobRepository();
        var publisher = new CapturingEventPublisher();
        var unitOfWork = new CountingUnitOfWork();
        var handler = new SubmitJobHandler(
            repository,
            new ThrowingQuotaService(),
            publisher,
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new SubmitJobCommand(Guid.NewGuid(), "send-email", "{}"),
            CancellationToken.None));

        Assert.Empty(repository.AddedJobs);
        Assert.Empty(publisher.Events);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    private sealed class InMemoryJobRepository : IJobRepository
    {
        public List<Job> AddedJobs { get; } = [];

        public Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(AddedJobs.SingleOrDefault(x => x.Id == id));

        public Task<List<Job>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult(AddedJobs.Where(x => x.TenantId == tenantId).ToList());

        public Task AddAsync(Job job, CancellationToken ct = default)
        {
            AddedJobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<Job?> GetNextQueuedAsync(CancellationToken ct = default) =>
            Task.FromResult(AddedJobs.FirstOrDefault(x => x.Status == JobStatus.Queued));

        public void Update(Job job)
        {
        }
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class CapturingEventPublisher : IEventPublisher
    {
        public List<object> Events { get; } = [];

        public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
        {
            Events.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingTenantQuotaService : ITenantQuotaService
    {
        public Guid LastTenantId { get; private set; }

        public Task EnforceAsync(Guid tenantId, CancellationToken ct = default)
        {
            LastTenantId = tenantId;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingQuotaService : ITenantQuotaService
    {
        public Task EnforceAsync(Guid tenantId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Quota exceeded");
    }
}