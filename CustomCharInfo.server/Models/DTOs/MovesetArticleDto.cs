using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class MovesetArticleDto
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public string ModdedName { get; set; }
        public string? Description { get; set; }
    }
}

