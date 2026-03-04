namespace Common.LanguageExtensions.Contracts;

public interface IDataModel : IDataModel<Guid> { }

public interface IDataModel<TKey>
{
    public TKey Id { get; }
}