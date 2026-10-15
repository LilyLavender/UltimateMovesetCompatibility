using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class AddWatchedRepoDto
    {
        // Any form GitHubRepoRef.TryParse accepts.
        [Required, MaxLength(500)]
        public string Input { get; set; }
    }

    // What the Repo Releases page found on its last check. All null when the repo has no releases.
    public class WatchedRepoCheckResultDto
    {
        public DateTime? LastReleaseAt { get; set; }

        [MaxLength(100)]
        public string? LastReleaseTag { get; set; }

        [MaxLength(500)]
        public string? LastReleaseUrl { get; set; }
    }

    // Mirrored by WatchedRepoStatus in the client's globals.js.
    public static class WatchedRepoStatus
    {
        public const string Unchecked = "unchecked";
        public const string NoReleases = "noReleases";
        public const string NeedsUpdate = "needsUpdate";
        public const string UpToDate = "upToDate";
    }

    public class WatchedRepoDto
    {
        public int WatchedRepoId { get; set; }
        public string Owner { get; set; }
        public string Repo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public DateTime? LastReleaseAt { get; set; }
        public string? LastReleaseTag { get; set; }
        public string? LastReleaseUrl { get; set; }
        public int AttributedPluginCount { get; set; }
        public string Status { get; set; }
    }
}
