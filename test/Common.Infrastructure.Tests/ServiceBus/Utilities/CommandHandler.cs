using Common.Infrastructure.ServiceBus.NServiceBus;
using Common.Infrastructure.Tests.ServiceBus.Utilities;

namespace Common.Infrastructure.Tests.ServiceBus
{
    public class CommandHandler : IHandleMessages<Command>
    {
        public Task Handle(Command message, IMessageHandlerContext context)
        {
            return context.ReplyWithSuccess(new Event(message.UserId));
        }
    }
}
