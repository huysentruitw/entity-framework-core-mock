/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace EntityFrameworkCoreMock
{
    public sealed class DbSetBackingStore<TEntity>
        where TEntity : class
    {
        private readonly KeyFactoryNormalizer<TEntity> _keyFactoryNormalizer;
        private readonly Dictionary<object, TEntity> _entities = new Dictionary<object, TEntity>();
        private readonly Dictionary<object, TEntity> _snapshot = new Dictionary<object, TEntity>();
        private List<DbSetChange> _changes = new List<DbSetChange>();
        private readonly KeyContext _keyContext = new KeyContext();

        public DbSetBackingStore(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory)
        {
            _keyFactoryNormalizer = new KeyFactoryNormalizer<TEntity>(keyFactory ?? throw new ArgumentNullException(nameof(keyFactory)));
            initialEntities?.ToList().ForEach(x => _entities.Add(_keyFactoryNormalizer.GenerateKey(x, _keyContext), Clone(x)));
        }

        public IQueryable<TEntity> GetDataAsQueryable() => _entities.Values.AsQueryable();

        /// <summary>
        /// Registers the addition of a new entity.
        /// </summary>
        /// <param name="entity">The new entity.</param>
        public void Add(TEntity entity) => _changes.Add(DbSetChange.Add(entity));

        /// <summary>
        /// Registers the addition of one or more entities.
        /// </summary>
        /// <param name="entities">The list of entities.</param>
        public void Add(IEnumerable<TEntity> entities) => _changes.AddRange(DbSetChange.Add(entities));

        /// <summary>
        /// Find an entity by its key.
        /// </summary>
        /// <param name="keyValues">The key.</param>
        /// <returns>The entity or null in case no entity with a matching key was found.</returns>
        public TEntity Find(object[] keyValues)
        {
            var tupleType = Type.GetType($"System.Tuple`{keyValues.Length}");
            if (tupleType == null) throw new InvalidOperationException($"No tuple type found for {keyValues.Length} generic arguments");

            var keyTypes = keyValues.Select(x => x.GetType()).ToArray();
            var constructor = tupleType.MakeGenericType(keyTypes).GetConstructor(keyTypes);
            if (constructor == null) throw new InvalidOperationException("No tuple constructor found for key values");

            var key = constructor.Invoke(keyValues);
            return _entities.TryGetValue(key, out var entity) ? entity : null;
        }

        /// <summary>
        /// Registers the update of an entity.
        /// </summary>
        /// <param name="entity"></param>
        public void Update(TEntity entity) => _changes.Add(DbSetChange.Update(entity));

        /// <summary>
        /// Registers the update of one or more entities.
        /// </summary>
        /// <param name="entities"></param>
        public void Update(IEnumerable<TEntity> entities) => _changes.AddRange(DbSetChange.Update(entities));

        /// <summary>
        /// Registers the removal of an entity.
        /// </summary>
        /// <param name="entity">The removed entity.</param>
        public void Remove(TEntity entity) => _changes.Add(DbSetChange.Remove(entity));

        /// <summary>
        /// Registers the removal of one or more entities.
        /// </summary>
        /// <param name="entities">The list of removed entities.</param>
        public void Remove(IEnumerable<TEntity> entities) => _changes.AddRange(DbSetChange.Remove(entities));

        /// <summary>
        /// Applies the registered changes to the collection of entities.
        /// </summary>
        /// <returns>The number of changes that got applied.</returns>
        public int ApplyChanges()
        {
            var changes = Interlocked.Exchange(ref _changes, new List<DbSetChange>());
            foreach (var change in changes)
            {
                if (change.IsAdd) AddEntity(change.Entity);
                else if (change.IsUpdate) UpdateEntity(change.Entity);
                else if (change.IsRemove) RemoveEntity(change.Entity);
            }

            return _changes.Count;
        }

        /// <summary>
        /// Updates the snapshot of the entities that is used to detect updated properties.
        /// </summary>
        public void UpdateSnapshot()
        {
            _snapshot.Clear();
            foreach (var kvp in _entities)
                _snapshot.Add(kvp.Key, Clone(kvp.Value));
        }

        /// <summary>
        /// Gets a list of entities that have one or more properties updated (as compared to the last snapshot).
        /// </summary>
        /// <returns>The list of updated entities.</returns>
        public UpdatedEntityInfo<TEntity>[] GetUpdatedEntities()
        {
            return _entities
                .Join(
                    _snapshot,
                    entity => entity.Key,
                    snapshot => snapshot.Key,
                    (entity, snapshot) =>
                        new UpdatedEntityInfo<TEntity>
                        {
                            Entity = entity.Value,
                            UpdatedProperties = Diff(snapshot.Value, entity.Value)
                        }
                    )
                .Where(x => x.UpdatedProperties.Any())
                .ToArray();
        }

        private void AddEntity(TEntity entity)
        {
            var key = _keyFactoryNormalizer.GenerateKey(entity, _keyContext);
            if (_entities.ContainsKey(key)) ThrowDbUpdateException();
            _entities.Add(key, entity);
        }

        private void UpdateEntity(TEntity entity)
        {
            var key = _keyFactoryNormalizer.GenerateKey(entity, _keyContext);
            if (!_entities.ContainsKey(key)) ThrowDbUpdateException();
            _entities[key] = entity;
        }

        private void RemoveEntity(TEntity entity)
        {
            var key = _keyFactoryNormalizer.GenerateKey(entity, _keyContext);
            if (!_entities.Remove(key)) ThrowDbUpdateConcurrencyException();
        }

        private static void ThrowDbUpdateException()
        {
            // TODO implement
        }

        private static void ThrowDbUpdateConcurrencyException()
        {
            // TODO implement
        }

        private static UpdatePropertyInfo[] Diff(TEntity snapshot, TEntity current)
        {
            var properties = snapshot.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.CanWrite && x.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(x => x.GetSetMethod() != null)
                .ToArray();

            return properties
                .Select(x => new UpdatePropertyInfo
                {
                    Name = x.Name,
                    Original = x.GetValue(snapshot),
                    New = x.GetValue(current)
                })
                .Where(x => !object.Equals(x.New, x.Original))
                .ToArray();
        }

        private static TEntity Clone(TEntity original) => CloneFuncCache.GetOrAdd(original.GetType(), CreateCloneFunc)(original);
        private static readonly ConcurrentDictionary<Type, Func<TEntity, TEntity>> CloneFuncCache = new ConcurrentDictionary<Type, Func<TEntity, TEntity>>();
        private static Func<TEntity, TEntity> CreateCloneFunc(Type entityType)
        {
            var properties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.CanWrite && x.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(x => x.GetSetMethod(nonPublic: true) != null)
                .ToArray();

            var original = Expression.Parameter(typeof(TEntity), "original");
            var clone = Expression.Variable(entityType, "clone");
            var newClone = Expression.New(entityType);
            var cloneBlock = Expression.Block(
                new[] { clone },
                Expression.Assign(clone, newClone),
                Expression.Block(
                    properties.Select(propertyInfo =>
                    {
                        var getter = Expression.Property(Expression.Convert(original, entityType), propertyInfo);
                        var setter = propertyInfo.GetSetMethod(true);
                        return Expression.Call(clone, setter, getter);
                    })
                ),
                clone);

            return Expression.Lambda<Func<TEntity, TEntity>>(cloneBlock, original).Compile();
        }

        private class DbSetChange
        {
            public bool IsAdd { get; private set; }

            public bool IsUpdate { get; private set; }

            public bool IsRemove { get; private set; }

            public TEntity Entity { get; private set; }

            public static DbSetChange Add(TEntity entity) => new DbSetChange { IsAdd = true, Entity = entity };

            public static IEnumerable<DbSetChange> Add(IEnumerable<TEntity> entities) => entities.Select(DbSetChange.Add);

            public static DbSetChange Update(TEntity entity) => new DbSetChange { IsUpdate = true, Entity = entity };

            public static IEnumerable<DbSetChange> Update(IEnumerable<TEntity> entities) => entities.Select(DbSetChange.Update);

            public static DbSetChange Remove(TEntity entity) => new DbSetChange { IsRemove = true, Entity = entity };

            public static IEnumerable<DbSetChange> Remove(IEnumerable<TEntity> entities) => entities.Select(DbSetChange.Remove);
        }
    }
}
