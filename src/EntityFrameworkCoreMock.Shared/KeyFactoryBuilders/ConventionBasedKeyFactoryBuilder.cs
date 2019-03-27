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
