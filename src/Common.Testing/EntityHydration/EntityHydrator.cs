using Newtonsoft.Json;
using System.Reflection;

namespace Common.Testing.EntityHydration;

public static class EntityHydrator
{
    public static T Hydrate<T>(object obj)
    {
        var entityWritableProperties = typeof(T).GetProperties()
            .Where(property => property.CanWrite)
            .ToList();

        var objectProperties = obj.GetType()
            .GetProperties()
            .ToList();

        foreach (var property in entityWritableProperties)
        {
            if (objectProperties.Any(prop => IsMatchingProperty(prop, property)) == false)
            {
                throw new InvalidOperationException($"property {property.Name} not defined");
            }
        }

        foreach (var property in objectProperties)
        {
            if (entityWritableProperties.Any(prop => IsMatchingProperty(prop, property)) == false)
            {
                throw new InvalidOperationException($"property {property.Name} does not exist on {typeof(T).Name}");
            }
        }

        var json = JsonConvert.SerializeObject(obj);
        var entity = JsonConvert.DeserializeObject<T>(json);

        return entity!;
    }

    private static bool IsMatchingProperty(PropertyInfo expected, PropertyInfo actual)
    {
        return expected.Name == actual.Name
            && expected.PropertyType == actual.PropertyType;
    }
}
