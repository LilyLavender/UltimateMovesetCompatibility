using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class ReleaseState
    {
        [Key]
        public int ReleaseStateId { get; set; }

        [Required, MaxLength(64)]
        public string ReleaseStateName { get; set; }
    }
}
