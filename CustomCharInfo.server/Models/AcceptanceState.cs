using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CustomCharInfo.server.Models
{
    public class AcceptanceState
    {
        public int AcceptanceStateId { get; set; }
        public string AcceptanceStateName { get; set; }

        public ICollection<ActionLog> ActionLogs { get; set; }
    }
}