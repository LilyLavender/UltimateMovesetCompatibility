using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using Amazon.S3;
using Amazon.S3.Model;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/admin/image-gc")]
    public class ImageGarbageCollectionController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IAmazonS3 _s3;

        private readonly IConfiguration _config;

        // Objects newer than this are never deletable even if unreferenced,
        // so an image mid-flight in the upload-then-attach flow isn't deleted out from under a save that hasn't completed yet.
        private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(48);

        private sealed record ScannedImage(string Key, string Url, long SizeBytes, DateTime LastModified, bool InUse, bool Deletable);

        public ImageGarbageCollectionController(AppDbContext context, UserManager<ApplicationUser> userManager, IAmazonS3 s3, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _s3 = s3;
            _config = config;
        }

        private async Task<ApplicationUser?> GetAdminUserAsync()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            return user != null && user.UserTypeId == 3 ? user : null;
        }

        [HttpGet("scan")]
        [Authorize]
        public async Task<IActionResult> Scan()
        {
            if (await GetAdminUserAsync() == null)
                return Forbid();

            var images = await ScanAllAsync();
            return Ok(images);
        }

        [HttpPost("execute")]
        [Authorize]
        public async Task<IActionResult> Execute([FromBody] List<string> keys)
        {
            var admin = await GetAdminUserAsync();
            if (admin == null)
                return Forbid();

            if (keys == null || keys.Count == 0)
                return BadRequest("No keys provided.");

            // Re-derive which keys are actually deletable rather than trusting the client-supplied list
            var deletableKeys = (await ScanAllAsync())
                .Where(i => i.Deletable)
                .Select(i => i.Key)
                .ToHashSet();
            var toDelete = keys.Where(deletableKeys.Contains).ToList();
            var skipped = keys.Except(toDelete).ToList();

            var bucket = _config["R2:BucketName"];
            var deleted = new List<string>();
            var failed = new List<string>();

            foreach (var chunk in toDelete.Chunk(1000))
            {
                var request = new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = chunk.Select(k => new KeyVersion { Key = k }).ToList(),
                };

                try
                {
                    var response = await _s3.DeleteObjectsAsync(request);
                    deleted.AddRange(response.DeletedObjects.Select(d => d.Key));
                    failed.AddRange(response.DeleteErrors.Select(e => e.Key));
                }
                catch (AmazonS3Exception)
                {
                    failed.AddRange(chunk);
                }
            }

            Console.WriteLine($"[ImageGC] Admin {admin.UserName} ({admin.Id}) deleted {deleted.Count} image(s) at {DateTime.UtcNow:o}: {string.Join(", ", deleted)}");

            return Ok(new { deleted, failed, skipped });
        }

        private async Task<List<ScannedImage>> ScanAllAsync()
        {
            var bucket = _config["R2:BucketName"];
            var publicBaseUrl = _config["R2:PublicBaseUrl"]?.TrimEnd('/') ?? "";

            var referencedKeys = await GetReferencedKeysAsync(publicBaseUrl);
            var allObjects = await ListAllObjectsAsync(bucket);
            var cutoff = DateTime.UtcNow - GracePeriod;

            return allObjects
                .OrderBy(o => o.Key)
                .Select(o =>
                {
                    var inUse = referencedKeys.Contains(o.Key);
                    var deletable = !inUse && o.LastModified.ToUniversalTime() < cutoff;
                    return new ScannedImage(o.Key, $"{publicBaseUrl}/{o.Key}", o.Size, o.LastModified, inUse, deletable);
                })
                .ToList();
        }

        private async Task<HashSet<string>> GetReferencedKeysAsync(string publicBaseUrl)
        {
            var referencedUrls = new List<string?>();
            referencedUrls.AddRange(await _context.Movesets.Select(m => m.ThumbhImageUrl).ToListAsync());
            referencedUrls.AddRange(await _context.Movesets.Select(m => m.MovesetHeroImageUrl).ToListAsync());
            referencedUrls.AddRange(await _context.Series.Select(s => s.SeriesIconUrl).ToListAsync());
            referencedUrls.AddRange(await _context.BlogPosts.Select(b => b.BlogImageUrl).ToListAsync());
            referencedUrls.AddRange(await _context.Modders.Select(m => m.PfpUrl).ToListAsync());
            referencedUrls.AddRange(await _context.BannerImages.Select(b => b.ImageUrl).ToListAsync());

            var prefix = publicBaseUrl + "/";
            return referencedUrls
                .Where(u => !string.IsNullOrWhiteSpace(u) && u!.StartsWith(prefix))
                .Select(u => u!.Substring(prefix.Length))
                .ToHashSet();
        }

        private async Task<List<S3Object>> ListAllObjectsAsync(string? bucket)
        {
            var allObjects = new List<S3Object>();
            string? continuationToken = null;
            do
            {
                var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucket,
                    Prefix = "uploads/",
                    ContinuationToken = continuationToken,
                });
                allObjects.AddRange(response.S3Objects ?? new List<S3Object>());
                continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
            } while (continuationToken != null);

            return allObjects;
        }
    }
}
