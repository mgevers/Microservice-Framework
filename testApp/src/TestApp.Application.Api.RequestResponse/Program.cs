using Common.Infrastructure.Auth;
using Common.Infrastructure;
using Common.Core.Boundary;
using Common.Infrastructure.ServiceBus.NServiceBus.Options;
using NServiceBus.Transport;
using TestApp.Core.Boundary;
using Common.Infrastructure.ServiceBus.NServiceBus.Configuration;

namespace TestApp.Application.Api.RequestResponse;

public partial class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCommonConfiguration();
        ConfigureServices(builder.Services);

        var app = builder.Build();
        ConfigureApplication(app);

        return app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddJwtAndAPIKeyAuthentication("", "");
    }

    private static void ConfigureApplication(WebApplication app)
    {
        app.AddCommonConfiguration();
    }

    private static EndpointConfiguration GetEndpointConfiguration<T>(HostBuilderContext hostBuilderContext)
        where T : TransportDefinition
    {
        var options = new NServiceBusOptions()
        {
            CommandTimeout = TimeSpan.FromSeconds(1),
            DelayedRetries = 0,
            ImmediateRetries = 0,
        };

        return EndpointConfigurationFactory.GetEndpointConfigurationForMessageProcessing<T>(
            endpointName: $"{TestAppConstants.ContextName}-{CommonConstants.MessageProcessor}",
            options: options,
            typesToScan: typeof(AddCharacterCommand).Assembly.DefinedTypes.ToList());
    }
}
