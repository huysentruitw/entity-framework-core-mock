using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCoreMock.Tests.Models
{
    [Table("LoggingRepository")]
    public class Issue20Model
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LoggingRepositoryId { get; set; }

        public string Url { get; set; }
    }
}
