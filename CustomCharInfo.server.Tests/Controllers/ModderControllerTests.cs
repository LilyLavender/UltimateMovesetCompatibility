using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class ModderControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public ModderControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private ModderController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new ModderController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task CreateModder_AlreadyAModder_ReturnsBadRequest()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder, modderId: 1);
            var controller = CreateController("user-1");

            var result = await controller.CreateModder(new CreateModderDto());

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateModder_HasPendingApplication_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            SeedData.AddActionLog(_db.Context, itemId: 999, acceptanceStateId: AcceptanceStates.PendingAdminHard, DateTime.UtcNow, userId: "user-1", itemTypeId: ItemTypes.Modder);

            var controller = CreateController("user-1");

            var result = await controller.CreateModder(new CreateModderDto());

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateModder_NewApplicant_CreatesModderAndSubmittedLog()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController("user-1");

            var result = await controller.CreateModder(new CreateModderDto { Bio = "Hello" });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var modder = Assert.Single(_db.Context.Modders);
            Assert.Equal("Hello", modder.Bio);
            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(2, log.AcceptanceStateId);
        }

        [Fact]
        public async Task UpdateModder_UnrelatedUser_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "owner-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, "owner-1", "Owner");
            SeedData.AddUser(_db.Context, "stranger-1", userTypeId: UserTypes.User);

            var controller = CreateController("stranger-1");

            var result = await controller.UpdateModder(1, new UpdateModderDto { Bio = "hacked" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateModder_Owner_UpdatesProfile()
        {
            var owner = SeedData.AddUser(_db.Context, "owner-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, owner.Id, "Owner");

            var controller = CreateController(owner.Id);

            var result = await controller.UpdateModder(1, new UpdateModderDto { Bio = "Updated bio" });

            Assert.IsType<NoContentResult>(result);
            var modder = await _db.Context.Modders.FindAsync(1);
            Assert.Equal("Updated bio", modder!.Bio);
        }

        [Fact]
        public async Task UpdateModder_RejectedProfile_ReturnsForbid()
        {
            var owner = SeedData.AddUser(_db.Context, "owner-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, owner.Id, "Owner");
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.Rejected, DateTime.UtcNow, userId: owner.Id, itemTypeId: ItemTypes.Modder);

            var controller = CreateController(owner.Id);

            var result = await controller.UpdateModder(1, new UpdateModderDto { Bio = "Updated bio" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateModder_Admin_UsesAdminApprovedState()
        {
            var owner = SeedData.AddUser(_db.Context, "owner-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, owner.Id, "Owner");
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);

            var controller = CreateController(admin.Id);

            await controller.UpdateModder(1, new UpdateModderDto { Bio = "Admin edit" });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(7, log.AcceptanceStateId);
        }

        [Fact]
        public async Task DeleteModder_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.Modder);
            _db.Context.Modders.Add(new Modder { ModderId = 1, UserId = "user-1", Name = "ToDelete" });
            _db.Context.SaveChanges();

            var controller = CreateController("user-1");

            var result = await controller.DeleteModder(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteModder_Admin_RemovesModder()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            SeedData.AddUser(_db.Context, "target-1", userTypeId: UserTypes.Modder);
            _db.Context.Modders.Add(new Modder { ModderId = 1, UserId = "target-1", Name = "ToDelete" });
            _db.Context.SaveChanges();

            var controller = CreateController("admin-1");

            var result = await controller.DeleteModder(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_db.Context.Modders);
        }

        [Fact]
        public async Task GetModder_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();

            var result = await controller.GetModder(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetModders_AdminSeesBlockedModdersOnlyWithIncludeHidden()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var owner = SeedData.AddUser(_db.Context, "owner-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, owner.Id, "Owner");
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.Rejected, DateTime.UtcNow, userId: owner.Id, itemTypeId: ItemTypes.Modder);

            var controller = CreateController("admin-1");

            var hidden = Assert.IsType<OkObjectResult>(await controller.GetModders());
            Assert.Empty((IEnumerable<object>)hidden.Value!);

            var shown = Assert.IsType<OkObjectResult>(await controller.GetModders(includeHidden: true));
            Assert.Single((IEnumerable<object>)shown.Value!);
        }

        [Fact]
        public async Task GetModder_BlockedForNonAdminStranger()
        {
            var owner = SeedData.AddUser(_db.Context, "owner-1", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, owner.Id, "Owner");
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.Rejected, DateTime.UtcNow, userId: owner.Id, itemTypeId: ItemTypes.Modder);

            var controller = CreateController();

            var result = await controller.GetModder(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ModderIsAdmin_NoAssociatedUser_ReturnsFalse()
        {
            var controller = CreateController();

            var result = await controller.ModderIsAdmin(999);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var isAdmin = (bool)ok.Value!.GetType().GetProperty("isAdmin")!.GetValue(ok.Value)!;
            Assert.False(isAdmin);
        }

        [Fact]
        public async Task ModderIsAdmin_AdminUser_ReturnsTrue()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin, modderId: 42);

            var controller = CreateController();

            var result = await controller.ModderIsAdmin(42);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var isAdmin = (bool)ok.Value!.GetType().GetProperty("isAdmin")!.GetValue(ok.Value)!;
            Assert.True(isAdmin);
        }
    }
}
