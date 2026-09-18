using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class CompatibilityControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public CompatibilityControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
            _db.Context.VanillaChars.Add(new VanillaChar { VanillaCharInternalName = "luigi", DisplayName = "Luigi" });
            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private CompatibilityController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var controller = new CompatibilityController(_db.Context, userManager.Object);
            controller.SetFakeUser();
            return controller;
        }

        private Moveset AddMoveset(int id, string slottedId, string vanillaCharInternalName = "mario", int? slotsStart = null, int? slotsEnd = null)
        {
            var moveset = new Moveset
            {
                MovesetId = id,
                ModdedCharName = $"Char{id}",
                VanillaCharInternalName = vanillaCharInternalName,
                SlottedId = slottedId,
                SlotsStart = slotsStart,
                SlotsEnd = slotsEnd,
                ReleaseStateId = ReleaseStates.Released
            };
            _db.Context.Movesets.Add(moveset);
            _db.Context.SaveChanges();
            return moveset;
        }

        private HookableStatus AddHookableStatus(int id, string name)
        {
            var status = new HookableStatus { HookableStatusId = id, Name = name };
            _db.Context.HookableStatuses.Add(status);
            _db.Context.SaveChanges();
            return status;
        }

        private Hook AddHook(int id, int hookableStatusId)
        {
            var hook = new Hook { HookId = id, Offset = $"0x{id}", Description = "Test hook", HookableStatusId = hookableStatusId };
            _db.Context.Hooks.Add(hook);
            _db.Context.SaveChanges();
            return hook;
        }

        private void AttachHook(int movesetId, int hookId)
        {
            _db.Context.MovesetHooks.Add(new MovesetHook { MovesetId = movesetId, HookId = hookId, SortOrder = 0 });
            _db.Context.SaveChanges();
        }

        private Article AddArticle(int id, string vanillaCharInternalName)
        {
            var article = new Article { ArticleId = id, VanillaCharInternalName = vanillaCharInternalName, ArticleName = "TestArticle" };
            _db.Context.Articles.Add(article);
            _db.Context.SaveChanges();
            return article;
        }

        private void AttachArticle(int movesetId, int articleId)
        {
            _db.Context.MovesetArticles.Add(new MovesetArticle { MovesetId = movesetId, ArticleId = articleId, ModdedName = "Cape", SortOrder = 0 });
            _db.Context.SaveChanges();
        }

        [Fact]
        public async Task PredictCompatibility_HookUsableMoreThanOnce_ReturnsWarning()
        {
            var status = AddHookableStatus(HookableStatuses.MoreThanOnce, "Can be hooked more than once");
            var hook = AddHook(1, status.HookableStatusId);
            var a = AddMoveset(1, "aone");
            var b = AddMoveset(2, "btwo");
            AttachHook(a.MovesetId, hook.HookId);
            AttachHook(b.MovesetId, hook.HookId);

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;
            var pairs = (System.Collections.IEnumerable)body.GetType().GetProperty("Pairs")!.GetValue(body)!;
            var pair = pairs.Cast<object>().Single();
            Assert.Equal("warning", pair.GetType().GetProperty("Severity")!.GetValue(pair));
        }

        [Fact]
        public async Task PredictCompatibility_HookOnceOnly_ReturnsIncompatible()
        {
            var status = AddHookableStatus(HookableStatuses.OnlyOnce, "Can only be hooked once");
            var hook = AddHook(1, status.HookableStatusId);
            var a = AddMoveset(1, "aone");
            var b = AddMoveset(2, "btwo");
            AttachHook(a.MovesetId, hook.HookId);
            AttachHook(b.MovesetId, hook.HookId);

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var overall = ok.Value!.GetType().GetProperty("OverallSeverity")!.GetValue(ok.Value);
            Assert.Equal("incompatible", overall);
        }

        [Fact]
        public async Task PredictCompatibility_UntestedHook_ReturnsPredictedIncompat()
        {
            var status = AddHookableStatus(HookableStatuses.Untested, "Untested");
            var hook = AddHook(1, status.HookableStatusId);
            var a = AddMoveset(1, "aone");
            var b = AddMoveset(2, "btwo");
            AttachHook(a.MovesetId, hook.HookId);
            AttachHook(b.MovesetId, hook.HookId);

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var overall = ok.Value!.GetType().GetProperty("OverallSeverity")!.GetValue(ok.Value);
            Assert.Equal("predicted-incompat", overall);
        }

        [Fact]
        public async Task PredictCompatibility_SameArticleSameChar_ReturnsIncompatible()
        {
            var article = AddArticle(1, "mario");
            var a = AddMoveset(1, "aone", "mario");
            var b = AddMoveset(2, "btwo", "mario");
            AttachArticle(a.MovesetId, article.ArticleId);
            AttachArticle(b.MovesetId, article.ArticleId);

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var overall = ok.Value!.GetType().GetProperty("OverallSeverity")!.GetValue(ok.Value);
            Assert.Equal("incompatible", overall);
        }

        [Fact]
        public async Task PredictCompatibility_SameArticleDifferentCharOverlappingSlots_ReturnsWarning()
        {
            var article = AddArticle(1, "mario");
            var a = AddMoveset(1, "aone", "mario", slotsStart: 0, slotsEnd: 5);
            var b = AddMoveset(2, "btwo", "luigi", slotsStart: 3, slotsEnd: 8);
            AttachArticle(a.MovesetId, article.ArticleId);
            AttachArticle(b.MovesetId, article.ArticleId);

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var overall = ok.Value!.GetType().GetProperty("OverallSeverity")!.GetValue(ok.Value);
            Assert.Equal("warning", overall);
        }

        [Fact]
        public async Task PredictCompatibility_NoConflicts_ReturnsCompatible()
        {
            AddMoveset(1, "aone");
            AddMoveset(2, "btwo");

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var overall = ok.Value!.GetType().GetProperty("OverallSeverity")!.GetValue(ok.Value);
            Assert.Equal("compatible", overall);
        }

        [Fact]
        public async Task PredictCompatibility_AcceptsSlottedIdsInsteadOfMovesetIds()
        {
            AddMoveset(1, "aone");
            AddMoveset(2, "btwo");

            var controller = CreateController();
            var result = await controller.PredictCompatibility("aone,btwo");

            var ok = Assert.IsType<OkObjectResult>(result);
            var overall = ok.Value!.GetType().GetProperty("OverallSeverity")!.GetValue(ok.Value);
            Assert.Equal("compatible", overall);
        }

        [Fact]
        public async Task PredictCompatibility_UnknownMoveset_ReturnsNotFound()
        {
            AddMoveset(1, "aone");

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1,999");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task PredictCompatibility_TooManyMovesets_ReturnsBadRequest()
        {
            for (int i = 1; i <= 11; i++)
                AddMoveset(i, $"slot{(char)('a' + i)}");

            var controller = CreateController();
            var ids = string.Join(",", Enumerable.Range(1, 11));
            var result = await controller.PredictCompatibility(ids);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PredictCompatibility_FewerThanTwoMovesets_ReturnsBadRequest()
        {
            AddMoveset(1, "aone");

            var controller = CreateController();
            var result = await controller.PredictCompatibility("1");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetReports_AcceptsSlottedIds()
        {
            AddMoveset(1, "aone");
            AddMoveset(2, "btwo");

            var controller = CreateController();
            var result = await controller.GetReports("aone", "btwo");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetReports_UnknownMoveset_ReturnsNotFound()
        {
            AddMoveset(1, "aone");

            var controller = CreateController();
            var result = await controller.GetReports("aone", "nonexistent");

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
