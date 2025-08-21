namespace Common.Testing.ServiceBus;

public class FakeMessageSession : IMessageSession
{
    public Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        return FakeServiceBus.Session.Publish(message, publishOptions, cancellationToken);
    }

    public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        return FakeServiceBus.Session.Publish(messageConstructor, publishOptions, cancellationToken);
    }

    public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        return FakeServiceBus.Session.Send(message, sendOptions, cancellationToken);
    }

    public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        return FakeServiceBus.Session.Send(messageConstructor, sendOptions, cancellationToken);
    }

    public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        return FakeServiceBus.Session.Subscribe(eventType, subscribeOptions, cancellationToken);
    }

    public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
    {
        return FakeServiceBus.Session.Unsubscribe(eventType, unsubscribeOptions, cancellationToken);
    }
}
