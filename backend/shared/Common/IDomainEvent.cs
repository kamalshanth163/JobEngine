using MediatR;

namespace JobEngine.Shared.Common;

// INotification = MediatR can publish this via mediator.Publish()
// We dispatch domain events AFTER SaveChangesAsync so they only fire
// when the DB transaction committed successfully
public interface IDomainEvent : INotification { }