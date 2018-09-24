using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCoreMock.Shared.Tests.Models
{
    public class TenantUser : User
    {
        [Key, Column(Order = 1), MaxLength(50)]
        public string Tenant { get; set; }
    }
}
