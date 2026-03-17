using MediatR;

namespace JobEngine.Shared.Common;

// MediatR INotification = can be published via mediator.Publish()
// We dispatch these after SaveChangesAsync so they only fire if the
// DB write succeeded — prevents events for transactions that rolled back
public interface IDomainEvent : INotification { }