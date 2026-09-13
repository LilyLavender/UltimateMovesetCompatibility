using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class CreatePluginDto
    {
        [Required, MaxLength(64)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(255)]
        public string? DefaultLearnMoreUrl { get; set; }

        // At most one of these may be set. Neither set = "other" (case 3).
        public int? MovesetId { get; set; }
        public int? DependencyId { get; set; }

        // First version, required. A Plugin isn't meaningful without at least one hash.
        [Required, MaxLength(32)]
        public string VersionLabel { get; set; }
        [Required, MaxLength(64)]
        public string Hash { get; set; }
        [MaxLength(255)]
        public string? LearnMoreUrl { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdatePluginDto
    {
        [MaxLength(64)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        [MaxLength(255)]
        public string? DefaultLearnMoreUrl { get; set; }
    }

    public class CreatePluginVersionDto
    {
        [Required, MaxLength(32)]
        public string VersionLabel { get; set; }
        [Required, MaxLength(64)]
        public string Hash { get; set; }
        [MaxLength(255)]
        public string? LearnMoreUrl { get; set; }
        public string? Notes { get; set; }
    }

    // Admin-only: editing an existing version's claims bypasses review
    public class UpdatePluginVersionDto
    {
        [MaxLength(32)]
        public string? VersionLabel { get; set; }
        [MaxLength(255)]
        public string? LearnMoreUrl { get; set; }
    }

    public class PluginVersionDto
    {
        public int PluginVersionId { get; set; }
        public string VersionLabel { get; set; }
        public string Hash { get; set; }
        public string? LearnMoreUrl { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? AcceptanceStateId { get; set; }
        public string? AcceptanceStateName { get; set; }
    }

    public class PluginDto
    {
        public int PluginId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? DefaultLearnMoreUrl { get; set; }
        public int? MovesetId { get; set; }
        public string? MovesetName { get; set; }
        public int? DependencyId { get; set; }
        public string? DependencyName { get; set; }
        public int OwnerModderId { get; set; }
        public List<PluginVersionDto> Versions { get; set; } = new();
    }

    public class PluginSearchResultDto
    {
        public int PluginId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? DefaultLearnMoreUrl { get; set; }
    }

    public class IdentifyPluginResultDto
    {
        public string AttachmentType { get; set; } // "Moveset" | "Dependency" | "Other"
        public string PluginName { get; set; }
        public string? PluginDescription { get; set; }
        public int? MovesetId { get; set; }
        public string? MovesetName { get; set; }
        public int? DependencyId { get; set; }
        public string? DependencyName { get; set; }
        public string MatchedVersionLabel { get; set; }
        public bool IsCurrent { get; set; }
        public string? CurrentVersionLabel { get; set; }
        public string? LearnMoreUrl { get; set; }
    }
}
