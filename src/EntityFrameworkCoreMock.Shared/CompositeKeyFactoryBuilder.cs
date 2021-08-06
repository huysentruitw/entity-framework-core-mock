/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EntityFrameworkCoreMock
{
    public sealed class CompositeKeyFactoryBuilder : IKeyFactoryBuilder
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
