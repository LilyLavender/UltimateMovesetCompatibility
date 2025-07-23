using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class CreateModderDto
    {
        public string? Bio { get; set; }

        [Required]
        public int GamebananaId { get; set; }

        public string? DiscordUsername { get; set; }
    }
}