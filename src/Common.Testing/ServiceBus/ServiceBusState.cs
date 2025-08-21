namespace Common.Testing.ServiceBus;

public class ServiceBusState
{
    public ServiceBusState(
        IReadOnlyCollection<object> sentMessages,
        IReadOnlyCollection<object> publishedMessages,
        IReadOnlyCollection<object> repliedMessages)
    {
        SentMessages = sentMessages.ToList();
        PublishedMessages = publishedMessages.ToList();
        RepliedMessages = repliedMessages.ToList();
    }

    public IReadOnlyList<object> SentMessages { get; }
    public IReadOnlyList<object> PublishedMessages { get; }
    public IReadOnlyList<object> RepliedMessages { get; }
}
