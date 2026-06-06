using JobService.Application.Common.Interfaces;
using MassTransit;

namespace JobService.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _pub;
    public MassTransitEventPublisher(IPublishEndpoint pub) => _pub = pub;

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
        => _pub.Publish(@event, ct);
}
