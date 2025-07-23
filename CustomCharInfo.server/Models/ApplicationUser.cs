using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CustomCharInfo.server.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Modder link
        public int? ModderId { get; set; }
        public Modder? Modder { get; set; }

        // User role
        public int UserTypeId { get; set; }
        public UserType UserType { get; set; }
    }
}