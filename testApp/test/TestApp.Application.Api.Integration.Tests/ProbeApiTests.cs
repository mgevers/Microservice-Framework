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

        using (CurrentTime.UseMockUtcNow(mockNow))
        {
            var jwt = FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123"));

            var probeResponse = new ProbeResponse(
            [
                new("userId", "123"),
                new("exp", $"{ConvertToUnixTimestamp(mockNow.AddMinutes(20))}"),
                new("iss", $"{FakeJwtTokens.Issuer}"),
            ]);

            await Arrange(jwt)
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

        await Arrange(apiKey: apiKey)
            .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/private"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertOutput(Result.Success(probeResponse));
    }

    [Fact]
    public async Task Act_AppliesConfigureServicesDelegate()
    {
        var wasApplied = false;
        var setup = new ApiTestSetup<TestAppWebApplicationFactory, Program>(
            this.factory,
            DatabaseState.Empty,
            isReadOnlyDatabase: true,
            jwt: null,
            configureServices: services => wasApplied = true);

        await setup.Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/public"));

        Assert.True(wasApplied);
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> Arrange(string? jwt = null, string? apiKey = null)
    {
        return new ApiTestSetup<TestAppWebApplicationFactory, Program>(
            this.factory,
            DatabaseState.Empty,
            isReadOnlyDatabase: true,
            jwt,
            apiKey: apiKey);
    }

    private static double ConvertToUnixTimestamp(DateTime date)
    {
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }
}
