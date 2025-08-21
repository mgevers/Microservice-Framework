using Ardalis.Result;
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

public class CharacterRequestHandlerTests
{
    [Fact]
    public async Task CanCreateCharacter()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<AddCharacterRequestHandler>(DatabaseState.Empty)
            .Handle(new AddCharacterRequest(character.Id, character.Name))
            .AssertOutput(Result.Success())
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertPublishedEvent(new CharacterAddedEvent(character.Id));
    }

    [Fact]
    public async Task CreateCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<AddCharacterRequestHandler>(
                databaseState: DatabaseState.Empty, 
                isReadOnlyDatabase: true)
            .Handle(new AddCharacterRequest(character.Id, character.Name))
            .AssertOutput(Result.CriticalError("cannot write to readonly database"))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterRequest)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanUpdateCharacter()
    {
        var id = Guid.NewGuid();
        var character = DataModels.CreateCharacter(id, "james bond");
        var updatedCharacter = DataModels.CreateCharacter(id, "jimmy bond");

        await Arrange<UpdateCharacterRequestHandler>(new DatabaseState(character))
            .Handle(new UpdateCharacterRequest(id, updatedCharacter.Name))
            .AssertOutput(Result.Success())
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(updatedCharacter))
            .AssertPublishedEvent(new CharacterUpdatedEvent(id));
    }

    [Fact]
    public async Task UpdateCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<UpdateCharacterRequestHandler>(
                databaseState: new DatabaseState(character),
                isReadOnlyDatabase: true)
            .Handle(new UpdateCharacterRequest(character.Id, "new name"))
            .AssertOutput(Result.CriticalError("cannot write to readonly database"))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanRemoveCharacter()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<RemoveCharacterRequestHandler>(new DatabaseState(character))
            .Handle(new RemoveCharacterRequest(character.Id))
            .AssertOutput(Result.Success())
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterRequest)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertPublishedEvent(new CharacterRemovedEvent(character.Id));
    }

    [Fact]
    public async Task RemoveCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<RemoveCharacterRequestHandler>(
                databaseState: new DatabaseState(character),
                isReadOnlyDatabase: true)
            .Handle(new RemoveCharacterRequest(character.Id))
            .AssertOutput(Result.CriticalError("cannot write to readonly database"))
            .AssertLogs(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    private static HandlerTestSetup<THandler> Arrange<THandler>(DatabaseState databaseState, bool isReadOnlyDatabase = false)
    {
        return new HandlerTestSetup<THandler>(databaseState, isReadOnlyDatabase, ConfigureMocker);
    }

    private static void ConfigureMocker(AutoMocker mocker)
    {
        mocker.Use<IRepository<Character>>(new FakeRepository<Character>());
    }
}
