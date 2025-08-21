namespace Common.Testing.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DatabaseState
    {
        private readonly IReadOnlyDictionary<Type, IReadOnlyList<object>> entities;

        public DatabaseState(object entity)
            : this([entity])
        {
        }

        public DatabaseState(params object[] entities)
            : this(entities.ToList())
        {
        }

        public DatabaseState(IReadOnlyCollection<object> entities)
        {
            this.entities = entities
                .GroupBy(entity => entity.GetType())
                .ToDictionary(
                    keySelector: entityGroup => entityGroup.Key,
                    elementSelector: entityGroup => entityGroup.ToList().AsReadOnly() as IReadOnlyList<object>);
        }

        public IReadOnlyList<object> GetEntities(Type entityType)
        {
            return this.entities.ContainsKey(entityType) ? this.entities[entityType] : Array.Empty<object>();
        }

        public IReadOnlyList<Type> GetEntityTypes()
        {
            return this.entities.Keys.ToList();
        }

        public IReadOnlyList<object> GetAllEntities()
        {
            return this.entities.Values.SelectMany(entities => entities).ToList();
        }

        public static DatabaseState Empty => new(Array.Empty<object>().ToList());
    }
}
