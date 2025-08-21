namespace Common.Infrastructure.Persistence;

public interface IDataModel : IDataModel<Guid> { }

public interface IDataModel<TKey>
{
    public TKey Id { get; }
}