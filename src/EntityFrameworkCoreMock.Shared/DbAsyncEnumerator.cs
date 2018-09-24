using System.Collections.Generic;
using System.Threading;
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

        public void Dispose() => _inner.Dispose();

        public Task<bool> MoveNext(CancellationToken cancellationToken) => Task.FromResult(_inner.MoveNext());

        public TEntity Current => _inner.Current;
    }
}
