using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class Series
    {
        [Key]
        public int SeriesId { get; set; }

        [Required, MaxLength(64)]
        public string SeriesName { get; set; }
        
        [MaxLength(255)]
        public string? SeriesIconUrl { get; set; }
    }
}
