using Common.LanguageExtensions;
using Common.Testing.FluentTesting;
using Common.Testing.Integration.FluentTesting;
using Common.Testing.Persistence;
using Common.Testing.Web.Auth;
using Common.Testing.Web.FluentTesting;
using System.Net;
using System.Security.Claims;
using TestApp.Core.Boundary;
using TestApp.Tests;
using Xunit;

namespace TestApp.Application.AsyncApi.Integration.Tests;

public class CharacterControllerTests(TestAppWebApplicationFactory factory) : IClassFixture<TestAppWebApplicationFactory>
{
    private readonly TestAppWebApplicationFactory factory = factory;

    [Fact]
    public async Task CanAddCharacter()
    {
        var character = DataModels.CreateCharacter();
        var command = new AddCharacterCommand(character.Id, character.Name);
        var authToken = FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123"));

        await 
            Arrange(authToken: authToken)
            .Act(httpClient => httpClient.PostAsync("api/characters", command))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Accepted)
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task AddCharacter_WhenClientNotAuthorized_ReturnsUnauthorized()
    {
        var character = DataModels.CreateCharacter();
        var command = new AddCharacterCommand(character.Id, character.Name);

        await
            Arrange()
            .Act(httpClient => httpClient.PostAsync("api/characters", command))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            .AssertDatabase(DatabaseState.Empty)
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanUpdateCharacter()
    {
        var id = Guid.NewGuid();
        var character = DataModels.CreateCharacter(id);
        var command = new UpdateCharacterCommand(id, "new name");
        var authToken = FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123"));

        await
            Arrange(
                databaseState: new DatabaseState(character),
                authToken: authToken)
            .Act(httpClient => httpClient.PutAsync("api/characters", command))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Accepted)
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task UpdateCharacter_WhenClientNotAuthorized_ReturnsUnauthorized()
    {
        var character = DataModels.CreateCharacter();
        var command = new UpdateCharacterCommand(character.Id, character.Name);

        await
            Arrange(new DatabaseState(character))
            .Act(httpClient => httpClient.PutAsync("api/characters", command))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task CanRemoveCharacter()
    {
        var character = DataModels.CreateCharacter();
        var command = new RemoveCharacterRequest(character.Id);
        var authToken = FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123"));

        await
            Arrange(
                databaseState: new DatabaseState(character),
                authToken: authToken)
            .Act(httpClient => httpClient.DeleteAsync("api/characters", command))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Accepted)
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    [Fact]
    public async Task RemoveCharacter_WhenClientNotAuthorized_ReturnsUnauthorized()
    {
        var character = DataModels.CreateCharacter();
        var command = new RemoveCharacterRequest(character.Id);

        await
            Arrange(new DatabaseState(character))
            .Act(httpClient => httpClient.DeleteAsync("api/characters", command))
            .AssertHttpResponse(httpResponse => httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            .AssertDatabase(new DatabaseState(character))
            .AssertNoPublishedEvents();
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> Arrange(
        DatabaseState? databaseState = null,
        bool isReadOnlyDatabase = false,
        string? authToken = null)
    {
        return ApiTestSetup<TestAppWebApplicationFactory, Program>.ArrangeWithAuthToken(
            factory,
            databaseState,
            authToken,
            isReadOnlyDatabase);
    }
}
