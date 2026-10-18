using Common.Infrastructure.ServiceBus.NServiceBus.RequestResponse;
using Common.Infrastructure.Tests.ServiceBus.Utilities;
using Common.Testing.FluentTesting;
using Common.Testing.Persistence;
using CSharpFunctionalExtensions;
using Xunit;

namespace Common.Infrastructure.Tests.ServiceBus.RequestResponse
{
    public class NServiceBusRequestResponseCommandSenderTests
    {
        [Fact]
        public async Task SendsCommandAndReceivesSuccessResult()
        {
            var userId = Guid.NewGuid();

            await Arrange(DatabaseState.Empty)
                .Handle(new Command(userId))
                .AssertPublishedEvent(new Event(userId))
                .AssertRepliedMessage(new CommandResult(Result.Success()))
                .AssertDatabase(DatabaseState.Empty);
        }

        private static HandlerTestSetup<CommandHandler> Arrange(DatabaseState databaseState)
        {
            return new HandlerTestSetup<CommandHandler>(databaseState, isReadOnlyDatabase: false);
        }
    }
}
