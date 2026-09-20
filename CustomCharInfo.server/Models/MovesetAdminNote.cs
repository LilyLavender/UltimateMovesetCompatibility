using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A private note admins leave on a moveset, for example why it is or is not an admin pick.
    // Kept in its own table, with no navigation from Moveset, so no public projection can pick it up by accident.
    public class MovesetAdminNote
    {
        [Key]
        public int MovesetId { get; set; }

        [Required, MaxLength(1000)]
        public string Note { get; set; }

        public string? UpdatedByUserId { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
