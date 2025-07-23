namespace CustomCharInfo.server.Models.DTOs
{
    public class HookDto
    {
        public int HookId { get; set; }
        public string Offset { get; set; }
        public string Description { get; set; }
        public int HookableStatusId { get; set; }
        public string HookableStatus { get; set; }
    }
}