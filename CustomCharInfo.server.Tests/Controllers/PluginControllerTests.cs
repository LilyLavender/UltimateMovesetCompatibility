using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class PluginControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();
        private const string Hash1 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string Hash2 = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        private static readonly string Hash3 = new string('c', 64);
        private static readonly string Hash4 = new string('d', 64);

        public PluginControllerTests()
        {
            // ItemTypeId 5 ("Plugin") is seeded via HasData in AppDbContext,
            // so EnsureCreated already has it unlike ids 1-4, which SeedData.SeedLookups adds manually.
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private PluginController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new PluginController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        // SlottedId can't contain digits, so a letters-only value is derived from the test moveset id
        // to keep it unique across AddMoveset calls in the same test.
        private static string SlottedIdFor(int id)
        {
            var n = id;
            var suffix = "";
            do
            {
                suffix = (char)('a' + (n % 26)) + suffix;
                n /= 26;
            } while (n > 0);
            return "slot" + suffix;
        }

        private Moveset AddMoveset(int movesetId)
        {
            var moveset = new Moveset
            {
                MovesetId = movesetId,
                ModdedCharName = "Test Char",
                VanillaCharInternalName = "mario",
                SlottedId = SlottedIdFor(movesetId)
            };
            _db.Context.Movesets.Add(moveset);
            _db.Context.SaveChanges();
            return moveset;
        }

        private void AddModderToMoveset(int movesetId, int modderId)
        {
            _db.Context.MovesetModders.Add(new MovesetModder { MovesetId = movesetId, ModderId = modderId });
            _db.Context.SaveChanges();
        }

        private CreatePluginDto BaseDto(string hash) => new CreatePluginDto
        {
            Name = "Test Plugin",
            VersionLabel = "1.0.0",
            Hash = hash
        };

        [Fact]
        public async Task CreatePlugin_MovesetAttached_ByNonModder_ReturnsForbid()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;

            var result = await controller.CreatePlugin(dto);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task CreatePlugin_MovesetAttached_ByModder_IsLiveWithNoActionLog()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");

            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;

            var result = await controller.CreatePlugin(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(_db.Context.Plugins);
            Assert.Empty(_db.Context.ActionLogs);
            Assert.True(_db.Context.PluginVersions.Single().IsCurrent);
        }

        [Fact]
        public async Task CreatePlugin_Standalone_AnyModder_CreatesHardPendingLog()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            var result = await controller.CreatePlugin(BaseDto(Hash1));

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(5, log.ItemTypeId);
            Assert.Equal(2, log.AcceptanceStateId); // hard-pending
        }

        [Fact]
        public async Task CreatePlugin_Standalone_ByAdmin_IsAutoAcceptedButStillLogged()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "admin-1", "Admin One");
            var controller = CreateController("admin-1");

            await controller.CreatePlugin(BaseDto(Hash1));

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(7, log.AcceptanceStateId); // admin auto-accept
        }

        [Fact]
        public async Task CreatePlugin_BothMovesetAndDependencySet_ReturnsBadRequest()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;
            dto.DependencyId = 1;

            var result = await controller.CreatePlugin(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePlugin_DuplicateHash_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            await controller.CreatePlugin(BaseDto(Hash1));
            var result = await controller.CreatePlugin(BaseDto(Hash1));

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Single(_db.Context.Plugins);
        }

        [Fact]
        public async Task Identify_MovesetAttached_VisibleImmediately()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");
            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;
            await controller.CreatePlugin(dto);

            var result = await controller.Identify(Hash1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<IdentifyPluginResultDto>(ok.Value);
            Assert.Equal("Moveset", body.AttachmentType);
            Assert.True(body.IsCurrent);
        }

        [Fact]
        public async Task Identify_StandalonePending_NotVisible()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");
            await controller.CreatePlugin(BaseDto(Hash1));

            var result = await controller.Identify(Hash1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Identify_StandaloneAccepted_BecomesVisible()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");
            await controller.CreatePlugin(BaseDto(Hash1));

            var pluginVersionId = _db.Context.PluginVersions.Single().PluginVersionId;
            SeedData.AddActionLog(_db.Context, pluginVersionId, acceptanceStateId: AcceptanceStates.AutoAccepted, DateTime.UtcNow.AddMinutes(1), "log-author", itemTypeId: ItemTypes.Plugin);

            var result = await controller.Identify(Hash1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<IdentifyPluginResultDto>(ok.Value);
            Assert.Equal("Other", body.AttachmentType);
        }

        [Fact]
        public async Task IdentifyBatch_MixedKnownAndUnknown_ReturnsPerHashResultsInOrder()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");
            await controller.CreatePlugin(BaseDto(Hash1));
            var pluginVersionId = _db.Context.PluginVersions.Single().PluginVersionId;
            SeedData.AddActionLog(_db.Context, pluginVersionId, acceptanceStateId: AcceptanceStates.AutoAccepted, DateTime.UtcNow.AddMinutes(1), "log-author", itemTypeId: ItemTypes.Plugin);

            var result = await controller.IdentifyBatch(new BatchIdentifyRequestDto { Hashes = new List<string> { Hash1, Hash2 } });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<List<BatchIdentifyResultDto>>(ok.Value);
            Assert.Equal(2, body.Count);
            Assert.Equal(Hash1, body[0].Hash);
            Assert.True(body[0].Found);
            Assert.NotNull(body[0].Result);
            Assert.Equal(Hash2, body[1].Hash);
            Assert.False(body[1].Found);
            Assert.Null(body[1].Result);
        }

        [Fact]
        public async Task IdentifyBatch_UnknownHash_LogsToUnknownPluginHashes()
        {
            var controller = CreateController();

            await controller.IdentifyBatch(new BatchIdentifyRequestDto { Hashes = new List<string> { Hash1 } });

            var unknown = Assert.Single(_db.Context.UnknownPluginHashes);
            Assert.Equal(Hash1, unknown.Hash);
            Assert.Equal(1, unknown.CheckCount);
        }

        [Fact]
        public async Task IdentifyBatch_InvalidHashFormat_MarkedNotFoundWithoutLogging()
        {
            var controller = CreateController();

            var result = await controller.IdentifyBatch(new BatchIdentifyRequestDto { Hashes = new List<string> { "not-a-valid-hash" } });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<List<BatchIdentifyResultDto>>(ok.Value);
            Assert.False(body[0].Found);
            Assert.Empty(_db.Context.UnknownPluginHashes);
        }

        [Fact]
        public async Task IdentifyBatch_TooManyHashes_ReturnsBadRequest()
        {
            var controller = CreateController();
            var hashes = Enumerable.Range(0, 51).Select(i => new string(char.ToLower((char)('a' + i % 26)), 64)).ToList();

            var result = await controller.IdentifyBatch(new BatchIdentifyRequestDto { Hashes = hashes });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task IdentifyBatch_NoHashes_ReturnsBadRequest()
        {
            var controller = CreateController();

            var result = await controller.IdentifyBatch(new BatchIdentifyRequestDto { Hashes = new List<string>() });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddPluginVersion_ByVersionMethod_RecomputesCurrent()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");
            var created = await controller.CreatePlugin(BaseDto(Hash1));
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;

            await controller.AddPluginVersion(pluginId, new CreatePluginVersionDto
            {
                VersionLabel = "2.0.0",
                Hash = Hash2
            });

            var versions = _db.Context.PluginVersions.ToList();
            var v1 = versions.Single(v => v.Hash == Hash1);
            var v2 = versions.Single(v => v.Hash == Hash2);
            Assert.False(v1.IsCurrent);
            Assert.True(v2.IsCurrent);
        }

        [Fact]
        public async Task UpdatePluginVersion_Standalone_ByNonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");
            var created = await controller.CreatePlugin(BaseDto(Hash1));
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;
            var versionId = _db.Context.PluginVersions.Single().PluginVersionId;

            var result = await controller.UpdatePluginVersion(pluginId, versionId, new UpdatePluginVersionDto { VersionLabel = "9.9.9" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreatePlugin_SecondMovesetPlugin_HigherVersionBecomesCurrentAcrossSiblings()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");

            var dto1 = BaseDto(Hash1);
            dto1.MovesetId = 1;
            dto1.VersionLabel = "1.0.0";
            await controller.CreatePlugin(dto1);

            var dto2 = BaseDto(Hash2);
            dto2.MovesetId = 1;
            dto2.VersionLabel = "2.0.0";
            await controller.CreatePlugin(dto2);

            Assert.Equal(2, _db.Context.Plugins.Count(p => p.MovesetId == 1));
            var v1 = _db.Context.PluginVersions.Single(v => v.Hash == Hash1);
            var v2 = _db.Context.PluginVersions.Single(v => v.Hash == Hash2);
            Assert.False(v1.IsCurrent);
            Assert.True(v2.IsCurrent);
        }

        [Fact]
        public async Task DeletePlugin_RemovingCurrentMovesetPlugin_PromotesRemainingSibling()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");

            var dto1 = BaseDto(Hash1);
            dto1.MovesetId = 1;
            dto1.VersionLabel = "1.0.0";
            var created1 = await controller.CreatePlugin(dto1);
            var plugin1Id = ((PluginDto)((CreatedAtActionResult)created1.Result!).Value!).PluginId;

            var dto2 = BaseDto(Hash2);
            dto2.MovesetId = 1;
            dto2.VersionLabel = "2.0.0";
            await controller.CreatePlugin(dto2);

            await controller.DeletePlugin(plugin1Id);

            var remaining = _db.Context.PluginVersions.Single();
            Assert.Equal(Hash2, remaining.Hash);
            Assert.True(remaining.IsCurrent);
        }

        [Fact]
        public async Task AddPluginVersion_OnMovesetPlugin_ReturnsBadRequest()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");

            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;
            var created = await controller.CreatePlugin(dto);
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;

            var result = await controller.AddPluginVersion(pluginId, new CreatePluginVersionDto { VersionLabel = "2.0.0", Hash = Hash2 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeletePluginVersion_OnMovesetPlugin_ReturnsBadRequest()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");

            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;
            var created = await controller.CreatePlugin(dto);
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;
            var versionId = _db.Context.PluginVersions.Single().PluginVersionId;

            var result = await controller.DeletePluginVersion(pluginId, versionId);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePluginVersion_OnMovesetPlugin_ReturnsBadRequest()
        {
            AddMoveset(1);
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            AddModderToMoveset(1, 1);
            var controller = CreateController("user-1");

            var dto = BaseDto(Hash1);
            dto.MovesetId = 1;
            var created = await controller.CreatePlugin(dto);
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;
            var versionId = _db.Context.PluginVersions.Single().PluginVersionId;

            var result = await controller.UpdatePluginVersion(pluginId, versionId, new UpdatePluginVersionDto { VersionLabel = "9.9.9" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePluginVersion_Standalone_ByAdmin_Succeeds()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var creatorController = CreateController("user-1");
            var created = await creatorController.CreatePlugin(BaseDto(Hash1));
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;
            var versionId = _db.Context.PluginVersions.Single().PluginVersionId;

            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin, modderId: 2);
            SeedData.AddModder(_db.Context, 2, "admin-1", "Admin One");
            var adminController = CreateController("admin-1");

            var result = await adminController.UpdatePluginVersion(pluginId, versionId, new UpdatePluginVersionDto { VersionLabel = "9.9.9" });

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("9.9.9", _db.Context.PluginVersions.Single().VersionLabel);
        }

        [Fact]
        public async Task SearchPlugins_Standalone_ReturnsOnlyOtherPlugins()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");
            await controller.CreatePlugin(BaseDto(Hash1));

            AddMoveset(1);
            AddModderToMoveset(1, 1);
            var movesetDto = BaseDto(Hash2);
            movesetDto.MovesetId = 1;
            await controller.CreatePlugin(movesetDto);

            var result = await controller.SearchPlugins(dependencyId: null, standalone: true);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<PluginSearchResultDto>>(ok.Value);
            var item = Assert.Single(list);
            Assert.Equal("Test Plugin", item.Name);
        }

        [Fact]
        public async Task AddPluginVersion_TiedVersionNumbers_AreBothCurrentTogether()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            var created = await controller.CreatePlugin(new CreatePluginDto
            {
                Name = "Test Plugin", VersionLabel = "1.0 (standalone)", Hash = Hash1
            });
            var pluginId = ((PluginDto)((CreatedAtActionResult)created.Result!).Value!).PluginId;

            await controller.AddPluginVersion(pluginId, new CreatePluginVersionDto { VersionLabel = "1.0 (bundled)", Hash = Hash2 });
            var afterHash2 = _db.Context.PluginVersions.ToList();
            Assert.Equal(2, afterHash2.Count);
            Assert.True(afterHash2.Single(v => v.Hash == Hash1).IsCurrent);
            Assert.True(afterHash2.Single(v => v.Hash == Hash2).IsCurrent);

            var hash3Result = await controller.AddPluginVersion(pluginId, new CreatePluginVersionDto { VersionLabel = "2.0 (standalone)", Hash = Hash3 });
            Assert.IsType<CreatedAtActionResult>(hash3Result.Result);
            var afterHash3 = _db.Context.PluginVersions.ToList();
            Assert.Equal(3, afterHash3.Count);
            Assert.False(afterHash3.Single(v => v.Hash == Hash1).IsCurrent);
            Assert.False(afterHash3.Single(v => v.Hash == Hash2).IsCurrent);
            Assert.True(afterHash3.Single(v => v.Hash == Hash3).IsCurrent);

            await controller.AddPluginVersion(pluginId, new CreatePluginVersionDto { VersionLabel = "2.0 (bundled)", Hash = Hash4 });

            var versions = _db.Context.PluginVersions.ToList();
            Assert.False(versions.Single(v => v.Hash == Hash1).IsCurrent);
            Assert.False(versions.Single(v => v.Hash == Hash2).IsCurrent);
            Assert.True(versions.Single(v => v.Hash == Hash3).IsCurrent);
            Assert.True(versions.Single(v => v.Hash == Hash4).IsCurrent);
        }

        [Fact]
        public async Task CreatePlugin_VersionLabelWithVPrefix_IsNormalized()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            await controller.CreatePlugin(new CreatePluginDto { Name = "Test Plugin", VersionLabel = "  Version 2.0", Hash = Hash1 });

            Assert.Equal("2.0", _db.Context.PluginVersions.Single().VersionLabel);
        }

        [Fact]
        public async Task CreatePlugin_FreeformVersionLabel_IsAccepted()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "user-1", "Modder One");
            var controller = CreateController("user-1");

            var result = await controller.CreatePlugin(new CreatePluginDto { Name = "Test Plugin", VersionLabel = "Beta 2.0", Hash = Hash1 });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("Beta 2.0", _db.Context.PluginVersions.Single().VersionLabel);
        }

        [Fact]
        public async Task SearchPlugins_NoFilterGiven_ReturnsBadRequest()
        {
            var controller = CreateController("user-1");

            var result = await controller.SearchPlugins(dependencyId: null, standalone: false);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
