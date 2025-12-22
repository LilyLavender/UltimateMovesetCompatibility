using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public UploadController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        [HttpPost("moveset-image")]
        [Authorize]
        public async Task<IActionResult> UploadMovesetImage([FromForm] ImageUploadDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId < 2)
                return Forbid();

            if (dto.File == null || string.IsNullOrWhiteSpace(dto.Type) || string.IsNullOrWhiteSpace(dto.ItemName))
                return BadRequest("Missing file, type, or itemName");

            var allowedTypes = new Dictionary<string, (int width, int height)>
            {
                { "thumb_h", (340, 82) },
                { "moveset_hero", (1200, 1200) },
                { "series_icon", (800, 800) },
            };

            if (!allowedTypes.TryGetValue(dto.Type, out var expectedSize))
                return BadRequest("Invalid type. Must be one of thumb_h, moveset_hero, series_icon");

            using var imageStream = dto.File.OpenReadStream();
            using var image = Image.Load(imageStream);
            if (image.Width != expectedSize.width || image.Height != expectedSize.height)
                return BadRequest($"Invalid image dimensions. {dto.Type} must be {expectedSize.width}x{expectedSize.height}");

            var rawName = Path.GetFileNameWithoutExtension(dto.ItemName);
            var safeName = Regex.Replace(rawName, "[^a-zA-Z0-9]", "");

            if (dto.Type != "series_icon")
            {
                safeName = safeName.ToLower();
            }

            string fileName;
            string uploadDir;

            if (dto.Type == "series_icon")
            {
                fileName = $"{safeName}SeriesIcon{Path.GetExtension(dto.File.FileName)}";
                uploadDir = Path.Combine("wwwroot", "uploads", "series-icons");
            }
            else
            {
                fileName = $"{dto.Type}_{safeName}{Path.GetExtension(dto.File.FileName)}";
                uploadDir = Path.Combine("wwwroot", "uploads", "moveset-ui");
            }

            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, fileName);

            using var saveStream = System.IO.File.Create(filePath);
            await dto.File.CopyToAsync(saveStream);

            var relativeUrl = dto.Type == "series_icon"
                ? $"/uploads/series-icons/{fileName}"
                : $"/uploads/moveset-ui/{fileName}";

            return Ok(new { url = relativeUrl });
        }
        
        [HttpPost("blog-image")]
        [Authorize]
        public async Task<IActionResult> UploadBlogImage([FromForm] BlogImageUploadDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId < 2)
                return Forbid();

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("File is missing.");

            string uploadDir = Path.Combine("wwwroot", "uploads", "blog-images");
            Directory.CreateDirectory(uploadDir);

            string fileExt = Path.GetExtension(dto.File.FileName);
            string fileName;
            string filePath;

            do
            {
                fileName = $"{Guid.NewGuid()}{fileExt}";
                filePath = Path.Combine(uploadDir, fileName);
            }
            while (System.IO.File.Exists(filePath));

            using var saveStream = System.IO.File.Create(filePath);
            await dto.File.CopyToAsync(saveStream);

            var relativeUrl = $"/uploads/blog-images/{fileName}";

            return Ok(new { url = relativeUrl });
        }
    }
}
