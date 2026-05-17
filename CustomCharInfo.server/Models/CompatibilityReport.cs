using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class CompatibilityReport
    {
        [Key]
        public int ReportId { get; set; }

        [Required]
        public int MovesetId1 { get; set; }

        [Required]
        public int MovesetId2 { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public bool IsCompatible { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
