using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class UserControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public UserControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private UserController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new UserController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task GetAllUsers_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            var controller = CreateController("user-1");

            var result = await controller.GetAllUsers();

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetAllUsers_PartitionsUsersAndModdersCorrectly()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);

            // In both: a user linked to a modder
            var linkedUser = SeedData.AddUser(_db.Context, "linked-1", userTypeId: 2, modderId: 10);
            SeedData.AddModder(_db.Context, 10, linkedUser.Id, "LinkedModder");

            // Only a user, no modder
            SeedData.AddUser(_db.Context, "solo-user-1", userTypeId: 1);

            // Only a modder, no user pointing at it (simulates the ApplicationUser.ModderId cache
            // desyncing from Modder.UserId - see ActionLogsController.cs's sync comment)
            var orphanOwner = SeedData.AddUser(_db.Context, "orphan-owner-1", userTypeId: 1);
            SeedData.AddModder(_db.Context, 20, orphanOwner.Id, "OrphanModder");

            var controller = CreateController("admin-1");

            var result = await controller.GetAllUsers();

            var ok = Assert.IsType<OkObjectResult>(result);
            var inBoth = (System.Collections.IEnumerable)ok.Value!.GetType().GetProperty("inBoth")!.GetValue(ok.Value)!;
            var onlyUsers = (System.Collections.IEnumerable)ok.Value!.GetType().GetProperty("onlyUsers")!.GetValue(ok.Value)!;
            var onlyModders = (System.Collections.IEnumerable)ok.Value!.GetType().GetProperty("onlyModders")!.GetValue(ok.Value)!;

            Assert.Single(inBoth.Cast<object>());
            // solo-user-1, admin-1, orphan-owner-1, and log-author (seeded by SeedLookups) all lack a modder link
            Assert.Equal(4, onlyUsers.Cast<object>().Count());
            Assert.Single(onlyModders.Cast<object>());
        }
    }
}
