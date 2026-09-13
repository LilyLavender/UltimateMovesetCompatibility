using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    // Tracks hashes submitted to PluginController.Identify that never matched any PluginVersion.
    // Lets admins see what's being checked that isn't registered yet.
    public class UnknownPluginHash
    {
        [Key]
        public int UnknownPluginHashId { get; set; }

        [Required, MaxLength(64)]
        public string Hash { get; set; }

        public int CheckCount { get; set; }

        public DateTime FirstCheckedAt { get; set; }
        public DateTime LastCheckedAt { get; set; }
    }
}
