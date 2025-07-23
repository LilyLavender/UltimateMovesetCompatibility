using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class ImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string ItemName { get; set; }
    }

    public class BlogImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}