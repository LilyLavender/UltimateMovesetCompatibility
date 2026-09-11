using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class BlogControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public BlogControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private BlogController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new BlogController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task PatchBlogPostImage_Admin_FillsEmptyImage()
        {
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            _db.Context.BlogPosts.Add(new BlogPost { BlogPostId = 1, BlogTitle = "Title", BlogText = "Text", UserId = admin.Id, PostedDate = DateTime.UtcNow });
            _db.Context.SaveChanges();
            var controller = CreateController(admin.Id);

            var result = await controller.PatchBlogPostImage(1, new BlogPostImageDto { BlogImageUrl = "/uploads/blog.png" });

            Assert.IsType<NoContentResult>(result);
            var post = await _db.Context.BlogPosts.FindAsync(1);
            Assert.Equal("/uploads/blog.png", post!.BlogImageUrl);
        }

        [Fact]
        public async Task PatchBlogPostImage_AlreadySet_ReturnsConflict()
        {
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            _db.Context.BlogPosts.Add(new BlogPost { BlogPostId = 1, BlogTitle = "Title", BlogText = "Text", UserId = admin.Id, PostedDate = DateTime.UtcNow, BlogImageUrl = "/uploads/existing.png" });
            _db.Context.SaveChanges();
            var controller = CreateController(admin.Id);

            var result = await controller.PatchBlogPostImage(1, new BlogPostImageDto { BlogImageUrl = "/uploads/new.png" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task PatchBlogPostImage_NonAdmin_ReturnsForbid()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            _db.Context.BlogPosts.Add(new BlogPost { BlogPostId = 1, BlogTitle = "Title", BlogText = "Text", UserId = user.Id, PostedDate = DateTime.UtcNow });
            _db.Context.SaveChanges();
            var controller = CreateController(user.Id);

            var result = await controller.PatchBlogPostImage(1, new BlogPostImageDto { BlogImageUrl = "/uploads/blog.png" });

            Assert.IsType<ForbidResult>(result);
        }
    }
}
