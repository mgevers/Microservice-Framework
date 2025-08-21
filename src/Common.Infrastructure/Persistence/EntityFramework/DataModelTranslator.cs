using CSharpFunctionalExtensions;

namespace Common.Infrastructure.Persistence.EntityFramework;

public abstract class DataModelTranslator<TEntity, TDataModel>
    where TEntity : Entity<Guid>
    where TDataModel : IDataModel
{
    private IDictionary<Guid, TDataModel> dataModels = new Dictionary<Guid, TDataModel>();

    public TEntity GetEntity(TDataModel dataModel)
    {
        if (dataModels.ContainsKey(dataModel.Id))
        {
            dataModels[dataModel.Id] = dataModel;
        }
        else
        {
            dataModels.Add(dataModel.Id, dataModel);
        }

        return GetEntityFromDataModel(dataModel);
    }

    public TDataModel GetDataModel(TEntity entity)
    { 
        if (dataModels.ContainsKey(entity.Id))
        {
            return UpdateDataModelFromEntity(dataModels[entity.Id], entity);
        }
        else
        {
            var dataModel = GetDataModelFromEntity(entity);
            dataModels.Add(dataModel.Id, dataModel);
            return dataModel;
        }
    }

    protected abstract TEntity GetEntityFromDataModel(TDataModel dataModel);

    protected abstract TDataModel GetDataModelFromEntity(TEntity entity);

    protected abstract TDataModel UpdateDataModelFromEntity(TDataModel dataModel, TEntity entity);
}
