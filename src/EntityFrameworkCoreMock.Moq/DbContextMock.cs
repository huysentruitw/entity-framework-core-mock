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
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace EntityFrameworkCoreMock
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    public class DbContextMock<TDbContext> : Mock<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IKeyFactoryBuilder _keyFactoryBuilder;
        private readonly Dictionary<MemberInfo, IDbSetMock> _dbSetCache = new Dictionary<MemberInfo, IDbSetMock>();
        private readonly Dictionary<MemberInfo, IDbQueryMock> _dbQueryCache = new Dictionary<MemberInfo, IDbQueryMock>();

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

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IEnumerable<TEntity> initialEntities = null)
            where TEntity : class
            => CreateDbSetMock(dbSetSelector, _keyFactoryBuilder.BuildKeyFactory<TEntity>(), initialEntities);

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, Func<TEntity, KeyContext, object> entityKeyFactory, IEnumerable<TEntity> initialEntities = null)
            where TEntity : class
        {
            if (dbSetSelector == null) throw new ArgumentNullException(nameof(dbSetSelector));
            if (entityKeyFactory == null) throw new ArgumentNullException(nameof(entityKeyFactory));

            var memberInfo = ((MemberExpression)dbSetSelector.Body).Member;
            if (_dbSetCache.ContainsKey(memberInfo)) throw new ArgumentException($"DbSetMock for {memberInfo.Name} already created", nameof(dbSetSelector));
            var mock = new DbSetMock<TEntity>(initialEntities, entityKeyFactory);
            mock.Setup(x => x.AsQueryable()).Returns(() => mock.Object);
            Setup(dbSetSelector).Returns(() => mock.Object);
            Setup(x => x.Set<TEntity>()).Returns(() => mock.Object);
            _dbSetCache.Add(memberInfo, mock);
            return mock;
        }

        public void Reset()
        {
            MockExtensions.Reset(this);
            _dbSetCache.Clear();
            _dbQueryCache.Clear();
            Setup(x => x.SaveChanges()).Returns(SaveChanges);
            Setup(x => x.SaveChangesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(SaveChanges);
            Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SaveChanges);

            // MockEntryCall<It.IsAnyType>(); //my best shot but sadly doesn't work like that
            
            var mockDbFacade = new Mock<DatabaseFacade>(Object);
            var mockTransaction = new Mock<IDbContextTransaction>();
            mockDbFacade.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);
            mockDbFacade.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(mockTransaction.Object));
            Setup(x => x.Database).Returns(mockDbFacade.Object);
        }

        // public virtual void MockEntryCall<TEntity>() where TEntity : class
        // {
        //     Setup(x => x.Entry(It.IsAny<TEntity>())).Returns<EntityEntry<TEntity>>(EntryMock);
        // }
        //
        // public virtual EntityEntry<TEntity> EntryMock<TEntity>(TEntity entity)
        //     where TEntity : class
        // {
        //     var entry = EntryWithoutDetectChangesMock(entity);
        //     // TryDetectChanges(entry);
        //     return entry;
        // }
        //
        // private EntityEntry<TEntity> EntryWithoutDetectChangesMock<TEntity>(TEntity entity)
        //     where TEntity : class
        // {
        //     var mock = new Mock<EntityEntry<TEntity>>(entity);
        //     mock.Setup(x => x.Reload()).Verifiable();
        //     mock.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Verifiable();
        //     return mock.Object;
        // }

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
