using Common.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Common.Infrastructure.Tests.ServiceBus;

public class JwtIssuerTests
{
    [Theory]
    [InlineData("tenant.auth0.com")]
    [InlineData("https://tenant.auth0.com")]
    [InlineData("https://tenant.auth0.com/")]
    public void Authority_AndIssuer_MatchWhatAuth0PutsInItsTokens(string domain)
    {
        var services = new ServiceCollection().AddLogging().AddJwtAuthentication(domain, "my-audience");
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");

        Assert.Equal("https://tenant.auth0.com/", options.Authority);
        Assert.Equal("https://tenant.auth0.com/", options.TokenValidationParameters.ValidIssuer);
        Assert.Equal(["my-audience"], options.TokenValidationParameters.ValidAudiences);
    }

    [Fact]
    public void ApiKeyVariant_UsesTheSameIssuer()
    {
        var services = new ServiceCollection().AddLogging().AddJwtAndAPIKeyAuthentication("tenant.auth0.com", "my-audience");
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");

        Assert.Equal("https://tenant.auth0.com/", options.TokenValidationParameters.ValidIssuer);
    }

    [Fact]
    public void AnExplicitIssuer_IsUsedAsGiven_ForProvidersLikeKeycloak()
    {
        var services = new ServiceCollection().AddLogging().AddJwtAuthentication(options =>
        {
            options.Issuer = "https://auth.example/realms/gamestats";
            options.Audience = "my-audience";
        });
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");

        Assert.Equal("https://auth.example/realms/gamestats", options.Authority);
        Assert.Equal("https://auth.example/realms/gamestats", options.TokenValidationParameters.ValidIssuer);
    }
}
