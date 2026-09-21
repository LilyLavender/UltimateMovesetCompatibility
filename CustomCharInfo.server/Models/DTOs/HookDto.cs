namespace CustomCharInfo.server.Models.DTOs
{
    public class HookDto
    {
        public int HookId { get; set; }

        // The offset at the newest game version. Offsets holds every version with newest first.
        public string Offset { get; set; }
        public string? GameVersion { get; set; }
        public int? OffsetStateId { get; set; }
        public List<HookOffsetDto> Offsets { get; set; }

        public string Description { get; set; }
        public int HookableStatusId { get; set; }
        public string HookableStatus { get; set; }
    }
}
