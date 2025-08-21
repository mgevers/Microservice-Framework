namespace Common.Infrastructure.ServiceBus.NServiceBus.RequestResponse
{
    public class CallbackCommandHandler : IHandleMessages<CommandResult>
    {
        public Task Handle(CommandResult message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }
}
