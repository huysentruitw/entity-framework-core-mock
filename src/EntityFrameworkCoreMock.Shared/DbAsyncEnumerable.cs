using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFrameworkCoreMock
{
    public class DbAsyncEnumerable<TEntity> : IAsyncEnumerable<TEntity>, IOrderedQueryable<TEntity>
    {
        private readonly IEnumerable<TEntity> _enumerable;

        public DbAsyncEnumerable(Expression expression)
        {
            Expression = expression;
            _enumerable = CompileExpression<IEnumerable<TEntity>>(expression);
        }

        public DbAsyncEnumerable(IEnumerable<TEntity> enumerable)
        {
            Expression = enumerable.AsQueryable().Expression;
            _enumerable = enumerable;
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
            => _enumerable.GetEnumerator();

        IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetEnumerator()
            => new DbAsyncEnumerator<TEntity>(this.AsEnumerable().GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator()
            => _enumerable.GetEnumerator();

        public Type ElementType => typeof(TEntity);

        public Expression Expression { get; }

        public IQueryProvider Provider => new DbAsyncQueryProvider<TEntity>(_enumerable.AsQueryable().Provider);

        private static T CompileExpression<T>(Expression expression)
            => Expression.Lambda<Func<T>>(
                    body: new Visitor().Visit(expression) ?? throw new InvalidOperationException("Visitor returns null"),
                    parameters: (IEnumerable<ParameterExpression>) null)
                .Compile()();

        private class Visitor : ExpressionVisitor { }
    }
}
