using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // What a credited modder did on a moveset. Seeded rows named by ContributionRoles in LookupIds.cs.
    public class ContributionRole
    {
        [Key]
        public int ContributionRoleId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
