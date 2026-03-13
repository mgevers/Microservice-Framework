using Ardalis.Result;
using Common.LanguageExtensions.Contracts;
using Common.LanguageExtensions.Utilities;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using TestApp.Core.Boundary;
using TestApp.Core.Domain;

namespace TestApp.Core.CommandHandlers;

public class RemoveCharacterConsumer : IConsumer<RemoveCharacterCommand>
{
    private readonly IRepository<Character> repository;
    private readonly ILogger<RemoveCharacterConsumer> logger;

    public RemoveCharacterConsumer(IRepository<Character> repository, ILogger<RemoveCharacterConsumer> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<RemoveCharacterCommand> context)
    {
        logger.LogInformation($"received command: {nameof(RemoveCharacterCommand)}");

        await repository.LoadById(context.Message.CharacterId, context.CancellationToken)
            .Bind(character => repository.Delete(character, context.CancellationToken))
            .Tap(() => context.Publish(new CharacterRemovedEvent(context.Message.CharacterId)));
    }
}

public class RemoveCharacterCommandHandler : IHandleMessages<RemoveCharacterCommand>
{
    private readonly IRepository<Character> repository;
    private readonly ILogger<RemoveCharacterCommandHandler> logger;

    public RemoveCharacterCommandHandler(IRepository<Character> repository, ILogger<RemoveCharacterCommandHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Handle(RemoveCharacterCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation($"received command: {nameof(RemoveCharacterCommand)}");

        await repository.LoadById(message.CharacterId, context.CancellationToken)
            .Bind(character => repository.Delete(character, context.CancellationToken))
            .Tap(() => context.Publish(new CharacterRemovedEvent(message.CharacterId)));
    }
}

public class RemoveCharacterRequestHandler : IRequestHandler<RemoveCharacterRequest, Result>
{
    private readonly IRepository<Character> repository;
    private readonly IMessageSession messageSession;
    private readonly ILogger<RemoveCharacterRequestHandler> logger;

    public RemoveCharacterRequestHandler(
        IRepository<Character> repository,
        IMessageSession messageSession,
        ILogger<RemoveCharacterRequestHandler> logger)
    {
        this.repository = repository;
        this.messageSession = messageSession;
        this.logger = logger;
    }

    public async Task<Result> Handle(RemoveCharacterRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"received command: {nameof(RemoveCharacterRequest)}");

        var stupidResult = await repository.LoadById(request.CharacterId, cancellationToken)
            .Bind(character => repository.Delete(character, cancellationToken))
            .Tap(() => messageSession.Publish(new CharacterRemovedEvent(request.CharacterId)));

        return stupidResult.IsSuccess
            ? Result.Success()
            : Result.CriticalError([.. stupidResult.Errors]);
    }
}
