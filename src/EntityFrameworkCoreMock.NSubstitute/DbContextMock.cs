/*
 * Copyright 2017-2019 Paul Michaels, Wouter Huysentruit
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EntityFrameworkCoreMock.NSubstitute
{
    public class DbContextMock<TDbContext> where TDbContext : DbContext
    {
        private readonly IKeyFactoryBuilder _keyFactoryBuilder;
        private readonly Dictionary<MemberInfo, IDbSetMock> _dbSetCache = new Dictionary<MemberInfo, IDbSetMock>();
        private readonly Dictionary<MemberInfo, IDbQueryMock> _dbQueryCache = new Dictionary<MemberInfo, IDbQueryMock>();

        public TDbContext Object { get; set; }

        public DbContextMock(params object[] args)
            : this(new AttributeBasedKeyFactoryBuilder<KeyAttribute>(), args)
        {
        }

        private DbContextMock(IKeyFactoryBuilder keyFactoryBuilder, params object[] args)
        {
            Object = Substitute.For<TDbContext>(args);
            _keyFactoryBuilder = keyFactoryBuilder ?? throw new ArgumentNullException(nameof(keyFactoryBuilder));
            Reset();
        }

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IEnumerable<TEntity> initialEntities = null)
            where TEntity : class
            => CreateDbSetMock(dbSetSelector, _keyFactoryBuilder.BuildKeyFactory<TEntity>(), initialEntities);

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(
            Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector,
            Func<TEntity, KeyContext, object> entityKeyFactory,
            IEnumerable<TEntity> initialEntities = null)
            where TEntity : class
        {
            if (dbSetSelector == null) throw new ArgumentNullException(nameof(dbSetSelector));
            if (entityKeyFactory == null) throw new ArgumentNullException(nameof(entityKeyFactory));

            var memberInfo = ((MemberExpression)dbSetSelector.Body).Member;
            if (_dbSetCache.ContainsKey(memberInfo)) throw new ArgumentException($"DbSetMock for {memberInfo.Name} already created", nameof(dbSetSelector));
            var mock = new DbSetMock<TEntity>(initialEntities, entityKeyFactory);
            Object.Set<TEntity>().Returns(mock.Object);

            dbSetSelector.Compile()(Object).Returns(mock.Object);

            _dbSetCache.Add(memberInfo, mock);
            return mock;
        }

        public DbQueryMock<TEntity> CreateDbQueryMock<TEntity>(Expression<Func<TDbContext, DbQuery<TEntity>>> dbQuerySelector, IEnumerable<TEntity> entities = null)
            where TEntity : class
        {
            if (dbQuerySelector == null) throw new ArgumentNullException(nameof(dbQuerySelector));

            var memberInfo = ((MemberExpression)dbQuerySelector.Body).Member;
            if (_dbQueryCache.ContainsKey(memberInfo)) throw new ArgumentException($"DbQueryMock for {memberInfo.Name} already created", nameof(dbQuerySelector));
            var mock = new DbQueryMock<TEntity>(entities);
            Object.Query<TEntity>().Returns(mock.Object);

            dbQuerySelector.Compile()(Object).Returns(mock.Object);

            _dbQueryCache.Add(memberInfo, mock);
            return mock;
        }

        public void Reset()
        {
            _dbSetCache.Clear();
            _dbQueryCache.Clear();
            Object.ClearReceivedCalls();
            Object.SaveChanges().Returns(_ => SaveChanges());
            Object.SaveChanges(Arg.Any<bool>()).Returns(_ => SaveChanges());
            Object.SaveChangesAsync().Returns(_ => SaveChanges());
            Object.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ => SaveChanges());
        }

        // Facilitates unit-testing
        internal void RegisterDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IDbSetMock dbSet)
            where TEntity : class
        {
            var memberInfo = ((MemberExpression)dbSetSelector.Body).Member;
            _dbSetCache.Add(memberInfo, dbSet);
        }

        // Facilitates unit-testing
        internal void RegisterDbQueryMock<TEntity>(Expression<Func<TDbContext, DbQuery<TEntity>>> dbQuerySelector, IDbQueryMock dbQuery)
            where TEntity : class
        {
            var memberInfo = ((MemberExpression)dbQuerySelector.Body).Member;
            _dbQueryCache.Add(memberInfo, dbQuery);
        }

        private int SaveChanges() => _dbSetCache.Values.Aggregate(0, (seed, dbSet) => seed + dbSet.SaveChanges());
    }
}
