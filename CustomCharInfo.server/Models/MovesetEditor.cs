using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // A modder who may edit a moveset without being credited on it.
    // FullAccess editors may also change the Modders and Editors lists; partial editors may not.
    public class MovesetEditor
    {
        [Required]
        public int MovesetId { get; set; }
        public Moveset Moveset { get; set; }

        [Required]
        public int ModderId { get; set; }
        public Modder Modder { get; set; }

        public bool FullAccess { get; set; }
    }
}
