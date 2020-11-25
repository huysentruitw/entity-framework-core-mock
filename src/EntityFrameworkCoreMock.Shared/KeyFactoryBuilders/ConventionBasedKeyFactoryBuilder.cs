/*
 * Copyright 2017-2020 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Linq;
using System.Reflection;
using EntityFrameworkCoreMock.Shared.KeyFactoryBuilders;

namespace EntityFrameworkCoreMock
{
    public sealed class ConventionBasedKeyFactoryBuilder : KeyFactoryBuilderBase
    {
        protected override PropertyInfo[] ResolveKeyProperties<T>()
        {
            var entityType = typeof(T);
            var keyProperties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.Name.Equals("Id") || x.Name.Equals($"{entityType.Name}Id"))
                .ToArray();
            if (!keyProperties.Any()) throw new InvalidOperationException($"Entity type {entityType.Name} does not contain any property named Id or {entityType.Name}Id");
            if (keyProperties.Count() > 1) throw new InvalidOperationException($"Entity type {entityType.Name} contains multiple conventional id properties");
            return keyProperties;
        }
    }
}
