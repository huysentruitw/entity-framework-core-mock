/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityFrameworkCoreMock
{
    public class DbAsyncEnumerator<TEntity> : IAsyncEnumerator<TEntity>
    {
        private readonly IEnumerator<TEntity> _inner;

        public DbAsyncEnumerator(IEnumerator<TEntity> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
            => new ValueTask<bool>(_inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return default;
        }

        public TEntity Current => _inner.Current;
    }
}
