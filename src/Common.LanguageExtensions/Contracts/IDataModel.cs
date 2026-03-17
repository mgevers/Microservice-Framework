namespace Common.LanguageExtensions.Contracts;

public interface IDataModel : IDataModel<Guid> { }

public interface IDataModel<TKey> : IDataModelBase
{
    TKey Id { get; }
}

public interface IDataModelBase
{
    object GetId();
}