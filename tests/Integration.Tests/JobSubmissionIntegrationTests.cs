using JobEngine.Shared.Contracts.Events;
using JobService.Application;
using JobService.Application.Commands;
using JobService.Application.Common.Interfaces;
using JobService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

public sealed class JobSubmissionIntegrationTests
{
    [Fact]
    public async Task SubmitJob_ThroughMediator_PersistsAndPublishes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IEventPublisher, CapturingEventPublisher>();
        services.AddSingleton<IUnitOfWork, CountingUnitOfWork>();
        services.AddSingleton<ITenantQuotaService, AllowAllQuotaService>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var tenantId = Guid.NewGuid();
        var jobId = await mediator.Send(new SubmitJobCommand(
            tenantId,
            "send-email",
            "{\"to\":\"user@test\"}",
            Priority: 2,
            MaxAttempts: 4));

        var repo = (InMemoryJobRepository)provider.GetRequiredService<IJobRepository>();
        var uow = (CountingUnitOfWork)provider.GetRequiredService<IUnitOfWork>();
        var bus = (CapturingEventPublisher)provider.GetRequiredService<IEventPublisher>();

        var job = Assert.Single(repo.AddedJobs);
        Assert.Equal(jobId, job.Id);
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(2, uow.SaveCalls);

        var evt = Assert.IsType<JobSubmittedEvent>(Assert.Single(bus.Events));
        Assert.Equal(jobId, evt.JobId);
        Assert.Equal(tenantId, evt.TenantId);
        Assert.Equal("send-email", evt.JobType);
    }

    [Fact]
    public async Task SubmitJob_ThroughMediator_StopsWhenQuotaFails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IEventPublisher, CapturingEventPublisher>();
        services.AddSingleton<IUnitOfWork, CountingUnitOfWork>();
        services.AddSingleton<ITenantQuotaService, RejectAllQuotaService>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Send(new SubmitJobCommand(Guid.NewGuid(), "send-email", "{}")));

        var repo = (InMemoryJobRepository)provider.GetRequiredService<IJobRepository>();
        var uow = (CountingUnitOfWork)provider.GetRequiredService<IUnitOfWork>();
        var bus = (CapturingEventPublisher)provider.GetRequiredService<IEventPublisher>();

        Assert.Empty(repo.AddedJobs);
        Assert.Empty(bus.Events);
        Assert.Equal(0, uow.SaveCalls);
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

    private sealed class AllowAllQuotaService : ITenantQuotaService
    {
        public Task EnforceAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class RejectAllQuotaService : ITenantQuotaService
    {
        public Task EnforceAsync(Guid tenantId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Quota exceeded");
    }
}
