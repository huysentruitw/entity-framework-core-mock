/*
 * Copyright 2017-2020 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;

namespace EntityFrameworkCoreMock
{
    public class DbSetMock<TEntity> : Mock<DbSet<TEntity>>, IDbSetMock
        where TEntity : class
    {
        private readonly DbSetBackingStore<TEntity> _store;

        public DbSetMock(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory, bool asyncQuerySupport = true)
        {
            _store = new DbSetBackingStore<TEntity>(initialEntities, keyFactory);

            var data = _store.GetDataAsQueryable();
            As<IQueryable<TEntity>>().Setup(x => x.Provider).Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            As<IQueryable<TEntity>>().Setup(x => x.Expression).Returns(data.Expression);
            As<IQueryable<TEntity>>().Setup(x => x.ElementType).Returns(data.ElementType);
            As<IQueryable<TEntity>>().Setup(x => x.GetEnumerator()).Returns(() => data.GetEnumerator());
            As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(() => data.GetEnumerator());

            if (asyncQuerySupport)
            {
                As<IAsyncEnumerable<TEntity>>().Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(() => new DbAsyncEnumerator<TEntity>(data.GetEnumerator()));
            }

            Setup(x => x.Add(It.IsAny<TEntity>())).Callback<TEntity>(_store.Add);
            Setup(x => x.AddRange(It.IsAny<TEntity[]>())).Callback<TEntity[]>(_store.Add);
            Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(_store.Add);
            Setup(x => x.Update(It.IsAny<TEntity>())).Callback<TEntity>(_store.Update);
            Setup(x => x.UpdateRange(It.IsAny<TEntity[]>())).Callback<TEntity[]>(_store.Update);
            Setup(x => x.UpdateRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(_store.Update);
            Setup(x => x.Remove(It.IsAny<TEntity>())).Callback<TEntity>(_store.Remove);
            Setup(x => x.RemoveRange(It.IsAny<TEntity[]>())).Callback<TEntity[]>(_store.Remove);
            Setup(x => x.RemoveRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(_store.Remove);

            Setup(x => x.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>())).Callback<TEntity, CancellationToken>((x, _) => _store.Add(x)).ReturnsAsync(default(EntityEntry<TEntity>));
            Setup(x => x.AddRangeAsync(It.IsAny<TEntity[]>())).Callback<TEntity[]>(_store.Add).Returns(Task.CompletedTask);
            Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>(), It.IsAny<CancellationToken>())).Callback<IEnumerable<TEntity>, CancellationToken>((x, _) => _store.Add(x)).Returns(Task.CompletedTask);

            Setup(x => x.Find(It.IsAny<object[]>())).Returns<object[]>(_store.Find);
            Setup(x => x.FindAsync(It.IsAny<object[]>())).Returns<object[]>(x => new ValueTask<TEntity>(_store.Find(x)));
            Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns<object[], CancellationToken>((x, _) => new ValueTask<TEntity>(_store.Find(x)));

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
