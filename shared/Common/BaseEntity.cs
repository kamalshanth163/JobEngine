namespace JobEngine.Shared.Common;

// Every entity inherits from this.
// private setters = state only changes through domain methods
// DomainEvents = raised inside entity, dispatched AFTER SaveChanges
//   so events only fire when the DB write actually succeeded
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _events = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent e) => _events.Add(e);
    public void ClearDomainEvents() => _events.Clear();
    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}