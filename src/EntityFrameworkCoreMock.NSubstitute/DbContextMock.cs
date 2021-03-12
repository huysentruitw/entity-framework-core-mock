/*
 * Copyright 2017-2020 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Collections.Generic;
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
            : this(new CompositeKeyFactoryBuilder(), args)
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

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(IEnumerable<TEntity> initialEntities)
            where TEntity : class
        {
            var expParam = Expression.Parameter(typeof(TDbContext));
            var expBody = Expression.PropertyOrField(expParam, typeof(TEntity).Name);
            var exp = Expression.Lambda<Func<TDbContext, DbSet<TEntity>>>(expBody, expParam);
            return CreateDbSetMock(exp, initialEntities);
        }

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

        private int SaveChanges() => _dbSetCache.Values.Aggregate(0, (seed, dbSet) => seed + dbSet.SaveChanges());
    }
}
