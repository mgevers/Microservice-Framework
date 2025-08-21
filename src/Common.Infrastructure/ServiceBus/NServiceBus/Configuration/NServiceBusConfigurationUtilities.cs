using System.Reflection;

namespace Common.Infrastructure.ServiceBus.NServiceBus.Configuration
{
    public static class NServiceBusConfigurationUtilities
    {
        public static IReadOnlyCollection<Type> GetMessagehandlerTypesFromAssembly(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => type.GetInterfaces().Any(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IHandleMessages<>)))
                .ToList();
        }
    }
}
