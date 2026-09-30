using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    // Contribution roles and the show-on-card flag on credited modders.
    public class MovesetContributionRoleTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        private const int LeadModderId = 5;
        private const int SecondModderId = 6;

        public MovesetContributionRoleTests()
        {
            SeedData.SeedLookups(_db.Context);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Test Series" });
            _db.Context.SaveChanges();

            SeedData.AddUser(_db.Context, "lead-1", UserTypes.Modder, LeadModderId);
            SeedData.AddModder(_db.Context, LeadModderId, "lead-1", "Lead");
            SeedData.AddUser(_db.Context, "second-1", UserTypes.Modder, SecondModderId);
            SeedData.AddModder(_db.Context, SecondModderId, "second-1", "Second");
        }

        public void Dispose() => _db.Dispose();

        private MovesetController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new MovesetController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        private static CreateMovesetDto BaseDto() => new()
        {
            ModdedCharName = "Waluigi",
            VanillaCharInternalName = "mario",
            SeriesId = 1,
            SlottedId = "slotwaluigi",
            ReplacementId = "slotwaluigi",
            ReleaseStateId = ReleaseStates.Released,
            PrivateMoveset = false,
            DependencyIds = new List<int>(),
            Hooks = new List<MovesetHookDto>(),
            Articles = new List<MovesetArticleDto>()
        };

        private static List<MovesetModderDto> TwoCredits(bool secondOnCard = true) => new()
        {
            new() { ModderId = LeadModderId, RoleIds = new List<int> { ContributionRoles.Animation, ContributionRoles.Coding } },
            new() { ModderId = SecondModderId, RoleIds = new List<int> { ContributionRoles.Sounds }, ShowOnCard = secondOnCard }
        };

        private async Task<int> CreateWithTwoCredits(bool secondOnCard = true)
        {
            var dto = BaseDto();
            dto.Modders = TwoCredits(secondOnCard);
            var result = await CreateController("lead-1").PostMoveset(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            return Assert.IsType<Moveset>(created.Value).MovesetId;
        }

        // A create logs hard-pending, which hides the moveset from strangers and keeps later edits hard.
        private void Accept(int movesetId) =>
            SeedData.AddActionLog(_db.Context, movesetId, acceptanceStateId: AcceptanceStates.Accepted, DateTime.UtcNow, userId: "log-author");

        // Only edit logs carry a diff, so this skips the create and accept logs.
        private Task<ActionLog> EditLog() =>
            _db.Context.ActionLogs.Where(a => a.Diff != null).OrderByDescending(a => a.CreatedAt).FirstAsync();

        private static List<object> Unwrap(ActionResult<IEnumerable<object>> result)
        {
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            return ((IEnumerable<object>)ok.Value!).ToList();
        }

        private static T? Prop<T>(object item, string name) =>
            (T?)item.GetType().GetProperty(name)!.GetValue(item);

        [Fact]
        public async Task PostMoveset_WithRoles_StoresRolesAndCardFlagInOrder()
        {
            var id = await CreateWithTwoCredits(secondOnCard: false);

            var credits = await _db.Context.MovesetModders
                .Include(mm => mm.Roles)
                .Where(mm => mm.MovesetId == id)
                .OrderBy(mm => mm.SortOrder)
                .ToListAsync();

            Assert.Equal(new[] { LeadModderId, SecondModderId }, credits.Select(c => c.ModderId));
            Assert.Equal(new[] { ContributionRoles.Coding, ContributionRoles.Animation }, credits[0].Roles.Select(r => r.ContributionRoleId).OrderBy(r => r));
            Assert.True(credits[0].ShowOnCard);
            Assert.Equal(new[] { ContributionRoles.Sounds }, credits[1].Roles.Select(r => r.ContributionRoleId));
            Assert.False(credits[1].ShowOnCard);
        }

        [Fact]
        public async Task PostMoveset_ModderIdsOnly_StillWorksWithNoRolesAndOnCard()
        {
            var dto = BaseDto();
            dto.ModderIds = new List<int> { LeadModderId };

            var result = await CreateController("lead-1").PostMoveset(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var credit = await _db.Context.MovesetModders.Include(mm => mm.Roles).SingleAsync();
            Assert.Empty(credit.Roles);
            Assert.True(credit.ShowOnCard);
        }

        [Fact]
        public async Task PostMoveset_UnknownRole_ReturnsBadRequest()
        {
            var dto = BaseDto();
            dto.Modders = new List<MovesetModderDto> { new() { ModderId = LeadModderId, RoleIds = new List<int> { 99 } } };

            var result = await CreateController("lead-1").PostMoveset(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostMoveset_NobodyOnCard_ReturnsBadRequest()
        {
            var dto = BaseDto();
            dto.Modders = new List<MovesetModderDto> { new() { ModderId = LeadModderId, ShowOnCard = false } };

            var result = await CreateController("lead-1").PostMoveset(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostMoveset_NoCredits_ReturnsBadRequest()
        {
            var dto = BaseDto();
            dto.Modders = new List<MovesetModderDto>();

            var result = await CreateController("lead-1").PostMoveset(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutMoveset_ReplacesRolesAndLogsThemInTheDiff()
        {
            var id = await CreateWithTwoCredits();
            var dto = BaseDto();
            dto.Modders = new List<MovesetModderDto>
            {
                new() { ModderId = LeadModderId, RoleIds = new List<int> { ContributionRoles.Modelling }, ShowOnCard = true },
                new() { ModderId = SecondModderId, RoleIds = new List<int>(), ShowOnCard = false }
            };

            var result = await CreateController("lead-1").PutMoveset(id, dto);

            Assert.IsType<NoContentResult>(result);
            var credits = await _db.Context.MovesetModders.Include(mm => mm.Roles).Where(mm => mm.MovesetId == id).OrderBy(mm => mm.SortOrder).ToListAsync();
            Assert.Equal(new[] { ContributionRoles.Modelling }, credits[0].Roles.Select(r => r.ContributionRoleId));
            Assert.Empty(credits[1].Roles);
            Assert.False(credits[1].ShowOnCard);
            Assert.Equal(1, await _db.Context.MovesetModderRoles.CountAsync());

            var editLog = await EditLog();
            Assert.Contains("Lead (Modelling)", editLog.Diff);
            Assert.Contains("Second (hidden from card)", editLog.Diff);
            Assert.Contains("Lead (Coding, Animation)", editLog.Diff);
        }

        [Fact]
        public async Task PutMoveset_RoleChangeAlone_IsASoftEdit()
        {
            var id = await CreateWithTwoCredits();
            Accept(id);
            var dto = BaseDto();
            dto.Modders = TwoCredits();
            dto.Modders[0].RoleIds = new List<int> { ContributionRoles.Effects };

            await CreateController("lead-1").PutMoveset(id, dto);

            var editLog = await EditLog();
            Assert.Equal(AcceptanceStates.PendingAdminSoft, editLog.AcceptanceStateId);
        }

        [Fact]
        public async Task GetMoveset_CarriesRoleIdsAndShowOnCardPerCredit()
        {
            var id = await CreateWithTwoCredits(secondOnCard: false);
            Accept(id);

            var result = Assert.IsType<OkObjectResult>((await CreateController().GetMoveset(id.ToString())).Result);
            var dto = Assert.IsType<MovesetDetailDto>(result.Value);

            Assert.Equal(new[] { ContributionRoles.Coding, ContributionRoles.Animation }, dto.MovesetModders[0].RoleIds);
            Assert.True(dto.MovesetModders[0].ShowOnCard);
            Assert.Equal(new[] { ContributionRoles.Sounds }, dto.MovesetModders[1].RoleIds);
            Assert.False(dto.MovesetModders[1].ShowOnCard);
        }

        [Fact]
        public async Task GetMovesets_CardModdersOmitsOffCardCreditsButModdersKeepsThem()
        {
            await CreateWithTwoCredits(secondOnCard: false);

            var movesets = Unwrap(await CreateController("lead-1").GetMovesets(null, null, null, null, null, null, null, null, null));

            var item = Assert.Single(movesets);
            Assert.Equal(new[] { "user-lead-1", "user-second-1" }, Prop<List<string>>(item, "Modders"));
            Assert.Equal(new[] { "user-lead-1" }, Prop<List<string>>(item, "CardModders"));
            Assert.Null(Prop<List<int>>(item, "ModderRoleIds"));
        }

        [Fact]
        public async Task GetMovesets_WithModderIdFilter_CarriesThatModdersRoles()
        {
            await CreateWithTwoCredits();

            var movesets = Unwrap(await CreateController("lead-1").GetMovesets(null, null, SecondModderId, null, null, null, null, null, null));

            var item = Assert.Single(movesets);
            Assert.Equal(new[] { ContributionRoles.Sounds }, Prop<List<int>>(item, "ModderRoleIds"));
        }

        [Fact]
        public async Task GetMovesets_PrivateModder_RedactsCardModdersForStrangers()
        {
            var dto = BaseDto();
            dto.Modders = TwoCredits();
            dto.PrivateModder = true;
            var created = Assert.IsType<CreatedAtActionResult>((await CreateController("lead-1").PostMoveset(dto)).Result);
            var id = Assert.IsType<Moveset>(created.Value).MovesetId;
            SeedData.AddActionLog(_db.Context, id, acceptanceStateId: AcceptanceStates.Accepted, DateTime.UtcNow, userId: "log-author");

            var movesets = Unwrap(await CreateController().GetMovesets(null, null, null, null, null, null, null, null, null));

            var item = Assert.Single(movesets);
            Assert.Equal(new[] { "???" }, Prop<List<string>>(item, "CardModders"));
        }
    }
}
