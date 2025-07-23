namespace CustomCharInfo.server.Models
{
    public class UserType
    {
        public int UserTypeId { get; set; }
        public string UserTypeName { get; set; }

        public ICollection<ApplicationUser> Users { get; set; }
    }
}
