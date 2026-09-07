using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Authorization;
using Amazon.S3;
using Amazon.S3.Model;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IAmazonS3 _s3;

        private readonly IConfiguration _config;

        public UploadController(AppDbContext context, UserManager<ApplicationUser> userManager, IAmazonS3 s3, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _s3 = s3;
            _config = config;
        }

        private async Task<string> UploadToR2Async(IFormFile file, string key)
        {
            var bucket = _config["R2:BucketName"];
            using var stream = file.OpenReadStream();
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType,
                DisablePayloadSigning = true, // R2 doesn't support the SDK's default chunked/streaming SigV4 payload signing.
            });

            var publicBaseUrl = _config["R2:PublicBaseUrl"]?.TrimEnd('/');
            return $"{publicBaseUrl}/{key}";
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

            if (dto.File == null || string.IsNullOrWhiteSpace(dto.Type))
                return BadRequest("Missing file or type");

            var allowedTypes = new Dictionary<string, (int width, int height)>
            {
                { "thumb_h", (340, 82) },
                { "moveset_hero", (1200, 1200) },
                { "series_icon", (800, 800) },
            };

            if (!allowedTypes.TryGetValue(dto.Type, out var expectedSize))
                return BadRequest("Invalid type. Must be one of thumb_h, moveset_hero, series_icon");

            using (var imageStream = dto.File.OpenReadStream())
            using (var image = Image.Load(imageStream))
            {
                if (image.Width != expectedSize.width || image.Height != expectedSize.height)
                    return BadRequest($"Invalid image dimensions. {dto.Type} must be {expectedSize.width}x{expectedSize.height}");
            }

            // Create GUID filename
            var guid = Guid.NewGuid();
            var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            var fileName = $"{dto.Type}_{guid}{ext}";

            var keyPrefix = dto.Type == "series_icon" ? "series-icons" : "moveset-ui";
            var key = $"uploads/{keyPrefix}/{fileName}";

            var url = await UploadToR2Async(dto.File, key);

            return Ok(new { url });
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

            var fileExt = Path.GetExtension(dto.File.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var key = $"uploads/blog-images/{fileName}";

            var url = await UploadToR2Async(dto.File, key);

            return Ok(new { url });
        }
    }
}
