using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.LanguageExtensions.Contracts;

public class DomainEvent<T>
    where T : IEventData
{
    public DomainEvent(Guid AggregateId, int sequenceId, T data)
    {
        this.AggregateId = AggregateId;
        this.SequenceId = sequenceId;
        this.EventTypeName = data.GetType().Name!;
        this.Json = JsonConvert.SerializeObject(data);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    protected DomainEvent() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Guid AggregateId { get; protected set; }

    public int SequenceId { get; private set; }

    [NotMapped]
    public T Data => JsonConvert.DeserializeObject<T>(this.Json)!;

    public string Json { get; private set; }

    public string EventTypeName { get; private set; }

    public bool IsEventType<TCompare>(out TCompare? eventData)
        where TCompare : IEventData
    {
        var isEventType = this.EventTypeName == typeof(TCompare).Name;
        eventData = isEventType ?
            JsonConvert.DeserializeObject<TCompare>(this.Json)
            : default(TCompare);

        return isEventType;
    }
}
