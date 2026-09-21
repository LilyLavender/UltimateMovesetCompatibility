namespace CustomCharInfo.server.Models.DTOs
{
    // Offsets are edited per game version through PUT api/hooks/{id}/offsets/{gameVersionId}, not here.
    public class UpdateHookDto
    {
        public string? Description { get; set; }
        public int? HookableStatusId { get; set; }
        public string? Notes { get; set; }
    }
}
