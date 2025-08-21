using Ardalis.Result;
using Common.Infrastructure.Persistence;
using Common.LanguageExtensions.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using TestApp.Core.Boundary;
using TestApp.Core.Domain;

namespace TestApp.Core.CommandHandlers;

public class UpdateCharacterCommandHandler : IHandleMessages<UpdateCharacterCommand>
{
    private readonly IRepository<Character> repository;
    private readonly ILogger<UpdateCharacterCommandHandler> logger;

    public UpdateCharacterCommandHandler(IRepository<Character> repository, ILogger<UpdateCharacterCommandHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Handle(UpdateCharacterCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation($"received command: {nameof(UpdateCharacterCommand)}");
        await repository.LoadById(message.CharacterId, context.CancellationToken)
            .Bind(async character =>
            {
                character.Name = message.Name;
                var result = await repository.Update(character, context.CancellationToken);

                return result.AsResult();
            })
            .Tap(() => context.Publish(new CharacterUpdatedEvent(message.CharacterId)));
    }
}

public class UpdateCharacterRequestHandler : IRequestHandler<UpdateCharacterRequest, Result>
{
    private readonly IRepository<Character> repository;
    private readonly IMessageSession messageSession;
    private readonly ILogger<UpdateCharacterRequestHandler> logger;

    public UpdateCharacterRequestHandler(
        IRepository<Character> repository,
        IMessageSession messageSession,
        ILogger<UpdateCharacterRequestHandler> logger)
    {
        this.repository = repository;
        this.messageSession = messageSession;
        this.logger = logger;
    }

    public async Task<Result> Handle(UpdateCharacterRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"received command: {nameof(UpdateCharacterRequest)}");
        var result = await repository.LoadById(request.CharacterId, cancellationToken)
            .Bind(async character =>
            {
                character.Name = request.Name;
                var r = await repository.Update(character);

                return r.AsResult();
            })
            .Tap(() => messageSession.Publish(new CharacterUpdatedEvent(request.CharacterId)));

        return result.AsResult();
    }
}
