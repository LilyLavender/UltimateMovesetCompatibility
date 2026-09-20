using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class WatchedRepoControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public WatchedRepoControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
            SeedData.AddUser(_db.Context, "admin-1", UserTypes.Admin);
            SeedData.AddUser(_db.Context, "modder-1", UserTypes.Modder);
        }

        public void Dispose() => _db.Dispose();

        private WatchedRepoController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new WatchedRepoController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        [Theory]
        [InlineData("owner/repo", "owner", "repo")]
        [InlineData("  Owner/Repo.Name  ", "Owner", "Repo.Name")]
        [InlineData("github.com/owner/repo", "owner", "repo")]
        [InlineData("https://github.com/owner/repo", "owner", "repo")]
        [InlineData("https://www.github.com/owner/repo.git", "owner", "repo")]
        [InlineData("https://github.com/owner/repo/releases/tag/v1.0", "owner", "repo")]
        [InlineData("https://github.com/owner/repo/", "owner", "repo")]
        public void GitHubRepoRef_ParsesEveryAcceptedForm(string input, string owner, string repo)
        {
            Assert.True(GitHubRepoRef.TryParse(input, out var parsedOwner, out var parsedRepo));
            Assert.Equal(owner, parsedOwner);
            Assert.Equal(repo, parsedRepo);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("owner")]
        [InlineData("https://gitlab.com/owner/repo")]
        [InlineData("owner/re po")]
        [InlineData("-owner/repo")]
        public void GitHubRepoRef_RejectsBadInput(string input)
        {
            Assert.False(GitHubRepoRef.TryParse(input, out _, out _));
        }

        [Fact]
        public async Task NonAdmin_GetsForbidOnEveryEndpoint()
        {
            var controller = CreateController("modder-1");

            Assert.IsType<ForbidResult>((await controller.GetWatchedRepos()).Result);
            Assert.IsType<ForbidResult>((await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "a/b" })).Result);
            Assert.IsType<ForbidResult>(await controller.DeleteWatchedRepo(1));
        }

        [Fact]
        public async Task Add_NormalizesInputAndRecordsWhoAddedIt()
        {
            var controller = CreateController("admin-1");

            var result = await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "https://github.com/Owner/Repo/releases" });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<WatchedRepoDto>(created.Value);
            Assert.Equal("Owner", dto.Owner);
            Assert.Equal("Repo", dto.Repo);
            Assert.Equal("user-admin-1", dto.AddedByUsername);

            var row = Assert.Single(_db.Context.WatchedRepos);
            Assert.Equal("admin-1", row.AddedByUserId);
        }

        [Fact]
        public async Task Add_UnparseableInput_ReturnsBadRequest()
        {
            var controller = CreateController("admin-1");

            var result = await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "not a repo" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_db.Context.WatchedRepos);
        }

        [Fact]
        public async Task Add_DuplicateIgnoringCase_ReturnsConflict()
        {
            var controller = CreateController("admin-1");
            await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "owner/repo" });

            var result = await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "https://github.com/OWNER/REPO" });

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Single(_db.Context.WatchedRepos);
        }

        [Fact]
        public async Task Get_ListsSortedByOwnerThenRepo()
        {
            var controller = CreateController("admin-1");
            await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "zed/alpha" });
            await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "alpha/zed" });
            await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "alpha/beta" });

            var result = await controller.GetWatchedRepos();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<WatchedRepoDto>>(ok.Value);
            Assert.Equal(new[] { "alpha/beta", "alpha/zed", "zed/alpha" }, list.Select(r => $"{r.Owner}/{r.Repo}").ToArray());
        }

        [Fact]
        public async Task Delete_RemovesRowAndReportsMissing()
        {
            var controller = CreateController("admin-1");
            var added = (WatchedRepoDto)((CreatedAtActionResult)(await controller.AddWatchedRepo(new AddWatchedRepoDto { Input = "a/b" })).Result!).Value!;

            Assert.IsType<NoContentResult>(await controller.DeleteWatchedRepo(added.WatchedRepoId));
            Assert.Empty(_db.Context.WatchedRepos);
            Assert.IsType<NotFoundResult>(await controller.DeleteWatchedRepo(added.WatchedRepoId));
        }
    }
}
