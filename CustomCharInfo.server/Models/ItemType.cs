using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CustomCharInfo.server.Models
{
    public class ItemType
    {
        public int ItemTypeId { get; set; }
        public string ItemTypeName { get; set; }

        public ICollection<ActionLog> ActionLogs { get; set; }
    }
}