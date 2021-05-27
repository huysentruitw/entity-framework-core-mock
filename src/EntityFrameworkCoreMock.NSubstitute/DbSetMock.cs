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
using NSubstitute;

namespace EntityFrameworkCoreMock.NSubstitute
{
    public class DbSetMock<TEntity> : IDbSetMock
        where TEntity : class
    {
        private readonly DbSetBackingStore<TEntity> _store;

        public DbSet<TEntity> Object { get; }

        public DbSetMock(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory, bool asyncQuerySupport = true)
        {
            _store = new DbSetBackingStore<TEntity>(initialEntities, keyFactory);

            var data = _store.GetDataAsQueryable();

            Object = Substitute.For<DbSet<TEntity>, IQueryable<TEntity>, IAsyncEnumerable<TEntity>>();

            ((IQueryable<TEntity>)Object).Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            ((IQueryable<TEntity>)Object).Expression.Returns(data.Expression);
            ((IQueryable<TEntity>)Object).ElementType.Returns(data.ElementType);
            ((IQueryable<TEntity>)Object).GetEnumerator().Returns(a => data.GetEnumerator());
            ((IEnumerable)Object).GetEnumerator().Returns(a => data.GetEnumerator());

            if (asyncQuerySupport)
            {
                ((IAsyncEnumerable<TEntity>)Object).GetAsyncEnumerator(default).Returns(a => new DbAsyncEnumerator<TEntity>(data.GetEnumerator()));
            }

            Object.When(a => a.Add(Arg.Any<TEntity>())).Do(b => _store.Add(b.ArgAt<TEntity>(0)));
            Object.When(a => a.AddRange(Arg.Any<TEntity[]>())).Do(b => _store.Add(b.ArgAt<TEntity[]>(0)));
            Object.When(a => a.AddRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Add(b.ArgAt<IEnumerable<TEntity>>(0)));
            Object.When(a => a.Update(Arg.Any<TEntity>())).Do(b => _store.Update(b.ArgAt<TEntity>(0)));
            Object.When(a => a.UpdateRange(Arg.Any<TEntity[]>())).Do(b => _store.Update(b.ArgAt<TEntity[]>(0)));
            Object.When(a => a.UpdateRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Update(b.ArgAt<IEnumerable<TEntity>>(0)));
            Object.When(a => a.Remove(Arg.Any<TEntity>())).Do(b => _store.Remove(b.ArgAt<TEntity>(0)));
            Object.When(a => a.RemoveRange(Arg.Any<TEntity[]>())).Do(b => _store.Remove(b.ArgAt<TEntity[]>(0)));
            Object.When(a => a.RemoveRange(Arg.Any<IEnumerable<TEntity>>())).Do(b => _store.Remove(b.ArgAt<IEnumerable<TEntity>>(0)));

            Object.AddAsync(Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Returns(info =>
            {
                _store.Add(info.ArgAt<TEntity>(0));
                return default(EntityEntry<TEntity>);
            });
            Object.AddRangeAsync(Arg.Any<TEntity[]>()).Returns(info =>
            {
                _store.Add(info.ArgAt<TEntity[]>(0));
                return Task.CompletedTask;
            });
            Object.AddRangeAsync(Arg.Any<IEnumerable<TEntity>>(), Arg.Any<CancellationToken>()).Returns(info =>
            {
                _store.Add(info.ArgAt<IEnumerable<TEntity>>(0));
                return Task.CompletedTask;
            });

            Object.Find(Arg.Any<object[]>()).Returns(info => _store.Find(info.Args()[0] as object[]));
            Object.FindAsync(Arg.Any<object[]>()).Returns(info => new ValueTask<TEntity>(_store.Find(info.Args()[0] as object[])));
            Object.FindAsync(Arg.Any<object[]>(), Arg.Any<CancellationToken>()).Returns(info => new ValueTask<TEntity>(_store.Find(info.Args()[0] as object[])));

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
