namespace CustomCharInfo.server.Models
{
    public class BlogLike
    {
        public int BlogPostId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public BlogPost BlogPost { get; set; }
        public ApplicationUser User { get; set; }
    }
}
