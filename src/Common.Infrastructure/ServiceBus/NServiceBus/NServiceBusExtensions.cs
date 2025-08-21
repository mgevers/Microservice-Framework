using Common.Infrastructure.ServiceBus.NServiceBus.RequestResponse;
using CSharpFunctionalExtensions;

namespace Common.Infrastructure.ServiceBus.NServiceBus
{
    public static class NServiceBusExtensions
    {
        public static async Task ReplyWithSuccess(this IMessageHandlerContext context, IEvent @event)
        {
            await context.Reply(new CommandResult(Result.Success()));
            await context.Publish(@event);
        }

        public static async Task ReplyWithFailure(this IMessageHandlerContext context, string error, IEvent? @event = null)
        {
            await context.Reply(new CommandResult(Result.Failure(error)));
            if (@event != null)
                await context.Publish(@event);
        }
    }
}
