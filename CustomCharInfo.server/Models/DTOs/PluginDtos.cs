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
        public int CheckCount { get; set; }
        public DateTime? FirstCheckedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }
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

    public class UnknownPluginHashDto
    {
        public string Hash { get; set; }
        public int CheckCount { get; set; }
        public DateTime FirstCheckedAt { get; set; }
        public DateTime LastCheckedAt { get; set; }
    }

    public class IdentifyPluginResultDto
    {
        public string AttachmentType { get; set; } // "Moveset" | "Dependency" | "Other"
        public string PluginName { get; set; }
        public string? PluginDescription { get; set; }
        public int? MovesetId { get; set; }
        public string? MovesetSlottedId { get; set; }
        public string? MovesetName { get; set; }
        public int? DependencyId { get; set; }
        public string? DependencyName { get; set; }
        public string MatchedVersionLabel { get; set; }
        public bool IsCurrent { get; set; }
        public string? CurrentVersionLabel { get; set; }
        public string? LearnMoreUrl { get; set; }
    }

    public class BatchIdentifyRequestDto
    {
        [Required]
        public List<string> Hashes { get; set; }
    }

    public class BatchIdentifyResultDto
    {
        public string Hash { get; set; }
        public bool Found { get; set; }
        public IdentifyPluginResultDto? Result { get; set; }
    }

    // Admin-only hash matching for the Repo Releases page. Read-only, unlike identify.
    public class MatchHashesRequestDto
    {
        [Required]
        public List<string> Hashes { get; set; }
    }

    public class AssetHashDto
    {
        public string Hash { get; set; }
        public long Size { get; set; }
    }

    public static class MatchHashStatus
    {
        public const string Registered = "registered";
        public const string Unregistered = "unregistered";
        public const string Unseen = "unseen";
    }

    public class MatchHashResultDto
    {
        public string Hash { get; set; }
        public string Status { get; set; }

        // Registered only.
        public int? PluginId { get; set; }
        public string? PluginName { get; set; }
        public string? AttachmentType { get; set; }
        public int? MovesetId { get; set; }
        public string? MovesetName { get; set; }
        public int? DependencyId { get; set; }
        public string? DependencyName { get; set; }
        public string? VersionLabel { get; set; }
        public bool? IsCurrent { get; set; }
        public int? AcceptanceStateId { get; set; }

        // Unregistered only: what the identify endpoint recorded.
        public int? CheckCount { get; set; }
        public DateTime? FirstCheckedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }
    }

    // Admin-only batch registration of several versions under one plugin identity.
    public class BatchRegisterPluginsDto
    {
        // Exactly one of these is set.
        public int? PluginId { get; set; }
        public BatchRegisterNewPluginDto? NewPlugin { get; set; }

        [Required]
        public List<BatchRegisterVersionDto> Versions { get; set; }

        public string? Notes { get; set; }
    }

    public class BatchRegisterNewPluginDto
    {
        [Required, MaxLength(64)]
        public string Name { get; set; }
        public string? Description { get; set; }
        [MaxLength(255)]
        public string? DefaultLearnMoreUrl { get; set; }
        // Null means standalone (case 3). Moveset plugins are not supported in batch.
        public int? DependencyId { get; set; }
    }

    public class BatchRegisterVersionDto
    {
        [Required, MaxLength(32)]
        public string VersionLabel { get; set; }
        [Required, MaxLength(64)]
        public string Hash { get; set; }
        [MaxLength(255)]
        public string? LearnMoreUrl { get; set; }
    }
}
