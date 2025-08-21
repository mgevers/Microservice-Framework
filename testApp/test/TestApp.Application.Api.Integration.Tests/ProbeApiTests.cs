using Common.Infrastructure.Auth.Token;
using Common.LanguageExtensions;
using Common.LanguageExtensions.TestableAlternatives;
using Common.Testing.FluentTesting;
using Common.Testing.Integration.FluentTesting;
using Common.Testing.Persistence;
using Common.Testing.Web.Auth;
using Common.Testing.Web.FluentTesting;
using CSharpFunctionalExtensions;
using System.Security.Claims;
using TestApp.Application.Api.Controllers;
using Xunit;

namespace TestApp.Application.Api.Integration.Tests;

public class ProbeApiTests : IClassFixture<TestAppWebApplicationFactory>
{
    private readonly TestAppWebApplicationFactory factory;

    public ProbeApiTests(TestAppWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CanAccessPublicEndpoint()
    {
        await Arrange()
            .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/public"))
            .AssertOutput(Result.Success(new ProbeResponse(new List<ClaimView>())));
    }

    [Fact]
    public async Task AccessPrivateEndpointWithoutAuth_Fails()
    {
        await Arrange()
            .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/private"))
            .AssertOutput(Result.Failure<ProbeResponse>("http response was not a successful status code - Unauthorized"));
    }

    [Fact]
    public async Task CanAccessPrivateEndpointWithToken()
    {
        var mockNow = DateTime.Parse("1/1/2000");

        using (CurrentTime.UseMotckUtcNow(mockNow))
        using (var fakeOauth = FakeOAuthManager.AssignsTokenWithClaims(new Claim("userId", "123")))
        {
            var authToken = await fakeOauth.GetOAuth2Token(null!);

            var probeResponse = new ProbeResponse(
            [
                new("userId", "123"),
                new("exp", $"{ConvertToUnixTimestamp(authToken!.Expires)}"),
                new("iss", $"{FakeJwtTokens.Issuer}"),
            ]);

            await Arrange(authToken)
                .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/private"))
                .AssertDatabase(DatabaseState.Empty)
                .AssertOutput(Result.Success(probeResponse));
        }
    }

    [Fact]
    public async Task CanAccessPrivateEndpointWithApiKey()
    {
        var apiKey = "test-api-key";
        using var fakeOauth = FakeApiKeyAuthenticator.HydrateKeyWithClaims(apiKey, new Claim("userId", "123"));

        var probeResponse = new ProbeResponse(new List<ClaimView>()
        {
            new("userId", "123"),
        });

        await Arrange(apiKey)
            .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/private"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertOutput(Result.Success(probeResponse));
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> Arrange(string apiKey)
    {
        return new ApiTestSetup<TestAppWebApplicationFactory, Program>(
            this.factory,
            DatabaseState.Empty,
            isReadOnlyDatabase: true,
            apiKey);
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> Arrange(OAuth2Token? oAuth2Token = null)
    {
        return new ApiTestSetup<TestAppWebApplicationFactory, Program>(
            this.factory,
            DatabaseState.Empty,
            isReadOnlyDatabase: true,
            oAuth2Token);
    }

    private static double ConvertToUnixTimestamp(DateTime date)
    {
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }
}
