using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class GameVersionDto
    {
        public int GameVersionId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public bool IsLatest { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Body for adding a version and for previewing one. ShiftTable is the text format OffsetShifter.Parse reads.
    public class CreateGameVersionDto
    {
        [Required, MaxLength(16)]
        public string Name { get; set; }

        [Required]
        public string ShiftTable { get; set; }
    }

    public class OffsetPreviewRowDto
    {
        public int HookId { get; set; }
        public string Description { get; set; }
        public string OldOffset { get; set; }
        public string NewOffset { get; set; }
        public int OffsetStateId { get; set; }
    }

    public class GameVersionApplyResultDto
    {
        // Null on a preview.
        public GameVersionDto? Version { get; set; }
        public int Generated { get; set; }
        public int CarriedForward { get; set; }
        public List<OffsetPreviewRowDto> Rows { get; set; }
    }

    // One hook's offset in one game version, as returned inside HookDto.Offsets.
    public class HookOffsetDto
    {
        public int GameVersionId { get; set; }
        public string GameVersion { get; set; }
        public string Offset { get; set; }
        public int OffsetStateId { get; set; }
        public string OffsetState { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Body for PUT api/hooks/{id}/offsets/{gameVersionId}. Without Offset, the existing row is confirmed as is.
    public class ConfirmHookOffsetDto
    {
        [MaxLength(12)]
        public string? Offset { get; set; }

        public string? Notes { get; set; }
    }
}
