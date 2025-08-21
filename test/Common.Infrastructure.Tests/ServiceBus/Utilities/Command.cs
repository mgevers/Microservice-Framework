namespace Common.Infrastructure.Tests.ServiceBus
{
    public class Command : ICommand
    {
        public Command(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}
