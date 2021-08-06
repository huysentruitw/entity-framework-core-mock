/*
 * Copyright 2017-2021 Wouter Huysentruit
 *
 * See LICENSE file.
 */

namespace EntityFrameworkCoreMock
{
    public sealed class KeyContext
    {
        private long _nextIdentity = 1;

        public long NextIdentity => _nextIdentity++;
    }
}
