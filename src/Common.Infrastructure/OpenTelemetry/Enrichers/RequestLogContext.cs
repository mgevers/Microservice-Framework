using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Common.Infrastructure.OpenTelemetry.Enrichers;

public class RequestLogContext
{
    private readonly RequestDelegate _next;

    public RequestLogContext(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        {
            return _next(context);
        }
    }
}
