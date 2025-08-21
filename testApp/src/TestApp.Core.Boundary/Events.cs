namespace TestApp.Core.Boundary;

public record CharacterAddedEvent(Guid CharacterId): IEvent;

public record CharacterRemovedEvent(Guid CharacterId) : IEvent;

public record CharacterUpdatedEvent(Guid CharacterId) : IEvent;
