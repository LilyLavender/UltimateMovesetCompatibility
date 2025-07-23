using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class MovesetHookDto
    {
        [Required]
        public int HookId { get; set; }

        public string? Description { get; set; }
    }
}

