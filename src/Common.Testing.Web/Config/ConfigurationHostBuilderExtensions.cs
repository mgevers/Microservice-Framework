using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Testing.Integration.Config;

public static class ConfigurationHostBuilderExtensions
{
    public static IWebHostBuilder AddConfigurationSettings(this IWebHostBuilder builder, object configSettings)
    {
        var flattened = FlattenObject(configSettings);
        foreach (var (key, value) in flattened)
        {
            builder.UseSetting(key, value);
        }

        return builder;
    }

    private static Dictionary<string, string> FlattenObject(object obj)
    {
        var result = new Dictionary<string, string>();
        var json = JsonConvert.SerializeObject(obj);
        var jObject = JObject.Parse(json);

        FlattenJToken(jObject, string.Empty, result);
        return result;
    }

    private static void FlattenJToken(JToken token, string prefix, Dictionary<string, string> result)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}:{property.Name}";
                    FlattenJToken(property.Value, key, result);
                }
                break;

            case JTokenType.Array:
                int index = 0;
                foreach (var item in token.Children())
                {
                    var key = $"{prefix}:{index}";
                    FlattenJToken(item, key, result);
                    index++;
                }
                break;

            default:
                result[prefix] = token.ToString();
                break;
        }
    }
}
