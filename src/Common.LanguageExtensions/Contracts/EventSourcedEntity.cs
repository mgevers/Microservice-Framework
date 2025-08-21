using CSharpFunctionalExtensions;

namespace Common.LanguageExtensions.Contracts;

public abstract class EventSourcedEntity<TDomainEvent, TEventData> : Entity<Guid>
    where TDomainEvent : DomainEvent<TEventData>
    where TEventData : IEventData
{
    protected EventSourcedEntity(Guid id) : base(id) { }

    protected EventSourcedEntity() : base() { }

    public List<TDomainEvent> DomainEvents { get; protected set; } = new List<TDomainEvent>();

    protected void AddDomainEvent(TEventData eventData)
    {
        var events = DomainEvents.ToList();
        events.Add(this.CreateDomainEvent(sequenceId: this.DomainEvents.Count + 1, eventData));

        DomainEvents = events;
    }

    protected abstract TDomainEvent CreateDomainEvent(int sequenceId, TEventData eventData);
}
