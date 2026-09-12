namespace CustomCharInfo.server.Models
{
    public class UserIpAddress
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string IpAddress { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}
