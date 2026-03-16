using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Common.Infrastructure.Mediator;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        Assembly assembly,
        Action<MediatRServiceConfiguration>? configure = null)
    {
        return services.AddMediatR(configuration =>
        {
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
            configuration.RegisterServicesFromAssembly(assembly);

            configure?.Invoke(configuration);
        });
    }

    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        IReadOnlyCollection<Assembly> assemblies,
        Action<MediatRServiceConfiguration>? configure = null)
    {
        return services.AddMediatR(configuration =>
        {
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
            configuration.RegisterServicesFromAssemblies([.. assemblies]);

            configure?.Invoke(configuration);
        });
    }
}
