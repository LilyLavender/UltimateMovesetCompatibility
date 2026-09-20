using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class MovesetAdminNoteTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public MovesetAdminNoteTests()
        {
            SeedData.SeedLookups(_db.Context);
            SeedData.AddUser(_db.Context, "admin-1", UserTypes.Admin);
            SeedData.AddUser(_db.Context, "admin-2", UserTypes.Admin);
            SeedData.AddUser(_db.Context, "modder-1", UserTypes.Modder);
            _db.Context.Movesets.Add(new Moveset { MovesetId = 1, ModdedCharName = "Waluigi", VanillaCharInternalName = "mario", SlottedId = "slotwaluigi", ReleaseStateId = ReleaseStates.Released });
            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private MovesetController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new MovesetController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        private static T? Prop<T>(object item, string name) =>
            (T?)item.GetType().GetProperty(name)!.GetValue(item);

        [Fact]
        public async Task NonAdmin_GetsForbidOnBothEndpoints()
        {
            var controller = CreateController("modder-1");

            Assert.IsType<ForbidResult>(await controller.GetAdminNotes());
            Assert.IsType<ForbidResult>(await controller.SetAdminNote(1, new MovesetAdminNoteDto { Note = "x" }));
        }

        [Fact]
        public async Task SetAdminNote_CreatesThenUpdatesWithLatestEditor()
        {
            var first = await CreateController("admin-1").SetAdminNote(1, new MovesetAdminNoteDto { Note = "  needs consensus  " });
            var ok = Assert.IsType<OkObjectResult>(first);
            Assert.Equal("needs consensus", Prop<string>(ok.Value!, "Note"));
            Assert.Equal("user-admin-1", Prop<string>(ok.Value!, "UpdatedByUserName"));

            var firstAt = (await _db.Context.MovesetAdminNotes.SingleAsync()).UpdatedAt;

            await CreateController("admin-2").SetAdminNote(1, new MovesetAdminNoteDto { Note = "picked" });

            var note = await _db.Context.MovesetAdminNotes.SingleAsync();
            Assert.Equal("picked", note.Note);
            Assert.Equal("admin-2", note.UpdatedByUserId);
            Assert.True(note.UpdatedAt >= firstAt);

            var list = Assert.IsType<OkObjectResult>(await CreateController("admin-1").GetAdminNotes());
            var row = ((IEnumerable<object>)list.Value!).Single();
            Assert.Equal(1, Prop<int>(row, "MovesetId"));
            Assert.Equal("user-admin-2", Prop<string>(row, "UpdatedByUserName"));
        }

        [Fact]
        public async Task SetAdminNote_EmptyNote_RemovesRow()
        {
            await CreateController("admin-1").SetAdminNote(1, new MovesetAdminNoteDto { Note = "temp" });

            var result = await CreateController("admin-1").SetAdminNote(1, new MovesetAdminNoteDto { Note = "   " });

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(await _db.Context.MovesetAdminNotes.ToListAsync());
        }

        [Fact]
        public async Task SetAdminNote_UnknownMoveset_ReturnsNotFound()
        {
            Assert.IsType<NotFoundResult>(await CreateController("admin-1").SetAdminNote(99, new MovesetAdminNoteDto { Note = "x" }));
        }

        [Fact]
        public async Task DeletingMoveset_RemovesItsNote()
        {
            await CreateController("admin-1").SetAdminNote(1, new MovesetAdminNoteDto { Note = "gone soon" });

            var result = await CreateController("admin-1").DeleteMoveset(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(await _db.Context.MovesetAdminNotes.ToListAsync());
        }
    }
}
