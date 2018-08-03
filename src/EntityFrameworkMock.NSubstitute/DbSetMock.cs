using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using NSubstitute;

namespace EntityFrameworkMock.NSubstitute
{
    public sealed class DbSetMock<TEntity> : IDbSetMock
        where TEntity : class
    {
        private readonly DbSetBackingStore<TEntity> _store;

        public DbSet<TEntity> DbSet { get; }

        public DbSetMock(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory, bool asyncQuerySupport = true)
        {
            _store = new DbSetBackingStore<TEntity>(initialEntities, keyFactory);

            var data = _store.GetDataAsQueryable();

            Func<DbAsyncEnumerable<TEntity>> getQuery =
                () => new DbAsyncEnumerable<TEntity>(data.AsQueryable());

            DbSet = Substitute.For<DbSet<TEntity>, IQueryable<TEntity>, IDbAsyncEnumerable<TEntity>>();

            ((IQueryable<TEntity>)DbSet).Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            DbSet.AsQueryable().Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            DbSet.AsQueryable().Expression.Returns(data.Expression);
            DbSet.AsQueryable().ElementType.Returns(data.ElementType);
            ((IQueryable<TEntity>)DbSet).GetEnumerator().Returns(a => _store.GetDataEnumerator());

            if (asyncQuerySupport)
            {
                ((IDbAsyncEnumerable<TEntity>)DbSet).GetAsyncEnumerator()
                    .Returns(a => getQuery().GetAsyncEnumerator());
            }

            DbSet.AsNoTracking().Returns(DbSet);
            DbSet.Include(Arg.Any<string>()).Returns(DbSet);
            DbSet.AsNoTracking().Include(Arg.Any<string>()).Returns(DbSet);

            DbSet.When(a => a.Add(Arg.Any<TEntity>())).Do(b => _store.Add(b.ArgAt<TEntity>(0)));
            DbSet.When(a => a.AddRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Add(b.ArgAt<IEnumerable<TEntity>>(0)));
            DbSet.When(a => a.Remove(Arg.Any<TEntity>())).Do(b => _store.Remove(b.ArgAt<TEntity>(0)));
            DbSet.When(a => a.RemoveRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Remove(b.ArgAt<IEnumerable<TEntity>>(0)));

            _store.UpdateSnapshot();
        }

        public event EventHandler<SavedChangesEventArgs<TEntity>> SavedChanges;

        int IDbSetMock.SaveChanges()
        {
            var changes = _store.ApplyChanges();
            SavedChanges?.Invoke(this, new SavedChangesEventArgs<TEntity> { UpdatedEntities = _store.GetUpdatedEntities() });
            _store.UpdateSnapshot();
            return changes;
        }
    }
}