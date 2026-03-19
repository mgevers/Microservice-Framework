using Ardalis.Result;
using Common.Testing.Logging;
using Common.Testing.Mediator;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq.AutoMock;
using NServiceBus.Testing;

namespace Common.Testing.FluentTesting;

public static class RequestHandlerTest
{
    public static async Task<RequestHandlerTestResult<TRequestHandler>> Handle<TRequest, TRequestHandler>(
        this HandlerTestSetup<TRequestHandler> testSetup,
        TRequest request)
        where TRequest : class, IRequest<Result>
        where TRequestHandler : class, IRequestHandler<TRequest, Result>
    {
        var messageSession = new TestableMessageSession();
        var messageContext = new TestableMessageHandlerContext();

        var mocker = new AutoMocker();
        testSetup.ConfigureMocker?.Invoke(mocker);
        mocker.Use<IMessageSession>(messageSession);
        mocker.Use<IMessageHandlerContext>(messageContext);

        using (var fakeMediator = FakeMediator.WithSendResponse())
        using (FakeLoggingDatabase.Initialize(testSetup.LoggingConfiguration))
        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.DatabaseError))
        {
            mocker.Use<IMediator>(fakeMediator);

            var handler = mocker.GetRequiredService<TRequestHandler>();
            var result = await handler!.Handle(request, CancellationToken.None);
            var nServiceBusState = new ServiceBusState(
                sentMessages: messageSession.SentMessages
                    .Select(m => m.Message)
                    .Concat(messageContext.SentMessages.Select(m => m.Message))
                    .ToList(),
                publishedMessages: messageSession.PublishedMessages
                    .Select(m => m.Message)
                    .Concat(messageContext.PublishedMessages.Select(m => m.Message))
                    .ToList(),
                repliedMessages: messageContext.RepliedMessages
                    .Select(m => m.Message)
                    .ToList());

            var massTransitBusState = GetMassTransitServiceBusState<TRequest>(mocker);
            var mediatorBusState = GetMediatorServiceBusState(fakeMediator);

            return new RequestHandlerTestResult<TRequestHandler>(
                FakeDatabase.DatabaseState,
                CombineServiceBusStates(nServiceBusState, massTransitBusState, mediatorBusState),
                mocker,
                result,
                FakeLoggingDatabase.Logs.ToList());
        }        
    }

    public static async Task<RequestHandlerTestResult<TRequestHandler, TResult>> Handle<TRequest, TRequestHandler, TResult>(
        this HandlerTestSetup<TRequestHandler, TResult> testSetup,
        TRequest request)
        where TRequest : class, IRequest<Result<TResult>>
        where TRequestHandler : class, IRequestHandler<TRequest, Result<TResult>>
    {
        var messageSession = new TestableMessageSession();
        var mocker = new AutoMocker();
        testSetup.ConfigureMocker?.Invoke(mocker);
        mocker.Use<IMessageSession>(messageSession);

        using (var fakeMediator = FakeMediator.WithSendResponse())
        using (FakeLoggingDatabase.Initialize(testSetup.LoggingConfiguration))
        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.DatabaseError))
        {
            var handler = mocker.GetRequiredService<TRequestHandler>();
            var result = await handler!.Handle(request, CancellationToken.None);
            var nServiceBusState = new ServiceBusState(
                sentMessages: messageSession.SentMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
                publishedMessages: messageSession.PublishedMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
                repliedMessages: Array.Empty<IMessage>());

            var massTransitBusState = GetMassTransitServiceBusState<TRequest>(mocker);
            var mediatorBusState = GetMediatorServiceBusState(fakeMediator);

            return new RequestHandlerTestResult<TRequestHandler, TResult>(
                FakeDatabase.DatabaseState,
                CombineServiceBusStates(nServiceBusState, massTransitBusState, mediatorBusState),
                mocker,
                result,
                FakeLoggingDatabase.Logs.ToList());
        }
    }
    private static ServiceBusState GetMediatorServiceBusState(FakeMediator fakeMediator)
    {
        return new ServiceBusState(
            sentMessages: fakeMediator.SentMessages,
            publishedMessages: fakeMediator.PublishedMessages,
            repliedMessages: Array.Empty<object>());
    }

    private static ServiceBusState GetMassTransitServiceBusState<TMessage>(AutoMocker autoMocker)
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
            sentMessages: [.. sendEndpointMessages, .. consumeContextSentMessages],
            publishedMessages: [.. publishedEndpointMessages, .. consumeContextPublishedMessages],
            repliedMessages: consumeContextRepliedMessages);
    }

    private static ServiceBusState CombineServiceBusStates(params ServiceBusState[] states)
    {
        List<object> sentMessages = [];
        List<object> publishedMessages = [];
        List<object> repliedMessages = [];

        foreach (var state in states)
        {
            sentMessages.AddRange(state.SentMessages);
            publishedMessages.AddRange(state.PublishedMessages);
            repliedMessages.AddRange(state.RepliedMessages);
        }

        return new ServiceBusState(sentMessages, publishedMessages, repliedMessages);
    }
}
