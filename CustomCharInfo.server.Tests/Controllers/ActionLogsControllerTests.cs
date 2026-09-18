using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class ActionLogsControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public ActionLogsControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private ActionLogsController CreateController(string? currentUserId, bool withClaim = false)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new ActionLogsController(_db.Context, userManager.Object);
            if (withClaim && currentUserId != null)
                controller.SetFakeUser(currentUserId);
            else
                controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task GetActionLogs_NoAuthenticatedUser_ReturnsForbid()
        {
            var controller = CreateController(currentUserId: null);

            var result = await controller.GetActionLogs();

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLogs_NonAdminRequestingViewAll_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "regular-1", userTypeId: UserTypes.User);
            var controller = CreateController("regular-1");

            var result = await controller.GetActionLogs(viewAll: true);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLogs_NonAdminRequestingAnotherUsersLogs_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "regular-1", userTypeId: UserTypes.User);
            var controller = CreateController("regular-1");

            var result = await controller.GetActionLogs(targetUserId: "someone-else");

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLogs_AdminViewAll_ReturnsEveryLog()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var owner = SeedData.AddUser(_db.Context, "modder-user", userTypeId: UserTypes.Modder, modderId: 10);
            SeedData.AddModder(_db.Context, 10, owner.Id, "SomeModder");
            SeedData.AddActionLog(_db.Context, itemId: 10, acceptanceStateId: AcceptanceStates.PendingAdminSoft, DateTime.UtcNow, userId: owner.Id);

            var controller = CreateController("admin-1");

            // Action logs of type 2 (modder) need ItemTypeId = ItemTypes.Modder to be attributed correctly.
            var log = _db.Context.ActionLogs.First();
            log.ItemTypeId = ItemTypes.Modder;
            _db.Context.SaveChanges();

            var result = await controller.GetActionLogs(viewAll: true);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var logs = (IEnumerable<GetActionLogDto>)ok.Value!;
            Assert.Single(logs);
        }

        [Fact]
        public async Task GetActionLogs_ModderScopedToOwnMovesetLogs()
        {
            var owner = SeedData.AddUser(_db.Context, "modder-user", userTypeId: UserTypes.Modder, modderId: 20);
            SeedData.AddModder(_db.Context, 20, owner.Id, "MyModder");

            _db.Context.Movesets.Add(new Models.Moveset { MovesetId = 1, ModdedCharName = "Owned", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released });
            _db.Context.Movesets.Add(new Models.Moveset { MovesetId = 2, ModdedCharName = "NotOwned", VanillaCharInternalName = "mario", SlottedId = "slottwo", ReleaseStateId = ReleaseStates.Released });
            _db.Context.MovesetModders.Add(new Models.MovesetModder { MovesetId = 1, ModderId = 20, SortOrder = 0 });
            _db.Context.SaveChanges();

            var log1 = new ActionLog { ItemTypeId = ItemTypes.Moveset, ItemId = 1, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, UserId = owner.Id, Notes = "", CreatedAt = DateTime.UtcNow };
            var log2 = new ActionLog { ItemTypeId = ItemTypes.Moveset, ItemId = 2, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, UserId = owner.Id, Notes = "", CreatedAt = DateTime.UtcNow };
            _db.Context.ActionLogs.AddRange(log1, log2);
            _db.Context.SaveChanges();

            var controller = CreateController(owner.Id);

            var result = await controller.GetActionLogs();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var logs = ((IEnumerable<GetActionLogDto>)ok.Value!).ToList();
            Assert.Single(logs);
            var movesetId = (int)logs[0].Item!.GetType().GetProperty("MovesetId")!.GetValue(logs[0].Item)!;
            Assert.Equal(1, movesetId);
        }

        [Fact]
        public async Task GetActionLogs_ModderSeesHookTheyveEditedBefore()
        {
            var owner = SeedData.AddUser(_db.Context, "modder-user", userTypeId: UserTypes.Modder, modderId: 20);
            SeedData.AddModder(_db.Context, 20, owner.Id, "MyModder");
            var otherUser = SeedData.AddUser(_db.Context, "someone-else-1", userTypeId: UserTypes.Modder);

            var editedLog = new ActionLog { ItemTypeId = ItemTypes.Hook, ItemId = 1, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, UserId = owner.Id, Notes = "", CreatedAt = DateTime.UtcNow };
            var otherHookLog = new ActionLog { ItemTypeId = ItemTypes.Hook, ItemId = 2, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, UserId = otherUser.Id, Notes = "", CreatedAt = DateTime.UtcNow };
            _db.Context.ActionLogs.AddRange(editedLog, otherHookLog);
            _db.Context.SaveChanges();

            var controller = CreateController(owner.Id);

            var result = await controller.GetActionLogs();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var logs = ((IEnumerable<GetActionLogDto>)ok.Value!).ToList();
            Assert.Single(logs);
            Assert.Equal(editedLog.ActionLogId, logs[0].ActionLogId);
        }

        [Fact]
        public async Task GetActionLogs_NonModderUserSeesHookTheyveEditedBefore()
        {
            // Admins can edit hooks too, so a non-modder admin's own hook edits must still show
            // up in their own (non-viewAll) log list.
            var admin = SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var otherUser = SeedData.AddUser(_db.Context, "someone-else-1", userTypeId: UserTypes.Modder);

            var editedLog = new ActionLog { ItemTypeId = ItemTypes.Hook, ItemId = 1, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, UserId = admin.Id, Notes = "", CreatedAt = DateTime.UtcNow };
            var otherHookLog = new ActionLog { ItemTypeId = ItemTypes.Hook, ItemId = 2, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, UserId = otherUser.Id, Notes = "", CreatedAt = DateTime.UtcNow };
            _db.Context.ActionLogs.AddRange(editedLog, otherHookLog);
            _db.Context.SaveChanges();

            var controller = CreateController(admin.Id);

            var result = await controller.GetActionLogs();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var logs = ((IEnumerable<GetActionLogDto>)ok.Value!).ToList();
            Assert.Single(logs);
            Assert.Equal(editedLog.ActionLogId, logs[0].ActionLogId);
        }

        [Fact]
        public async Task GetActionLogsByItem_NoAuthenticatedUser_ReturnsForbid()
        {
            var controller = CreateController(null);

            var result = await controller.GetActionLogsByItem(1, 999);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLogsByItem_NonOwningModder_ReturnsForbid()
        {
            var owner = SeedData.AddUser(_db.Context, "modder-user", userTypeId: UserTypes.Modder, modderId: 20);
            SeedData.AddModder(_db.Context, 20, owner.Id, "MyModder");
            _db.Context.Movesets.Add(new Models.Moveset { MovesetId = 1, ModdedCharName = "NotOwned", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released });
            _db.Context.SaveChanges();

            var controller = CreateController(owner.Id);

            var result = await controller.GetActionLogsByItem(1, 1);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLogsByItem_Admin_ReturnsEmptyArray_WhenNoLogsExist()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            var result = await controller.GetActionLogsByItem(1, 999);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var logs = (IEnumerable<GetActionLogDto>)ok.Value!;
            Assert.Empty(logs);
        }

        [Fact]
        public async Task CreateActionLog_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "regular-1", userTypeId: UserTypes.User);
            var controller = CreateController("regular-1", withClaim: true);

            var dto = new ActionLogDto { UserId = "regular-1", ItemTypeId = ItemTypes.Moveset, ItemId = 1, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, Notes = "" };

            var result = await controller.CreateActionLog(dto);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task CreateActionLog_Admin_PersistsLog()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1", withClaim: true);

            var dto = new ActionLogDto { UserId = "admin-1", ItemTypeId = ItemTypes.Moveset, ItemId = 1, AcceptanceStateId = AcceptanceStates.PendingAdminSoft, Notes = "Looks good" };

            var result = await controller.CreateActionLog(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(_db.Context.ActionLogs);
        }

        [Fact]
        public async Task CreateActionLog_AcceptingModderApplication_PromotesOriginalSubmitterToModder()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var applicant = SeedData.AddUser(_db.Context, "applicant-1", userTypeId: UserTypes.User);
            SeedData.AddModder(_db.Context, 30, userId: applicant.Id, name: "NewModder");
            // Clear the auto-linked UserId so CreateActionLog has to (re)establish it.
            var modder = _db.Context.Modders.Single(m => m.ModderId == 30);
            modder.UserId = null!;
            applicant.ModderId = null;
            _db.Context.SaveChanges();

            SeedData.AddActionLog(_db.Context, itemId: 30, acceptanceStateId: AcceptanceStates.PendingAdminSoft, DateTime.UtcNow.AddMinutes(-5), userId: applicant.Id);
            var initialLog = _db.Context.ActionLogs.First();
            initialLog.ItemTypeId = ItemTypes.Modder;
            _db.Context.SaveChanges();

            var controller = CreateController("admin-1", withClaim: true);
            var dto = new ActionLogDto { UserId = "admin-1", ItemTypeId = ItemTypes.Modder, ItemId = 30, AcceptanceStateId = AcceptanceStates.Accepted, Notes = "Approved" };

            await controller.CreateActionLog(dto);

            var updatedApplicant = await _db.Context.Users.FindAsync(applicant.Id);
            var updatedModder = await _db.Context.Modders.FindAsync(30);

            Assert.Equal(30, updatedApplicant!.ModderId);
            Assert.Equal(2, updatedApplicant.UserTypeId);
            Assert.Equal(applicant.Id, updatedModder!.UserId);
        }

        [Fact]
        public async Task GetActionLog_NoAuthenticatedUser_ReturnsForbid()
        {
            var author = SeedData.AddUser(_db.Context, "author-1", userTypeId: UserTypes.User);
            _db.Context.Movesets.Add(new Models.Moveset { MovesetId = 1, ModdedCharName = "Some Moveset", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released });
            _db.Context.SaveChanges();
            var log = SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.PendingAdminSoft, DateTime.UtcNow, userId: author.Id);

            var controller = CreateController(null);

            var result = await controller.GetActionLog(log.ActionLogId);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLog_UnknownId_ReturnsNotFound()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            var result = await controller.GetActionLog(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetActionLog_KnownMovesetLog_IncludesItemDetails()
        {
            var author = SeedData.AddUser(_db.Context, "author-1", userTypeId: UserTypes.User);
            author.Email = "author@example.com";
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            _db.Context.Movesets.Add(new Models.Moveset { MovesetId = 1, ModdedCharName = "Some Moveset", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released });
            _db.Context.SaveChanges();
            var log = SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.PendingAdminSoft, DateTime.UtcNow, userId: author.Id);

            var controller = CreateController("admin-1");

            var result = await controller.GetActionLog(log.ActionLogId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = (GetActionLogDto)ok.Value!;
            Assert.NotNull(dto.Item);
            Assert.Equal("author@example.com", dto.User.Email);
        }

        [Fact]
        public async Task GetActionLog_NonAdminViewer_EmailIsHidden()
        {
            var author = SeedData.AddUser(_db.Context, "author-1", userTypeId: UserTypes.User);
            var owner = SeedData.AddUser(_db.Context, "modder-user", userTypeId: UserTypes.Modder, modderId: 20);
            SeedData.AddModder(_db.Context, 20, owner.Id, "MyModder");
            _db.Context.Movesets.Add(new Models.Moveset { MovesetId = 1, ModdedCharName = "Some Moveset", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released });
            _db.Context.MovesetModders.Add(new Models.MovesetModder { MovesetId = 1, ModderId = 20, SortOrder = 0 });
            _db.Context.SaveChanges();
            var log = SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.PendingAdminSoft, DateTime.UtcNow, userId: author.Id);

            var controller = CreateController(owner.Id);

            var result = await controller.GetActionLog(log.ActionLogId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = (GetActionLogDto)ok.Value!;
            Assert.Null(dto.User.Email);
        }
    }
}
