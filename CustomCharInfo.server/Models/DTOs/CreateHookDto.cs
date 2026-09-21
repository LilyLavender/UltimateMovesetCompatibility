using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class CreateHookDto
    {
        [Required, MaxLength(12)]
        public string Offset { get; set; }

        // The game version Offset was read from. Defaults to the newest version.
        public int? GameVersionId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int HookableStatusId { get; set; }

        public string? Notes { get; set; }
    }
}
