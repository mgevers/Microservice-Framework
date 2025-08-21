using Common.LanguageExtensions.TestableAlternatives;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Common.Infrastructure.Persistence.EntityFramework.ModelBuilding.ValueGenerators;

public class DateTimeValueGenerator : ValueGenerator<DateTime>
{
    public override bool GeneratesTemporaryValues => false;

    public override DateTime Next(EntityEntry entry)
    {
        return CurrentTime.UtcNow;
    }
}
