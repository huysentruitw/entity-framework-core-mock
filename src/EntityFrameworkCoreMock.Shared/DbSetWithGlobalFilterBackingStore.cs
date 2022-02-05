/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFrameworkCoreMock
{
    public class DbSetWithGlobalFilterBackingStore<TEntity> : DbSetBackingStore<TEntity>
        where TEntity : class
    {
        private Func<TEntity, bool> _compiledGlobalQueryFilter;
        private Func<TEntity, bool> CompiledGlobalQueryFilter => _compiledGlobalQueryFilter ??= _globalQueryFilter.Compile();

        private readonly Expression<Func<TEntity, bool>> _globalQueryFilter;

        public DbSetWithGlobalFilterBackingStore(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory, Expression<Func<TEntity, bool>> globalQueryFilter)
            : base(initialEntities, keyFactory) =>
            _globalQueryFilter = globalQueryFilter;

        public override IQueryable<TEntity> GetDataAsQueryable() => base.GetDataAsQueryable().Where(_globalQueryFilter);

        public override TEntity Find(object[] keyValues)
        {
            var entity = base.Find(keyValues);
            if (entity != null && CompiledGlobalQueryFilter(entity))
            {
                return entity;
            }
            return null;
        }
    }
}
