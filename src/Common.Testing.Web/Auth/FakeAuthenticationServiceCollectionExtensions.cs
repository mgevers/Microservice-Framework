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
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                var config = new OpenIdConnectConfiguration()
                {
                    Issuer = FakeJwtTokens.Issuer
                };

                config.SigningKeys.Add(FakeJwtTokens.SecurityKey);
                options.Configuration = config;
                options.TimeProvider = new FakeTimeProvider();
            })
            .AddScoped<JwtBearerHandler, FakeJwtBearerHandler>();
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
            var claimsResult = GetClaims(token, Options);

            var result = claimsResult.Item1 == null
                ? AuthenticateResult.Fail(claimsResult.Item2)
                : AuthenticateResult.Success(new AuthenticationTicket(claimsResult.Item1, "CustomJwtBearer"));

            return Task.FromResult(result);
        }

        private static (ClaimsPrincipal?, string) GetClaims(string token, JwtBearerOptions options)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken is null)
            {
                return (null, "Invalid JWT token format.");
            }

            var utcNow = options.TimeProvider?.GetUtcNow() ?? CurrentTime.UtcNow;

            if (jwtToken.ValidTo < utcNow)
            {
                return (null, $"Jwt Token is expired. Valid to - {jwtToken.ValidTo}, current time is: {utcNow}");
            }

            var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "Token");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return (claimsPrincipal, string.Empty);
        }
    }
}
