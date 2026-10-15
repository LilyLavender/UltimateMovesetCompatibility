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
            Assert.IsType<ForbidResult>((await controller.ReportCheckResult(1, new WatchedRepoCheckResultDto())).Result);
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
            Assert.Equal(WatchedRepoStatus.Unchecked, dto.Status);
            Assert.Null(dto.LastCheckedAt);

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
        private WatchedRepo AddWatched(string owner, string repo, DateTime? lastCheckedAt = null, DateTime? lastReleaseAt = null)
        {
            var watched = new WatchedRepo
            {
                Owner = owner,
                Repo = repo,
                CreatedAt = DateTime.UtcNow,
                LastCheckedAt = lastCheckedAt,
                LastReleaseAt = lastReleaseAt,
                LastReleaseTag = lastReleaseAt == null ? null : "v9",
                LastReleaseUrl = lastReleaseAt == null ? null : $"https://github.com/{owner}/{repo}/releases/tag/v9"
            };
            _db.Context.WatchedRepos.Add(watched);
            _db.Context.SaveChanges();
            return watched;
        }

        private void AddPluginLinkedTo(string link, DateTime versionCreatedAt, bool linkOnVersion = false)
        {
            SeedData.AddModder(_db.Context, 1, "modder-1", "Modder One");
            _db.Context.Plugins.Add(new Plugin
            {
                Name = "Plugin " + Guid.NewGuid().ToString("N")[..6],
                DefaultLearnMoreUrl = linkOnVersion ? null : link,
                OwnerModderId = 1,
                CreatedAt = versionCreatedAt,
                PluginVersions = new List<PluginVersion>
                {
                    new PluginVersion
                    {
                        VersionLabel = "1.0",
                        Hash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                        LearnMoreUrl = linkOnVersion ? link : null,
                        SubmittedByUserId = "modder-1",
                        CreatedAt = versionCreatedAt,
                        IsCurrent = true
                    }
                }
            });
            _db.Context.SaveChanges();
        }

        private async Task<WatchedRepoDto> GetOneAsync(int id)
        {
            var result = await CreateController("admin-1").GetWatchedRepos();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            return Assert.IsType<List<WatchedRepoDto>>(ok.Value).Single(r => r.WatchedRepoId == id);
        }

        [Fact]
        public async Task CheckResult_StampsTimeStoresReleaseAndClearsIt()
        {
            var watched = AddWatched("owner", "repo");
            var controller = CreateController("admin-1");
            var releaseAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = await controller.ReportCheckResult(watched.WatchedRepoId, new WatchedRepoCheckResultDto
            {
                LastReleaseAt = releaseAt,
                LastReleaseTag = "v2.0",
                LastReleaseUrl = "https://github.com/owner/repo/releases/tag/v2.0"
            });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<WatchedRepoDto>(ok.Value);
            Assert.NotNull(dto.LastCheckedAt);
            Assert.Equal(releaseAt, dto.LastReleaseAt);
            Assert.Equal("v2.0", dto.LastReleaseTag);
            Assert.Equal(WatchedRepoStatus.NeedsUpdate, dto.Status);

            var cleared = await controller.ReportCheckResult(watched.WatchedRepoId, new WatchedRepoCheckResultDto());
            var clearedDto = Assert.IsType<WatchedRepoDto>(Assert.IsType<OkObjectResult>(cleared.Result).Value);
            Assert.Null(clearedDto.LastReleaseAt);
            Assert.Null(clearedDto.LastReleaseTag);
            Assert.Equal(WatchedRepoStatus.NoReleases, clearedDto.Status);

            Assert.IsType<NotFoundResult>((await controller.ReportCheckResult(999, new WatchedRepoCheckResultDto())).Result);
        }

        [Fact]
        public async Task Status_NoAttributedPlugin_NeedsUpdate()
        {
            var watched = AddWatched("owner", "repo", DateTime.UtcNow, DateTime.UtcNow.AddDays(-10));

            var dto = await GetOneAsync(watched.WatchedRepoId);

            Assert.Equal(WatchedRepoStatus.NeedsUpdate, dto.Status);
            Assert.Equal(0, dto.AttributedPluginCount);
        }

        [Fact]
        public async Task Status_ReleaseNewerThanNewestVersion_NeedsUpdate()
        {
            var watched = AddWatched("owner", "repo", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));
            AddPluginLinkedTo("https://github.com/Owner/Repo", DateTime.UtcNow.AddDays(-5));

            var dto = await GetOneAsync(watched.WatchedRepoId);

            Assert.Equal(WatchedRepoStatus.NeedsUpdate, dto.Status);
            Assert.Equal(1, dto.AttributedPluginCount);
        }

        [Fact]
        public async Task Status_VersionRegisteredAfterRelease_UpToDate()
        {
            var watched = AddWatched("owner", "repo", DateTime.UtcNow, DateTime.UtcNow.AddDays(-5));
            AddPluginLinkedTo("https://github.com/owner/repo/releases/tag/v9", DateTime.UtcNow.AddDays(-1), linkOnVersion: true);

            var dto = await GetOneAsync(watched.WatchedRepoId);

            Assert.Equal(WatchedRepoStatus.UpToDate, dto.Status);
            Assert.Equal(1, dto.AttributedPluginCount);
        }

        [Fact]
        public async Task Status_UncheckedAndNoReleases()
        {
            var unchecked_ = AddWatched("a", "unchecked");
            var empty = AddWatched("a", "empty", DateTime.UtcNow, null);

            Assert.Equal(WatchedRepoStatus.Unchecked, (await GetOneAsync(unchecked_.WatchedRepoId)).Status);
            Assert.Equal(WatchedRepoStatus.NoReleases, (await GetOneAsync(empty.WatchedRepoId)).Status);
        }

    }
}
