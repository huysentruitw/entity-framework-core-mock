using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFrameworkMock.Internal
{
    // From https://msdn.microsoft.com/en-us/library/dn314429(v=vs.113).aspx
    internal class DbAsyncEnumerable<TEntity> : EnumerableQuery<TEntity>, IDbAsyncEnumerable<TEntity>, IQueryable<TEntity>
    {
        public DbAsyncEnumerable(IEnumerable<TEntity> enumerable)
            : base(enumerable)
        {
        }

        public DbAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        public IDbAsyncEnumerator<TEntity> GetAsyncEnumerator() => new DbAsyncEnumerator<TEntity>(this.AsEnumerable().GetEnumerator());

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() => GetAsyncEnumerator();

        IQueryProvider IQueryable.Provider => new DbAsyncQueryProvider<TEntity>(this);
    }
}
