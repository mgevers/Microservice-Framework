using System.Reflection;

namespace Common.Infrastructure.DataModeling
{
    public static class EntityHydrator
    {
        private static BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        public static T Hydrate<T>(object obj)
        {
            var constructor = typeof(T).GetConstructor(bindingFlags, Array.Empty<Type>())!;
            var entity = (T)constructor.Invoke(Array.Empty<object>());
            var allProps = GetBaseProps(typeof(T));
            var properties = allProps
                .Where(prop => prop.SetMethod != null)
                .ToList();

            foreach (var property in properties)
            {
                var inputProperty = obj.GetType().GetProperty(property.Name, bindingFlags);
                var propertyValue = inputProperty?.GetValue(obj);
                property.SetValue(entity, propertyValue);
            }

            return entity;
        }

        private static IReadOnlyList<PropertyInfo> GetBaseProps(Type? type)
        {
            if (type == null)
            {
                return Array.Empty<PropertyInfo>();
            }

            return type.GetProperties(bindingFlags)
                .Concat(GetBaseProps(type.BaseType))
                .DistinctBy(prop => prop.Name)
                .ToList();
        }
    }
}
