/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

using System;

namespace EntityFrameworkCoreMock
{
    public interface IKeyFactoryBuilder
    {
        Func<T, KeyContext, object> BuildKeyFactory<T>();
    }
}
