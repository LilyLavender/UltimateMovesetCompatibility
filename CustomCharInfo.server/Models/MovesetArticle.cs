using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class MovesetArticle
    {
        [Required]
        public int MovesetId { get; set; }
        public Moveset Moveset { get; set; }

        [Required]
        public int ArticleId { get; set; }
        public Article Article { get; set; }

        [Required, MaxLength(32)]
        public string ModdedName { get; set; }

        public string? Description { get; set; }
    }
}
