using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Common.LanguageExtensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddScopedAsAllImplementedInterfaces<T>(this IServiceCollection services)
        {
            return AddScopedAsAllImplementedInterfaces(services, typeof(T));
        }

        public static IServiceCollection AddScopedAsAllImplementedInterfaces(this IServiceCollection services, Type concreteType)
        {
            services.AddScoped(concreteType);

            foreach (var interfaceType in concreteType.GetInterfaces()) {
                services.AddScoped(serviceType: interfaceType, implementationFactory: serviceProvider => serviceProvider.GetRequiredService(concreteType));
            }

            return services;
        }

        public static IServiceCollection AddSingletonIncludingAllConcreteDependencies<TService>(this IServiceCollection services)
            where TService : class
        {
            return services
                .AddSingletonIncludingAllConcreteDependencies<TService, TService>();
        }

        public static IServiceCollection AddSingletonIncludingAllConcreteDependencies<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services
                .AddSingleton<TService, TImplementation>()
                .AddAllConcreteDependenciesAsSingletons(typeof(TImplementation));
        }

        private static IServiceCollection AddAllConcreteDependenciesAsSingletons(this IServiceCollection services, Type serviceType)
        {
            foreach (ConstructorInfo constructor in serviceType.GetConstructors()) {
                foreach (ParameterInfo parameter in constructor.GetParameters()) {
                    Type dependencyType = parameter.ParameterType;

                    if (dependencyType.IsAbstract == false) {
                        services.TryAddSingleton(dependencyType);
                        services.AddAllConcreteDependenciesAsSingletons(dependencyType);
                    }
                }
            }

            return services;
        }
    }
}
