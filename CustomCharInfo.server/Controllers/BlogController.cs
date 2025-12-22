using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/blog")]
    public class BlogController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public BlogController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogPostDto>>> GetBlogPosts()
        {
            var posts = await _context.BlogPosts
                .Include(p => p.User)
                .OrderByDescending(p => p.PostedDate)
                .Select(p => new BlogPostDto
                {
                    BlogPostId = p.BlogPostId,
                    BlogTitle = p.BlogTitle,
                    BlogText = p.BlogText,
                    BlogImageUrl = p.BlogImageUrl,
                    PostedDate = p.PostedDate,
                    AuthorUserName = p.User.UserName
                })
                .ToListAsync();

            return Ok(posts);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBlogPost(CreateBlogPostDto dto)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var blogPost = new BlogPost
            {
                BlogTitle = dto.BlogTitle,
                BlogText = dto.BlogText,
                BlogImageUrl = dto.BlogImageUrl,
                UserId = userId,
                PostedDate = DateTime.UtcNow
            };

            _context.BlogPosts.Add(blogPost);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlogPosts), new { id = blogPost.BlogPostId }, blogPost);
        }
    }
}
