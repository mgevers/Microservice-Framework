using Common.Infrastructure.Auth.ApiKey;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Common.Testing.Web.Auth;

public sealed class FakeApiKeyAuthenticator : IDisposable
{
    private static readonly AsyncLocal<IReadOnlyList<Claim>?> _asyncLocalClaims = new();
    private static readonly AsyncLocal<string?> _asyncLocalKey = new();
    private static IReadOnlyList<Claim>? Claims => _asyncLocalClaims.Value;
    private static string? Key => _asyncLocalKey.Value;

    private FakeApiKeyAuthenticator(string key, IReadOnlyCollection<Claim> claims)
    {
        _asyncLocalClaims.Value = claims?.ToList() ?? Array.Empty<Claim>() as IReadOnlyList<Claim>;
        _asyncLocalKey.Value = key;
    }

    public static FakeApiKeyAuthenticator HydrateKeyWithClaims(string key, params Claim[] claims)
    {
        return new FakeApiKeyAuthenticator(key, claims);
    }

    public static AuthenticationTicket GetAuthTicket(string key)
    {
        if (Key == null || Claims == null)
        {
            throw new ArgumentException("fake security manager was not properly initialized");
        }

        if (key != Key)
        {
            throw new ArgumentException($"key: '{key}' was not initialized");
        }

        var identity = new ClaimsIdentity(Claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, ApiKeyAuthenticationHandler.ApiKeyAuthenticationScheme);
    }

    public void Dispose()
    {
        _asyncLocalClaims.Value = null;
    }
}
