using Microsoft.Extensions.Logging;
using TestApp.Core.Boundary;

namespace TestApp.Core.EventHandlers;

public class LogWhenCharacterAddedEventHandler : IHandleMessages<CharacterAddedEvent>
{
    private readonly ILogger<LogWhenCharacterAddedEventHandler> logger;

    public LogWhenCharacterAddedEventHandler(ILogger<LogWhenCharacterAddedEventHandler> logger)
    {
        this.logger = logger;
    }

    public Task Handle(CharacterAddedEvent message, IMessageHandlerContext context)
    {
        logger.LogInformation("character {id} added", message.CharacterId);

        return Task.CompletedTask;
    }
}
