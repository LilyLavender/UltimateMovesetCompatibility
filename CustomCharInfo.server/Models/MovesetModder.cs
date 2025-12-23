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
    }
}
