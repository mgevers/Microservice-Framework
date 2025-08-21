using Common.Infrastructure.Auth.Token;
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

        using var fakeOauth = FakeOAuthManager.AssignsTokenWithClaims(new Claim("userId", "123"));

        await 
            Arrange(oAuth2Token: await fakeOauth.GetOAuth2Token(null!))
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

        using var fakeOauth = FakeOAuthManager.AssignsTokenWithClaims(new Claim("userId", "123"));

        await
            Arrange(
                databaseState: new DatabaseState(character),
                oAuth2Token: await fakeOauth.GetOAuth2Token(null!))
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

        using var fakeOauth = FakeOAuthManager.AssignsTokenWithClaims(new Claim("userId", "123"));

        await
            Arrange(
                databaseState: new DatabaseState(character),
                oAuth2Token: await fakeOauth.GetOAuth2Token(null!))
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
        OAuth2Token? oAuth2Token = null)
    {
        return new ApiTestSetup<TestAppWebApplicationFactory, Program>(
            this.factory,
            databaseState ?? DatabaseState.Empty,
            isReadOnlyDatabase,
            oAuth2Token);
    }
}
