using Common.Testing.Assert;

namespace Common.Testing.FluentTesting.Asserts;

public static class ServiceBusAssertExtensions
{
    public static async Task<T> AssertPublishedEvents<T>(this Task<T> resultTask, IReadOnlyCollection<object> expectedPublishedEvents)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(expectedPublishedEvents, result.ServiceBusState.PublishedMessages);
        return result;
    }

    public static async Task<T> AssertPublishedEvent<T>(this Task<T> resultTask, object expectedPublishedEvent)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(new[] { expectedPublishedEvent }, result.ServiceBusState.PublishedMessages);
        return result;
    }

    public static async Task<T> AssertRepliedMessages<T>(this Task<T> resultTask, IReadOnlyCollection<IMessage> expectedRepliedMessages)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(expectedRepliedMessages, result.ServiceBusState.RepliedMessages);
        return result;
    }

    public static async Task<T> AssertRepliedMessage<T>(this Task<T> resultTask, IMessage expectedRepliedEvent)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(new[] { expectedRepliedEvent }, result.ServiceBusState.RepliedMessages);
        return result;
    }

    public static async Task<T> AssertNoPublishedEvents<T>(this Task<T> resultTask)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertMessages(Array.Empty<IMessage>(), result.ServiceBusState.PublishedMessages);
        return result;
    }

    private static void AssertMessages(
        IReadOnlyCollection<object> expectedMessages,
        IReadOnlyCollection<object> actualMessages)
    {
        AssertExtensions.DeepEqual(
            expected: expectedMessages.Select(GetMessageAndType),
            actual: actualMessages.Select(GetMessageAndType));
    }

    private static object GetMessageAndType(object message)
    {
        return new
        {
            TypeFullName = message.GetType().FullName,
            Message = message,
        };
    }
}
