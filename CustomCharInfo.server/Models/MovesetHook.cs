using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class MovesetHook
    {
        [Required]
        public int MovesetId { get; set; }
        public Moveset Moveset { get; set; }

        [Required]
        public int HookId { get; set; }
        public Hook Hook { get; set; }

        public string? Description { get; set; }
    }
}
