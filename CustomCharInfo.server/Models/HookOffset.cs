using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A hook's address in one game version. Written only through HookOffsetService, which also keeps Hook.Offset current.
    public class HookOffset
    {
        [Required]
        public int HookId { get; set; }
        public Hook Hook { get; set; }

        [Required]
        public int GameVersionId { get; set; }
        public GameVersion GameVersion { get; set; }

        // Normalized by OffsetFormat: uppercase hex, no 0x prefix, no leading zeros.
        [Required, MaxLength(10)]
        public string Offset { get; set; }

        [Required]
        public int OffsetStateId { get; set; }
        public OffsetState OffsetState { get; set; }

        // Null when the site derived the offset.
        public string? SetByUserId { get; set; }
        public ApplicationUser? SetBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
