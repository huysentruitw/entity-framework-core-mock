using NSubstitute;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace EntityFrameworkMock.NSubstitute
{
    public class DbContextMock<TDbContext> where TDbContext : DbContext
    {
        private readonly IKeyFactoryBuilder _keyFactoryBuilder;
        private readonly Dictionary<MemberInfo, IDbSetMock> _dbSetCache = new Dictionary<MemberInfo, IDbSetMock>();
        public TDbContext DbContextObject { get; set; }

        public DbContextMock(params object[] args)
            : this(new AttributeBasedKeyFactoryBuilder<KeyAttribute>(), args)
        {

        }

        private DbContextMock(IKeyFactoryBuilder keyFactoryBuilder, params object[] args)
        {
            DbContextObject = Substitute.ForPartsOf<TDbContext>(args);
            _keyFactoryBuilder = keyFactoryBuilder ?? throw new ArgumentNullException(nameof(keyFactoryBuilder));
            Reset();
        }

        public DbSetMock<TEntity> CreateDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IEnumerable<TEntity> initialEntities = null)
            where TEntity : class
            => CreateDbSetMock(
                dbSetSelector, 
                _keyFactoryBuilder.BuildKeyFactory<TEntity>(),                 
                initialEntities);

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
            DbSetMock<TEntity> mock = new DbSetMock<TEntity>(initialEntities, entityKeyFactory);
            DbContextObject.Set<TEntity>().Returns(mock.DbSet);

            var propertyInfo = Helpers.ReflectionHelper.GetPropertyInfo(DbContextObject, dbSetSelector);
            propertyInfo.GetValue(DbContextObject).Returns(mock.DbSet);

            _dbSetCache.Add(memberInfo, mock);
            return mock;
        }

        public void Reset()
        {
            _dbSetCache.Clear();
            DbContextObject.SaveChanges().Returns(a => SaveChanges());
            DbContextObject.SaveChangesAsync().Returns(a => SaveChanges());
            DbContextObject.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(a => SaveChanges());
        }
        
        public void RegisterDbSetMock<TEntity>(Expression<Func<TDbContext, DbSet<TEntity>>> dbSetSelector, IDbSetMock dbSet)
            where TEntity : class
        {
            var memberInfo = ((MemberExpression)dbSetSelector.Body).Member;
            _dbSetCache.Add(memberInfo, dbSet);
        }

        private int SaveChanges() => _dbSetCache.Values.Aggregate(0, (seed, dbSet) => seed + dbSet.SaveChanges());
    }

}