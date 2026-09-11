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
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            var controller = CreateController("user-1");

            var result = await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "New Series" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateSeries_Modder_CreatesSeriesAndSubmittedLog()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
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
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var controller = CreateController("admin-1");

            await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "Admin Series" });

            var log = Assert.Single(_db.Context.ActionLogs);
            Assert.Equal(7, log.AcceptanceStateId);
        }

        [Fact]
        public async Task CreateSeries_DuplicateName_ReturnsConflict()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Existing" });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.CreateSeries(new UpdateSeriesDto { SeriesName = "existing" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task PatchSeriesImage_FillsEmptyIcon()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
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
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
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
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            var controller = CreateController("user-1");

            var result = await controller.PatchSeriesImage(1, new SeriesImageDto { SeriesIconUrl = "/uploads/icon.png" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateSeries_NotYetActionable_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: 1, DateTime.UtcNow, userId: "modder-1", itemTypeId: 3);

            var controller = CreateController("modder-1");

            var result = await controller.UpdateSeries(1, new UpdateSeriesDto { SeriesName = "Renamed" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateSeries_AfterAcceptance_UpdatesAndLogsDiff()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Original" });
            _db.Context.SaveChanges();
            SeedData.AddActionLog(_db.Context, itemId: 1, acceptanceStateId: 3, DateTime.UtcNow, userId: "modder-1", itemTypeId: 3);

            var controller = CreateController("modder-1");

            var result = await controller.UpdateSeries(1, new UpdateSeriesDto { SeriesName = "Renamed" });

            Assert.IsType<NoContentResult>(result);
            var series = await _db.Context.Series.FindAsync(1);
            Assert.Equal("Renamed", series!.SeriesName);
        }

        [Fact]
        public async Task DeleteSeries_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "modder-1", userTypeId: 2);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "ToDelete" });
            _db.Context.SaveChanges();

            var controller = CreateController("modder-1");

            var result = await controller.DeleteSeries(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteSeries_Admin_RemovesSeries()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
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
            _db.Context.Movesets.Add(new Moveset { MovesetId = 1, ModdedCharName = "Secret", VanillaCharInternalName = "mario", SeriesId = 1, PrivateMoveset = true, ReleaseStateId = 1 });
            _db.Context.SaveChanges();

            var controller = CreateController();

            var result = await controller.GetOneSeries(1);

            Assert.IsType<ForbidResult>(result.Result);
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
