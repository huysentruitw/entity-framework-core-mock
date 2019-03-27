using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCoreMock.Shared.Tests.Models
{
    public class UserWithKeyAttribute
    {
        [Key, Column(Order = 0)]
        public Guid UserId { get; set; }

        public string FullName { get; set; }
    }
}
