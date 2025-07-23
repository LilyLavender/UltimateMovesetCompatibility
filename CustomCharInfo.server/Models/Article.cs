using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class Article
    {
        [Key]
        public int ArticleId { get; set; }

        [Required, MaxLength(12)]
        public string VanillaCharInternalName { get; set; }
        public VanillaChar VanillaChar { get; set; }

        [Required, MaxLength(24)]
        public string ArticleName { get; set; }

        public ICollection<MovesetArticle> MovesetArticles { get; set; }
    }
}
