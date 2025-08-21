using Ardalis.Result;
using Common.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace Common.Testing.Persistence;

public sealed class FakeDatabase : IDisposable
{
    private static readonly AsyncLocal<Dictionary<Type, List<object>>?> data = new();
    private static readonly AsyncLocal<bool?> isReadOnlyDatabase = new();

    public static Dictionary<Type, List<object>> Data => data.Value!;
    public static bool IsReadOnly => isReadOnlyDatabase.Value!.Value;

    private FakeDatabase(DatabaseState databaseState, bool isReadOnly)
    {
        data.Value = new Dictionary<Type, List<object>>();
        isReadOnlyDatabase.Value = isReadOnly;

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

    public static FakeDatabase SeedData(DatabaseState state, bool isReadOnlyDatabase)
    {
        return new FakeDatabase(state, isReadOnlyDatabase);
    }

    public static DatabaseState DatabaseState => new(Data.Values.SelectMany(e => e).ToList());

    public static Result<TEntity> InsertEntity<TEntity>(TEntity entity)
        where TEntity : IDataModel
    {
        if (IsReadOnly)
        {
            return Result<TEntity>.CriticalError("cannot write to readonly database");
        }

        var entities = GetEntityData(typeof(TEntity));
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).Id.Equals(entity.Id));

        if (existingEntity != null)
        {
            return Result<TEntity>.Conflict($"conflict - entity with id {entity.Id} already exists");
        }

        entities.Add(entity);
        return Result.Success(entity);
    }

    public static Result<TEntity> UpdateEntity<TEntity>(TEntity entity)
        where TEntity : IDataModel
    {
        if (IsReadOnly)
        {
            return Result<TEntity>.CriticalError("cannot write to readonly database");
        }

        var entities = GetEntityData(typeof(TEntity));
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).Id.Equals(entity.Id));

        if (existingEntity == null )
        {
            return Result<TEntity>.Conflict("cannot update entity - not found");
        }

        if (existingEntity != null)
        {
            entities.Remove(existingEntity);
        }

        entities.Add(entity);
        return Result.Success(entity);
    }

    public static Result<TEntity> UpsertEntity<TEntity>(TEntity entity)
        where TEntity : IDataModel
    {
        if (IsReadOnly)
        {
            return Result<TEntity>.CriticalError("cannot write to readonly database");
        }

        var entities = GetEntityData(typeof(TEntity));
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).Id.Equals(entity.Id));

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
        where TEntity : IDataModel
    {
        if (IsReadOnly)
        {
            return Result.CriticalError("cannot write to readonly database");
        }

        var entities = GetEntityData(typeof(TEntity));
        var existingEntity = entities.SingleOrDefault(e => ((TEntity)e).Id.Equals(entity.Id));

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
    