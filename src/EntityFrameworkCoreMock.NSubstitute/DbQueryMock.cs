/*
 * Copyright 2017-2019 Wouter Huysentruit
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EntityFrameworkCoreMock
{
    public class DbQueryMock<TEntity> : IDbQueryMock
        where TEntity : class
    {
        private readonly IEnumerable<TEntity> _entities;

        public DbQuery<TEntity> Object { get; }

        public DbQueryMock(IEnumerable<TEntity> entities, bool asyncQuerySupport = true)
        {
            _entities = (entities ?? Enumerable.Empty<TEntity>()).ToList();

            var data = _entities.AsQueryable();

            Object = Substitute.For<DbQuery<TEntity>, IQueryable<TEntity>, IAsyncEnumerable<TEntity>>();

            ((IQueryable<TEntity>)Object).Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            Object.AsQueryable().Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            Object.AsQueryable().Expression.Returns(data.Expression);
            Object.AsQueryable().ElementType.Returns(data.ElementType);
            ((IQueryable<TEntity>)Object).GetEnumerator().Returns(a => data.GetEnumerator());
            ((IEnumerable)Object).GetEnumerator().Returns(a => data.GetEnumerator());

            if (asyncQuerySupport)
            {
                ((IAsyncEnumerable<TEntity>)Object).GetEnumerator().Returns(a => new DbAsyncEnumerator<TEntity>(data.GetEnumerator()));
            }
        }
    }
}
