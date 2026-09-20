using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class AddWatchedRepoDto
    {
        // Any form GitHubRepoRef.TryParse accepts.
        [Required, MaxLength(500)]
        public string Input { get; set; }
    }

    public class WatchedRepoDto
    {
        public int WatchedRepoId { get; set; }
        public string Owner { get; set; }
        public string Repo { get; set; }
        public string? AddedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
