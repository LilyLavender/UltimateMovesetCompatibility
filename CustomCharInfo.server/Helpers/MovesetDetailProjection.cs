using System.Linq.Expressions;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

namespace CustomCharInfo.server.Helpers
{
    // The full detail shape for one moveset, kept as an expression so EF translates it into a single query.
    public static class MovesetDetailProjection
    {
        public static readonly Expression<Func<Moveset, MovesetDetailDto>> FromMoveset =
            m => new MovesetDetailDto
            {
                MovesetId = m.MovesetId,
                ModdedCharName = m.ModdedCharName,
                VanillaCharInternalName = m.VanillaCharInternalName,
                SeriesId = m.SeriesId,
                SlottedId = m.SlottedId,
                ReplacementId = m.ReplacementId,
                SlotsStart = m.SlotsStart,
                SlotsEnd = m.SlotsEnd,
                ReleaseStateId = m.ReleaseStateId,

                HasGlobalOpff = m.HasGlobalOpff,
                HasCharacterOpff = m.HasCharacterOpff,
                HasAgentInit = m.HasAgentInit,
                HasGlobalOnLinePre = m.HasGlobalOnLinePre,
                HasGlobalOnLineEnd = m.HasGlobalOnLineEnd,

                ModPageUrl = m.ModPageUrl,
                GamebananaWipId = m.GamebananaWipId,
                BackgroundColor = m.BackgroundColor,
                ModsWikiLink = m.ModsWikiLink,
                ReleaseDate = m.ReleaseDate,
                ModpackName = m.ModpackName,
                SourceCode = m.SourceCode,

                AdminPick = m.AdminPick,
                PrivateMoveset = m.PrivateMoveset,
                PrivateModder = m.PrivateModder,
                IsJokeMoveset = m.IsJokeMoveset,
                Subtitle = m.Subtitle,

                ThumbhImageUrl = m.ThumbhImageUrl,
                MovesetHeroImageUrl = m.MovesetHeroImageUrl,

                VanillaChar = m.VanillaChar == null ? null : new VanillaCharSummaryDto
                {
                    VanillaCharInternalName = m.VanillaChar.VanillaCharInternalName,
                    DisplayName = m.VanillaChar.DisplayName
                },

                ReleaseState = m.ReleaseState == null ? null : new ReleaseStateSummaryDto
                {
                    ReleaseStateId = m.ReleaseState.ReleaseStateId,
                    ReleaseStateName = m.ReleaseState.ReleaseStateName
                },

                Series = m.Series == null ? null : new SeriesSummaryDto
                {
                    SeriesId = m.Series.SeriesId,
                    SeriesName = m.Series.SeriesName,
                    SeriesIconUrl = m.Series.SeriesIconUrl
                },

                MovesetDependencies = m.MovesetDependencies
                    .Select(md => new MovesetDependencyDetailDto
                    {
                        Dependency = new DependencySummaryDto
                        {
                            DependencyId = md.Dependency.DependencyId,
                            Name = md.Dependency.Name,
                            DownloadLink = md.Dependency.DownloadLink
                        }
                    }).ToList(),

                MovesetModders = m.MovesetModders
                    .OrderBy(mm => mm.SortOrder)
                    .Select(mm => new MovesetModderDetailDto
                    {
                        SortOrder = mm.SortOrder,
                        Modder = new ModderSummaryDto
                        {
                            ModderId = mm.Modder.ModderId,
                            Name = mm.Modder.Name,
                            Bio = mm.Modder.Bio,
                            GamebananaId = mm.Modder.GamebananaId,
                            DiscordUsername = mm.Modder.DiscordUsername,
                            UserId = mm.Modder.UserId
                        }
                    }).ToList(),

                MovesetArticles = m.MovesetArticles
                    .OrderBy(ma => ma.SortOrder)
                    .Select(ma => new MovesetArticleDetailDto
                    {
                        ModdedName = ma.ModdedName,
                        Description = ma.Description,
                        SortOrder = ma.SortOrder,
                        Article = new ArticleSummaryDto
                        {
                            ArticleId = ma.Article.ArticleId,
                            VanillaCharInternalName = ma.Article.VanillaCharInternalName,
                            ArticleName = ma.Article.ArticleName
                        }
                    }).ToList(),

                MovesetHooks = m.MovesetHooks
                    .OrderBy(mh => mh.SortOrder)
                    .Select(mh => new MovesetHookDetailDto
                    {
                        Description = mh.Description,
                        SortOrder = mh.SortOrder,
                        Hook = new HookSummaryDto
                        {
                            HookId = mh.Hook.HookId,
                            Offset = mh.Hook.Offset,
                            Description = mh.Hook.Description,
                            HookableStatusId = mh.Hook.HookableStatusId
                        }
                    }).ToList()
            };
    }
}
