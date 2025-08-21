using Common.Infrastructure.Auth.ApiKey;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Common.Testing.Web.Auth;

public sealed class FakeApiKeySecurityManager : IApiKeySecurityManager
{
    public Task<AuthenticateResult> AuthenticateApiKey(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(ApiKeyAuthenticationHandler.ApiKeyHeaderName, out StringValues value))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string keyValue = value!;
        if (string.IsNullOrEmpty(keyValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var ticket = FakeApiKeyAuthenticator.GetAuthTicket(keyValue);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
