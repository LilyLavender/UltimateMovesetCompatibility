using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A GitHub repository admins recheck for new plugin releases on the Repo Releases admin page.
    // Checks happen in the browser. The page reports back when it last checked and what the newest release was.
    public class WatchedRepo
    {
        [Key]
        public int WatchedRepoId { get; set; }

        [Required, MaxLength(100)]
        public string Owner { get; set; }

        [Required, MaxLength(100)]
        public string Repo { get; set; }

        public string? AddedByUserId { get; set; }
        public ApplicationUser? AddedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastCheckedAt { get; set; }

        // The newest release found by the last check, prereleases included. null when the repo had none.
        public DateTime? LastReleaseAt { get; set; }

        [MaxLength(100)]
        public string? LastReleaseTag { get; set; }

        [MaxLength(500)]
        public string? LastReleaseUrl { get; set; }
    }
}
