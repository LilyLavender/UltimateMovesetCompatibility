using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models.DTOs;
using SixLabors.ImageSharp;
using Amazon.S3;
using Amazon.S3.Model;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api")]
    public class BannerImageController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IAmazonS3 _s3;

        private readonly IConfiguration _config;

        // Server-trusted extension -> content-type map, same allowlist as UploadController.
        private static readonly Dictionary<string, string> AllowedImageContentTypes = new()
        {
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" },
        };

        public BannerImageController(AppDbContext context, UserManager<ApplicationUser> userManager, IAmazonS3 s3, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _s3 = s3;
            _config = config;
        }

        private async Task<ApplicationUser?> GetAdminUserAsync()
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            return user.IsAdmin() ? user : null;
        }

        [HttpGet("banner-images")]
        public async Task<IActionResult> GetBannerImages()
        {
            var images = await _context.BannerImages
                .AsNoTracking()
                .OrderBy(b => b.BannerImageId)
                .Select(b => new { b.BannerImageId, b.ImageUrl })
                .ToListAsync();

            return Ok(images);
        }

        [HttpPost("admin/banner-images")]
        [Authorize]
        public async Task<IActionResult> UploadBannerImage([FromForm] BannerImageUploadDto dto)
        {
            if (await GetAdminUserAsync() == null)
                return Forbid();

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("File is missing.");

            var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (!AllowedImageContentTypes.TryGetValue(ext, out var contentType))
                return BadRequest("Invalid file type. Must be one of: " + string.Join(", ", AllowedImageContentTypes.Keys));

            try
            {
                using var imageStream = dto.File.OpenReadStream();
                using var image = Image.Load(imageStream);
            }
            catch (UnknownImageFormatException)
            {
                return BadRequest("File is not a valid image.");
            }

            var key = $"uploads/banner-images/{Guid.NewGuid()}{ext}";
            var bucket = _config["R2:BucketName"];

            using (var stream = dto.File.OpenReadStream())
            {
                await _s3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    DisablePayloadSigning = true,
                });
            }

            var publicBaseUrl = _config["R2:PublicBaseUrl"]?.TrimEnd('/');
            var url = $"{publicBaseUrl}/{key}";

            var banner = new BannerImage { ImageUrl = url, CreatedAt = DateTime.UtcNow };
            _context.BannerImages.Add(banner);
            await _context.SaveChangesAsync();

            return Ok(new { banner.BannerImageId, banner.ImageUrl });
        }

        [HttpDelete("admin/banner-images/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteBannerImage(int id)
        {
            if (await GetAdminUserAsync() == null)
                return Forbid();

            var banner = await _context.BannerImages.FindAsync(id);
            if (banner == null)
                return NotFound();

            var publicBaseUrl = _config["R2:PublicBaseUrl"]?.TrimEnd('/') ?? "";
            var prefix = publicBaseUrl + "/";
            if (banner.ImageUrl.StartsWith(prefix))
            {
                var key = banner.ImageUrl.Substring(prefix.Length);
                var bucket = _config["R2:BucketName"];
                try
                {
                    await _s3.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = bucket,
                        Key = key,
                    });
                }
                catch (AmazonS3Exception)
                {
                    // DB row is still removed even if the R2 object is already gone;
                    // the GC tool will never flag it since it's about to stop being referenced anyway.
                }
            }

            _context.BannerImages.Remove(banner);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
