using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;

namespace Common.Infrastructure.Persistence.EntityFramework.ValueObjectConverters
{
    public class ValueObjectValueConverterSelector : ValueConverterSelector
    {
        private readonly IReadOnlyDictionary<Type, ValueConverterInfo> converters;

        public ValueObjectValueConverterSelector(ValueConverterSelectorDependencies dependencies, Assembly assembly)
            : base(dependencies)
        {
            this.converters = ValueConverterLocator.GetValueConverters(assembly)
                .ToDictionary(
                    converter => converter.ModelClrType,
                    converter => GetValueConverterInfo(converter));
        }

        public override IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type? providerClrType = null)
        {
            Type actualModelClrType = UnwrapNullableType(modelClrType);

            IReadOnlyCollection<ValueConverterInfo> valueObjectConverterInfos = this.converters.ContainsKey(actualModelClrType)
                ? new[] { this.converters[actualModelClrType] }
                : Array.Empty<ValueConverterInfo>();

            return base.Select(modelClrType, providerClrType).Concat(valueObjectConverterInfos);
        }

        private static ValueConverterInfo GetValueConverterInfo(ValueConverter converter)
        {
            return new ValueConverterInfo(
                modelClrType: converter.ModelClrType,
                providerClrType: converter.ProviderClrType,
                factory: info => ValueConverterLocator.InstantiateValueConverter(converter.GetType(), info.MappingHints));
        }

        private static Type UnwrapNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }
    }
}
