using Common.LanguageExtensions;
using Common.LanguageExtensions.TestableAlternatives;
using Common.Testing.FluentTesting;
using Common.Testing.Integration.Auth;
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
        {
            var authToken = FakeJwtTokens.GenerateJwtToken(new Claim("userId", "123"));

            var probeResponse = new ProbeResponse(
            [
                new("userId", "123"),
                new("exp", $"{ConvertToUnixTimestamp(mockNow.Add(FakeJwtTokens.TokenLifetime))}"),
                new("iss", $"{FakeJwtTokens.Issuer}"),
            ]);

            await ArrangeWithToken(authToken)
                .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/private"))
                .AssertDatabase(DatabaseState.Empty)
                .AssertOutput(Result.Success(probeResponse));
        }
    }

    [Fact]
    public async Task CanAccessPrivateEndpointWithApiKey()
    {
        var apiKey = FakeApiKeyAuthenticator.CreateKeyWithClaims(new Claim("userId", "123"));

        var probeResponse = new ProbeResponse(new List<ClaimView>()
        {
            new("userId", "123"),
        });

        await ArrangeWithKey(apiKey)
            .Act(httpClient => httpClient.GetAsync<ProbeResponse>("api/probe/private"))
            .AssertDatabase(DatabaseState.Empty)
            .AssertOutput(Result.Success(probeResponse));
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> Arrange()
    {
        return ApiTestSetup<TestAppWebApplicationFactory, Program>.ArrangeWithoutAuth(factory);
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> ArrangeWithKey(string apiKey)
    {
        return ApiTestSetup<TestAppWebApplicationFactory, Program>.ArrangeWithApiKey(factory, apiKey: apiKey);
    }

    private ApiTestSetup<TestAppWebApplicationFactory, Program> ArrangeWithToken(string? authToken = null)
    {
        return ApiTestSetup<TestAppWebApplicationFactory, Program>.ArrangeWithAuthToken(
            factory,
            authToken: authToken,
            isReadOnlyDatabase: true);
    }

    private static double ConvertToUnixTimestamp(DateTime date)
    {
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }
}
