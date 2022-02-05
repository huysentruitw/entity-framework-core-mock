/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace EntityFrameworkCoreMock
{
    public class DbContextMock<TDbContext> : Mock<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IKeyFactoryBuilder _keyFactoryBuilder;
        private readonly Dictionary<Type, IDbSetMock> _dbSetCache = new Dictionary<Type, IDbSetMock>();

        public DbContextMock(params object[] args)
            : this(new CompositeKeyFactoryBuilder(), args)
        {
        }

        private DbContextMock(IKeyFactoryBuilder keyFactoryBuilder, params object[] args)
            : base(args)
        {
            _keyFactoryBuilder = keyFactoryBuilder ?? throw new ArgumentNullException(nameof(keyFactoryBuilder));
            Reset();
        }

        public Func<TEntity, KeyContext, object> GetDefaultEntityKeyFactory<TEntity>() =>
            _keyFactoryBuilder.BuildKeyFactory<TEntity>();

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IEnumerable<TEntity> initialEntities = null, Expression<Func<TEntity, bool>> globalQueryFilter = null)
            where TEntity : class
            => CreateDbSetMock(dbSetSelector, GetDefaultEntityKeyFactory<TEntity>(), initialEntities, globalQueryFilter);

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, Func<TEntity, KeyContext, object> entityKeyFactory, IEnumerable<TEntity> initialEntities = null, Expression<Func<TEntity, bool>> globalQueryFilter = null)
            where TEntity : class
        {
            if (entityKeyFactory == null) throw new ArgumentNullException(nameof(entityKeyFactory));

            var store = globalQueryFilter == null
                ? new DbSetBackingStore<TEntity>(initialEntities, entityKeyFactory)
                : new DbSetWithGlobalFilterBackingStore<TEntity>(initialEntities, entityKeyFactory, globalQueryFilter);
            return CreateDbSetMock(dbSetSelector, store);
        }

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, DbSetBackingStore<TEntity> store)
            where TEntity : class
        {
            if (dbSetSelector == null) throw new ArgumentNullException(nameof(dbSetSelector));

            var entityType = typeof(TEntity);
            if (_dbSetCache.ContainsKey(entityType)) throw new ArgumentException($"DbSetMock for entity {entityType.Name} already created", nameof(dbSetSelector));
            var mock = new DbSetMock<TEntity>(store);
            Setup(dbSetSelector).Returns(() => mock.Object);
            Setup(x => x.Set<TEntity>()).Returns(() => mock.Object);
            Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback<TEntity>(entity => Object.Set<TEntity>().Add(entity));
            Setup(x => x.Remove(It.IsAny<TEntity>()))
                .Callback<TEntity>(entity => Object.Set<TEntity>().Remove(entity));
            _dbSetCache.Add(entityType, mock);
            return mock;
        }

        public void Reset()
        {
            MockExtensions.Reset(this);
            _dbSetCache.Clear();
            Setup(x => x.SaveChanges()).Returns(SaveChanges);
            Setup(x => x.SaveChangesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(SaveChanges);
            Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SaveChanges);

            var mockDbFacade = new Mock<DatabaseFacade>(Object);
            var mockTransaction = new Mock<IDbContextTransaction>();
            mockDbFacade.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);
            mockDbFacade.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(mockTransaction.Object));
            Setup(x => x.Database).Returns(mockDbFacade.Object);
        }

        // Facilitates unit-testing
        internal void RegisterDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IDbSetMock dbSet)
            where TEntity : class
        {
            var entityType = typeof(TEntity);
            _dbSetCache.Add(entityType, dbSet);
        }

        private int SaveChanges() => _dbSetCache.Values.Aggregate(0, (seed, dbSet) => seed + dbSet.SaveChanges());
    }
}
