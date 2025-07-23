namespace CustomCharInfo.server.Models
{
    public class ActionLogDto
    {
        public string UserId { get; set; }
        public int ItemTypeId { get; set; }
        public int ItemId { get; set; }
        public int AcceptanceStateId { get; set; }
        public string Notes { get; set; }
    }
}