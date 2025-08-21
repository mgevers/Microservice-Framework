namespace Common.Testing.Assert
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public static class DeepEqualSerializer
    {
        public static string SerializeObject<T>(
            T value,
            IReadOnlyCollection<Expression<Func<T, object>>>? blacklistProperties,
            bool includeNonPublicProperties = false)
        {
            blacklistProperties ??= Array.Empty<Expression<Func<T, object>>>();

            IContractResolver contractResolver = new BlacklistPropertiesContractResolver<T>(blacklistProperties, includeNonPublicProperties);

            var jsonSerializerSettings = new JsonSerializerSettings() {
                ContractResolver = contractResolver,
            };

            string rawJson = JsonConvert.SerializeObject(value: value, settings: jsonSerializerSettings);
            return JsonConvert.SerializeObject(NormalizeJToken(JToken.Parse(rawJson)));
        }

        private static JToken NormalizeJToken(JToken jToken)
        {
            if (jToken is JObject jObject) {
                var result = new JObject();

                foreach (JProperty property in jObject.Properties().OrderBy(property => property.Name)) {
                    result.Add(propertyName: property.Name, value: NormalizeJToken(property.Value));
                }

                return result;
            }
            else if (jToken is JArray jArray) {
                var result = new JArray();

                foreach (JToken item in jArray) {
                    result.Add(NormalizeJToken(item));
                }

                return result;
            }
            else {
                return jToken;
            }
        }
    }
}
