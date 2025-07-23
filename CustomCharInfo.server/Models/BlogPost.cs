using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomCharInfo.server.Models
{

    [Table("BlogPost")]
    public class BlogPost
    {
        public int BlogPostId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string BlogTitle { get; set; }

        [Required]
        public string BlogText { get; set; }

        public DateTime PostedDate { get; set; }

        public string? BlogImageUrl { get; set; }

        public ApplicationUser User { get; set; }
    }
}