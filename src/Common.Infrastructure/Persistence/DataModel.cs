using CSharpFunctionalExtensions;

namespace Common.Infrastructure.Persistence;

public class DataModel : Entity<Guid>, IDataModel
{
    public DataModel(Guid id)
        : base(id) { }

    public DataModel()
        : base() { }
}
