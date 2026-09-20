using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class SeriesControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public SeriesControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private SeriesController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new SeriesController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task CreateSeries_NonModder_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController("user-1");

            var result = await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "New Series" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateSeries_Modder_CreatesSeriesAndSubmittedLog()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            var controller = CreateController("modder-1");

            var result = await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "New Series" });

            Assert.IsType<CreatedAtActionResult>(result);
            var series = Assert.Single(_db.Context.Series);
            Assert.Equal("New Series", series.SeriesName);
            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(2, log.AcceptanceStateId); // non-admin submission is "pending"
        }

        [Fact]
        public async Task CreateSeries_Admin_UsesAdminApprovedState()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            var controller = CreateController("admin-1");

            await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "Admin Series" });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(7, log.AcceptanceStateId);
        }

        [Fact]
        public async Task CreateSeries_DuplicateName_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Existing" });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "existing" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task PatchSeriesImage_FillsEmptyIcon()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            var controller = CreateController("modder-1");

            var result = await controller.PatchSeriesImage(1, new SeriesImageDto { SeriesIconUrl = "/uploads/icon.png" });

            Assert.IsType<NoContentResult>(result);
            var series = await _db.Context.Series.FindAsync(1);
            Assert.Equal("/uploads/icon.png", series!.SeriesIconUrl);
            Assert.Empty(_db.Context.ActionLogs);
        }

        [Fact]
        public async Task PatchSeriesImage_AlreadySet_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original", SeriesIconUrl = "/uploads/existing.png" });
            _db.Context.SaveChanges();
            var controller = CreateController("modder-1");

            var result = await controller.PatchSeriesImage(1, new SeriesImageDto { SeriesIconUrl = "/uploads/new.png" });

            Assert.IsType<ConflictObjectResult>(result);
            var series = await _db.Context.Series.FindAsync(1);
            Assert.Equal("/uploads/existing.png", series!.SeriesIconUrl);
        }

        [Fact]
        public async Task PatchSeriesImage_NonModder_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            var controller = CreateController("user-1");

            var result = await controller.PatchSeriesImage(1, new SeriesImageDto { SeriesIconUrl = "/uploads/icon.png" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateSeries_NotYetActionable_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.PendingAdminSoft, DateTime.UtcNow, userId: "modder-1", itemTypeId: ItemTypes.Series);

            var controller = CreateController("modder-1");

            var result = await controller.UpdateSeries(1, new UpdateSeriesDto { SeriesName = "Renamed" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateSeries_AfterAcceptance_UpdatesAndLogsDiff()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.PendingUserSoft, DateTime.UtcNow, userId: "modder-1", itemTypeId: ItemTypes.Series);

            var controller = CreateController("modder-1");

            var result = await controller.UpdateSeries(1, new UpdateSeriesDto { SeriesName = "Renamed" });

            Assert.IsType<NoContentResult>(result);
            var series = await _db.Context.Series.FindAsync(1);
            Assert.Equal("Renamed", series!.SeriesName);
        }

        [Fact]
        public async Task DeleteSeries_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: UserTypes.Modder);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "ToDelete" });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.DeleteSeries(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteSeries_Admin_RemovesSeries()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "ToDelete" });
            _db.Context.SaveChanges();

            var controller = CreateController("admin-1");

            var result = await controller.DeleteSeries(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_db.Context.Series);
        }

        [Fact]
        public async Task GetOneSeries_PrivateSeriesWithNoPublicMovesets_ReturnsForbidForStranger()
        {
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Hidden" });
            _db.Context.Movesets.Add(new Moveset { MovesetId = 1, ModdedCharName = "Secret", VanillaCharInternalName = "mario", SlottedId = "slotone", SeriesId = 1, PrivateMoveset = true, ReleaseStateId = ReleaseStates.Released });
            _db.Context.SaveChanges();

            var controller = CreateController();

            var result = await controller.GetOneSeries(1);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetSeries_AdminCountsBlockedMovesetsOnlyWithIncludeHidden()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);
            _db.Context.Series.Add(new Series { SeriesId = 100, SeriesName = "Custom" });
            _db.Context.Movesets.Add(new Moveset { MovesetId = 1, ModdedCharName = "Rejected", VanillaCharInternalName = "mario", SlottedId = "slotone", SeriesId = 100, ReleaseStateId = ReleaseStates.Released });
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: AcceptanceStates.Rejected, DateTime.UtcNow, userId: "log-author");

            var controller = CreateController("admin-1");

            static int CountFor(IActionResult result)
            {
                var ok = Assert.IsType<OkObjectResult>(result);
                var row = ((IEnumerable<object>)ok.Value!).Single();
                return (int)row.GetType().GetProperty("MovesetCount")!.GetValue(row)!;
            }

            Assert.Equal(0, CountFor(await controller.GetSeries()));
            Assert.Equal(1, CountFor(await controller.GetSeries(includeHidden: true)));
        }

        [Fact]
        public async Task RequestSeriesEdit_EditorOfMovesetInSeries_IsAllowed()
        {
            var editor = SeedData.AddUser(_db.Context, "editor-1", userTypeId: UserTypes.Modder, modderId: 3);
            SeedData.AddModder(_db.Context, 3, editor.Id, "Editor");
            _db.Context.Series.Add(new Series { SeriesId = 100, SeriesName = "Custom" });
            _db.Context.Movesets.Add(new Moveset { MovesetId = 1, ModdedCharName = "Edited", VanillaCharInternalName = "mario", SlottedId = "slotone", SeriesId = 100, ReleaseStateId = ReleaseStates.Released });
            _db.Context.MovesetEditors.Add(new MovesetEditor { MovesetId = 1, ModderId = 3, FullAccess = false });
            _db.Context.SaveChanges();

            var controller = CreateController("editor-1");

            var request = await controller.RequestSeriesEdit(100, new RequestEditSeriesDto { Notes = "Please" });
            Assert.IsType<OkObjectResult>(request);

            var detail = Assert.IsType<OkObjectResult>((await controller.GetOneSeries(100)).Result);
            Assert.True(Assert.IsType<ReturnSeriesDto>(detail.Value).UserOwnsMoveset);
        }

        [Fact]
        public async Task GetOneSeries_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();

            var result = await controller.GetOneSeries(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
