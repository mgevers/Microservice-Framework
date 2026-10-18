namespace Common.Infrastructure.Persistence.Cosmos;

public interface ICosmosDataModel : IDataModel
{
    string? ETag { get; set; }
}
