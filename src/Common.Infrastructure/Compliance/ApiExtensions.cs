using System.Reflection;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Compliance;

public static class ApiExtensions
{
    /// <summary>MediatR for the handlers of the assembly, with the audit behavior in front of every handler.</summary>
    public static IServiceCollection AddAuditedMediatR(this IServiceCollection services, Assembly handlers) =>
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(handlers);
            config.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

    /// <summary>
    /// MediatR for the handlers of the assemblies, with the audit behavior in front of every handler. For a host with several
    /// audit trails registered by key: <paramref name="auditTrailKey"/> picks the key of the trail for the type of a request.
    /// </summary>
    public static IServiceCollection AddAuditedMediatR(this IServiceCollection services, Func<Type, object> auditTrailKey, params Assembly[] handlers)
    {
        services.AddSingleton(new AuditTrailKey(auditTrailKey));

        return services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(handlers);
            config.AddOpenBehavior(typeof(KeyedAuditBehavior<,>));
        });
    }

    /// <summary>Problem details, and a 504 when the message processor does not answer a command in time.</summary>
    public static IServiceCollection AddRequestTimeoutProblemDetails(this IServiceCollection services) =>
        services
            .AddProblemDetails()
            .AddExceptionHandler<RequestTimeoutExceptionHandler>();

    /// <summary>
    /// Live means the process runs. Ready also checks the checks that are tagged "ready", such as Postgres and the bus.
    /// The health endpoints hold no data, so they stay open for the orchestrator.
    /// </summary>
    public static void MapServiceHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
    }

    /// <summary>Turns a query result into an HTTP response. Ardalis returns a typed result that MVC needs converted.</summary>
    public static IActionResult ToResponse<T>(this Result<T> result, ControllerBase controller)
    {
        IConvertToActionResult actionResult = result.ToActionResult(controller);

        return actionResult.Convert();
    }

    /// <summary>Sends a command to the message processor, waits for its <see cref="Result"/>, and turns it into an HTTP response.</summary>
    public static async Task<IActionResult> SendForResponse<TCommand>(
        this IRequestClient<TCommand> client,
        TCommand command,
        ControllerBase controller,
        CancellationToken cancellationToken)
        where TCommand : class =>
        (await client.SendForResult(command, cancellationToken)).ToActionResult(controller);

    /// <summary>Sends a command to the message processor and waits for its <see cref="Result"/>.</summary>
    public static async Task<Result> SendForResult<TCommand>(
        this IRequestClient<TCommand> client,
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : class =>
        (await client.GetResponse<Result>(command, cancellationToken)).Message;
}
