using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkMock.Tests.Models
{
    public class User
    {
        [Key, Column(Order = 0)]
        public Guid Id { get; set; }

        public string FullName { get; set; }
    }
}
