using Common.Infrastructure.OpenTelemetry;
using Common.Infrastructure.ServiceBus.MassTransit;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Infrastructure;

public static class MessageProcessor
{
    public static HostApplicationBuilder AddCommonConfiguration(
        this HostApplicationBuilder builder,
        string serviceName,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        AddLogging(builder, serviceName);
        AddMessageProcessing(builder, configureBus);

        return builder;
    }

    public static IHostApplicationBuilder AddLogging(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        builder.UseSerilog(serviceName);
        builder.Services.AddLogging();

        return builder;
    }

    public static IHostApplicationBuilder AddMessageProcessing(
        this IHostApplicationBuilder builder,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        builder
            .SetupMassTransit(options =>
            {
                builder.Configuration.GetSection("Messaging").Bind(options);
            },
            configureBus: config =>
            {
                configureBus?.Invoke(config);
            });

        return builder;
    }
}
