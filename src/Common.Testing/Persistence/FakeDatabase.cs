using Ardalis.Result;
using Common.LanguageExtensions.Contracts;
using System.Linq.Expressions;

namespace Common.Testing.Persistence;

public sealed class FakeDatabase : IDisposable
{
    private static readonly AsyncLocal<Dictionary<Type, List<object>>?> data = new();
    private static readonly AsyncLocal<Result?> errorResult = new();

    public static Dictionary<Type, List<object>> Data => data.Value!;

    private FakeDatabase(DatabaseState databaseState, Result? error)
    {
        data.Value = new Dictionary<Type, List<object>>();
        errorResult.Value = error;

        foreach (var entityType in databaseState.GetEntityTypes())
        {
            foreach (var entity in databaseState.GetEntities(entityType))
            {
                if (!Data.TryGetValue(entityType, out List<object>? value))
                {
                    value = new List<object>();
                    Data.Add(entityType, value);
                }

                value.Add(entity);
            }
        }
    }

    public static FakeDatabase SeedData(DatabaseState state, Result? error)
    {
        return new FakeDatabase(state, error);
    }

    public static DatabaseState DatabaseState => new(Data.Values.SelectMany(e => e).ToList());

    public static Result<TEntity> InsertEntity<TEntity>(TEntity entity)
        where TEntity : IDataModelBase
    {
        if (errorResult.Value is not null)
        {
            return errorResult.Value;
        }

        var entities = GetEntityData(entity.GetType());
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).GetId().Equals(entity.GetId()));

        if (existingEntity != null)
        {
            return Result<TEntity>.Conflict($"conflict - entity with id {entity.GetId()} already exists");
        }

        entities.Add(entity);
        return Result.Success(entity);
    }

    public static Result<TEntity> UpdateEntity<TEntity>(TEntity entity)
        where TEntity : IDataModelBase
    {
        if (errorResult.Value is not null)
        {
            return errorResult.Value;
        }

        var entities = GetEntityData(entity.GetType());
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).GetId().Equals(entity.GetId()));

        if (existingEntity == null)
        {
            return Result<TEntity>.Conflict("cannot update entity - not found");
        }

        entities.Remove(existingEntity);
        entities.Add(entity);

        return Result.Success(entity);
    }

    public static Result<TEntity> UpsertEntity<TEntity>(TEntity entity)
        where TEntity : IDataModelBase
    {
        if (errorResult.Value is not null)
        {
            return errorResult.Value;
        }

        var entities = GetEntityData(entity.GetType());
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).GetId().Equals(entity.GetId()));

        if (existingEntity != null)
        {
            entities.Remove(existingEntity);
        }

        entities.Add(entity);
        return Result.Success(entity);
    }

    public static IReadOnlyList<TEntity> Query<TEntity>(Expression<Func<TEntity, bool>>? queryFunc = null)
    {
        var allEntities = GetEntityData(typeof(TEntity))
            .Cast<TEntity>()
            .ToList();

        if (queryFunc != null)
        {
            var matchingEntities = allEntities
                .Where(queryFunc.Compile())
                .ToList();

            return matchingEntities;
        }
        else
        {
            return allEntities;
        }
    }

    public static Result DeleteEntity<TEntity>(TEntity entity)
        where TEntity : IDataModelBase
    {
        if (errorResult.Value is not null)
        {
            return errorResult.Value;
        }

        var entities = GetEntityData(entity.GetType());
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).GetId().Equals(entity.GetId()));

        if (existingEntity == null)
        {
            return Result.NotFound("entity not found");
        }

        entities.Remove(existingEntity);
        return Result.Success();
    }

    public void Dispose()
    {
        data.Value = null;
        errorResult.Value = null;
    }

    private static List<object> GetEntityData(Type entityType)
    {
        if (Data.ContainsKey(entityType) == false)
        {
            Data.Add(entityType, new List<object>());
        }

        return Data[entityType];
    }
}
    