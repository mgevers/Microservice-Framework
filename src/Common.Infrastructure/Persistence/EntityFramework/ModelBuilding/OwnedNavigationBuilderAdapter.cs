using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Common.Infrastructure.Persistence.EntityFramework.ModelBuilding
{
    internal class OwnedNavigationBuilderAdapter : IBuilderAdapter
    {
        private readonly OwnedNavigationBuilder builder;

        public OwnedNavigationBuilderAdapter(OwnedNavigationBuilder builder)
        {
            this.builder = builder;
        }

        public PropertyBuilder Property(string propertyName)
        {
            return this.builder.Property(propertyName);
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
