using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public static class MassTransitMessageHandlerTest
{
    public static async Task<MessageHandlerTestResult<TMessageHandler>> Handle<TMessage, TMessageHandler>(
        this HandlerTestSetup<TMessageHandler> testSetup,
        TMessage request)
        where TMessage : class
        where TMessageHandler : class, IConsumer<TMessage>
    {
        var mocker = new AutoMocker();
        testSetup.ConfigureMocker?.Invoke(mocker);
        var consumeContextMock = new Mock<ConsumeContext<TMessage>>();
        consumeContextMock
            .Setup(m => m.Message)
            .Returns(request);
        mocker.Use(consumeContextMock);

        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.IsReadOnlyDatabase))
        {
            var handler = mocker.GetRequiredService<TMessageHandler>();

            try
            {
                await handler!.Consume(mocker.Get<ConsumeContext<TMessage>>());

                return new MessageHandlerTestResult<TMessageHandler>(
                    FakeDatabase.DatabaseState,
                    GetServiceBusState<TMessage>(mocker),
                    mocker);
            }
            catch (Exception ex)
            {
                return new MessageHandlerTestResult<TMessageHandler>(
                    FakeDatabase.DatabaseState,
                    GetServiceBusState<TMessage>(mocker),
                    mocker,
                    ex);
            }
        }        
    }

    private static ServiceBusState GetServiceBusState<TMessage>(AutoMocker autoMocker)
        where TMessage : class
    {
        var mockSendEndpoint = autoMocker.GetMock<ISendEndpoint>();
        var sendEndpointMessages = mockSendEndpoint.Invocations
            .Where(i => i.Method.Name.Contains("Send"))
            .Select(i => i.Arguments.First())
            .ToList();

        var mockPublishEndpoint = autoMocker.GetMock<IPublishEndpoint>();
        var publishedEndpointMessages = mockPublishEndpoint.Invocations
            .Where(i => i.Method.Name.Contains("Publish"))
            .Select(i => i.Arguments.First())
            .ToList();

        var mockConsumeContext = autoMocker.GetMock<ConsumeContext<TMessage>>();

        var consumeContextSentMessages = mockConsumeContext.Invocations
            .Where(i => i.Method.Name.Contains("Send"))
            .Select(i => i.Arguments.First())
            .ToList();

        var consumeContextPublishedMessages = mockConsumeContext.Invocations
            .Where(i => i.Method.Name.Contains("Publish"))
            .Select(i => i.Arguments.First())
            .ToList();

        var consumeContextRepliedMessages = mockConsumeContext.Invocations
            .Where(i => i.Method.Name.Contains("Respond"))
            .Select(i => i.Arguments.First())
            .ToList();

        return new ServiceBusState(
            sentMessages: [ ..sendEndpointMessages, .. consumeContextSentMessages],
            publishedMessages: [.. publishedEndpointMessages, .. consumeContextPublishedMessages],
            repliedMessages: consumeContextRepliedMessages);
    }
}
