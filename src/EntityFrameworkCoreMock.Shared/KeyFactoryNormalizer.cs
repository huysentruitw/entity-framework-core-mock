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
