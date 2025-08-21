using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;

namespace Common.Infrastructure.Persistence.EntityFramework.ValueObjectConverters
{
    internal static class ValueConverterLocator
    {
        public static IReadOnlyCollection<ValueConverter> GetValueConverters(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => typeof(ValueConverter).IsAssignableFrom(type))
                .Select(converterType => InstantiateValueConverter(converterType))
                .ToList();
        }

        public static ValueConverter InstantiateValueConverter(Type converterType, ConverterMappingHints? mappingHints = null)
        {
            return (Activator.CreateInstance(converterType, mappingHints) as ValueConverter)!;
        }
    }
}
