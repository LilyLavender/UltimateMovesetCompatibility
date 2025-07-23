using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class CreateHookDto
    {
        [Required, MaxLength(10)]
        public string Offset { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int HookableStatusId { get; set; }
    }
}