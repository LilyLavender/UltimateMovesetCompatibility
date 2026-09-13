using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A Plugin is attached to exactly one of MovesetId/DependencyId, or neither ("other").
    public class Plugin
    {
        [Key]
        public int PluginId { get; set; }

        [Required, MaxLength(64)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(255)]
        public string? DefaultLearnMoreUrl { get; set; }

        public int? MovesetId { get; set; }
        public Moveset? Moveset { get; set; }

        public int? DependencyId { get; set; }
        public Dependency? Dependency { get; set; }

        [Required]
        public int OwnerModderId { get; set; }
        public Modder OwnerModder { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<PluginVersion> PluginVersions { get; set; }
    }
}
