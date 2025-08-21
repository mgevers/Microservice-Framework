using Common.Infrastructure.Auth.ApiKey;
using Common.Testing.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Common.Testing.Integration.Auth;

public sealed class FakeApiKeyAuthenticator
{
    public static string CreateKeyWithClaims(params Claim[] claims)
    {
        return FakeJwtTokens.GenerateJwtToken(claims);
    }
    public static string CreateKeyWithClaims(IReadOnlyCollection<string> roles, params Claim[] claims)
    {
        return FakeJwtTokens.GenerateJwtToken(roles, claims);
    }

    public static AuthenticationTicket GetAuthTicket(string fakeJwtToken)
    {
        var token = new JwtSecurityToken(fakeJwtToken);
        var claims = token.Claims
            .Where(claim => claim.Type != "exp" && claim.Type != "iss")
            .ToList();

        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, ApiKeyAuthenticationHandler.ApiKeyAuthenticationScheme);
    }
}
