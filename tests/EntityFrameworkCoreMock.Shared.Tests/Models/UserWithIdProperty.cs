using System;

namespace EntityFrameworkCoreMock.Shared.Tests.Models
{
    public class UserWithIdProperty
    {
        public Guid Id { get; set; }

        public string FullName { get; set; }
    }
}
