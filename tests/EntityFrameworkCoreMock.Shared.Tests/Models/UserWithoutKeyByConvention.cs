using System;

namespace EntityFrameworkCoreMock.Shared.Tests.Models
{
    public class UserWithoutKeyByConvention
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; }
    }
}
