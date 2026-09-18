using CustomCharInfo.server.Models;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomCharInfo.server.Tests.Data
{
    // Join-table FKs (MovesetModder/MovesetHook/MovesetArticle/MovesetDependency) rely on EF's
    // default convention (non-nullable FK -> cascade) rather than an explicit OnDelete call in
    // AppDbContext. These tests confirm that convention actually holds so a Moveset delete doesn't
    // leave orphaned join rows behind.
    public class CascadeDeleteTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public CascadeDeleteTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task DeletingMoveset_RemovesMovesetModderRow()
        {
            var user = SeedData.AddUser(_db.Context, "modder-user", userTypeId: UserTypes.Modder, modderId: 1);
            SeedData.AddModder(_db.Context, 1, user.Id, "SomeModder");
            var moveset = new Moveset { MovesetId = 1, ModdedCharName = "Test", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released };
            _db.Context.Movesets.Add(moveset);
            _db.Context.MovesetModders.Add(new MovesetModder { MovesetId = 1, ModderId = 1, SortOrder = 0 });
            await _db.Context.SaveChangesAsync();

            _db.Context.Movesets.Remove(moveset);
            await _db.Context.SaveChangesAsync();

            Assert.Empty(await _db.Context.MovesetModders.ToListAsync());
        }

        [Fact]
        public async Task DeletingMoveset_RemovesMovesetHookRow()
        {
            _db.Context.HookableStatuses.Add(new HookableStatus { HookableStatusId = HookableStatuses.Untested, Name = "Confirmed" });
            var hook = new Hook { HookId = 1, Offset = "0x1", Description = "Test hook", HookableStatusId = HookableStatuses.Untested };
            _db.Context.Hooks.Add(hook);
            var moveset = new Moveset { MovesetId = 1, ModdedCharName = "Test", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released };
            _db.Context.Movesets.Add(moveset);
            await _db.Context.SaveChangesAsync();
            _db.Context.MovesetHooks.Add(new MovesetHook { MovesetId = 1, HookId = 1 });
            await _db.Context.SaveChangesAsync();

            _db.Context.Movesets.Remove(moveset);
            await _db.Context.SaveChangesAsync();

            Assert.Empty(await _db.Context.MovesetHooks.ToListAsync());
            // The Hook itself is a shared, independently-owned entity and must survive.
            Assert.Single(await _db.Context.Hooks.ToListAsync());
        }

        [Fact]
        public async Task DeletingMoveset_RemovesMovesetArticleRow()
        {
            var article = new Article { ArticleId = 1, VanillaCharInternalName = "mario", ArticleName = "Cape" };
            _db.Context.Articles.Add(article);
            var moveset = new Moveset { MovesetId = 1, ModdedCharName = "Test", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released };
            _db.Context.Movesets.Add(moveset);
            await _db.Context.SaveChangesAsync();
            _db.Context.MovesetArticles.Add(new MovesetArticle { MovesetId = 1, ArticleId = 1, ModdedName = "Cape", SortOrder = 0 });
            await _db.Context.SaveChangesAsync();

            _db.Context.Movesets.Remove(moveset);
            await _db.Context.SaveChangesAsync();

            Assert.Empty(await _db.Context.MovesetArticles.ToListAsync());
        }

        [Fact]
        public async Task DeletingMoveset_RemovesMovesetDependencyRow()
        {
            var dependency = new Dependency { DependencyId = 1, Name = "SomeLib", DownloadLink = "http://example.com" };
            _db.Context.Dependencies.Add(dependency);
            var moveset = new Moveset { MovesetId = 1, ModdedCharName = "Test", VanillaCharInternalName = "mario", SlottedId = "slotone", ReleaseStateId = ReleaseStates.Released };
            _db.Context.Movesets.Add(moveset);
            await _db.Context.SaveChangesAsync();
            _db.Context.MovesetDependencies.Add(new MovesetDependency { MovesetId = 1, DependencyId = 1 });
            await _db.Context.SaveChangesAsync();

            _db.Context.Movesets.Remove(moveset);
            await _db.Context.SaveChangesAsync();

            Assert.Empty(await _db.Context.MovesetDependencies.ToListAsync());
        }
    }
}
