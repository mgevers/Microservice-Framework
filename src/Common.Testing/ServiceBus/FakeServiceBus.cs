using NServiceBus.Testing;

namespace Common.Testing.ServiceBus;

public sealed class FakeServiceBus : IDisposable
{
    private static readonly AsyncLocal<TestableMessageSession?> asyncLocalSession = new();
    public static TestableMessageSession Session => GetTestableMessageSession();

    private FakeServiceBus(TestableMessageSession session)
    {
        asyncLocalSession.Value = session;
    }

    public static FakeServiceBus Initialize()
    {
        return new FakeServiceBus(new TestableMessageSession());
    }

    public static ServiceBusState ServiceBusState => GetServiceBusState();

    public void Dispose()
    {
        asyncLocalSession.Value = null;
    }

    private static ServiceBusState GetServiceBusState()
    {
        return new ServiceBusState(
            sentMessages: Session.SentMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
            publishedMessages: Session.PublishedMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
            repliedMessages: Array.Empty<IMessage>());
    }

    private static TestableMessageSession GetTestableMessageSession()
    {
        if (asyncLocalSession.Value == null)
        {
            throw new Exception($"attempting to use {nameof(FakeServiceBus)} before initialization, or after disposal");
        }

        return asyncLocalSession.Value;
    }
}
