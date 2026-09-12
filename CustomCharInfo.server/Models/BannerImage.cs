using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class BannerImage
    {
        [Key]
        public int BannerImageId { get; set; }

        [Required, MaxLength(255)]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
