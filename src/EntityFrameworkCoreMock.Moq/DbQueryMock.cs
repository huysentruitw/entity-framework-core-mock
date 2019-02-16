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
using Moq;

namespace EntityFrameworkCoreMock
{
    public class DbQueryMock<TEntity> : Mock<DbQuery<TEntity>>, IDbQueryMock
        where TEntity : class
    {
        private readonly IEnumerable<TEntity> _entities;

        public DbQueryMock(IEnumerable<TEntity> entities, bool asyncQuerySupport = true)
        {
            _entities = (entities ?? Enumerable.Empty<TEntity>()).ToList();

            var data = _entities.AsQueryable();
            As<IQueryable<TEntity>>().Setup(x => x.Provider).Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            As<IQueryable<TEntity>>().Setup(x => x.Expression).Returns(data.Expression);
            As<IQueryable<TEntity>>().Setup(x => x.ElementType).Returns(data.ElementType);
            As<IQueryable<TEntity>>().Setup(x => x.GetEnumerator()).Returns(() => data.GetEnumerator());
            As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(() => data.GetEnumerator());

            if (asyncQuerySupport)
            {
                As<IAsyncEnumerable<TEntity>>().Setup(x => x.GetEnumerator()).Returns(() => new DbAsyncEnumerator<TEntity>(data.GetEnumerator()));
            }
        }
    }
}
