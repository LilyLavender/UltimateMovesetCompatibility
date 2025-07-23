namespace CustomCharInfo.server.Models.DTOs
{
    public class UpdateHookDto
    {
        public string? Offset { get; set; }
        public string? Description { get; set; }
        public int? HookableStatusId { get; set; }
    }
}