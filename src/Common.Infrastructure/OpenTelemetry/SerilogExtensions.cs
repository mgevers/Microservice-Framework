using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

namespace Common.Infrastructure.OpenTelemetry
{
    public static class SerilogExtensions
    {
        public static IHostApplicationBuilder UseSerilog(
            this IHostApplicationBuilder builder,
            Action<LoggerConfiguration, IServiceProvider>? customConfiguration = null,
            bool logToAppInsights = false,
            bool logToConsole = false,
            bool logToOtel = true)
        {
            builder.Services.AddSerilog((services, configuration) =>
            {
                configuration.ReadFrom.Configuration(builder.Configuration);
                if (!string.IsNullOrEmpty(builder.Configuration.GetValue<string>("DD_API_KEY")))
                {
                    var ddService = builder.Configuration.GetValue<string>("DD_SERVICE");
                    var ddVersion = builder.Configuration.GetValue<string>("DD_VERSION");
                    var ddEnv = builder.Configuration.GetValue<string>("DD_ENV");

                    if (string.IsNullOrEmpty(ddService) || string.IsNullOrEmpty(ddVersion) || string.IsNullOrEmpty(ddEnv))
                    {

                        throw new Exception("DD_SERVICE, DD_VERSION, and DD_ENV must be defined in configuration if DD_API_KEY is defined.");
                    }

                    var ddTags = new List<string>();
                    var tagsFromEnv = builder.Configuration.GetValue<string>("DD_TAGS");
                    if (!string.IsNullOrEmpty(tagsFromEnv))
                    {
                        ddTags.AddRange(tagsFromEnv.Split(','));
                    }
                    ddTags.Add($"version:{ddVersion}");
                    ddTags.Add($"service:{ddService}");
                    ddTags.Add($"env:{ddEnv}");
                    var config = new DatadogConfiguration(url: "https://http-intake.logs.us3.datadoghq.com", useSSL: true, port: 443, useTCP: false);
                    configuration.WriteTo.DatadogLogs(builder.Configuration.GetValue<string>("DD_API_KEY"),
                        source: "csharp",
                        service: ddService,
                        tags: ddTags.ToArray(),
                        configuration: config,
                        host: Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName);
                }

                if (logToConsole)
                {
                    configuration.WriteTo.Console();
                }

                if (logToAppInsights)
                {
                    configuration.WriteTo.ApplicationInsights(services.GetService<TelemetryConfiguration>(), TelemetryConverter.Traces);
                }

                if (logToOtel)
                {
                    configuration.WriteTo.OpenTelemetry();
                }
;
                customConfiguration?.Invoke(configuration, services);
            });

            return builder;
        }
    }
}
