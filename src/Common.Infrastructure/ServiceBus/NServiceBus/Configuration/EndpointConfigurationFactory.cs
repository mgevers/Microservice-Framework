using Common.Infrastructure.ServiceBus.NServiceBus.Options;
using Common.Infrastructure.ServiceBus.NServiceBus.RequestResponse;
using Newtonsoft.Json;
using NServiceBus.Transport;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common.Infrastructure.ServiceBus.NServiceBus.Configuration;

public static class EndpointConfigurationFactory
{
    public static EndpointConfiguration GetEndpointConfigurationForCommandSending<T>(
        string endpointName,
        string destinationEndpointName,
        Assembly commandAssembly,
        NServiceBusOptions options)
        where T : TransportDefinition
    {

        var endpointConfiguration = GetCommonEndpointConfiguration<T>(
            endpointName,
            options,
            typesToScan: new[] { typeof(CallbackCommandHandler) },
            configureTransportDelegate: transport => transport.Routing().RouteToEndpoint(commandAssembly, destinationEndpointName));

        return endpointConfiguration;
    }

    public static EndpointConfiguration GetEndpointConfigurationForMessageProcessing<T>(
        string endpointName,
        NServiceBusOptions options,
        IReadOnlyCollection<Type> typesToScan)
        where T : TransportDefinition
    {
        return GetCommonEndpointConfiguration<T>(endpointName, options, typesToScan);
    }

    private static EndpointConfiguration GetCommonEndpointConfiguration<T>(
        string endpointName,
        NServiceBusOptions nServiceBusSettings,
        IReadOnlyCollection<Type> typesToScan,
        Action<TransportExtensions<T>>? configureTransportDelegate = null)
        where T : TransportDefinition
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        ConfigureTransport(endpointConfiguration, nServiceBusSettings, configureTransportDelegate);
        ConfigureSerialization(endpointConfiguration);
        ConfigureRetries(endpointConfiguration, nServiceBusSettings);
        ConfigureAssemblyScanner(endpointConfiguration, typesToScan);
        endpointConfiguration.EnableInstallers();

        return endpointConfiguration;
    }

    private static void ConfigureTransport<T>(EndpointConfiguration configuration,
        NServiceBusOptions nServiceBusSettings,
        Action<TransportExtensions<T>>? configureTransportDelegate)
        where T : TransportDefinition
    {
        Type transportType = typeof(T);

        if (transportType == typeof(AzureServiceBusTransport))
        {
            ConfigureAzureServiceBusTransport(configuration, nServiceBusSettings, configureTransportDelegate as Action<TransportExtensions<AzureServiceBusTransport>>);
        }
        else if (transportType == typeof(LearningTransport))
        {
            ConfigureLearningTransport(configuration, nServiceBusSettings, configureTransportDelegate as Action<TransportExtensions<LearningTransport>>);
        }
        else
        {
            throw new NotSupportedException($"transport: {transportType.Name} not supported");
        }
    }

    private static void ConfigureAzureServiceBusTransport(
        EndpointConfiguration configuration,
        NServiceBusOptions nServiceBusSettings,
        Action<TransportExtensions<AzureServiceBusTransport>>? configureTransportDelegate)
    {
        var transport = configuration
            .UseTransport<AzureServiceBusTransport>()
            .ConnectionString(nServiceBusSettings.AzureConnectionString)
            .SubscriptionRuleNamingConvention(NServiceBusNameShortener.Shorten);

        configureTransportDelegate?.Invoke(transport);
    }

    private static void ConfigureLearningTransport(
        EndpointConfiguration configuration,
        NServiceBusOptions nServiceBusSettings,
        Action<TransportExtensions<LearningTransport>>? configureTransportDelegate)
    {
        var transport = configuration.UseTransport<LearningTransport>();
        configureTransportDelegate?.Invoke(transport);
    }

    private static void ConfigureSerialization(EndpointConfiguration configuration)
    {
        configuration
            .UseSerialization<NewtonsoftJsonSerializer>()
            .Settings(new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None });
    }

    private static void ConfigureRetries(EndpointConfiguration configuration, NServiceBusOptions nServiceBusSettings)
    {
        configuration.Recoverability()
            .Immediate(customization => customization.NumberOfRetries(nServiceBusSettings.ImmediateRetries))
            .Delayed(customization => customization.NumberOfRetries(nServiceBusSettings.DelayedRetries));
    }

    private static void ConfigureAssemblyScanner(EndpointConfiguration configuration, IReadOnlyCollection<Type> wantedTypes)
    {
        IReadOnlyCollection<string> binAssemblyNames = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
            .Select(Path.GetFileName)
            .Where(fileName => string.IsNullOrEmpty(fileName) == false)
            .ToList()!;

        IReadOnlyCollection<string> appDomainAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.IsDynamic == false)
            .Select(assembly => Path.GetFileName(assembly.Location))
            .Where(fileName => string.IsNullOrEmpty(fileName) == false)
            .ToList();

        IReadOnlyCollection<string> allAssemblyNames = binAssemblyNames.Concat(appDomainAssemblyNames).Distinct().ToList();

        IReadOnlyCollection<Assembly> wantedAssemblies = wantedTypes.Select(type => type.Assembly).Distinct().ToList();
        IReadOnlyCollection<string> wantedAssemblyNames = wantedAssemblies.Select(assembly => Path.GetFileName(assembly.Location)).ToList();
        IReadOnlyCollection<string> systemAssemblyNames = allAssemblyNames.Where(assemblyName => Regex.IsMatch(input: assemblyName, pattern: @"^NServiceBus\..+\.dll$")).ToList();
        Type[] unwantedTypesFromWantedAssembly = wantedAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Except(wantedTypes)
            .ToArray();

        var assemblyScannerConfiguration = configuration.AssemblyScanner();

        assemblyScannerConfiguration.ScanAppDomainAssemblies = true;
        assemblyScannerConfiguration.ExcludeAssemblies(allAssemblyNames.Except(wantedAssemblyNames.Concat(systemAssemblyNames)).ToArray());
        assemblyScannerConfiguration.ExcludeTypes(unwantedTypesFromWantedAssembly);
    }
}
