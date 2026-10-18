namespace Common.Testing.Assert
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class BlacklistPropertiesContractResolver<T> : DefaultContractResolver
    {
        private readonly IReadOnlyCollection<string> blackListPropertyNames;
        private readonly bool includeNonPublicProperties;

        public BlacklistPropertiesContractResolver(
            IReadOnlyCollection<Expression<Func<T, object>>> blacklistProperties,
            bool includeNonPublicProperties = false)
        {
            this.blackListPropertyNames = GetPropertyNames(blacklistProperties);
            this.includeNonPublicProperties = includeNonPublicProperties;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = this.includeNonPublicProperties
                ? this.CreatePropertiesIncludingNonPublicProperties(type, memberSerialization)
                : base.CreateProperties(type, memberSerialization);

            foreach (JsonProperty property in properties) {
                property.Readable = true;
            }

            if (type == typeof(T)) {
                return properties
                    .Where(property => this.blackListPropertyNames.Contains(property.PropertyName) == false)
                    .ToList();
            }
            else {
                return properties;
            }
        }

        private static IReadOnlyCollection<string> GetPropertyNames(IReadOnlyCollection<Expression<Func<T, object>>> expressions)
        {
            return expressions
                .Select(expression => GetMemberName(expression))
                .ToList();
        }

        private static string GetMemberName(Expression<Func<T, object>> expression)
        {
            Expression member = expression.Body is MemberExpression ? expression.Body : (expression.Body as UnaryExpression)!.Operand;
            return (member as MemberExpression)!.Member.Name;
        }

        private List<JsonProperty> CreatePropertiesIncludingNonPublicProperties(Type type, MemberSerialization memberSerialization)
        {
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(property => this.CreateProperty(property, memberSerialization))
                .ToList();
        }
    }
}
