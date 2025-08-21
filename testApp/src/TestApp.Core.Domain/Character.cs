using Common.Infrastructure.Persistence;

namespace TestApp.Core.Domain;

public class Character : DataModel
{
    public Character(Guid id, string name)
        : base(id)
    {
        Name = name;
    }

    public string Name { get; set; }
}
