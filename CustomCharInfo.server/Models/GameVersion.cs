using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A release of SSBU that hook offsets are recorded against.
    // The version with the highest SortOrder is the one every offset display uses.
    public class GameVersion
    {
        [Key]
        public int GameVersionId { get; set; }

        [Required, MaxLength(16)]
        public string Name { get; set; }

        public int SortOrder { get; set; }

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<HookOffset> HookOffsets { get; set; }
    }
}
