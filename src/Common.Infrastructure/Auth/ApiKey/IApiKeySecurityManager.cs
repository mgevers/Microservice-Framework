using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Common.Infrastructure.Auth.ApiKey;

public interface IApiKeySecurityManager
{
    Task<AuthenticateResult> AuthenticateApiKey(HttpRequest request);
}
