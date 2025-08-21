using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Common.Infrastructure.Auth.ApiKey;

/// <inheritdoc />
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeySecurityManager apiKeySecurityManager) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{

    public const string ApiKeyHeaderName = "X-API-Key";
    public const string ApiKeyAuthenticationScheme = "ApiKey";
    private readonly IApiKeySecurityManager _apiKeySecurityManger = apiKeySecurityManager;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return _apiKeySecurityManger.AuthenticateApiKey(Request);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"{ApiKeyAuthenticationScheme} \", charset=\"UTF-8\"";
        await base.HandleChallengeAsync(properties);
    }
}