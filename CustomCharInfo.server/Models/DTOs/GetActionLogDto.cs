namespace CustomCharInfo.server.Models
{
    public class GetActionLogDto
    {
        public int ActionLogId { get; set; }
        public UserSummaryDto User { get; set; }
        public ItemTypeDto ItemType { get; set; }
        public object Item { get; set; }
        public AcceptanceStateDto AcceptanceState { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserSummaryDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class ItemTypeDto
    {
        public int ItemTypeId { get; set; }
        public string ItemTypeName { get; set; }
    }

    public class AcceptanceStateDto
    {
        public int AcceptanceStateId { get; set; }
        public string AcceptanceStateName { get; set; }
    }
}
