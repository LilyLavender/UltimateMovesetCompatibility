using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CustomCharInfo.server.Models
{
    public class ActionLog
    {
        public int ActionLogId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int ItemTypeId { get; set; }
        public ItemType ItemType { get; set; }

        public int ItemId { get; set; }

        public int AcceptanceStateId { get; set; }
        public AcceptanceState AcceptanceState { get; set; }

        public string Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } 
    }
}