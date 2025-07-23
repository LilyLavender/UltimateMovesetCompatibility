namespace CustomCharInfo.server.Models.DTOs
{

    public class BlogPostDto
    {
        public int BlogPostId { get; set; }
        public string BlogTitle { get; set; }
        public string BlogText { get; set; }
        public string? BlogImageUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public string AuthorUserName { get; set; }
    }

    public class CreateBlogPostDto
    {
        public string BlogTitle { get; set; }
        public string BlogText { get; set; }
        public string? BlogImageUrl { get; set; }
    }
}