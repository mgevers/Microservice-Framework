using System.Security.Claims;
using Common.Infrastructure.Compliance;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Common.Infrastructure.Auth.Eos;

public static class EosAuthenticationExtensions
{
    public const string Scheme = "Eos";
    public const string SelectorScheme = "BearerOrEos";
    public const string HttpClientName = "eos-jwks";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Lets a player sign in with an EOS Connect ID token, next to the token of the identity provider of the service.
    /// Call it after <c>AddJwtAuthentication</c>. It does nothing if <see cref="EosOptions.Audience"/> is empty.
    /// </summary>
    /// <remarks>
    /// The service reads the <c>iss</c> claim only to choose the scheme. Each scheme then checks the whole token.
    /// An EOS token never gives permissions: the service removes any <c>permissions</c> claim from it, so the player
    /// passes only the policies that accept the owner of the account.
    /// </remarks>
    public static IServiceCollection AddEosPlayerAuthentication(this IServiceCollection services, Action<EosOptions> configure)
    {
        var eos = new EosOptions();
        configure(eos);
        if (!eos.Enabled)
        {
            return services;
        }

        services.AddHttpClient(HttpClientName);

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = SelectorScheme;
                options.DefaultChallengeScheme = SelectorScheme;
            })
            .AddJwtBearer(Scheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    IssuerValidator = (issuer, _, _) => eos.IsIssuer(issuer)
                        ? issuer
                        : throw new SecurityTokenInvalidIssuerException("The issuer is not EOS.") { InvalidIssuer = issuer },
                    ValidateAudience = true,
                    ValidAudience = eos.Audience!.Trim(),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = ClockSkew,
                };
                // Keep the claim names of the token, so that the setting SubjectClaim names a claim of the token.
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents { OnTokenValidated = context => ValidateClaims(context, eos) };
            })
            .AddPolicyScheme(SelectorScheme, SelectorScheme, options =>
                options.ForwardDefaultSelector = context => ChooseScheme(context.Request.Headers.Authorization.ToString(), eos));

        services
            .AddOptions<JwtBearerOptions>(Scheme)
            .Configure<IHttpClientFactory>((options, factory) =>
                options.ConfigurationManager = new JwksConfigurationManager(factory.CreateClient(HttpClientName), eos.EffectiveJwksUrl));

        return services;
    }

    private static string ChooseScheme(string authorization, EosOptions eos)
    {
        const string prefix = "Bearer ";
        if (authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization[prefix.Length..].Trim();
            var handler = new JsonWebTokenHandler();

            if (handler.CanReadToken(token) && eos.IsIssuer(handler.ReadJsonWebToken(token).Issuer))
            {
                return Scheme;
            }
        }

        return JwtBearerDefaults.AuthenticationScheme;
    }

    private static Task ValidateClaims(TokenValidatedContext context, EosOptions eos)
    {
        var identity = (ClaimsIdentity)context.Principal!.Identity!;

        // A token of EOS holds no permissions. Remove any claim of that name, so that no token grants them by mistake.
        foreach (var claim in identity.FindAll(PermissionClaim.Type).ToList())
        {
            identity.RemoveClaim(claim);
        }

        // The document of EOS asks for a check that the token was issued in the past.
        if (context.SecurityToken is JsonWebToken jwt && jwt.TryGetPayloadValue<long>("iat", out var iat)
            && DateTimeOffset.FromUnixTimeSeconds(iat) > DateTimeOffset.UtcNow + ClockSkew)
        {
            context.Fail("The token was issued in the future.");
            return Task.CompletedTask;
        }

        var subject = identity.FindFirst(eos.EffectiveSubjectClaim)?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Fail("The token has no subject.");
            return Task.CompletedTask;
        }

        // The services read the player from the subject claim.
        foreach (var claim in identity.FindAll(c => c.Type is "sub" or ClaimTypes.NameIdentifier).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim("sub", subject));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));

        return Task.CompletedTask;
    }
}
