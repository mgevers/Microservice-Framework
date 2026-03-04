using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Infrastructure.OpenTelemetry;

public static class OpenTelemetryHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder, string serviceName)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceInstanceId: Environment.MachineName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        var excludedPaths = new[] { "/health", "/metrics", "/swagger" };

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(meterBuilder => {
                meterBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value;
                            return !excludedPaths.Any(excludedPath =>
                            {
                                return path?.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase) == true;
                            });
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
            });

        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
        builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());

        builder.Services.AddMetrics();

        return builder;
    }
}
