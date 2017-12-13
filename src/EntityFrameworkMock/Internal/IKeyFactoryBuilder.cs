using System;

namespace EntityFrameworkMock.Internal
{
    internal interface IKeyFactoryBuilder
    {
        Func<T, KeyContext, object> BuildKeyFactory<T>();
    }
}
