using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class Hook
    {
        [Key]
        public int HookId { get; set; }

        // The offset at the newest game version, copied from HookOffsets by HookOffsetService.
        // Per-version history lives in HookOffsets; this column exists so read sites need no join.
        [Required, MaxLength(10)]
        public string Offset { get; set; }

        public ICollection<HookOffset> HookOffsets { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int HookableStatusId { get; set; }
        public HookableStatus HookableStatus { get; set; }

        public ICollection<MovesetHook> MovesetHooks { get; set; }
    }
}
