using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Services;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    // Covers the two admin endpoints behind the Repo Releases page: match-hashes and batch.
    public class PluginAdminToolsTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();
        private readonly FakeAssetHasher _hasher = new();
        private static readonly string Hash1 = new string('a', 64);
        private static readonly string Hash2 = new string('b', 64);
        private static readonly string Hash3 = new string('c', 64);
        private static readonly string Hash4 = new string('d', 64);

        public PluginAdminToolsTests()
        {
            SeedData.SeedLookups(_db.Context);
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "admin-1", "Admin One");
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder, modderId: 2);
            SeedData.AddModder(_db.Context, 2, "modder-1", "Modder One");
            _db.Context.Dependencies.Add(new Dependency { DependencyId = 1, Name = "HDR", DownloadLink = "https://example.com" });
            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private PluginController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new PluginController(_db.Context, userManager.Object, _hasher);
            controller.SetFakeUser();
            return controller;
        }

        private Plugin AddStandalonePlugin(string hash, string label = "1.0.0", int acceptanceStateId = AcceptanceStates.AutoAccepted)
        {
            var plugin = new Plugin
            {
                Name = "Existing Plugin",
                OwnerModderId = 2,
                CreatedAt = DateTime.UtcNow,
                PluginVersions = new List<PluginVersion>
                {
                    new PluginVersion { VersionLabel = label, Hash = hash, SubmittedByUserId = "modder-1", CreatedAt = DateTime.UtcNow, IsCurrent = true }
                }
            };
            _db.Context.Plugins.Add(plugin);
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, plugin.PluginVersions.First().PluginVersionId, acceptanceStateId, DateTime.UtcNow, "modder-1", ItemTypes.Plugin);
            return plugin;
        }

        private void AddUnknownHash(string hash, int checkCount)
        {
            _db.Context.UnknownPluginHashes.Add(new UnknownPluginHash
            {
                Hash = hash,
                CheckCount = checkCount,
                FirstCheckedAt = DateTime.UtcNow.AddDays(-2),
                LastCheckedAt = DateTime.UtcNow.AddDays(-1)
            });
            _db.Context.SaveChanges();
        }

        private static BatchRegisterVersionDto Row(string hash, string label = "1.0.0") =>
            new BatchRegisterVersionDto { Hash = hash, VersionLabel = label };

        [Fact]
        public async Task MatchHashes_NonAdmin_ReturnsForbid()
        {
            var controller = CreateController("modder-1");

            var result = await controller.MatchHashes(new MatchHashesRequestDto { Hashes = new List<string> { Hash1 } });

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task MatchHashes_ReportsAllThreeStatusesInInputOrder()
        {
            AddStandalonePlugin(Hash1, acceptanceStateId: AcceptanceStates.PendingAdminHard);
            AddUnknownHash(Hash2, checkCount: 7);
            var controller = CreateController("admin-1");

            var result = await controller.MatchHashes(new MatchHashesRequestDto { Hashes = new List<string> { Hash3, Hash2, Hash1.ToUpperInvariant() } });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<MatchHashResultDto>>(ok.Value);
            Assert.Equal(3, list.Count);

            Assert.Equal(MatchHashStatus.Unseen, list[0].Status);
            Assert.Equal(Hash3, list[0].Hash);

            Assert.Equal(MatchHashStatus.Unregistered, list[1].Status);
            Assert.Equal(7, list[1].CheckCount);

            Assert.Equal(MatchHashStatus.Registered, list[2].Status);
            Assert.Equal(Hash1, list[2].Hash);
            Assert.Equal("Existing Plugin", list[2].PluginName);
            Assert.Equal("Other", list[2].AttachmentType);
            Assert.Equal("1.0.0", list[2].VersionLabel);
            Assert.True(list[2].IsCurrent);
            Assert.Equal(AcceptanceStates.PendingAdminHard, list[2].AcceptanceStateId);
        }

        [Fact]
        public async Task MatchHashes_NeverWritesCountsOrUnknownRows()
        {
            var plugin = AddStandalonePlugin(Hash1);
            AddUnknownHash(Hash2, checkCount: 3);
            var controller = CreateController("admin-1");

            await controller.MatchHashes(new MatchHashesRequestDto { Hashes = new List<string> { Hash1, Hash2, Hash3 } });

            var version = _db.Context.PluginVersions.Single(v => v.PluginId == plugin.PluginId);
            Assert.Equal(0, version.CheckCount);
            Assert.Null(version.LastCheckedAt);
            var unknown = Assert.Single(_db.Context.UnknownPluginHashes);
            Assert.Equal(3, unknown.CheckCount);
        }

        [Fact]
        public async Task MatchHashes_RejectsEmptyOversizedAndMalformedInput()
        {
            var controller = CreateController("admin-1");

            Assert.IsType<BadRequestObjectResult>((await controller.MatchHashes(new MatchHashesRequestDto { Hashes = new List<string>() })).Result);
            Assert.IsType<BadRequestObjectResult>((await controller.MatchHashes(new MatchHashesRequestDto { Hashes = new List<string> { "nope" } })).Result);
            var tooMany = Enumerable.Range(0, 201).Select(i => new string('a', 63) + (i % 10)).ToList();
            Assert.IsType<BadRequestObjectResult>((await controller.MatchHashes(new MatchHashesRequestDto { Hashes = tooMany })).Result);
        }

        private const string AssetUrl = "https://github.com/ultimate-research/nro-hook-plugin/releases/download/v0.4.0/libnro_hook.nro";

        [Fact]
        public async Task HashAsset_NonAdmin_ReturnsForbidWithoutDownloading()
        {
            var controller = CreateController("modder-1");

            var result = await controller.HashAsset(AssetUrl, CancellationToken.None);

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(_hasher.Calls);
        }

        [Theory]
        [InlineData("https://example.com/evil.nro")]
        [InlineData("http://github.com/o/r/releases/download/v1/a.nro")]
        [InlineData("https://github.com/o/r/archive/refs/tags/v1.zip")]
        [InlineData("https://api.github.com/repos/o/r/releases/assets/1")]
        [InlineData("not a url")]
        [InlineData("")]
        public async Task HashAsset_RejectsAnythingButAGitHubReleaseDownload(string url)
        {
            var controller = CreateController("admin-1");

            var result = await controller.HashAsset(url, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_hasher.Calls);
        }

        [Fact]
        public async Task HashAsset_ReturnsHashAndSizeFromTheHasher()
        {
            _hasher.Handler = _ => new AssetHashResult(Hash4, 53248);
            var controller = CreateController("admin-1");

            var result = await controller.HashAsset(AssetUrl, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<AssetHashDto>(ok.Value);
            Assert.Equal(Hash4, dto.Hash);
            Assert.Equal(53248, dto.Size);
            Assert.Equal(new Uri(AssetUrl), Assert.Single(_hasher.Calls));
        }

        [Fact]
        public async Task HashAsset_MapsTooLargeAndDownloadFailures()
        {
            var controller = CreateController("admin-1");

            _hasher.Handler = _ => throw new AssetTooLargeException(GitHubAssetHasher.MaxBytes);
            var tooLarge = Assert.IsType<ObjectResult>((await controller.HashAsset(AssetUrl, CancellationToken.None)).Result);
            Assert.Equal(413, tooLarge.StatusCode);

            _hasher.Handler = _ => throw new HttpRequestException("boom");
            var failed = Assert.IsType<ObjectResult>((await controller.HashAsset(AssetUrl, CancellationToken.None)).Result);
            Assert.Equal(502, failed.StatusCode);
        }

        [Fact]
        public async Task Batch_NonAdmin_ReturnsForbid()
        {
            var controller = CreateController("modder-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "New" },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash1) }
            });

            Assert.IsType<ForbidResult>(result.Result);
            Assert.Empty(_db.Context.Plugins);
        }

        [Fact]
        public async Task Batch_NewStandalonePlugin_CreatesEveryVersionAndAutoAcceptedLogs()
        {
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "Fresh", Description = "desc", DefaultLearnMoreUrl = "https://example.com/mod" },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash1, "v1.0.0"), Row(Hash2, "2.0.0"), Row(Hash3, "1.5.0") },
                Notes = "from repo"
            });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<PluginDto>(created.Value);
            Assert.Equal("Fresh", dto.Name);
            Assert.Equal("desc", dto.Description);
            Assert.Equal(3, dto.Versions.Count);

            var plugin = Assert.Single(_db.Context.Plugins);
            Assert.Equal(1, plugin.OwnerModderId);
            Assert.Null(plugin.DependencyId);

            var versions = _db.Context.PluginVersions.OrderBy(v => v.VersionLabel).ToList();
            Assert.Equal(new[] { "1.0.0", "1.5.0", "2.0.0" }, versions.Select(v => v.VersionLabel).ToArray());
            Assert.Single(versions, v => v.IsCurrent);
            Assert.True(versions.Single(v => v.VersionLabel == "2.0.0").IsCurrent);

            var logs = _db.Context.ActionLogs.ToList();
            Assert.Equal(3, logs.Count);
            Assert.All(logs, l => Assert.Equal(AcceptanceStates.AutoAccepted, l.AcceptanceStateId));
            Assert.All(logs, l => Assert.Equal(ItemTypes.Plugin, l.ItemTypeId));
            Assert.All(logs, l => Assert.Equal("from repo", l.Notes));
            Assert.Equal(versions.Select(v => v.PluginVersionId).OrderBy(x => x), logs.Select(l => l.ItemId).OrderBy(x => x));
        }

        [Fact]
        public async Task Batch_NewDependencyPlugin_AttachesAndDropsDescription()
        {
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "HDR", Description = "ignored", DependencyId = 1 },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash1) }
            });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var plugin = Assert.Single(_db.Context.Plugins);
            Assert.Equal(1, plugin.DependencyId);
            Assert.Null(plugin.Description);
        }

        [Fact]
        public async Task Batch_MissingDependency_ReturnsNotFoundWithoutWriting()
        {
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "X", DependencyId = 99 },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash1) }
            });

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Empty(_db.Context.Plugins);
        }

        [Fact]
        public async Task Batch_ExistingPlugin_AddsVersionsAndRecomputesCurrent()
        {
            var plugin = AddStandalonePlugin(Hash1, label: "1.0.0");
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                PluginId = plugin.PluginId,
                Versions = new List<BatchRegisterVersionDto> { Row(Hash2, "1.1.0"), Row(Hash3, "0.9.0") }
            });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(_db.Context.Plugins);
            var versions = _db.Context.PluginVersions.Where(v => v.PluginId == plugin.PluginId).ToList();
            Assert.Equal(3, versions.Count);
            Assert.True(versions.Single(v => v.VersionLabel == "1.1.0").IsCurrent);
            Assert.False(versions.Single(v => v.VersionLabel == "1.0.0").IsCurrent);
            Assert.Equal(3, _db.Context.ActionLogs.Count());
        }

        [Fact]
        public async Task Batch_MovesetPluginTarget_ReturnsBadRequest()
        {
            _db.Context.Movesets.Add(new Moveset { MovesetId = 1, ModdedCharName = "Waluigi", VanillaCharInternalName = "mario", SlottedId = "slotwaluigi" });
            _db.Context.Plugins.Add(new Plugin
            {
                PluginId = 5,
                Name = "Waluigi plugin",
                MovesetId = 1,
                OwnerModderId = 2,
                CreatedAt = DateTime.UtcNow,
                PluginVersions = new List<PluginVersion>
                {
                    new PluginVersion { VersionLabel = "1.0", Hash = Hash1, SubmittedByUserId = "modder-1", CreatedAt = DateTime.UtcNow, IsCurrent = true }
                }
            });
            _db.Context.SaveChanges();
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                PluginId = 5,
                Versions = new List<BatchRegisterVersionDto> { Row(Hash2) }
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Single(_db.Context.PluginVersions);
        }

        [Fact]
        public async Task Batch_BothOrNeitherTarget_ReturnsBadRequest()
        {
            var controller = CreateController("admin-1");
            var versions = new List<BatchRegisterVersionDto> { Row(Hash1) };

            Assert.IsType<BadRequestObjectResult>((await controller.BatchRegister(new BatchRegisterPluginsDto { Versions = versions })).Result);
            Assert.IsType<BadRequestObjectResult>((await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                PluginId = 1,
                NewPlugin = new BatchRegisterNewPluginDto { Name = "X" },
                Versions = versions
            })).Result);
        }

        [Fact]
        public async Task Batch_DuplicateOrMalformedRows_RejectsWholeBatchNamingRows()
        {
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "X" },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash1), Row(Hash1, "1.1"), Row("bad", "1.2"), Row(Hash2, "  ") }
            });

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            var message = Assert.IsType<string>(bad.Value);
            Assert.Contains("Row 2", message);
            Assert.Contains("Row 3", message);
            Assert.Contains("Row 4", message);
            Assert.Empty(_db.Context.Plugins);
        }

        [Fact]
        public async Task Batch_AlreadyRegisteredHash_ReturnsConflictWithoutWriting()
        {
            AddStandalonePlugin(Hash1);
            var controller = CreateController("admin-1");

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "X" },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash2), Row(Hash1.ToUpperInvariant()) }
            });

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Contains(Hash1, Assert.IsType<string>(conflict.Value));
            Assert.Single(_db.Context.Plugins);
            Assert.Single(_db.Context.PluginVersions);
        }

        [Fact]
        public async Task Batch_TooManyVersions_ReturnsBadRequest()
        {
            var controller = CreateController("admin-1");
            var rows = Enumerable.Range(0, 101).Select(i => Row(new string('a', 61) + i.ToString("D3"), $"1.{i}")).ToList();

            var result = await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "X" },
                Versions = rows
            });

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_db.Context.Plugins);
        }

        [Fact]
        public async Task Batch_AbsorbsUnknownHashHistoryOntoTheNewVersion()
        {
            AddUnknownHash(Hash4, checkCount: 2);
            var unknown = _db.Context.UnknownPluginHashes.Single();
            var controller = CreateController("admin-1");

            await controller.BatchRegister(new BatchRegisterPluginsDto
            {
                NewPlugin = new BatchRegisterNewPluginDto { Name = "X" },
                Versions = new List<BatchRegisterVersionDto> { Row(Hash4), Row(Hash3, "1.1") }
            });

            Assert.Empty(_db.Context.UnknownPluginHashes);
            var absorbed = _db.Context.PluginVersions.Single(v => v.Hash == Hash4);
            Assert.Equal(2, absorbed.CheckCount);
            Assert.Equal(unknown.FirstCheckedAt, absorbed.FirstCheckedAt);
            Assert.Equal(unknown.LastCheckedAt, absorbed.LastCheckedAt);
            var fresh = _db.Context.PluginVersions.Single(v => v.Hash == Hash3);
            Assert.Equal(0, fresh.CheckCount);
            Assert.Null(fresh.FirstCheckedAt);

            var match = await controller.MatchHashes(new MatchHashesRequestDto { Hashes = new List<string> { Hash4 } });
            var list = Assert.IsType<List<MatchHashResultDto>>(Assert.IsType<OkObjectResult>(match.Result).Value);
            Assert.Equal(MatchHashStatus.Registered, Assert.Single(list).Status);
        }
    }
}
