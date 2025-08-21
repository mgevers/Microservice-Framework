using Common.Infrastructure.Auth.Token;
using System.Security.Claims;

namespace Common.Testing.Web.Auth;

public sealed class FakeOAuthManager : IOAuthManager, IDisposable
{
    private static readonly AsyncLocal<IReadOnlyList<Claim>?> _asyncLocalClaims = new();
    private static IReadOnlyList<Claim>? Claims => _asyncLocalClaims.Value;

    private FakeOAuthManager(IReadOnlyCollection<Claim>? claims)
    {
        _asyncLocalClaims.Value = claims?.ToList();
    }

    public static FakeOAuthManager AssignsTokenWithClaims(params Claim[] claims)
    {
        return new FakeOAuthManager(claims);
    }

    public Task<OAuth2Token?> GetOAuth2Token(OAuthOptions options)
    {
        if (Claims == null)
        {
            return Task.FromResult<OAuth2Token?>(null);
        }

        var tokenString = FakeJwtTokens.GenerateJwtToken(Claims);

        var token = new OAuth2Token
        {
            AccessToken = tokenString,
            TokenType = "Bearer",
            ExpiresIn = (int)TimeSpan.FromMinutes(20).TotalSeconds
        };

        return Task.FromResult<OAuth2Token?>(token);
    }


    public void Dispose()
    {
        _asyncLocalClaims.Value = null;
    }
}
