using Common.LanguageExtensions;
using Common.Testing.FluentTesting;
using Common.Testing.Integration.FluentTesting;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Common.Testing.Web.Auth;
using Common.Testing.Web.FluentTesting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using TestApp.Core.Boundary;
using TestApp.Tests;
using Xunit;

namespace TestApp.Application.Api.Integration.Tests;

public class CharacterControllerTests(TestAppWebApplicationFactory factory) : IClassFixture<TestAppWebApplicationFactory>
{
    private readonly TestAppWebApplicationFactory factory = factory;

    [Fact]
    public async Task CanAddCharacter()
    {
        var character = DataModels.CreateCharacter();
        var request = new AddCharacterRequest(character.Id, character.Name);

        await 
            Arrange(jwt: FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123")))
            .Act(httpClient => httpClient.PostAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.OK)
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertPublishedEvent(new CharacterAddedEvent(character.Id));
    }

    [Fact]
    public async Task AddCharacter_WhenClientNotAuthorized_ReturnsUnauthorized()
    {
        var character = DataModels.CreateCharacter();
        var request = new AddCharacterRequest(character.Id, character.Name);

        await
            Arrange()
            .Act(httpClient => httpClient.PostAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task AddCharacter_WhenDatabaseLocked_ReturnsError()
    {
        var character = DataModels.CreateCharacter();
        var request = new AddCharacterRequest(character.Id, character.Name);

        await
            Arrange(
                isReadOnlyDatabase: true,
                jwt: FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123")))
            .Act(httpClient => httpClient.PostAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.InternalServerError)
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(AddCharacterRequest)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanUpdateCharacter()
    {
        var id = Guid.NewGuid();
        var character = DataModels.CreateCharacter(id);
        var updatedCharacter = DataModels.CreateCharacter(id);
        var request = new UpdateCharacterRequest(id, updatedCharacter.Name);

        await
            Arrange(
                databaseState: new DatabaseState(character),
                jwt: FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123")))
            .Act(httpClient => httpClient.PutAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.OK)
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(updatedCharacter))
            .AssertPublishedEvent(new CharacterUpdatedEvent(id));
    }

    [Fact]
    public async Task UpdateCharacter_WhenClientNotAuthorized_ReturnsUnauthorized()
    {
        var character = DataModels.CreateCharacter();
        var request = new UpdateCharacterRequest(character.Id, character.Name);

        await
            Arrange(new DatabaseState(character))
            .Act(httpClient => httpClient.PutAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task UpdateCharacter_WhenDatabaseLocked_ReturnsError()
    {
        var character = DataModels.CreateCharacter();
        var request = new AddCharacterRequest(character.Id, character.Name);

        await
            Arrange(
                databaseState: new DatabaseState(character),
                isReadOnlyDatabase: true,
                jwt: FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123")))
            .Act(httpClient => httpClient.PutAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.InternalServerError)
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(UpdateCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanRemoveCharacter()
    {
        var character = DataModels.CreateCharacter();
        var request = new RemoveCharacterRequest(character.Id);

        await
            Arrange(
                databaseState: new DatabaseState(character),
                jwt: FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123")))
            .Act(httpClient => httpClient.DeleteAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.OK)
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterRequest)}"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertPublishedEvent(new CharacterRemovedEvent(character.Id));
    }

    [Fact]
    public async Task RemoveCharacter_WhenClientNotAuthorized_ReturnsUnauthorized()
    {
        var character = DataModels.CreateCharacter();
        var request = new RemoveCharacterRequest(character.Id);

        await
            Arrange(new DatabaseState(character))
            .Act(httpClient => httpClient.DeleteAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task RemoveCharacter_WhenDatabaseLocked_ReturnsError()
    {
        var character = DataModels.CreateCharacter();
        var request = new RemoveCharacterRequest(character.Id);

        await
            Arrange(
                databaseState: new DatabaseState(character),
                isReadOnlyDatabase: true,
                jwt: FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123")))
            .Act(httpClient => httpClient.DeleteAsync("api/characters", request))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.InternalServerError)
            .AssertLog(new LogEntry(LogLevel.Information, $"received command: {nameof(RemoveCharacterRequest)}"))
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> Arrange(
        DatabaseState? databaseState = null,
        bool isReadOnlyDatabase = false,
        string? jwt = null)
    {
        return new ApiTestSetup<TestAppWebApplicationFactory, Program>(
            this.factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            jwt);
    }
}
