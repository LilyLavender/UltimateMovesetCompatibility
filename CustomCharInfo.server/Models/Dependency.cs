using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class Dependency
    {
        [Key]
        public int DependencyId { get; set; }

        [Required, MaxLength(32)]
        public string Name { get; set; }

        [Required]
        public string DownloadLink { get; set; }

        public ICollection<MovesetDependency> MovesetDependencies { get; set; }
    }
}
