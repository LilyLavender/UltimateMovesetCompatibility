using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class CompatibilityBatchReportTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();
        private const string UserId = "user-1";

        public CompatibilityBatchReportTests()
        {
            SeedData.SeedLookups(_db.Context);
            SeedData.AddUser(_db.Context, UserId, UserTypes.User);
            SeedData.AddUser(_db.Context, "user-2", UserTypes.User);
        }

        public void Dispose() => _db.Dispose();

        private CompatibilityController CreateController(string? currentUserId = UserId)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new CompatibilityController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        private void AddMovesets(params int[] ids)
        {
            foreach (var id in ids)
            {
                _db.Context.Movesets.Add(new Moveset
                {
                    MovesetId = id,
                    ModdedCharName = $"Char{id}",
                    VanillaCharInternalName = "mario",
                    SlottedId = $"slot{(char)('a' + id)}",
                    ReleaseStateId = ReleaseStates.Released
                });
            }
            _db.Context.SaveChanges();
        }

        private void AddReport(int lo, int hi, bool isCompatible, string userId = UserId)
        {
            _db.Context.CompatibilityReports.Add(new CompatibilityReport
            {
                MovesetId1 = lo,
                MovesetId2 = hi,
                UserId = userId,
                IsCompatible = isCompatible,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            _db.Context.SaveChanges();
        }

        private static T? Prop<T>(object item, string name) =>
            (T?)item.GetType().GetProperty(name)!.GetValue(item);

        private static BatchCompatibilityReportDto Dto(params int[] ids) =>
            new() { MovesetIds = ids.ToList() };

        [Fact]
        public async Task Batch_ThreeMovesets_CreatesEveryPairCompatible()
        {
            AddMovesets(1, 2, 3);

            var result = await CreateController().SubmitBatchCompatible(Dto(3, 1, 2));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(3, Prop<int>(ok.Value!, "PairCount"));
            Assert.Equal(3, Prop<int>(ok.Value!, "Created"));
            Assert.Equal(0, Prop<int>(ok.Value!, "Updated"));

            var reports = await _db.Context.CompatibilityReports.OrderBy(r => r.MovesetId1).ThenBy(r => r.MovesetId2).ToListAsync();
            Assert.Equal(new[] { (1, 2), (1, 3), (2, 3) }, reports.Select(r => (r.MovesetId1, r.MovesetId2)));
            Assert.All(reports, r => Assert.True(r.IsCompatible));
            Assert.All(reports, r => Assert.Equal(UserId, r.UserId));
        }

        [Fact]
        public async Task Batch_ExistingIncompatibleVote_IsFlippedToCompatible()
        {
            AddMovesets(1, 2);
            AddReport(1, 2, isCompatible: false);

            var result = await CreateController().SubmitBatchCompatible(Dto(1, 2));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, Prop<int>(ok.Value!, "Created"));
            Assert.Equal(1, Prop<int>(ok.Value!, "Updated"));
            var report = await _db.Context.CompatibilityReports.SingleAsync();
            Assert.True(report.IsCompatible);
        }

        [Fact]
        public async Task Batch_ExistingCompatibleVote_IsLeftAlone()
        {
            AddMovesets(1, 2);
            AddReport(1, 2, isCompatible: true);
            var before = (await _db.Context.CompatibilityReports.SingleAsync()).CreatedAt;

            var result = await CreateController().SubmitBatchCompatible(Dto(1, 2));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, Prop<int>(ok.Value!, "Created"));
            Assert.Equal(0, Prop<int>(ok.Value!, "Updated"));
            var report = await _db.Context.CompatibilityReports.SingleAsync();
            Assert.True(report.IsCompatible);
            Assert.Equal(before, report.CreatedAt);
        }

        [Fact]
        public async Task Batch_DoesNotTouchOtherUsersVotes()
        {
            AddMovesets(1, 2);
            AddReport(1, 2, isCompatible: false, userId: "user-2");

            await CreateController().SubmitBatchCompatible(Dto(1, 2));

            var other = await _db.Context.CompatibilityReports.SingleAsync(r => r.UserId == "user-2");
            Assert.False(other.IsCompatible);
            Assert.Equal(2, await _db.Context.CompatibilityReports.CountAsync());
        }

        [Fact]
        public async Task Batch_FewerThanTwo_ReturnsBadRequest()
        {
            AddMovesets(1);
            Assert.IsType<BadRequestObjectResult>(await CreateController().SubmitBatchCompatible(Dto(1)));
        }

        [Fact]
        public async Task Batch_MoreThanTwentyFive_ReturnsBadRequest()
        {
            var ids = Enumerable.Range(1, 26).ToArray();
            AddMovesets(ids);
            Assert.IsType<BadRequestObjectResult>(await CreateController().SubmitBatchCompatible(Dto(ids)));
        }

        [Fact]
        public async Task Batch_Duplicates_ReturnsBadRequest()
        {
            AddMovesets(1, 2);
            Assert.IsType<BadRequestObjectResult>(await CreateController().SubmitBatchCompatible(Dto(1, 2, 1)));
        }

        [Fact]
        public async Task Batch_MissingMoveset_ReturnsNotFound()
        {
            AddMovesets(1, 2);
            Assert.IsType<NotFoundObjectResult>(await CreateController().SubmitBatchCompatible(Dto(1, 2, 99)));
            Assert.Equal(0, await _db.Context.CompatibilityReports.CountAsync());
        }

        [Fact]
        public async Task Batch_Anonymous_ReturnsUnauthorized()
        {
            AddMovesets(1, 2);
            Assert.IsType<UnauthorizedResult>(await CreateController(currentUserId: null).SubmitBatchCompatible(Dto(1, 2)));
        }
    }
}
