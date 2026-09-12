using Amazon.S3;
using Amazon.S3.Model;
using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class BannerImageControllerTests : IDisposable
    {
        private const string PublicBaseUrl = "https://images.example.com";
        private const string Bucket = "test-bucket";

        // Minimal 1x1 transparent PNG, so SixLabors.ImageSharp.Image.Load succeeds.
        private static readonly byte[] OnePixelPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        private readonly TestDbContextFactory _db = new();
        private readonly Mock<IAmazonS3> _s3 = new();

        public BannerImageControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private BannerImageController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["R2:BucketName"] = Bucket,
                    ["R2:PublicBaseUrl"] = PublicBaseUrl,
                })
                .Build();

            var controller = new BannerImageController(_db.Context, userManager.Object, _s3.Object, config);
            controller.SetFakeUser();
            return controller;
        }

        private static IFormFile MakePngFile(string fileName = "banner.png") =>
            new FormFile(new MemoryStream(OnePixelPng), 0, OnePixelPng.Length, "File", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };

        private static T? Prop<T>(object item, string name)
        {
            var value = item.GetType().GetProperty(name)!.GetValue(item);
            return (T?)value;
        }

        [Fact]
        public async Task GetBannerImages_ReturnsAllImages()
        {
            _db.Context.BannerImages.Add(new BannerImage { ImageUrl = $"{PublicBaseUrl}/uploads/banner-images/a.png", CreatedAt = DateTime.UtcNow });
            _db.Context.BannerImages.Add(new BannerImage { ImageUrl = $"{PublicBaseUrl}/uploads/banner-images/b.png", CreatedAt = DateTime.UtcNow });
            _db.Context.SaveChanges();

            var controller = CreateController();

            var result = await controller.GetBannerImages();

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ((IEnumerable<object>)ok.Value!).ToList();
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task UploadBannerImage_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            var controller = CreateController("user-1");

            var result = await controller.UploadBannerImage(new BannerImageUploadDto { File = MakePngFile() });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UploadBannerImage_InvalidExtension_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var controller = CreateController("admin-1");

            var result = await controller.UploadBannerImage(new BannerImageUploadDto { File = MakePngFile("banner.txt") });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadBannerImage_Admin_UploadsToR2AndCreatesRow()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            var controller = CreateController("admin-1");

            var result = await controller.UploadBannerImage(new BannerImageUploadDto { File = MakePngFile() });

            var ok = Assert.IsType<OkObjectResult>(result);
            var url = Prop<string>(ok.Value!, "ImageUrl");
            Assert.StartsWith($"{PublicBaseUrl}/uploads/banner-images/", url);

            Assert.Single(_db.Context.BannerImages);
            _s3.Verify(s => s.PutObjectAsync(
                It.Is<PutObjectRequest>(r => r.BucketName == Bucket && r.Key.StartsWith("uploads/banner-images/")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteBannerImage_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            var banner = new BannerImage { ImageUrl = $"{PublicBaseUrl}/uploads/banner-images/a.png", CreatedAt = DateTime.UtcNow };
            _db.Context.BannerImages.Add(banner);
            _db.Context.SaveChanges();

            var controller = CreateController("user-1");

            var result = await controller.DeleteBannerImage(banner.BannerImageId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteBannerImage_MissingId_ReturnsNotFound()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var controller = CreateController("admin-1");

            var result = await controller.DeleteBannerImage(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteBannerImage_Admin_DeletesFromR2AndRemovesRow()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var banner = new BannerImage { ImageUrl = $"{PublicBaseUrl}/uploads/banner-images/a.png", CreatedAt = DateTime.UtcNow };
            _db.Context.BannerImages.Add(banner);
            _db.Context.SaveChanges();

            _s3.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectResponse());

            var controller = CreateController("admin-1");

            var result = await controller.DeleteBannerImage(banner.BannerImageId);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_db.Context.BannerImages);
            _s3.Verify(s => s.DeleteObjectAsync(
                It.Is<DeleteObjectRequest>(r => r.BucketName == Bucket && r.Key == "uploads/banner-images/a.png"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
