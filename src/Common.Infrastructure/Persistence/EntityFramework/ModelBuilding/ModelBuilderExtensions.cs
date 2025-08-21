using Common.Infrastructure.Persistence.EntityFramework.ModelBuilding.ValueGenerators;
using Common.LanguageExtensions.Contracts;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Common.Infrastructure.Persistence.EntityFramework.ModelBuilding
{
    public static class ModelBuilderExtensions
    {
        private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        public static ModelBuilder RegisterDomainEventsAsOwnedEntities(this ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes();

            IReadOnlyList<Type> eventSourcedTypes = entityTypes
                .Select(referenceType => referenceType.ClrType)
                .Where(type => type.GetProperty(nameof(EventSourcedEntity<DomainEvent<IEventData>,IEventData>.DomainEvents), bindingFlags) != null)
                .ToList();

            foreach (var entityType in eventSourcedTypes)
            {
                var entityBuilder = new EntityTypeBuilderAdapter(modelBuilder.Entity(entityType));
                var genericTypes = entityType.BaseType!.GenericTypeArguments;
                var eventType = genericTypes[0];

                var eventBuilder = entityBuilder
                    .OwnsMany(eventType, nameof(EventSourcedEntity<DomainEvent<IEventData>, IEventData>.DomainEvents));
                
                eventBuilder.HasKey(nameof(DomainEvent<IEventData>.AggregateId), nameof(DomainEvent<IEventData>.SequenceId));
                eventBuilder.Property<DateTime>("UtcDateRecorded")
                    .ValueGeneratedOnAdd()
                    .HasValueGenerator((property, entityType) => new DateTimeValueGenerator());
            }

            return modelBuilder;
        }

        public static ModelBuilder RegisterValueObjectsAsOwnedEntities(this ModelBuilder modelBuilder)
        {
            IReadOnlyList<Type> nonOwnedEntityTypes = modelBuilder.Model.GetEntityTypes()
                .Select(referenceType => referenceType.ClrType)
                .Where(referenceClrType => typeof(Entity<Guid>).IsAssignableFrom(referenceClrType))
                .ToList();

            foreach (var entityType in nonOwnedEntityTypes)
            {
                RegisterOwnedEntities(entityType, new EntityTypeBuilderAdapter(modelBuilder.Entity(entityType)));
            }

            return modelBuilder;
        }

        private static void RegisterOwnedEntities(Type type, IBuilderAdapter builderAdapter)
        {
            var properties = type.GetProperties(bindingFlags);

            foreach (var property in properties)
            {
                if (PropertyIsWritable(property) && PropertyIsMapped(property) && TypeIsNotPrimitive(property.PropertyType))
                {
                    var collectionType = GetCollectionType(property.PropertyType);
                    var isCollection = collectionType != null;
                    var ownedType = collectionType ?? property.PropertyType;

                    if (TypeIsNotPrimitive(ownedType))
                    {
                        OwnedNavigationBuilder ownedNavigationBuilder = isCollection
                            ? builderAdapter.OwnsMany(ownedType, property.Name)
                            : builderAdapter.OwnsOne(ownedType, property.Name);

                        RegisterOwnedEntities(
                            type: ownedNavigationBuilder.OwnedEntityType.ClrType,
                            builderAdapter: new OwnedNavigationBuilderAdapter(ownedNavigationBuilder));
                    }
                }
            }
        }

        private static bool PropertyIsWritable(PropertyInfo property)
        {
            return property.DeclaringType!.GetProperty(property.Name, bindingFlags)!.CanWrite;
        }

        private static bool PropertyIsMapped(PropertyInfo property)
        {
            return property.GetCustomAttribute(typeof(NotMappedAttribute)) == null;
        }

        private static bool TypeIsNotPrimitive(Type type)
        {
            return type.IsValueType == false
                && type != typeof(string)
                && TypeIsNotSimpleValueObjectOfPrimitive(type);

        }

        private static bool TypeIsNotSimpleValueObjectOfPrimitive(Type type)
        {
            return type.BaseType == null
                || type.BaseType.IsGenericType == false
                || type.BaseType.GetGenericTypeDefinition() != typeof(SimpleValueObject<>)
                || TypeIsNotPrimitive(type.BaseType.GetGenericArguments().Single());
        }

        private static Type? GetCollectionType(Type type)
        {
            return type.GetInterfaces()
                .SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()
                .Single();
        }
    }
}
