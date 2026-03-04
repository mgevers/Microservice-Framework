using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Common.Infrastructure.OpenTelemetry
{
    public static class SerilogExtensions
    {
        public static IHostApplicationBuilder UseSerilog(
            this IHostApplicationBuilder builder,
            string serviceName,
            bool logToConsole = false,
            bool logToOtel = true)
        {
            var loggerConfiguration = GetLoggerConfiguration(
                builder.Configuration,
                serviceName,
                logToConsole,
                logToOtel);

            Log.Logger = loggerConfiguration.CreateLogger();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
            builder.Services.AddSerilog();

            if (logToOtel)
            {
                builder.ConfigureOpenTelemetry(serviceName);
            }

            return builder;
        }

        private static LoggerConfiguration GetLoggerConfiguration(
            IConfiguration configuration,
            string serviceName,
            bool logToConsole = false,
            bool logToOtel = true)
        {
            var loggerConfiguration = new LoggerConfiguration();

            loggerConfiguration.ReadFrom.Configuration(configuration);

            if (logToConsole)
            {
                loggerConfiguration.WriteTo.Console();
            }

            if (logToOtel)
            {
                var otelProtocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];

                loggerConfiguration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]; ;
                    options.Protocol = otelProtocol?.ToLower() != "grpc" ? OtlpProtocol.HttpProtobuf : OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName,
                        ["service.instance.id"] = Environment.MachineName,
                    };
                });
            }

            loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("SourceName", serviceName);

            return loggerConfiguration;
        }
    }
}
