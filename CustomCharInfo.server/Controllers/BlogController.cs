using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;
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
            var userId = _userManager.GetUserId(User);

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
                    AuthorUserName = p.User.UserName,
                    LikeCount = _context.BlogLikes.Count(bl => bl.BlogPostId == p.BlogPostId),
                    UserLiked = userId != null && _context.BlogLikes.Any(bl => bl.BlogPostId == p.BlogPostId && bl.UserId == userId)
                })
                .ToListAsync();

            return Ok(posts);
        }

        [Authorize]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();

            var existing = await _context.BlogLikes
                .FirstOrDefaultAsync(bl => bl.BlogPostId == id && bl.UserId == userId);

            if (existing != null)
                _context.BlogLikes.Remove(existing);
            else
                _context.BlogLikes.Add(new BlogLike { BlogPostId = id, UserId = userId, CreatedAt = DateTime.UtcNow });

            await _context.SaveChangesAsync();

            var likeCount = await _context.BlogLikes.CountAsync(bl => bl.BlogPostId == id);
            return Ok(new { likeCount, userLiked = existing == null });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBlogPost(CreateBlogPostDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            var blogPost = new BlogPost
            {
                BlogTitle = dto.BlogTitle,
                BlogText = dto.BlogText,
                BlogImageUrl = dto.BlogImageUrl,
                UserId = user.Id,
                PostedDate = DateTime.UtcNow
            };

            _context.BlogPosts.Add(blogPost);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlogPosts), new { id = blogPost.BlogPostId }, blogPost);
        }

        // Attaches an image uploaded just after a create.
        // Completes the create->upload->attach sequence started by CreateBlogPost.
        // Only fills the field if it's still empty
        [Authorize]
        [HttpPatch("{id}/image")]
        public async Task<IActionResult> PatchBlogPostImage(int id, [FromBody] BlogPostImageDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
                return NotFound();

            if (!string.IsNullOrEmpty(blogPost.BlogImageUrl))
                return Conflict("BlogImageUrl is already set; use a full update to change it.");

            blogPost.BlogImageUrl = dto.BlogImageUrl;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
