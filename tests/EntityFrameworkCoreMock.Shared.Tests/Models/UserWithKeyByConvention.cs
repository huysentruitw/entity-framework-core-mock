using System;

namespace EntityFrameworkCoreMock.Shared.Tests.Models
{
    public class UserWithKeyByConvention
    {
        public Guid UserWithKeyByConventionId { get; set; }

        public string FullName { get; set; }
    }
}
