using Common.LanguageExtensions.TestableAlternatives;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Common.Testing.Web.Auth;

public static class FakeJwtTokens
{
    private static readonly JwtSecurityTokenHandler _tokenHandler = new();
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    private static readonly byte[] _key = new byte[32];

    public static string Issuer { get; } = Guid.NewGuid().ToString();

    public static SecurityKey SecurityKey { get; }

    public static SigningCredentials SigningCredentials { get; }

    static FakeJwtTokens()
    {
        _rng.GetBytes(_key);
        SecurityKey = new SymmetricSecurityKey(_key) { KeyId = Guid.NewGuid().ToString() };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
    }

    public static string GenerateJwtToken(params Claim[] claims)
    {
        var token = new JwtSecurityToken(Issuer, null, claims.Distinct(), null, CurrentTime.UtcNow.AddMinutes(20), SigningCredentials);
        var tokenString = _tokenHandler.WriteToken(token);

        return tokenString;
    }

    public static string GenerateJwtToken(IEnumerable<Claim> claims)
    {
        return GenerateJwtToken(claims.ToArray());
    }
}