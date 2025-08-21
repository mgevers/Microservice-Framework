using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Common.Infrastructure.Persistence.EntityFramework.ModelBuilding
{
    internal interface IBuilderAdapter
    {
        PropertyBuilder Property(string propertyName);

        OwnedNavigationBuilder OwnsOne(Type ownedType, string navigation);

        OwnedNavigationBuilder OwnsMany(Type ownedType, string navigation);
    }
}
