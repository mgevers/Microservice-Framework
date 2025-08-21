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
        builder.Logging.AddOpenTelemetry(config =>
        {
            config.IncludeScopes = true;
            config.IncludeFormattedMessage = true;
        });

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(meterBuilder => {
                meterBuilder.AddAspNetCoreInstrumentation();
                meterBuilder.AddHttpClientInstrumentation();
            })
            .WithTracing(tracingBuilder =>
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector();

                tracingBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation()
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
