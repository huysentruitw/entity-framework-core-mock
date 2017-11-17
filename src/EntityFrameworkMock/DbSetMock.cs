/*
 * Copyright 2017 Wouter Huysentruit
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Threading;
using EntityFrameworkMock.Internal;
using Moq;

namespace EntityFrameworkMock
{
    public sealed class DbSetMock<TEntity> : Mock<DbSet<TEntity>>, IDbSetMock
        where TEntity : class
    {
        private readonly Func<TEntity, object> _keyFactory;
        private readonly Dictionary<object, TEntity> _entities = new Dictionary<object, TEntity>();
        private readonly Dictionary<object, TEntity> _snapshot = new Dictionary<object, TEntity>();
        private List<DbSetOperation> _operations = new List<DbSetOperation>();

        public DbSetMock(IEnumerable<TEntity> initialEntities, Func<TEntity, object> keyFactory, bool asyncQuerySupport = true)
        {
            _keyFactory = keyFactory;
            initialEntities?.ToList().ForEach(x => _entities.Add(_keyFactory(x), Clone(x)));

            var data = _entities.Values.AsQueryable();
            As<IQueryable<TEntity>>().Setup(x => x.Provider).Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            As<IQueryable<TEntity>>().Setup(x => x.Expression).Returns(data.Expression);
            As<IQueryable<TEntity>>().Setup(x => x.ElementType).Returns(data.ElementType);
            As<IQueryable<TEntity>>().Setup(x => x.GetEnumerator()).Returns(_entities.Values.GetEnumerator());
            if (asyncQuerySupport) As<IDbAsyncEnumerable<TEntity>>().Setup(x => x.GetAsyncEnumerator()).Returns(new DbAsyncEnumerator<TEntity>(_entities.Values.GetEnumerator()));
            Setup(x => x.AsNoTracking()).Returns(() => Object);

            Setup(x => x.Add(It.IsAny<TEntity>())).Callback<TEntity>(x => _operations.Add(DbSetOperation.Add(x)));
            Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(x => _operations.AddRange(DbSetOperation.Add(x)));
            Setup(x => x.Remove(It.IsAny<TEntity>())).Callback<TEntity>(x => _operations.Add(DbSetOperation.Remove(x)));
            Setup(x => x.RemoveRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(x => _operations.AddRange(DbSetOperation.Remove(x)));

            UpdateSnapshot();
        }

        public event EventHandler<SavedChangesEventArgs<TEntity>> SavedChanges;

        int IDbSetMock.SaveChanges()
        {
            var operations = Interlocked.Exchange(ref _operations, new List<DbSetOperation>());
            foreach (var operation in operations)
            {
                if (operation.IsAdd) AddEntity(operation.Entity);
                else if (operation.IsRemove) RemoveEntity(operation.Entity);
            }
            SavedChanges?.Invoke(this, new SavedChangesEventArgs<TEntity> { UpdatedEntities = GetUpdatedEntities() });
            UpdateSnapshot();
            return operations.Count;
        }

        private void AddEntity(TEntity entity)
        {
            var key = _keyFactory(entity);
            if (_entities.ContainsKey(key)) throw new DbUpdateException();
            _entities.Add(key, entity);
        }

        private void RemoveEntity(TEntity entity)
        {
            var key = _keyFactory(entity);
            _entities.Remove(key);
        }

        private UpdatedEntityInfo<TEntity>[] GetUpdatedEntities()
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

        private void UpdateSnapshot()
        {
            _snapshot.Clear();
            foreach (var kvp in _entities)
                _snapshot.Add(kvp.Key, Clone(kvp.Value));
        }

        private static UpdatePropertyInfo[] Diff(TEntity snapshot, TEntity current)
        {
            var type = snapshot.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.CanWrite)
                .Where(x => x.GetCustomAttribute<NotMappedAttribute>() == null)
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

        private static TEntity Clone(TEntity original)
        {
            var type = original.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.CanWrite)
                .Where(x => x.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToArray();

            return properties.Aggregate((TEntity)Activator.CreateInstance(type), (clone, pi) =>
            {
                pi.SetValue(clone, pi.GetValue(original));
                return clone;
            });
        }

        private class DbSetOperation
        {
            public bool IsAdd { get; private set; }

            public bool IsRemove { get; private set; }

            public TEntity Entity { get; private set; }

            public static DbSetOperation Add(TEntity entity) => new DbSetOperation { IsAdd = true, Entity = entity };

            public static IEnumerable<DbSetOperation> Add(IEnumerable<TEntity> entities) => entities.Select(DbSetOperation.Add);

            public static DbSetOperation Remove(TEntity entity) => new DbSetOperation { IsRemove = true, Entity = entity };

            public static IEnumerable<DbSetOperation> Remove(IEnumerable<TEntity> entities) => entities.Select(DbSetOperation.Remove);
        }
    }
}
