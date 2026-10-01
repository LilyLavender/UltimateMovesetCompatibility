using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class MovesetModder
    {
        [Required]
        public int MovesetId { get; set; }
        public Moveset Moveset { get; set; }

        [Required]
        public int ModderId { get; set; }
        public Modder Modder { get; set; }

        public int SortOrder { get; set; }

        // Whether the name appears on the moveset card's creator line. The moveset page lists every credit.
        public bool ShowOnCard { get; set; } = true;

        public ICollection<MovesetModderRole> Roles { get; set; } = new List<MovesetModderRole>();
    }
}
