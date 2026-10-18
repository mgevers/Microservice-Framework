using Ardalis.Result;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
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
        where TRequest : IRequest<Result>
        where TRequestHandler : class, IRequestHandler<TRequest, Result>
    {
        var messageSession = new TestableMessageSession();
        var mocker = new AutoMocker();
        testSetup.ConfigureMocker?.Invoke(mocker);
        mocker.Use<IMessageSession>(messageSession);

        using (FakeDatabase.SeedData(testSetup.DatabaseState, testSetup.IsReadOnlyDatabase))
        {
            var handler = mocker.GetRequiredService<TRequestHandler>();
            var result = await handler!.Handle(request, CancellationToken.None);
            var busState = new ServiceBusState(
                sentMessages: messageSession.SentMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
                publishedMessages: messageSession.PublishedMessages.Select(m => m.Message).Cast<IMessage>().ToList(),
                repliedMessages: Array.Empty<IMessage>());

            return new RequestHandlerTestResult<TRequestHandler>(FakeDatabase.DatabaseState, busState, mocker, result);
        }        
    }
}
