using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomCharInfo.server.Models
{
    [Table("HookableStatus")]
    public class HookableStatus
    {
        [Key]
        public int HookableStatusId { get; set; }

        [Required, MaxLength(32)]
        public string Name { get; set; }

        public ICollection<Hook> Hooks { get; set; }
    }
}