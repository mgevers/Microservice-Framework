using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Common.Infrastructure.Auth;
using Common.Infrastructure.Auth.Eos;
using Common.Infrastructure.Compliance;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Common.Infrastructure.Tests.Auth;

/// <summary>
/// Runs a small web host with the real token checks (no fake handler), a stub for the keys of Epic,
/// and tokens that the test signs with its own RSA key.
/// </summary>
public class EosAuthenticationTests : IAsyncLifetime
{
    private const string Audience = "xyz-game-client-id";
    private const string EosIssuer = "https://api.epicgames.dev/auth/v1";
    private const string KeycloakIssuer = "https://keycloak.test/realms/game";
    private const string Puid = "0002a1b2c3d4e5f60718293a4b5c6d7e";

    private static readonly RsaSecurityKey EosKey = NewKey("eos-key-1");
    private static readonly RsaSecurityKey OtherKey = NewKey("other-key");

    private WebApplication app = null!;
    private HttpClient client = null!;
    private StubKeyServer keyServer = null!;

    private static RsaSecurityKey NewKey(string id) => new(RSA.Create(2048)) { KeyId = id };

    public async Task InitializeAsync()
    {
        keyServer = new StubKeyServer(EosKey);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddJwtAuthentication(options =>
            {
                options.Issuer = KeycloakIssuer;
                options.Audience = "game-api";
            })
            .AddEosPlayerAuthentication(options =>
            {
                options.Audience = Audience;
                options.JwksUrl = "https://eos.test/keys";
            })
            .AddPermissionAuthorization(policies => policies
                .Require("Admin", "game:admin")
                .RequireOrOwner("Own", "game:read", "accountId"));
        builder.Services.AddHttpClient(EosAuthenticationExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => keyServer);

        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/who", (HttpContext http) => http.User.FindFirstValue("sub") ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier))
            .RequireAuthorization();
        app.MapGet("/admin", () => "admin").RequireAuthorization("Admin");
        app.MapGet("/accounts/{accountId}", (string accountId) => accountId).RequireAuthorization("Own");
        await app.StartAsync();

        client = app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    private static string Token(
        SecurityKey? key = null,
        string issuer = EosIssuer,
        string audience = Audience,
        string subject = Puid,
        DateTime? expires = null,
        DateTime? issuedAt = null,
        params Claim[] extra)
    {
        var claims = new List<Claim> { new("sub", subject) };
        claims.AddRange(extra);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = (issuedAt ?? DateTime.UtcNow).AddSeconds(-5),
            IssuedAt = issuedAt ?? DateTime.UtcNow,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(key ?? EosKey, SecurityAlgorithms.RsaSha256),
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateJwtSecurityToken(descriptor));
    }

    private async Task<HttpStatusCode> Get(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);

        return response.StatusCode;
    }

    [Fact]
    public async Task AValidEosToken_SignsThePlayerInBySubject()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/who");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token());

        using var response = await client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, string.Join(" | ", response.Headers.WwwAuthenticate));
        Assert.Equal(Puid, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ThePlayer_PassesAnOwnerPolicyForTheOwnAccountOnly()
    {
        Assert.Equal(HttpStatusCode.OK, await Get($"/accounts/{Puid}", Token()));
        Assert.Equal(HttpStatusCode.Forbidden, await Get("/accounts/someone-else", Token()));
    }

    [Fact]
    public async Task AnEosToken_NeverGivesPermissions_EvenIfItHasThePermissionClaim()
    {
        var token = Token(extra: [new Claim(PermissionClaim.Type, "game:admin"), new Claim(PermissionClaim.Type, "game:read")]);

        Assert.Equal(HttpStatusCode.Forbidden, await Get("/admin", token));
        Assert.Equal(HttpStatusCode.Forbidden, await Get("/accounts/someone-else", token));
    }

    [Theory]
    [InlineData("wrong audience")]
    [InlineData("signed by another key")]
    [InlineData("expired")]
    [InlineData("issued in the future")]
    [InlineData("issuer on a look-alike host")]
    [InlineData("issuer that is not EOS")]
    [InlineData("no token signature")]
    public async Task ABadToken_IsRefused(string kind)
    {
        var token = kind switch
        {
            "wrong audience" => Token(audience: "another-game"),
            "signed by another key" => Token(key: OtherKey),
            "expired" => Token(expires: DateTime.UtcNow.AddMinutes(-10), issuedAt: DateTime.UtcNow.AddMinutes(-20)),
            "issued in the future" => Token(issuedAt: DateTime.UtcNow.AddHours(1), expires: DateTime.UtcNow.AddHours(2)),
            "issuer on a look-alike host" => Token(issuer: "https://api.epicgames.dev.evil.example/auth"),
            "issuer that is not EOS" => Token(issuer: "https://evil.example"),
            _ => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(EosIssuer, Audience, [new Claim("sub", Puid)], expires: DateTime.UtcNow.AddMinutes(5))),
        };

        Assert.Equal(HttpStatusCode.Unauthorized, await Get("/who", token));
    }

    [Fact]
    public async Task ATokenWithAnUnknownKeyId_IsRefusedWithoutAServerError()
    {
        var unknown = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "a-key-that-the-server-does-not-know" };

        Assert.Equal(HttpStatusCode.Unauthorized, await Get("/who", Token(key: unknown)));
    }

    [Fact]
    public async Task IfTheKeysCannotBeLoaded_TheTokenIsRefusedWithoutAServerError()
    {
        keyServer.Down = true;

        var status = await Get("/who", Token());

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task ATokenThatIsNotAJwt_IsRefused()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await Get("/who", "not-a-token"));
    }

    [Fact]
    public async Task TheKeysAreFetchedOnceAndKept()
    {
        await Get("/who", Token());
        await Get("/who", Token());
        await Get("/who", Token());

        Assert.Equal(1, keyServer.Requests);
    }

    [Fact]
    public async Task ATokenOfTheOtherIssuerStillUsesItsOwnScheme()
    {
        // A token of Keycloak is routed to the Keycloak scheme, which refuses it here because the host has no Keycloak to ask.
        var keycloakToken = Token(issuer: KeycloakIssuer, audience: "game-api");

        var status = await Get("/who", keycloakToken);

        Assert.NotEqual(HttpStatusCode.OK, status);
        Assert.Equal(0, keyServer.Requests);
    }

    [Fact]
    public void IsIssuer_AcceptsTheBaseAndItsPaths_AndNothingElse()
    {
        var options = new EosOptions();

        Assert.True(options.IsIssuer("https://api.epicgames.dev"));
        Assert.True(options.IsIssuer("https://api.epicgames.dev/auth/v1"));
        Assert.False(options.IsIssuer("https://api.epicgames.dev.evil.example"));
        Assert.False(options.IsIssuer("https://evil.example/https://api.epicgames.dev"));
        Assert.False(options.IsIssuer(null));
    }

    [Fact]
    public void Eos_IsOffWithoutAnAudience_AndEmptySettingsMeanTheDefaults()
    {
        var options = new EosOptions { Audience = " ", JwksUrl = "", IssuerBase = "", SubjectClaim = "" };

        Assert.False(options.Enabled);
        Assert.Equal(EosOptions.DefaultJwksUrl, options.EffectiveJwksUrl);
        Assert.Equal(EosOptions.DefaultIssuerBase, options.EffectiveIssuerBase);
        Assert.Equal("sub", options.EffectiveSubjectClaim);

        var services = new ServiceCollection().AddEosPlayerAuthentication(o => o.Audience = null);
        Assert.Empty(services);
    }

    /// <summary>Answers the request for the keys of Epic with the public key of the test.</summary>
    private sealed class StubKeyServer(RsaSecurityKey key) : HttpMessageHandler
    {
        public int Requests;
        public bool Down;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Requests);
            if (Down)
            {
                throw new HttpRequestException("The key server is down.");
            }

            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
            var json = $$"""{"keys":[{"kty":"RSA","use":"sig","kid":"{{jwk.Kid}}","n":"{{jwk.N}}","e":"{{jwk.E}}"}]}""";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
