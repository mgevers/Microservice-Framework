namespace Common.Infrastructure.Tests.ServiceBus.Utilities
{
    public class Event : IEvent
    {
        public Event(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}
