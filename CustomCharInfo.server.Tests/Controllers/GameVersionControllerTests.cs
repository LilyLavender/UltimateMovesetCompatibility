using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Services;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class GameVersionControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        private const string Table = "0x0 0xFFF +0x0\n0x2000 0x2FFF +0x100\n0x5000 0x5FFF -0x10";

        public GameVersionControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
            _db.Context.HookableStatuses.Add(new HookableStatus { HookableStatusId = HookableStatuses.Untested, Name = "Untested" });
            _db.Context.SaveChanges();
            SeedData.AddGameVersion(_db.Context, 1, "13.0.4", 1);
            SeedData.AddHookWithOffset(_db.Context, 1, "500", 1, "in first range");
            SeedData.AddHookWithOffset(_db.Context, 2, "2010", 1, "shifted up");
            SeedData.AddHookWithOffset(_db.Context, 3, "5020", 1, "shifted down");
            SeedData.AddHookWithOffset(_db.Context, 4, "9000", 1, "in a gap");
        }

        public void Dispose() => _db.Dispose();

        private GameVersionController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new GameVersionController(_db.Context, userManager.Object, new HookOffsetService(_db.Context));
            controller.SetFakeUser();
            return controller;
        }

        private static CreateGameVersionDto Dto(string name = "13.0.5", string table = Table) =>
            new() { Name = name, ShiftTable = table };

        [Fact]
        public async Task GetGameVersions_MarksTheNewestAsLatest()
        {
            SeedData.AddGameVersion(_db.Context, 2, "13.0.5", 2);
            var controller = CreateController();

            var result = await controller.GetGameVersions();

            var list = Assert.IsType<List<GameVersionDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(new[] { "13.0.5", "13.0.4" }, list.Select(v => v.Name));
            Assert.True(list[0].IsLatest);
            Assert.False(list[1].IsLatest);
        }

        [Fact]
        public async Task CreateGameVersion_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            Assert.IsType<ForbidResult>((await controller.CreateGameVersion(Dto())).Result);
            Assert.IsType<ForbidResult>((await controller.PreviewGameVersion(Dto())).Result);
            Assert.IsType<ForbidResult>(await controller.DeleteGameVersion(1));
        }

        [Fact]
        public async Task CreateGameVersion_WritesOffsetsShiftsAndCurrentOffsets()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            var result = await controller.CreateGameVersion(Dto());

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var body = Assert.IsType<GameVersionApplyResultDto>(created.Value);
            Assert.Equal("13.0.5", body.Version!.Name);
            Assert.Equal(3, body.Generated);
            Assert.Equal(1, body.CarriedForward);

            var version = await _db.Context.GameVersions.SingleAsync(v => v.Name == "13.0.5");
            Assert.Equal(2, version.SortOrder);
            Assert.Equal("admin-1", version.CreatedByUserId);
            Assert.Equal(3, await _db.Context.GameVersionShifts.CountAsync(s => s.FromGameVersionId == 1 && s.ToGameVersionId == version.GameVersionId));

            var rows = await _db.Context.HookOffsets
                .Where(o => o.GameVersionId == version.GameVersionId)
                .ToDictionaryAsync(o => o.HookId);
            Assert.Equal("500", rows[1].Offset);
            Assert.Equal(OffsetStates.Generated, rows[1].OffsetStateId);
            Assert.Equal("2110", rows[2].Offset);
            Assert.Equal("5010", rows[3].Offset);
            Assert.Equal("9000", rows[4].Offset);
            Assert.Equal(OffsetStates.CarriedForward, rows[4].OffsetStateId);
            Assert.All(rows.Values, r => Assert.Null(r.SetByUserId));

            var hooks = await _db.Context.Hooks.ToDictionaryAsync(h => h.HookId, h => h.Offset);
            Assert.Equal("2110", hooks[2]);
            Assert.Equal("5010", hooks[3]);
            Assert.Equal("9000", hooks[4]);

            // The 13.0.4 rows are untouched.
            Assert.Equal("2010", (await _db.Context.HookOffsets.SingleAsync(o => o.HookId == 2 && o.GameVersionId == 1)).Offset);
        }

        [Fact]
        public async Task CreateGameVersion_DuplicateName_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            var result = await controller.CreateGameVersion(Dto(name: " 13.0.4 "));

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(1, await _db.Context.GameVersions.CountAsync());
        }

        [Fact]
        public async Task CreateGameVersion_BadTable_ReturnsBadRequestNamingTheLine()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            var result = await controller.CreateGameVersion(Dto(table: "0x0 0x10 +0x0\nnonsense"));

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Line 2", bad.Value!.ToString());
            Assert.Equal(1, await _db.Context.GameVersions.CountAsync());
        }

        [Fact]
        public async Task CreateGameVersion_TableThatMergesTwoHooks_ReturnsConflictAndWritesNothing()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            // Hook 1 at 0x500 moves to 0x2010, where hook 2 sits in a range that does not move.
            var result = await controller.CreateGameVersion(Dto(table: "0x0 0xFFF +0x1B10\n0x2000 0x2FFF +0x0"));

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(1, await _db.Context.GameVersions.CountAsync());
            Assert.Equal(4, await _db.Context.HookOffsets.CountAsync());
        }

        [Fact]
        public async Task PreviewGameVersion_ReturnsRowsWithoutWriting()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            var result = await controller.PreviewGameVersion(Dto());

            var body = Assert.IsType<GameVersionApplyResultDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Null(body.Version);
            Assert.Equal(4, body.Rows.Count);
            var gap = body.Rows.Single(r => r.HookId == 4);
            Assert.Equal("9000", gap.OldOffset);
            Assert.Equal("9000", gap.NewOffset);
            Assert.Equal(OffsetStates.CarriedForward, gap.OffsetStateId);

            Assert.Equal(1, await _db.Context.GameVersions.CountAsync());
            Assert.Equal(4, await _db.Context.HookOffsets.CountAsync());
            Assert.Equal("2010", (await _db.Context.Hooks.FindAsync(2))!.Offset);
        }

        [Fact]
        public async Task DeleteGameVersion_OnlyVersion_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            Assert.IsType<BadRequestObjectResult>(await controller.DeleteGameVersion(1));
            Assert.Equal(1, await _db.Context.GameVersions.CountAsync());
        }

        [Fact]
        public async Task DeleteGameVersion_NotTheNewest_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            SeedData.AddGameVersion(_db.Context, 2, "13.0.5", 2);
            var controller = CreateController("admin-1");

            Assert.IsType<BadRequestObjectResult>(await controller.DeleteGameVersion(1));
            Assert.Equal(2, await _db.Context.GameVersions.CountAsync());
        }

        [Fact]
        public async Task DeleteGameVersion_Newest_RemovesItsRowsAndResyncsCurrentOffsets()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");
            var created = await controller.CreateGameVersion(Dto());
            var versionId = ((GameVersionApplyResultDto)((CreatedAtActionResult)created.Result!).Value!).Version!.GameVersionId;
            Assert.Equal("2110", (await _db.Context.Hooks.FindAsync(2))!.Offset);

            var result = await controller.DeleteGameVersion(versionId);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(1, await _db.Context.GameVersions.CountAsync());
            Assert.Equal(0, await _db.Context.GameVersionShifts.CountAsync());
            Assert.Equal(4, await _db.Context.HookOffsets.CountAsync());
            Assert.All(await _db.Context.HookOffsets.ToListAsync(), o => Assert.Equal(1, o.GameVersionId));
            Assert.Equal("2010", (await _db.Context.Hooks.FindAsync(2))!.Offset);
            Assert.Equal("5020", (await _db.Context.Hooks.FindAsync(3))!.Offset);
        }
    }
}
