using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    // Editors: modders who may edit a moveset without being credited on it.
    // Full-access editors may also change the Modders and Editors lists; partial editors may not.
    public class MovesetEditorTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        private const int CreditedModderId = 5;
        private const int FullEditorId = 6;
        private const int PartialEditorId = 7;
        private const int StrangerModderId = 8;
        private const int AdminModderId = 9;

        public MovesetEditorTests()
        {
            SeedData.SeedLookups(_db.Context);
            _db.Context.Series.Add(new Series { SeriesId = 1, SeriesName = "Test Series" });
            _db.Context.SaveChanges();

            AddModderUser("owner-1", CreditedModderId, "Credited");
            AddModderUser("full-1", FullEditorId, "FullEditor");
            AddModderUser("partial-1", PartialEditorId, "PartialEditor");
            AddModderUser("other-1", StrangerModderId, "Stranger");
            AddModderUser("admin-1", AdminModderId, "AdminModder", UserTypes.Admin);
        }

        public void Dispose() => _db.Dispose();

        private void AddModderUser(string userId, int modderId, string name, int userTypeId = UserTypes.Modder)
        {
            SeedData.AddUser(_db.Context, userId, userTypeId, modderId);
            SeedData.AddModder(_db.Context, modderId, userId, name);
        }

        private MovesetController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new MovesetController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        // Moveset 1 credited to modder 5, with a full editor (6) and a partial editor (7).
        private Moveset AddMovesetWithMembers(bool isPrivate = false)
        {
            var moveset = new Moveset
            {
                MovesetId = 1,
                ModdedCharName = "Waluigi",
                VanillaCharInternalName = "mario",
                SlottedId = "slotwaluigi",
                ReplacementId = "slotwaluigi",
                SeriesId = 1,
                ReleaseStateId = ReleaseStates.Released,
                PrivateMoveset = isPrivate
            };
            _db.Context.Movesets.Add(moveset);
            _db.Context.MovesetModders.Add(new MovesetModder { MovesetId = 1, ModderId = CreditedModderId, SortOrder = 0 });
            _db.Context.MovesetEditors.Add(new MovesetEditor { MovesetId = 1, ModderId = FullEditorId, FullAccess = true });
            _db.Context.MovesetEditors.Add(new MovesetEditor { MovesetId = 1, ModderId = PartialEditorId, FullAccess = false });
            _db.Context.SaveChanges();
            return moveset;
        }

        // A full update that changes only the subtitle and keeps every member as stored.
        private static CreateMovesetDto BaseDto() => new()
        {
            ModdedCharName = "Waluigi",
            VanillaCharInternalName = "mario",
            SeriesId = 1,
            SlottedId = "slotwaluigi",
            ReplacementId = "slotwaluigi",
            ReleaseStateId = ReleaseStates.Released,
            Subtitle = "edited",
            ModderIds = new List<int> { CreditedModderId },
            Editors = new List<MovesetEditorDto>
            {
                new() { ModderId = FullEditorId, FullAccess = true },
                new() { ModderId = PartialEditorId, FullAccess = false }
            },
            DependencyIds = new List<int>(),
            Hooks = new List<MovesetHookDto>(),
            Articles = new List<MovesetArticleDto>()
        };

        private static List<object> Unwrap(ActionResult<IEnumerable<object>> result)
        {
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            return ((IEnumerable<object>)ok.Value!).ToList();
        }

        [Fact]
        public async Task PutMoveset_FullEditor_Succeeds()
        {
            AddMovesetWithMembers();

            var result = await CreateController("full-1").PutMoveset(1, BaseDto());

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("edited", (await _db.Context.Movesets.FindAsync(1))!.Subtitle);
        }

        [Fact]
        public async Task PutMoveset_PartialEditor_SucceedsWhenMembersUnchanged()
        {
            AddMovesetWithMembers();

            var result = await CreateController("partial-1").PutMoveset(1, BaseDto());

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(2, await _db.Context.MovesetEditors.CountAsync());
        }

        [Fact]
        public async Task PutMoveset_PartialEditor_ChangingModders_ReturnsForbid()
        {
            AddMovesetWithMembers();
            var dto = BaseDto();
            dto.ModderIds!.Add(StrangerModderId);

            var result = await CreateController("partial-1").PutMoveset(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task PutMoveset_PartialEditor_ChangingEditors_ReturnsForbid()
        {
            AddMovesetWithMembers();
            var dto = BaseDto();
            dto.Editors![1].FullAccess = true;

            var result = await CreateController("partial-1").PutMoveset(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task PutMoveset_Stranger_ReturnsForbid()
        {
            AddMovesetWithMembers();

            var result = await CreateController("other-1").PutMoveset(1, BaseDto());

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task PutMoveset_AdminNotOnMoveset_ReturnsForbid()
        {
            AddMovesetWithMembers();

            var result = await CreateController("admin-1").PutMoveset(1, BaseDto());

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task PutMoveset_CreditedModder_SyncsEditorsAndDropsCreditedOnes()
        {
            AddMovesetWithMembers();
            var dto = BaseDto();
            dto.Editors = new List<MovesetEditorDto>
            {
                new() { ModderId = FullEditorId, FullAccess = false },
                new() { ModderId = CreditedModderId, FullAccess = true },
                new() { ModderId = StrangerModderId, FullAccess = true }
            };

            var result = await CreateController("owner-1").PutMoveset(1, dto);

            Assert.IsType<NoContentResult>(result);
            var editors = await _db.Context.MovesetEditors.OrderBy(e => e.ModderId).ToListAsync();
            Assert.Equal(new[] { (FullEditorId, false), (StrangerModderId, true) }, editors.Select(e => (e.ModderId, e.FullAccess)));

            var log = await _db.Context.ActionLogs.SingleAsync();
            Assert.Contains("Editors", log.Diff);
            Assert.Contains("Stranger (full)", log.Diff);
        }

        [Fact]
        public async Task GetMoveset_Editor_SeesHiddenMovesetWithEditorsAndAccessFlags()
        {
            AddMovesetWithMembers(isPrivate: true);
            SeedData.AddActionLog(_db.Context, 1, acceptanceStateId: AcceptanceStates.Rejected, DateTime.UtcNow, userId: "owner-1");

            var partial = Assert.IsType<OkObjectResult>((await CreateController("partial-1").GetMoveset("1")).Result);
            var partialDto = Assert.IsType<MovesetDetailDto>(partial.Value);
            Assert.True(partialDto.CanEdit);
            Assert.False(partialDto.CanManageMembers);
            Assert.Equal(2, partialDto.MovesetEditors!.Count);

            var full = Assert.IsType<OkObjectResult>((await CreateController("full-1").GetMoveset("1")).Result);
            Assert.True(Assert.IsType<MovesetDetailDto>(full.Value).CanManageMembers);
        }

        [Fact]
        public async Task GetMoveset_Stranger_GetsNoEditorsAndCannotEdit()
        {
            AddMovesetWithMembers();

            var result = Assert.IsType<OkObjectResult>((await CreateController("other-1").GetMoveset("1")).Result);
            var dto = Assert.IsType<MovesetDetailDto>(result.Value);

            Assert.False(dto.CanEdit);
            Assert.False(dto.CanManageMembers);
            Assert.Null(dto.MovesetEditors);
        }

        [Fact]
        public async Task GetMovesets_Editor_SeesOwnBlockedMovesetAndEditorIdFilterWorks()
        {
            AddMovesetWithMembers();
            SeedData.AddActionLog(_db.Context, 1, acceptanceStateId: AcceptanceStates.Rejected, DateTime.UtcNow, userId: "owner-1");

            var asEditor = await CreateController("partial-1").GetMovesets(null, null, null, null, null, null, null, null, null);
            Assert.Single(Unwrap(asEditor));

            var asStranger = await CreateController("other-1").GetMovesets(null, null, null, null, null, null, null, null, null);
            Assert.Empty(Unwrap(asStranger));

            var byEditor = await CreateController("partial-1").GetMovesets(null, null, null, null, null, null, null, null, null, editorId: PartialEditorId);
            Assert.Single(Unwrap(byEditor));

            var byNonEditor = await CreateController("partial-1").GetMovesets(null, null, null, null, null, null, null, null, null, editorId: StrangerModderId);
            Assert.Empty(Unwrap(byNonEditor));
        }

        [Fact]
        public async Task SearchMovesets_Editor_SeesOwnPrivateMoveset()
        {
            AddMovesetWithMembers(isPrivate: true);

            var ok = Assert.IsType<OkObjectResult>((await CreateController("partial-1").SearchMovesets("walu")).Result);

            Assert.Single((IEnumerable<object>)ok.Value!);
        }

        [Fact]
        public async Task PatchMovesetImages_Editor_Succeeds()
        {
            AddMovesetWithMembers();

            var result = await CreateController("partial-1").PatchMovesetImages(1, new MovesetImagesDto { ThumbhImageUrl = "/uploads/thumb.png" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PatchMovesetImages_AdminNotOnMoveset_ReturnsForbid()
        {
            AddMovesetWithMembers();

            var result = await CreateController("admin-1").PatchMovesetImages(1, new MovesetImagesDto { ThumbhImageUrl = "/uploads/thumb.png" });

            Assert.IsType<ForbidResult>(result);
        }
    }
}
