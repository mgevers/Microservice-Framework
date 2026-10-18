using Common.Testing.Assert;
using Common.Testing.Email;
using Moq.AutoMock;
using Ardalis.Result;
using Common.Infrastructure.Email;

namespace Common.Testing.FluentTesting;

/// <summary>Assertions on the commands that a test sent, the replies of a handler and the mail that a service sent.</summary>
public static class AssertMessagingFluentExtensions
{
    public static async Task<T> AssertSentMessages<T>(this Task<T> resultTask, IReadOnlyCollection<object> expectedSentMessages)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        AssertFluentExtensions.AssertMessages(expectedSentMessages, result.ServiceBusState.SentMessages);
        return result;
    }

    public static Task<T> AssertSentMessage<T>(this Task<T> resultTask, object expectedSentMessage)
        where T : IServiceBusTestResult =>
        resultTask.AssertSentMessages([expectedSentMessage]);

    public static Task<T> AssertNoSentMessages<T>(this Task<T> resultTask)
        where T : IServiceBusTestResult =>
        resultTask.AssertSentMessages([]);

    /// <summary>
    /// Runs your assertions on the one sent message of the type. Use it for a message that holds a value that is new on
    /// every call, such as ciphertext, and so cannot be compared as a whole.
    /// </summary>
    public static async Task<T> AssertSentMessage<T, TMessage>(this Task<T> resultTask, Action<TMessage> assert)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        assert(Xunit.Assert.Single(result.ServiceBusState.SentMessages.OfType<TMessage>()));
        return result;
    }

    /// <summary>The handler replied once, with a <see cref="Result"/> of this status.</summary>
    public static async Task<T> AssertRepliedResult<T>(this Task<T> resultTask, ResultStatus expectedStatus)
        where T : IServiceBusTestResult
    {
        var result = await resultTask;

        var reply = Xunit.Assert.IsAssignableFrom<Result>(Xunit.Assert.Single(result.ServiceBusState.RepliedMessages));
        Xunit.Assert.Equal(expectedStatus, reply.Status);
        return result;
    }

    /// <summary>Runs your assertions on the mail that the test's <see cref="InMemoryEmailSender"/> holds. The setup must register that sender as the <see cref="IEmailSender"/>.</summary>
    public static async Task<T> AssertMails<T>(this Task<T> resultTask, Action<IReadOnlyList<SentMail>> assert)
        where T : IAutoMockerTestResult
    {
        var result = await resultTask;

        assert(Xunit.Assert.IsType<InMemoryEmailSender>(result.AutoMocker.Get<IEmailSender>()).Mails);
        return result;
    }
}
