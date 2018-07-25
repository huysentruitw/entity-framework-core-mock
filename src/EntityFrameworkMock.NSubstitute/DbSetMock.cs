using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace EntityFrameworkMock.NSubstitute
{
    public sealed class DbSetMock<TEntity> : IDbSetMock
        where TEntity : class
    {
        private readonly DbSetBackingStore<TEntity> _store;
        private readonly DbSet<TEntity> _dbSet;

        public DbSetMock(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory, bool asyncQuerySupport = true)
        {
            _store = new DbSetBackingStore<TEntity>(initialEntities, keyFactory);
            
            var data = _store.GetDataAsQueryable();
           
            _dbSet = Substitute.ForPartsOf<DbSet<TEntity>>();
            
            _dbSet.AsQueryable().Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            _dbSet.AsQueryable().Expression.Returns(data.Expression);
            _dbSet.AsQueryable().ElementType.Returns(data.ElementType);
            _dbSet.AsQueryable().GetEnumerator().Returns(_store.GetDataEnumerator());

            if (asyncQuerySupport)
            {
                ((IDbAsyncEnumerable<TEntity>)_dbSet).GetAsyncEnumerator().Returns(new DbAsyncEnumerator<TEntity>(_store.GetDataEnumerator()));
            }
            _dbSet.AsNoTracking().Returns(_dbSet);
            _dbSet.AsNoTracking().Include(Arg.Any<string>()).Returns(_dbSet);

            _dbSet.When(a => a.Add(Arg.Any<TEntity>())).Do(b => _store.Add(b.ArgAt<TEntity>(1)));
            _dbSet.When(a => a.AddRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Add(b.ArgAt<IEnumerable<TEntity>>(1)));
            _dbSet.When(a => a.Remove(Arg.Any<TEntity>())).Do(b => _store.Remove(b.ArgAt<TEntity>(1)));
            _dbSet.When(a => a.RemoveRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Remove(b.ArgAt<IEnumerable<TEntity>>(1)));
           
            _store.UpdateSnapshot();
        }

        public IDbSet<TEntity> DbSet
        {
            get { return _dbSet; }
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