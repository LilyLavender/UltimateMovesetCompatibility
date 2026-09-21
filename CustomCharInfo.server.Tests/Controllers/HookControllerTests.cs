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
    public class HookControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public HookControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
            _db.Context.HookableStatuses.Add(new HookableStatus { HookableStatusId = HookableStatuses.Untested, Name = "Untested" });
            _db.Context.SaveChanges();
            SeedData.AddGameVersion(_db.Context, 1, "13.0.4", 1);
        }

        public void Dispose() => _db.Dispose();

        private HookController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new HookController(_db.Context, userManager.Object, new HookOffsetService(_db.Context));
            controller.SetFakeUser();
            return controller;
        }

        // A second version where everything at or above 0x1000 moves up by 0x100.
        private void AddShiftedVersion(int id = 2, string name = "13.0.5", int sortOrder = 2, int fromId = 1)
        {
            SeedData.AddGameVersion(_db.Context, id, name, sortOrder);
            _db.Context.GameVersionShifts.Add(new GameVersionShift
            {
                FromGameVersionId = fromId,
                ToGameVersionId = id,
                RangeStart = 0x1000,
                RangeEnd = 0xFFFF,
                Delta = 0x100
            });
            _db.Context.SaveChanges();
        }

        [Fact]
        public async Task CreateHook_NonModder_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController("user-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = HookableStatuses.Untested });

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task CreateHook_Modder_PersistsNormalizedHookWithConfirmedOffset()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "0x001234", Description = "Test", HookableStatusId = HookableStatuses.Untested });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var hook = Assert.Single(_db.Context.Hooks);
            Assert.Equal("1234", hook.Offset);

            var offset = Assert.Single(_db.Context.HookOffsets);
            Assert.Equal(hook.HookId, offset.HookId);
            Assert.Equal(1, offset.GameVersionId);
            Assert.Equal("1234", offset.Offset);
            Assert.Equal(OffsetStates.Confirmed, offset.OffsetStateId);
            Assert.Equal("modder-1", offset.SetByUserId);
        }

        [Fact]
        public async Task CreateHook_Modder_WritesActionLogAsPendingAdminSoft()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = HookableStatuses.Untested });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(1, log.AcceptanceStateId);
            Assert.Equal("modder-1", log.UserId);
            Assert.Contains("1234", log.Diff);
            Assert.Contains("13.0.4", log.Diff);
        }

        [Fact]
        public async Task CreateHook_DuplicateOffset_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");
            await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = HookableStatuses.Untested });

            var result = await controller.CreateHook(new CreateHookDto { Offset = "1234", Description = "Different description", HookableStatusId = HookableStatuses.Untested });

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Single(_db.Context.Hooks);
        }

        [Fact]
        public async Task CreateHook_BadOffset_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "0xNOPE", Description = "Test", HookableStatusId = HookableStatuses.Untested });

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_db.Context.Hooks);
        }

        [Fact]
        public async Task CreateHook_UnknownVersion_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "1234", GameVersionId = 99, Description = "Test", HookableStatusId = HookableStatuses.Untested });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateHook_AgainstOlderVersion_DerivesForwardThroughEveryLaterVersion()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion(id: 2, name: "13.0.5", sortOrder: 2, fromId: 1);
            AddShiftedVersion(id: 3, name: "13.0.6", sortOrder: 3, fromId: 2);
            var controller = CreateController("modder-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "2000", GameVersionId = 1, Description = "Old", HookableStatusId = HookableStatuses.Untested });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var hook = Assert.Single(_db.Context.Hooks);
            Assert.Equal("2200", hook.Offset);

            var rows = _db.Context.HookOffsets.OrderBy(o => o.GameVersionId).ToList();
            Assert.Equal(3, rows.Count);
            Assert.Equal(("2000", OffsetStates.Confirmed, "modder-1"), (rows[0].Offset, rows[0].OffsetStateId, rows[0].SetByUserId));
            Assert.Equal(("2100", OffsetStates.Generated, (string?)null), (rows[1].Offset, rows[1].OffsetStateId, rows[1].SetByUserId));
            Assert.Equal(("2200", OffsetStates.Generated, (string?)null), (rows[2].Offset, rows[2].OffsetStateId, rows[2].SetByUserId));
        }

        [Fact]
        public async Task CreateHook_AgainstOlderVersion_InAGap_CarriesForward()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            var controller = CreateController("modder-1");

            await controller.CreateHook(new CreateHookDto { Offset = "500", GameVersionId = 1, Description = "Gap", HookableStatusId = HookableStatuses.Untested });

            var newest = _db.Context.HookOffsets.Single(o => o.GameVersionId == 2);
            Assert.Equal("500", newest.Offset);
            Assert.Equal(OffsetStates.CarriedForward, newest.OffsetStateId);
            Assert.Equal("500", Assert.Single(_db.Context.Hooks).Offset);
        }

        [Fact]
        public async Task CreateHook_AgainstNewestVersion_WritesNoOlderRows()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            var controller = CreateController("modder-1");

            await controller.CreateHook(new CreateHookDto { Offset = "2100", Description = "New", HookableStatusId = HookableStatuses.Untested });

            var row = Assert.Single(_db.Context.HookOffsets);
            Assert.Equal(2, row.GameVersionId);
            Assert.Equal("2100", Assert.Single(_db.Context.Hooks).Offset);
        }

        [Fact]
        public async Task CreateHook_DerivedOffsetTakenByAnotherHook_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            SeedData.AddHookWithOffset(_db.Context, 1, "2100", 2, "already there");
            var controller = CreateController("modder-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "2000", GameVersionId = 1, Description = "Collides", HookableStatusId = HookableStatuses.Untested });

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Contains("13.0.5", conflict.Value!.ToString());
            Assert.Single(_db.Context.Hooks);
        }

        [Fact]
        public async Task CreateHook_Admin_StillWritesActionLogAsPendingAdminSoft()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = HookableStatuses.Untested });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(1, log.AcceptanceStateId);
        }

        [Fact]
        public async Task GetHooks_ReturnsCurrentOffsetStateAndHistoryNewestFirst()
        {
            AddShiftedVersion();
            var hook = SeedData.AddHookWithOffset(_db.Context, 1, "2000", 1, "Old");
            _db.Context.HookOffsets.Add(new HookOffset { HookId = 1, GameVersionId = 2, Offset = "2100", OffsetStateId = OffsetStates.Generated, UpdatedAt = DateTime.UtcNow });
            hook.Offset = "2100";
            _db.Context.SaveChanges();
            var controller = CreateController();

            var result = await controller.GetHooks();

            var list = Assert.IsAssignableFrom<IEnumerable<HookDto>>(Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
            var dto = Assert.Single(list);
            Assert.Equal("2100", dto.Offset);
            Assert.Equal("13.0.5", dto.GameVersion);
            Assert.Equal(OffsetStates.Generated, dto.OffsetStateId);
            Assert.Equal(new[] { "13.0.5", "13.0.4" }, dto.Offsets.Select(o => o.GameVersion));
            Assert.Equal("Generated", dto.Offsets[0].OffsetState);
            Assert.Equal("Confirmed", dto.Offsets[1].OffsetState);
        }

        [Fact]
        public async Task SetHookOffset_NonModder_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            SeedData.AddHookWithOffset(_db.Context, 1, "2000", 1);
            var controller = CreateController("user-1");

            var result = await controller.SetHookOffset(1, 1, new ConfirmHookOffsetDto());

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task SetHookOffset_UnknownHookOrVersion_ReturnsNotFound()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            SeedData.AddHookWithOffset(_db.Context, 1, "2000", 1);
            var controller = CreateController("modder-1");

            Assert.IsType<NotFoundResult>((await controller.SetHookOffset(99, 1, new ConfirmHookOffsetDto())).Result);
            Assert.IsType<NotFoundResult>((await controller.SetHookOffset(1, 99, new ConfirmHookOffsetDto())).Result);
        }

        [Fact]
        public async Task SetHookOffset_WithoutOffset_ConfirmsExistingRowAndLogs()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            var hook = SeedData.AddHookWithOffset(_db.Context, 1, "2000", 1);
            _db.Context.HookOffsets.Add(new HookOffset { HookId = 1, GameVersionId = 2, Offset = "2100", OffsetStateId = OffsetStates.Generated, UpdatedAt = DateTime.UtcNow });
            hook.Offset = "2100";
            _db.Context.SaveChanges();
            var controller = CreateController("modder-1");

            var result = await controller.SetHookOffset(1, 2, new ConfirmHookOffsetDto { Notes = "Checked in Ghidra" });

            var dto = Assert.IsType<HookOffsetDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal("2100", dto.Offset);
            Assert.Equal(OffsetStates.Confirmed, dto.OffsetStateId);

            var row = await _db.Context.HookOffsets.SingleAsync(o => o.HookId == 1 && o.GameVersionId == 2);
            Assert.Equal(OffsetStates.Confirmed, row.OffsetStateId);
            Assert.Equal("modder-1", row.SetByUserId);
            Assert.Equal("2100", (await _db.Context.Hooks.FindAsync(1))!.Offset);

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(AcceptanceStates.PendingAdminSoft, log.AcceptanceStateId);
            Assert.Equal("Checked in Ghidra", log.Notes);
            Assert.Contains("Generated", log.Diff);
            Assert.Contains("Confirmed", log.Diff);
            Assert.DoesNotContain("\"field\":\"Offset (13.0.5)\"", log.Diff);
        }

        [Fact]
        public async Task SetHookOffset_WithoutOffsetAndNoRow_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            SeedData.AddHookWithOffset(_db.Context, 1, "2100", 2);
            var controller = CreateController("modder-1");

            var result = await controller.SetHookOffset(1, 1, new ConfirmHookOffsetDto());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SetHookOffset_OnNewestVersion_UpdatesCurrentOffset()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            var hook = SeedData.AddHookWithOffset(_db.Context, 1, "2000", 1);
            _db.Context.HookOffsets.Add(new HookOffset { HookId = 1, GameVersionId = 2, Offset = "2100", OffsetStateId = OffsetStates.Generated, UpdatedAt = DateTime.UtcNow });
            hook.Offset = "2100";
            _db.Context.SaveChanges();
            var controller = CreateController("modder-1");

            var result = await controller.SetHookOffset(1, 2, new ConfirmHookOffsetDto { Offset = "0x2104" });

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("2104", (await _db.Context.Hooks.FindAsync(1))!.Offset);
            Assert.Equal("2104", (await _db.Context.HookOffsets.SingleAsync(o => o.HookId == 1 && o.GameVersionId == 2)).Offset);

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Contains("2100", log.Diff);
            Assert.Contains("2104", log.Diff);
        }

        [Fact]
        public async Task SetHookOffset_OnOlderVersion_LeavesCurrentOffsetAlone()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            AddShiftedVersion();
            SeedData.AddHookWithOffset(_db.Context, 1, "2100", 2);
            var controller = CreateController("modder-1");

            var result = await controller.SetHookOffset(1, 1, new ConfirmHookOffsetDto { Offset = "2000" });

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("2100", (await _db.Context.Hooks.FindAsync(1))!.Offset);
            var older = await _db.Context.HookOffsets.SingleAsync(o => o.HookId == 1 && o.GameVersionId == 1);
            Assert.Equal("2000", older.Offset);
            Assert.Equal(OffsetStates.Confirmed, older.OffsetStateId);
        }

        [Fact]
        public async Task SetHookOffset_TakenByAnotherHook_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            SeedData.AddHookWithOffset(_db.Context, 1, "2000", 1, "first");
            SeedData.AddHookWithOffset(_db.Context, 2, "3000", 1, "second");
            var controller = CreateController("modder-1");

            var result = await controller.SetHookOffset(2, 1, new ConfirmHookOffsetDto { Offset = "2000" });

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("3000", (await _db.Context.Hooks.FindAsync(2))!.Offset);
            Assert.Empty(_db.Context.ActionLogs);
        }

        [Fact]
        public async Task UpdateHook_UnknownId_ReturnsNotFound()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            var result = await controller.UpdateHook(999, new UpdateHookDto { Description = "New" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateHook_PartialDto_OnlyUpdatesProvidedFields()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "1", Description = "Old", HookableStatusId = HookableStatuses.Untested });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.UpdateHook(1, new UpdateHookDto { Description = "New Description" });

            Assert.IsType<NoContentResult>(result);
            var hook = await _db.Context.Hooks.FindAsync(1);
            Assert.Equal("New Description", hook!.Description);
            Assert.Equal("1", hook.Offset);

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(1, log.ItemId);
            Assert.Equal(1, log.AcceptanceStateId);
            Assert.Contains("Description", log.Diff);
            Assert.DoesNotContain("Offset", log.Diff);
        }

        [Fact]
        public async Task UpdateHook_UsesSubmittedNotes_NotAHardcodedDefault()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "1", Description = "Old", HookableStatusId = HookableStatuses.Untested });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            await controller.UpdateHook(1, new UpdateHookDto { Description = "New Description", Notes = "Fixed a typo" });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal("Fixed a typo", log.Notes);
        }

        [Fact]
        public async Task UpdateHook_NoNotesSubmitted_LogsEmptyNotes()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "1", Description = "Old", HookableStatusId = HookableStatuses.Untested });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            await controller.UpdateHook(1, new UpdateHookDto { Description = "New Description" });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal("", log.Notes);
        }

        [Fact]
        public async Task DeleteHook_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "1", Description = "ToDelete", HookableStatusId = HookableStatuses.Untested });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.DeleteHook(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteHook_Admin_RemovesHookAndItsOffsets()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            SeedData.AddHookWithOffset(_db.Context, 1, "1", 1, "ToDelete");

            var controller = CreateController("admin-1");

            var result = await controller.DeleteHook(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_db.Context.Hooks);
            Assert.Empty(_db.Context.HookOffsets);

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(5, log.AcceptanceStateId);
            Assert.Contains("ToDelete", log.Diff);
        }

        [Fact]
        public async Task GetHook_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();

            var result = await controller.GetHook(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
