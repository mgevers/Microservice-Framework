using Ardalis.Result;
using Common.Infrastructure.Persistence;
using Common.LanguageExtensions.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using TestApp.Core.Boundary;
using TestApp.Core.Domain;

namespace TestApp.Core.CommandHandlers;

public class AddCharacterCommandHandler : IHandleMessages<AddCharacterCommand>
{
    private readonly IRepository<Character> repository;
    private readonly ILogger<AddCharacterCommandHandler> logger;

    public AddCharacterCommandHandler(IRepository<Character> repository, ILogger<AddCharacterCommandHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Handle(AddCharacterCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation($"received command: {nameof(AddCharacterCommand)}");
        var character = new Character(message.CharacterId, message.Name);

        await repository.Create(character, context.CancellationToken)
            .Tap(() => context.Publish(new CharacterAddedEvent(message.CharacterId)));
    }
}

public class AddCharacterRequestHandler : IRequestHandler<AddCharacterRequest, Result>
{
    private readonly IMessageSession messageSession;
    private readonly IRepository<Character> repository;
    private readonly ILogger<AddCharacterRequestHandler> logger;

    public AddCharacterRequestHandler(
        IMessageSession messageSession,
        IRepository<Character> repository,
        ILogger<AddCharacterRequestHandler> logger)
    {
        this.messageSession = messageSession;
        this.repository = repository;
        this.logger = logger;
    }

    public async Task<Result> Handle(AddCharacterRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"received command: {nameof(AddCharacterRequest)}");
        var character = new Character(request.CharacterId, request.Name);

        var result = await repository.Create(character, cancellationToken)
            .Tap(() => messageSession.Publish(new CharacterAddedEvent(request.CharacterId), cancellationToken));

        return result.IsSuccess
            ? Result.Success()
            : Result.CriticalError([.. result.Errors]);
    }
}