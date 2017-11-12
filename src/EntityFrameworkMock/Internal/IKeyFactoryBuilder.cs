using System;

namespace EntityFrameworkMock.Internal
{
    internal interface IKeyFactoryBuilder
    {
        Func<T, object> BuildKeyFactory<T>();
    }
}
