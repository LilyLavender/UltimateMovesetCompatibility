using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class MovesetDependency
    {
        [Required]
        public int MovesetId { get; set; }
        public Moveset Moveset { get; set; }

        [Required]
        public int DependencyId { get; set; }
        public Dependency Dependency { get; set; }
    }
}
