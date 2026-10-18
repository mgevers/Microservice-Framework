using System.Net.Http.Headers;
using System.Security.Claims;
using Common.Infrastructure.Compliance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Common.Testing.Web.Auth;

public static class AuthenticatedClientExtensions
{
    /// <summary>
    /// A client with a fake token. The token has the subject (the caller) and the permissions, in the claim that the
    /// services read. Keycloak and Auth0 put the permissions in the same claim.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        string subject,
        params string[] permissions)
        where TProgram : class
    {
        var client = factory.CreateClient();
        var claims = permissions.Select(p => new Claim(PermissionClaim.Type, p)).Append(new Claim("sub", subject));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtTokens.GenerateJwtToken(claims));

        return client;
    }

    /// <summary>Gives the host a key for the message bodies. A service does not start without one.</summary>
    public static IWebHostBuilder UseTestMessageEncryptionKey(this IWebHostBuilder builder) =>
        builder.UseSetting(MessageEncryption.ConfigurationKey, Convert.ToBase64String(new byte[32]));
}
