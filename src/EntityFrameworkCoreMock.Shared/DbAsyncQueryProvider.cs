using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFrameworkCoreMock
{
    public class DbAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public DbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var entityType = expression.Type.IsGenericType && expression.Type.GenericTypeArguments.Length == 1
                ? expression.Type.GenericTypeArguments[0]
                : typeof(TEntity);

            if (entityType != typeof(TEntity))
            {
                var genericDbAsyncEnumerableType = typeof(DbAsyncEnumerable<>);
                var dbAsyncEnumerableType = genericDbAsyncEnumerableType.MakeGenericType(entityType);
                return (IQueryable)Activator.CreateInstance(dbAsyncEnumerableType, expression);
            }

            return new DbAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new DbAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression) => new DbAsyncEnumerable<TResult>(expression);

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute<TResult>(expression));
    }
}
