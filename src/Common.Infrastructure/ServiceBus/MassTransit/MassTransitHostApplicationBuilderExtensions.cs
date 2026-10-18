using Common.Infrastructure.ServiceBus.MassTransit.Observers;
using Common.Infrastructure.ServiceBus.MassTransit.Options;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Infrastructure.ServiceBus.MassTransit;

public static class MassTransitHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder SetupMassTransitWithOutbox<TDbContext>(
        this IHostApplicationBuilder builder,
        Action<OutboxOptions> outboxConfig,
        Action<RabbitMqTransportOptions> rabbitConfig,
        Action<IBusRegistrationConfigurator>? configureBus = null,
        byte[]? messageEncryptionKey = null)
        where TDbContext : DbContext
    {
        builder.Services
            .AddOptions<RabbitMqTransportOptions>()
            .Configure(rabbitConfig);

        builder.Services
            .AddMassTransit(config =>
            {
                // TODO switch to SQL
                config.SetInMemorySagaRepositoryProvider();
                config.SetKebabCaseEndpointNameFormatter();

                ConfigureOutbox<TDbContext>(config, outboxConfig);

                ConfigureRabbitMq(config, messageEncryptionKey);
                configureBus?.Invoke(config);
            })
            .AddSendObserver<SendObserver>()
            .AddReceiveObserver<ReceiveObserver>()
            .AddPublishObserver<PublishObserver>()
            .AddConsumeObserver<ConsumeObserver>();

        return builder;
    }

    public static IHostApplicationBuilder SetupMassTransit(
        this IHostApplicationBuilder builder,
        Action<RabbitMqTransportOptions> rabbitConfig,
        Action<IBusRegistrationConfigurator>? configureBus = null,
        byte[]? messageEncryptionKey = null)
    {
        builder.Services
            .AddOptions<RabbitMqTransportOptions>()
            .Configure(rabbitConfig);

        builder.Services
            .AddMassTransit(config =>
            {
                // TODO switch to SQL
                config.SetInMemorySagaRepositoryProvider();
                config.SetKebabCaseEndpointNameFormatter();

                ConfigureRabbitMq(config, messageEncryptionKey);
                configureBus?.Invoke(config);
            })
            .AddSendObserver<SendObserver>()
            .AddReceiveObserver<ReceiveObserver>()
            .AddPublishObserver<PublishObserver>()
            .AddConsumeObserver<ConsumeObserver>();

        return builder;
    }

    private static void ConfigureRabbitMq(IBusRegistrationConfigurator config, byte[]? messageEncryptionKey)
    {
        config.UsingRabbitMq((busContext, rabbitConfig) =>
        {
            rabbitConfig.UseNewtonsoftJsonSerializer();
            rabbitConfig.UseNewtonsoftJsonDeserializer();

            // With a key (AES-256), every message body is encrypted, so RabbitMQ stores it encrypted too.
            if (messageEncryptionKey is not null)
            {
                rabbitConfig.UseEncryption(messageEncryptionKey);
            }

            rabbitConfig.ConfigureEndpoints(busContext);
            rabbitConfig.AutoStart = true;

            rabbitConfig.UseMessageRetry(retry =>
            {
                retry.Immediate(5);
                retry.Interval(5, 2);
            });
        });
    }

    private static void ConfigureOutbox<TDbContext>(
        IBusRegistrationConfigurator config,
        Action<OutboxOptions> outboxConfig)
        where TDbContext : DbContext
    {
        var options = new OutboxOptions();
        outboxConfig(options);

        config.AddEntityFrameworkOutbox<TDbContext>(outboxConfig =>
        {
            if (options.Provider == OutboxDatabaseProvider.PostgreSql)
            {
                outboxConfig.UsePostgres();
            }
            else
            {
                outboxConfig.UseSqlServer();
            }

            outboxConfig.UseBusOutbox();

            outboxConfig.QueryDelay = TimeSpan.FromSeconds(1);
            outboxConfig.DuplicateDetectionWindow = TimeSpan.FromMinutes(options.DuplicateDetectionWindowMinutes);
        });
    }
}

