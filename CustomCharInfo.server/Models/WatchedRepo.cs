using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A GitHub repository admins recheck for new plugin releases on the Repo Releases admin page.
    // Just the reference; checks happen in the browser and nothing about their results is stored.
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
    }
}
