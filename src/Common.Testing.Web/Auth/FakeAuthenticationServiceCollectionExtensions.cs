using Common.Infrastructure.Auth.ApiKey;
using Common.LanguageExtensions.TestableAlternatives;
using Common.Testing.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Common.Testing.Integration.Auth;

public static class FakeAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection ConfigureFakeJwtTokens(this IServiceCollection services)
    {
        return services
            .Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = FakeJwtTokens.Issuer,
                    ValidateAudience = false
                };

                var config = new OpenIdConnectConfiguration()
                {
                    Issuer = FakeJwtTokens.Issuer
                };

                config.SigningKeys.Add(FakeJwtTokens.SecurityKey);
                options.Configuration = config;
                options.TimeProvider = new FakeTimeProvider();
            })
            .AddSingleton<JwtBearerHandler, FakeJwtBearerHandler>();
    }

    public static IServiceCollection ConfigureFakeApiKeys(this IServiceCollection services)
    {
        return services.AddSingleton<IApiKeySecurityManager, FakeApiKeySecurityManager>();
    }

    private class FakeJwtBearerHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : JwtBearerHandler(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header not found."));
            }

            var authorizationHeader = authorizationHeaderValues.FirstOrDefault();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Task.FromResult(AuthenticateResult.Fail("Bearer token not found in Authorization header."));
            }

            var token = authorizationHeader["Bearer ".Length..].Trim();
            var result = AuthenticateResult.Success(new AuthenticationTicket(GetClaims(token), "CustomJwtBearer"));

            return Task.FromResult(result);
        }

        private static ClaimsPrincipal GetClaims(string Token)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(Token) as JwtSecurityToken;

            var claimsIdentity = new ClaimsIdentity(token!.Claims, "Token");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }
    }
}
