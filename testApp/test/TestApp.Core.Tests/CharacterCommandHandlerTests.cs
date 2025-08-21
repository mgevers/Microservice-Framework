using Common.Infrastructure.Persistence;
using Common.Testing.FluentTesting;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Microsoft.Extensions.Logging;
using Moq.AutoMock;
using TestApp.Core.Boundary;
using TestApp.Core.CommandHandlers;
using TestApp.Core.Domain;
using TestApp.Tests;
using Xunit;

namespace TestApp.Core.Tests;

public class CharacterCommandHandlerTests
{
    [Fact]
    public async Task CanCreateCharacter()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<AddCharacterCommandHandler>(DatabaseState.Empty)
            .Handle(new AddCharacterCommand(character.Id, character.Name))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertPublishedEvent(new CharacterAddedEvent(character.Id));
    }

    [Fact]
    public async Task CreateCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<AddCharacterCommandHandler>(
                databaseState: DatabaseState.Empty,
                isReadOnlyDatabase: true)
            .Handle(new AddCharacterCommand(character.Id, character.Name))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterCommand)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanUpdateCharacter()
    {
        var id = Guid.NewGuid();
        var character = DataModels.CreateCharacter(id, "james bond");
        var updatedCharacter = DataModels.CreateCharacter(id, "jimmy bond");

        await Arrange<UpdateCharacterCommandHandler>(new DatabaseState(character))
            .Handle(new UpdateCharacterCommand(id, updatedCharacter.Name))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(updatedCharacter))
            .AssertPublishedEvent(new CharacterUpdatedEvent(id));
    }

    [Fact]
    public async Task UpdateCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<UpdateCharacterCommandHandler>(
                databaseState: new DatabaseState(character),
                isReadOnlyDatabase: true)
            .Handle(new UpdateCharacterCommand(character.Id, "new name"))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanRemoveCharacter()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<RemoveCharacterCommandHandler>(new DatabaseState(character))
            .Handle(new RemoveCharacterCommand(character.Id))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterCommand)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertPublishedEvent(new CharacterRemovedEvent(character.Id));
    }

    [Fact]
    public async Task RemoveCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<RemoveCharacterCommandHandler>(
                databaseState: new DatabaseState(character),
                isReadOnlyDatabase: true)
            .Handle(new RemoveCharacterCommand(character.Id))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    public static HandlerTestSetup<THandler> Arrange<THandler>(DatabaseState databaseState, bool isReadOnlyDatabase = false)
    {
        return new HandlerTestSetup<THandler>(databaseState, isReadOnlyDatabase, ConfigureMocker);
    }

    private static void ConfigureMocker(AutoMocker mocker)
    {
        mocker.Use<IRepository<Character>>(new FakeRepository<Character>());
    }
}
