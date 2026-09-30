using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    // One credited modder on a moveset, in credit order. Replaces a bare id in ModderIds.
    public class MovesetModderDto
    {
        [Required]
        public int ModderId { get; set; }

        // ContributionRoles ids. Empty or null means no roles.
        public List<int>? RoleIds { get; set; }

        public bool ShowOnCard { get; set; } = true;
    }
}
