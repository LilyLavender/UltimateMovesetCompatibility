using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class Hook
    {
        [Key]
        public int HookId { get; set; }
    
        [Required, MaxLength(10)]
        public string Offset { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int HookableStatusId { get; set; }
        public HookableStatus HookableStatus { get; set; }

        public ICollection<MovesetHook> MovesetHooks { get; set; }
    }
}
