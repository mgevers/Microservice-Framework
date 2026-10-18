using Common.Infrastructure.OpenTelemetry.Enrichers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace Common.Infrastructure;

public static class WebApi
{
    public static IHostApplicationBuilder AddCommonConfiguration(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
        });

        builder.Services
            .AddLogging()
            .AddCors()
            .AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddSwaggerGenNewtonsoftSupport();

        return builder;
    }

    /// <summary>
    /// Logs the route template, for example /tags/{tag}, and never the path. The path can hold sensitive values.
    /// </summary>
    public static void ConfigureRequestLogging(RequestLoggingOptions options)
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestRoute} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetMessageTemplateProperties = (httpContext, requestPath, elapsedMs, statusCode) =>
        [
            new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
            new LogEventProperty("RequestRoute", new ScalarValue(RouteTemplate(httpContext))),
            new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
            new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
        ];
    }

    public static string RouteTemplate(HttpContext httpContext) =>
        (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";

    public static void AddCommonConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(builder =>
                builder
                    .SetIsOriginAllowed(origin => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        }

        app.UseMiddleware<RequestLogContext>();
        app.UseSerilogRequestLogging(ConfigureRequestLogging);

        app
            .UseSwagger()
            .UseSwaggerUI();
    }
}
