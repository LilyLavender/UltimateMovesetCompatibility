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
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
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
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            _db.Context.BlogPosts.Add(new BlogPost { BlogPostId = 1, BlogTitle = "Title", BlogText = "Text", UserId = admin.Id, PostedDate = DateTime.UtcNow, BlogImageUrl = "/uploads/existing.png" });
            _db.Context.SaveChanges();
            var controller = CreateController(admin.Id);

            var result = await controller.PatchBlogPostImage(1, new BlogPostImageDto { BlogImageUrl = "/uploads/new.png" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        private ApplicationUser SeedPost()
        {
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            _db.Context.BlogPosts.Add(new BlogPost { BlogPostId = 1, BlogTitle = "Title", BlogText = "Text", UserId = admin.Id, PostedDate = DateTime.UtcNow });
            _db.Context.SaveChanges();
            return admin;
        }

        [Fact]
        public async Task ToggleLike_FirstCall_Likes()
        {
            SeedPost();
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController(user.Id);

            var result = await controller.ToggleLike(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, ok.Value!.GetType().GetProperty("likeCount")!.GetValue(ok.Value));
            Assert.Equal(true, ok.Value.GetType().GetProperty("userLiked")!.GetValue(ok.Value));
            Assert.Single(_db.Context.BlogLikes);
        }

        [Fact]
        public async Task ToggleLike_SecondCall_Unlikes()
        {
            SeedPost();
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController(user.Id);

            await controller.ToggleLike(1);
            var result = await controller.ToggleLike(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, ok.Value!.GetType().GetProperty("likeCount")!.GetValue(ok.Value));
            Assert.Equal(false, ok.Value.GetType().GetProperty("userLiked")!.GetValue(ok.Value));
            Assert.Empty(_db.Context.BlogLikes);
        }

        [Fact]
        public async Task ToggleLike_UnknownPost_ReturnsNotFound()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController(user.Id);

            var result = await controller.ToggleLike(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetBlogPosts_ReportsLikeCountAndUserLiked()
        {
            var admin = SeedPost();
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            _db.Context.BlogLikes.Add(new BlogLike { BlogPostId = 1, UserId = admin.Id, CreatedAt = DateTime.UtcNow });
            _db.Context.BlogLikes.Add(new BlogLike { BlogPostId = 1, UserId = user.Id, CreatedAt = DateTime.UtcNow });
            _db.Context.SaveChanges();

            var signedIn = await CreateController(user.Id).GetBlogPosts();
            var anonymous = await CreateController().GetBlogPosts();

            var post = Assert.Single(Assert.IsAssignableFrom<IEnumerable<BlogPostDto>>(Assert.IsType<OkObjectResult>(signedIn.Result).Value));
            Assert.Equal(2, post.LikeCount);
            Assert.True(post.UserLiked);
            var anonPost = Assert.Single(Assert.IsAssignableFrom<IEnumerable<BlogPostDto>>(Assert.IsType<OkObjectResult>(anonymous.Result).Value));
            Assert.Equal(2, anonPost.LikeCount);
            Assert.False(anonPost.UserLiked);
        }

        [Fact]
        public async Task PatchBlogPostImage_NonAdmin_ReturnsForbid()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            _db.Context.BlogPosts.Add(new BlogPost { BlogPostId = 1, BlogTitle = "Title", BlogText = "Text", UserId = user.Id, PostedDate = DateTime.UtcNow });
            _db.Context.SaveChanges();
            var controller = CreateController(user.Id);

            var result = await controller.PatchBlogPostImage(1, new BlogPostImageDto { BlogImageUrl = "/uploads/blog.png" });

            Assert.IsType<ForbidResult>(result);
        }
    }
}
