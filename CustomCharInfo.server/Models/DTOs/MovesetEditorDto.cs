using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    // One editor entry on a create or update request.
    public class MovesetEditorDto
    {
        [Required]
        public int ModderId { get; set; }

        public bool FullAccess { get; set; }
    }
}
