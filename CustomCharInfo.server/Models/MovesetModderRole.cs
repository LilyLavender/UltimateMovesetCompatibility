using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // One role a credited modder holds on a moveset. Cascades from the MovesetModder credit.
    public class MovesetModderRole
    {
        [Required]
        public int MovesetId { get; set; }

        [Required]
        public int ModderId { get; set; }
        public MovesetModder MovesetModder { get; set; }

        [Required]
        public int ContributionRoleId { get; set; }
        public ContributionRole ContributionRole { get; set; }
    }
}
