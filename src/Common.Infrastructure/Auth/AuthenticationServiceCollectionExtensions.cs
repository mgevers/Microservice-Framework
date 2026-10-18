using Common.Infrastructure.Auth.ApiKey;
using Common.Infrastructure.Auth.Token;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Common.Infrastructure.Auth;

public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// The issuer that Auth0 and similar providers put in their tokens: https://domain/ with a trailing slash.
    /// The domain can be a host name or a full https URL.
    /// </summary>
    public static string Issuer(string domain) =>
        $"https://{domain.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase).TrimEnd('/')}/";

    public static IServiceCollection AddJwtAndAPIKeyAuthentication(this IServiceCollection services, string domain, IReadOnlyCollection<string> audiences)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "JwtOrApiKey";
                options.DefaultChallengeScheme = "JwtOrApiKey";
            })
            .AddJwtBearer("Bearer", delegate (JwtBearerOptions c)
            {
                c.Authority = Issuer(domain);
                c.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudiences = audiences,
                    ValidIssuer = Issuer(domain),
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ClockSkew = TimeSpan.FromMinutes(5.0),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateLifetime = true
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.ApiKeyAuthenticationScheme, configureOptions: null)
            .AddPolicyScheme("JwtOrApiKey", JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string authorization = context.Request.Headers[HeaderNames.Authorization]!;
                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }
                    return ApiKeyAuthenticationHandler.ApiKeyAuthenticationScheme;
                };
            });

        return services;
    }

    public static IServiceCollection AddJwtAndAPIKeyAuthentication(this IServiceCollection services, string domain, string audience)
    {
        return services.AddJwtAndAPIKeyAuthentication(domain, new[] { audience });
    }

    public static IServiceCollection AddJwtAndAPIKeyAuthentication(this IServiceCollection services, Action<OAuthOptions> configure)
    {
        var options = new OAuthOptions();
        configure(options);

        return services.AddJwtAndAPIKeyAuthentication(domain: options.Domain, audience: options.Audience);
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, string domain, string audience)
    {
        return services.AddJwtAuthentication(domain, [audience]);
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, string domain, IReadOnlyCollection<string> audiences)
    {
        return services.AddJwtAuthenticationForIssuer(Issuer(domain), audiences);
    }

    /// <summary>
    /// For identity providers whose issuer is not https://domain/, such as Keycloak (https://host/realms/name).
    /// The issuer is used as given, and the provider's discovery document is read from it.
    /// </summary>
    public static IServiceCollection AddJwtAuthenticationForIssuer(this IServiceCollection services, string issuer, IReadOnlyCollection<string> audiences)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer("Bearer", delegate (JwtBearerOptions c)
            {
                c.Authority = issuer;
                c.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudiences = audiences,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ClockSkew = TimeSpan.FromMinutes(5.0),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateLifetime = true
                };
            });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, Action<OAuthOptions> configure)
    {
        var options = new OAuthOptions();
        configure(options);

        return string.IsNullOrWhiteSpace(options.Issuer)
            ? services.AddJwtAuthentication(domain: options.Domain, audience: options.Audience)
            : services.AddJwtAuthenticationForIssuer(options.Issuer, [options.Audience]);
    }
}
