/*
 * Copyright 2017-2020 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;
using System.Linq;
using System.Reflection;

namespace EntityFrameworkCoreMock
{
    internal sealed class KeyFactoryNormalizer<TEntity>
        where TEntity : class
    {
        private readonly Func<TEntity, KeyContext, object> _keyFactory;

        public KeyFactoryNormalizer(Func<TEntity, KeyContext, object> keyFactory)
        {
            _keyFactory = keyFactory;
        }

        public object GenerateKey(TEntity entity, KeyContext keyContext)
            => NormalizeKey(_keyFactory(entity, keyContext));

        private static object NormalizeKey(object key)
        {
            var keyType = key?.GetType();
            if (keyType == null) return null;

            if (keyType.FullName?.StartsWith("System.ValueTuple`") ?? false)
            {
                var valueTupleTypes = keyType.GetGenericArguments();
                var toTupleMethod = typeof(TupleExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(x => x.Name.Equals(nameof(TupleExtensions.ToTuple)) && x.ReturnType.Name.Equals($"Tuple`{valueTupleTypes.Length}"));
                if (toTupleMethod == null) throw new InvalidOperationException($"No {nameof(TupleExtensions.ToTuple)} extension method found");
                toTupleMethod = toTupleMethod?.MakeGenericMethod(valueTupleTypes);
                return toTupleMethod.Invoke(null, new[] { key });
            }

            if (!keyType.FullName?.StartsWith("System.Tuple`") ?? false)
            {
                var tupleType = Type.GetType("System.Tuple`1");
                if (tupleType == null) throw new InvalidOperationException($"No tuple type found for one generic arguments");
                var constructor = tupleType.MakeGenericType(keyType).GetConstructor(new[] { keyType });
                if (constructor == null) throw new InvalidOperationException($"No tuple constructor found for key in entity");
                return constructor.Invoke(new[] { key });
            }

            return key;
        }
    }
}
