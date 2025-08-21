using Common.LanguageExtensions.TestableAlternatives;
using TestApp.Core.Domain;

namespace TestApp.Tests;

public static class DataModels
{
    public static Character CreateCharacter(Guid? id = null, string? name = null)
    {
        return new Character(id ?? GuidProvider.NewGuid(), name ?? "james bond");
    }
}
