using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkMock.Internal
{
    // From https://msdn.microsoft.com/en-us/library/dn314429(v=vs.113).aspx
    internal class DbAsyncEnumerator<TEntity> : IDbAsyncEnumerator<TEntity>
    {
        private readonly IEnumerator<TEntity> _inner;

        public DbAsyncEnumerator(IEnumerator<TEntity> inner)
        {
            _inner = inner;
        }

        public void Dispose() => _inner.Dispose();

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken) => Task.FromResult(_inner.MoveNext());

        public TEntity Current => _inner.Current;

        object IDbAsyncEnumerator.Current => Current;
    }
}
