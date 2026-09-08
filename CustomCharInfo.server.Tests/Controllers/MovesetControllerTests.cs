using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class MovesetControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public MovesetControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private MovesetController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new MovesetController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        private Moveset AddMoveset(int id, string name, int releaseStateId = 1, bool isPrivate = false, bool? adminPick = null, DateTime? releaseDate = null)
        {
            var moveset = new Moveset
            {
                MovesetId = id,
                ModdedCharName = name,
                VanillaCharInternalName = "mario",
                ReleaseStateId = releaseStateId,
                PrivateMoveset = isPrivate,
                AdminPick = adminPick,
                ReleaseDate = releaseDate
            };
            _db.Context.Movesets.Add(moveset);
            _db.Context.SaveChanges();
            return moveset;
        }

        // The controller projects results into anonymous types, which are `internal` to the
        // server assembly - `dynamic` member access fails across the assembly boundary, so we
        // read properties via reflection instead.
        private static List<object> Unwrap(ActionResult<IEnumerable<object>> result)
        {
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            return ((IEnumerable<object>)ok.Value!).ToList();
        }

        private static T? Prop<T>(object item, string name)
        {
            var value = item.GetType().GetProperty(name)!.GetValue(item);
            return (T?)value;
        }

        [Fact]
        public async Task GetMovesets_AnonymousUser_HidesBlockedAcceptanceStates()
        {
            AddMoveset(1, "Visible One");
            var blocked = AddMoveset(2, "Rejected One");
            SeedData.AddActionLog(_db.Context, blocked.MovesetId, acceptanceStateId: 6, DateTime.UtcNow, userId: "log-author");

            var controller = CreateController();

            var result = await controller.GetMovesets(null, null, null, null, null, null, null, null, null);

            var movesets = Unwrap(result);
            Assert.Single(movesets);
            Assert.Equal("Visible One", Prop<string>(movesets[0], "ModdedCharName"));
        }

        [Fact]
        public async Task GetMovesets_AdminUser_SeesBlockedAcceptanceStates()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var blocked = AddMoveset(1, "Rejected One");
            SeedData.AddActionLog(_db.Context, blocked.MovesetId, acceptanceStateId: 6, DateTime.UtcNow, userId: "admin-1");

            var controller = CreateController("admin-1");

            var result = await controller.GetMovesets(null, null, null, null, null, null, null, null, null);

            Assert.Single(Unwrap(result));
        }

        [Fact]
        public async Task GetMovesets_OwnerModder_SeesOwnBlockedMoveset()
        {
            var owner = SeedData.AddUser(_db.Context, "owner-1", userTypeId: 2, modderId: 5);
            SeedData.AddModder(_db.Context, 5, owner.Id, "OwnerModder");
            var moveset = AddMoveset(1, "My Rejected Moveset");
            _db.Context.MovesetModders.Add(new MovesetModder { MovesetId = moveset.MovesetId, ModderId = 5, SortOrder = 0 });
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, moveset.MovesetId, acceptanceStateId: 6, DateTime.UtcNow, userId: owner.Id);

            var controller = CreateController(owner.Id);

            var result = await controller.GetMovesets(null, null, null, null, null, null, null, null, null);

            Assert.Single(Unwrap(result));
        }

        [Fact]
        public async Task GetMovesets_PrivateOnlyFilter_ReturnsOnlyPrivateMovesets()
        {
            AddMoveset(1, "Public", isPrivate: false);
            AddMoveset(2, "Private", isPrivate: true);

            var controller = CreateController();

            var result = await controller.GetMovesets(null, null, null, null, privateOnly: true, null, null, null, null);

            var movesets = Unwrap(result);
            Assert.Single(movesets);
            Assert.True(Prop<bool?>(movesets[0], "PrivateMoveset"));
        }

        [Fact]
        public async Task GetMovesets_AdminPickOnlyFilter_ReturnsOnlyAdminPicks()
        {
            AddMoveset(1, "Regular", adminPick: false);
            AddMoveset(2, "Featured", adminPick: true);

            var controller = CreateController();

            var result = await controller.GetMovesets(null, null, null, null, null, adminPickOnly: true, null, null, null);

            var movesets = Unwrap(result);
            Assert.Single(movesets);
            Assert.Equal("Featured", Prop<string>(movesets[0], "ModdedCharName"));
        }

        [Fact]
        public async Task GetMovesets_BetaOnlyFilter_MatchesReleaseState4()
        {
            AddMoveset(1, "Released", releaseStateId: 1);
            AddMoveset(2, "InBeta", releaseStateId: 4);

            var controller = CreateController();

            var result = await controller.GetMovesets(null, null, null, null, null, null, null, null, betaOnly: true);

            var movesets = Unwrap(result);
            Assert.Single(movesets);
            Assert.Equal("InBeta", Prop<string>(movesets[0], "ModdedCharName"));
        }

        [Fact]
        public async Task GetMovesets_UpcomingOnlyFilter_MatchesFutureReleaseDate()
        {
            AddMoveset(1, "Past", releaseDate: DateTime.UtcNow.AddDays(-5));
            AddMoveset(2, "Future", releaseDate: DateTime.UtcNow.AddDays(5));

            var controller = CreateController();

            var result = await controller.GetMovesets(null, null, null, null, null, null, upcomingOnly: true, null, null);

            var movesets = Unwrap(result);
            Assert.Single(movesets);
            Assert.Equal("Future", Prop<string>(movesets[0], "ModdedCharName"));
        }

        [Fact]
        public async Task GetMovesets_PrivateMoveset_NamesAreRedactedForNonOwners()
        {
            AddMoveset(1, "Secret Character", isPrivate: true);

            var controller = CreateController();

            var result = await controller.GetMovesets(null, null, null, null, null, null, null, null, null);

            var movesets = Unwrap(result);
            Assert.Single(movesets);
            Assert.Equal("???", Prop<string>(movesets[0], "ModdedCharName"));
        }

        [Fact]
        public async Task GetMoveset_PrivateMoveset_ReturnsNotFoundForStranger()
        {
            AddMoveset(1, "Secret", isPrivate: true);

            var controller = CreateController();

            var result = await controller.GetMoveset("1");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetMoveset_RejectedMoveset_ForbidsNonOwnerNonAdmin()
        {
            var moveset = AddMoveset(1, "Rejected", isPrivate: false);
            SeedData.AddActionLog(_db.Context, moveset.MovesetId, acceptanceStateId: 6, DateTime.UtcNow, userId: "log-author");

            var controller = CreateController();

            var result = await controller.GetMoveset("1");

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetMoveset_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();

            var result = await controller.GetMoveset("999");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetMoveset_BySlottedId_FindsMoveset()
        {
            var moveset = AddMoveset(1, "SlotBased");
            moveset.SlottedId = "abc123";
            _db.Context.SaveChanges();

            var controller = CreateController();

            var result = await controller.GetMoveset("abc123");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }
    }
}
