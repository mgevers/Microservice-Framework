using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Common.Infrastructure.Persistence.EntityFramework.ModelBuilding
{
    internal class EntityTypeBuilderAdapter : IBuilderAdapter
    {
        private readonly EntityTypeBuilder builder;

        public EntityTypeBuilderAdapter(EntityTypeBuilder builder)
        {
            this.builder = builder;
        }

        public PropertyBuilder Property(string propertyName)
        {
            return this.builder.Property(propertyName);
        }

        public PropertyBuilder<T> Property<T>(string propertyName)
        {
            return this.builder.Property<T>(propertyName); 
        }

        public OwnedNavigationBuilder OwnsOne(Type ownedType, string navigation)
        {
            return this.builder.OwnsOne(ownedType, navigation);
        }

        public OwnedNavigationBuilder OwnsMany(Type ownedType, string navigation)
        {
            return this.builder.OwnsMany(ownedType, navigation);
        }
    }
}
