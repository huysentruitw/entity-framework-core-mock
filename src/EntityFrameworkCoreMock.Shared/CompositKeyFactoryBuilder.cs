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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EntityFrameworkCoreMock
{
    public sealed class CompositKeyFactoryBuilder : IKeyFactoryBuilder
    {
        private readonly AttributeBasedKeyFactoryBuilder<KeyAttribute> _attributeBasedKeyFactoryBuilder
            = new AttributeBasedKeyFactoryBuilder<KeyAttribute>();
        private readonly ConventionBasedKeyFactoryBuilder _conventionBasedKeyFactoryBuilder
            = new ConventionBasedKeyFactoryBuilder();

        public Func<T, KeyContext, object> BuildKeyFactory<T>()
        {
            var exceptions = new List<Exception>();

            try
            {
                return _attributeBasedKeyFactoryBuilder.BuildKeyFactory<T>();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                return _conventionBasedKeyFactoryBuilder.BuildKeyFactory<T>();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            throw new AggregateException($"No key factory could be created for entity type {typeof(T).Name}, see inner exceptions", exceptions);
        }
    }
}
