using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class PluginVersion
    {
        [Key]
        public int PluginVersionId { get; set; }

        [Required]
        public int PluginId { get; set; }
        public Plugin Plugin { get; set; }

        [Required, MaxLength(32)]
        public string VersionLabel { get; set; }

        // Lowercase 64-char hex SHA-256, globally unique.
        // Immutable after creation; fixing a wrong hash requires deleting and resubmitting.
        [Required, MaxLength(64)]
        public string Hash { get; set; }

        [MaxLength(255)]
        public string? LearnMoreUrl { get; set; }

        // Cached, recomputed for the whole Plugin whenever a version is added. Never set directly by a user.
        public bool IsCurrent { get; set; }

        [Required]
        public string SubmittedByUserId { get; set; }
        public ApplicationUser SubmittedByUser { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
