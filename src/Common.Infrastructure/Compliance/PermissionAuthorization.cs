using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Compliance;

/// <summary>The claim that holds the permissions of the caller. Keycloak puts the realm roles there, and Auth0 puts the API permissions.</summary>
public static class PermissionClaim
{
    public const string Type = "permissions";
}

/// <summary>
/// The caller passes if the token has the permission. If <paramref name="OwnerRouteKey"/> is set, the owner passes too:
/// the caller whose subject is the value of that route parameter.
/// </summary>
public record PermissionRequirement(string Permission, string? OwnerRouteKey = null) : IAuthorizationRequirement;

public class PermissionHandler(IHttpContextAccessor accessor) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(PermissionClaim.Type, requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.OwnerRouteKey is not null)
        {
            var routeValue = accessor.HttpContext?.GetRouteValue(requirement.OwnerRouteKey) as string;
            var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(routeValue) && routeValue == subject)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class PermissionPolicies(AuthorizationOptions options)
{
    /// <summary>A signed-in caller whose token has the permission.</summary>
    public PermissionPolicies Require(string policy, string permission) => Add(policy, new PermissionRequirement(permission));

    /// <summary>A signed-in caller whose token has the permission, or the owner of the account in the route parameter <paramref name="ownerRouteKey"/>.</summary>
    public PermissionPolicies RequireOrOwner(string policy, string permission, string ownerRouteKey) =>
        Add(policy, new PermissionRequirement(permission, ownerRouteKey));

    private PermissionPolicies Add(string policy, PermissionRequirement requirement)
    {
        options.AddPolicy(policy, builder => builder.RequireAuthenticatedUser().AddRequirements(requirement));

        return this;
    }
}

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Adds the permission policies of a service, the current actor, and a fallback policy: an endpoint with no policy
    /// still needs a signed-in caller, so a new endpoint is never open by mistake.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services, Action<PermissionPolicies> policies)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentActor, HttpContextActor>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

        services.AddAuthorization(options =>
        {
            policies(new PermissionPolicies(options));
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });

        return services;
    }
}
