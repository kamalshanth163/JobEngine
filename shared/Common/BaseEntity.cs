namespace JobEngine.Shared.Common;

// Every domain entity inherits from this.
// Private setters = state changes only through domain methods.
// DomainEvents list = raised inside entities, dispatched after SaveChanges.
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    // Backing field — EF Core can populate via reflection,
    // but callers only get read-only IReadOnlyList
    private readonly List<IDomainEvent> _events = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent e) => _events.Add(e);
    public void ClearDomainEvents() => _events.Clear();

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}