using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models
{
    public class VanillaChar
    {
        [Key, MaxLength(12)]
        public string VanillaCharInternalName { get; set; }

        [Required, MaxLength(18)]
        public string DisplayName { get; set; }
    }
}
