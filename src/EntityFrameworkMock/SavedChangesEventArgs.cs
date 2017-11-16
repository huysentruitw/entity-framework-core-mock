/*
 * Copyright 2017 Wouter Huysentruit
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

namespace EntityFrameworkMock
{
    public class SavedChangesEventArgs<TEntity> : EventArgs
        where TEntity : class
    {
        public UpdatedEntityInfo<TEntity>[] UpdatedEntities { get; set; }
    }

    public class UpdatedEntityInfo<TEntity>
        where TEntity : class
    {
        public TEntity Entity { get; set; }

        public UpdatePropertyInfo[] UpdatedProperties { get; set; }
    }

    public class UpdatePropertyInfo
    {
        public string Name { get; set; }

        public object Original { get; set; }

        public object New { get; set; }
    }
}
