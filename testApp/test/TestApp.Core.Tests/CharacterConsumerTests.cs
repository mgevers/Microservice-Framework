using Ardalis.Result;
using Common.LanguageExtensions.Contracts;
using Common.Testing.FluentTesting;
using Common.Testing.FluentTesting.Asserts;
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

public class CharacterConsumerTests
{
    [Fact]
    public async Task CanCreateCharacter()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<AddCharacterConsumer>(DatabaseState.Empty)
            .Handle(new AddCharacterCommand(character.Id, character.Name))
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertPublishedEvent(new CharacterAddedEvent(character.Id));
    }

    [Fact]
    public async Task CreateCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<AddCharacterConsumer>(
                databaseState: DatabaseState.Empty,
                databaseError: Result.CriticalError("cannot write to readonly database"))
            .Handle(new AddCharacterCommand(character.Id, character.Name))
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterCommand)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanUpdateCharacter()
    {
        var id = Guid.NewGuid();
        var character = DataModels.CreateCharacter(id, "james bond");
        var updatedCharacter = DataModels.CreateCharacter(id, "jimmy bond");

        await Arrange<UpdateCharacterConsumer>(new DatabaseState(character))
            .Handle(new UpdateCharacterCommand(id, updatedCharacter.Name))
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(updatedCharacter))
            .AssertPublishedEvent(new CharacterUpdatedEvent(id));
    }

    [Fact]
    public async Task UpdateCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<UpdateCharacterConsumer>(
                databaseState: new DatabaseState(character),
                databaseError: Result.CriticalError("cannot write to readonly database"))
            .Handle(new UpdateCharacterCommand(character.Id, "new name"))
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanRemoveCharacter()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<RemoveCharacterConsumer>(new DatabaseState(character))
            .Handle(new RemoveCharacterCommand(character.Id))
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterCommand)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertPublishedEvent(new CharacterRemovedEvent(character.Id));
    }

    [Fact]
    public async Task RemoveCharacter_WhenDatabaseLocked_ReturnsFailure()
    {
        var character = DataModels.CreateCharacter();

        await Arrange<RemoveCharacterConsumer>(
                databaseState: new DatabaseState(character),
                databaseError: Result.CriticalError("cannot write to readonly database"))
            .Handle(new RemoveCharacterCommand(character.Id))
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterCommand)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    public static HandlerTestSetup<THandler> Arrange<THandler>(DatabaseState databaseState, Result? databaseError = null)
    {
        return new HandlerTestSetup<THandler>(databaseState, databaseError, configureMocker: ConfigureMocker);
    }

    private static void ConfigureMocker(AutoMocker mocker)
    {
        mocker.Use<IRepository<Character>>(new FakeRepository<Character>());
        mocker.Use<ILogger<AddCharacterConsumer>>(new FakeLogger<AddCharacterConsumer>(nameof(AddCharacterConsumer)));
        mocker.Use<ILogger<RemoveCharacterConsumer>>(new FakeLogger<RemoveCharacterConsumer>(nameof(RemoveCharacterConsumer)));
        mocker.Use<ILogger<UpdateCharacterConsumer>>(new FakeLogger<UpdateCharacterConsumer>(nameof(UpdateCharacterConsumer)));
    }
}
