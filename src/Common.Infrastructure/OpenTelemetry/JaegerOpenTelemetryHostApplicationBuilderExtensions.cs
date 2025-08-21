using Common.Infrastructure.OpenTelemetry.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Common.Infrastructure.OpenTelemetry;

public static class JaegerOpenTelemetryHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder ConfigureJaegerOpenTelemetry(this IHostApplicationBuilder builder)
    {
        var jaegerOptions = builder.Configuration
            .GetSection(nameof(JaegerOptions))
            .Get<JaegerOptions>();

        if (jaegerOptions == null)
        {
            return builder;
        }

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracingBuilder =>
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(jaegerOptions.ServiceName)
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector();

                tracingBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddJaegerExporter(o =>
                    {
                        o.AgentHost = jaegerOptions.AgentHost;
                        o.AgentPort = jaegerOptions.AgentPort;
                        o.MaxPayloadSizeInBytes = 4096;
                        o.ExportProcessorType = ExportProcessorType.Batch;
                        o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                        {
                            MaxQueueSize = 2048,
                            ScheduledDelayMilliseconds = 5000,
                            ExporterTimeoutMilliseconds = 30000,
                            MaxExportBatchSize = 512,
                        };
                    });
            });

        return builder;
    }
}
