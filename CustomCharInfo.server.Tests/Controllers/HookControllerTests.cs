using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class HookControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public HookControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
            _db.Context.HookableStatuses.Add(new HookableStatus { HookableStatusId = 1, Name = "Confirmed" });
            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private HookController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new HookController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task CreateHook_NonModder_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            var controller = CreateController("user-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = 1 });

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task CreateHook_Modder_PersistsHook()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            var controller = CreateController("modder-1");

            var result = await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = 1 });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(_db.Context.Hooks);
        }

        [Fact]
        public async Task CreateHook_Modder_WritesActionLogAsPendingAdminSoft()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            var controller = CreateController("modder-1");

            await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = 1 });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(1, log.AcceptanceStateId);
            Assert.Equal("modder-1", log.UserId);
            Assert.Contains("0x1234", log.Diff);
        }

        [Fact]
        public async Task CreateHook_Admin_StillWritesActionLogAsPendingAdminSoft()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var controller = CreateController("admin-1");

            await controller.CreateHook(new CreateHookDto { Offset = "0x1234", Description = "Test", HookableStatusId = 1 });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(1, log.AcceptanceStateId);
        }

        [Fact]
        public async Task UpdateHook_UnknownId_ReturnsNotFound()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            var controller = CreateController("modder-1");

            var result = await controller.UpdateHook(999, new UpdateHookDto { Description = "New" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateHook_PartialDto_OnlyUpdatesProvidedFields()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "0x1", Description = "Old", HookableStatusId = 1 });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.UpdateHook(1, new UpdateHookDto { Description = "New Description" });

            Assert.IsType<NoContentResult>(result);
            var hook = await _db.Context.Hooks.FindAsync(1);
            Assert.Equal("New Description", hook!.Description);
            Assert.Equal("0x1", hook.Offset);

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(4, log.ItemTypeId);
            Assert.Equal(1, log.ItemId);
            Assert.Equal(1, log.AcceptanceStateId);
            Assert.Contains("Description", log.Diff);
            Assert.DoesNotContain("Offset", log.Diff);
        }

        [Fact]
        public async Task DeleteHook_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "0x1", Description = "ToDelete", HookableStatusId = 1 });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.DeleteHook(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteHook_Admin_RemovesHook()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            _db.Context.Hooks.Add(new Hook { HookId = 1, Offset = "0x1", Description = "ToDelete", HookableStatusId = 1 });
            _db.Context.SaveChanges();

            var controller = CreateController("admin-1");

            var result = await controller.DeleteHook(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_db.Context.Hooks);

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
