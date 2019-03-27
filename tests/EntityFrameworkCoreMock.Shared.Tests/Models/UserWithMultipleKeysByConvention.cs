using System;

namespace EntityFrameworkCoreMock.Shared.Tests.Models
{
    public class UserWithMultipleKeysByConvention
    {
        public Guid Id { get; set; }

        public Guid UserWithMultipleKeysByConventionId { get; set; }

        public string FullName { get; set; }
    }
}
