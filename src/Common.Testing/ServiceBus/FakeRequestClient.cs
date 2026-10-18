using Ardalis.Result;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NServiceBus;

namespace Common.Testing.ServiceBus;

/// <summary>
/// A MassTransit request client for a command. It records the command as a sent message in <see cref="FakeServiceBus"/>
/// and replies with a fixed <see cref="Result"/>, so that no bus and no message processor run. It is the MassTransit
/// counterpart of <see cref="FakeMessageSession"/>.
/// </summary>
public static class FakeRequestClient
{
    public static IRequestClient<TCommand> Create<TCommand>(Result? reply = null)
        where TCommand : class
    {
        var response = new Mock<Response<Result>>();
        response.SetupGet(r => r.Message).Returns(reply ?? Result.Success());

        var client = new Mock<IRequestClient<TCommand>>();
        client
            .Setup(c => c.GetResponse<Result>(It.IsAny<TCommand>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .Returns(async (TCommand command, CancellationToken _, RequestTimeout _) =>
            {
                await FakeServiceBus.Session.Send(command);

                return response.Object;
            });

        return client.Object;
    }

    /// <summary>Replaces the request client of the command. The command goes to the fake bus, and the client replies with <paramref name="reply"/> (success if null).</summary>
    public static IServiceCollection AddFakeRequestClient<TCommand>(this IServiceCollection services, Result? reply = null)
        where TCommand : class =>
        services.RemoveAll<IRequestClient<TCommand>>().AddSingleton(Create<TCommand>(reply));
}
