using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkMock.Internal
{
    internal class AttributeBasedKeyFactoryBuilder<TAttribute> : IKeyFactoryBuilder
        where TAttribute : Attribute
    {
        public Func<T, object> BuildKeyFactory<T>()
        {
            var entityType = typeof(T);
            var keyProperties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetCustomAttribute(typeof(TAttribute)) != null)
                .ToArray();

            if (!keyProperties.Any()) throw new InvalidOperationException($"Entity type {entityType.Name} does not contain any property marked with {typeof(TAttribute).Name}");

            var tupleType = Type.GetType($"System.Tuple`{keyProperties.Length}");
            if (tupleType == null) throw new InvalidOperationException($"No tuple type found for {keyProperties.Length} generic arguments");

            var keyPropertyTypes = keyProperties.Select(x => x.PropertyType).ToArray();
            var constructor = tupleType.MakeGenericType(keyPropertyTypes).GetConstructor(keyPropertyTypes);
            if (constructor == null) throw new InvalidOperationException($"No tuple constructor found for key in {entityType.Name} entity");

            var arg = Expression.Parameter(entityType);
            var newTupleExpression = Expression.New(constructor, keyProperties.Select(x => Expression.Property(arg, x)));
            return Expression.Lambda<Func<T, object>>(newTupleExpression, arg).Compile();
        }
    }
}
