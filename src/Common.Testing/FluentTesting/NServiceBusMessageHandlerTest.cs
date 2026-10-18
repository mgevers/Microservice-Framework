using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq.AutoMock;
using NServiceBus.Testing;

namespace Common.Testing.FluentTesting;

public static class NServiceBusMessageHandlerTest
{
    public static async Task<MessageHandlerTestResult<TMessageHandler>> Handle<TMessage, TMessageHandler>(
        this HandlerTestSetup<TMessageHandler> testSetup,
        TMessage request)
        where TMessage : IMessage
        where TMessageHandler : class, IHandleMessages<TMessage>
    {
        var context = new TestableMessageHandlerContext();
        var mocker = new AutoMocker();
        testSetup.ConfigureMocker?.Invoke(mocker);
        mocker.Use<IMessageHandlerContext>(context);

        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.IsReadOnlyDatabase))
        {
            var handler = mocker.GetRequiredService<TMessageHandler>();

            await handler!.Handle(request, context);
            var busState = new ServiceBusState(
                sentMessages: context.SentMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
                publishedMessages: context.PublishedMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
                repliedMessages: context.RepliedMessages.Select(m => m.Message).Cast<IMessage>().ToList());

            return new MessageHandlerTestResult<TMessageHandler>(FakeDatabase.DatabaseState, busState, mocker);
        }        
    }
}
