using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class Modder
    {
        public string? Name { get; set; }

        [Key]
        public int ModderId { get; set; }

        public string? Bio { get; set; }

        public int? GamebananaId { get; set; }

        public string? DiscordUsername { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public ICollection<MovesetModder> MovesetModders { get; set; }
    }
}
