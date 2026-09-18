using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CustomCharInfo.server.Helpers
{
    // Builds the field-by-field diff stored on a moveset edit's ActionLog.
    // Runs against the entity before it is mutated, comparing it to the incoming DTO,
    // and resolves ids to names so admins read a series name rather than its number.
    public static class MovesetDiffBuilder
    {
        public static async Task<string?> BuildAsync(AppDbContext context, Moveset moveset, CreateMovesetDto dto)
        {
            // Batch lookups for human-readable diff values
            var allModderIds = moveset.MovesetModders.Select(m => m.ModderId)
                .Concat(dto.ModderIds ?? new()).Distinct().ToList();
            var allDepIds = moveset.MovesetDependencies.Select(d => d.DependencyId)
                .Concat(dto.DependencyIds ?? new()).Distinct().ToList();
            var allSeriesIds = new[] { moveset.SeriesId, (int?)dto.SeriesId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var allReleaseIds = new[] { moveset.ReleaseStateId, (int?)dto.ReleaseStateId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var allVanillaNames = new[] { moveset.VanillaCharInternalName, dto.VanillaCharInternalName }
                .Where(x => x != null).Distinct().ToList();
            var allHookIds = moveset.MovesetHooks.Select(h => h.HookId)
                .Concat((dto.Hooks ?? new()).Select(h => h.HookId)).Distinct().ToList();
            var allArticleIds = moveset.MovesetArticles.Select(a => a.ArticleId)
                .Concat((dto.Articles ?? new()).Select(a => a.ArticleId)).Distinct().ToList();

            var modderNameMap = await context.Modders
                .Where(m => allModderIds.Contains(m.ModderId))
                .ToDictionaryAsync(m => m.ModderId, m => m.Name ?? m.ModderId.ToString());
            var depNameMap = await context.Dependencies
                .Where(d => allDepIds.Contains(d.DependencyId))
                .ToDictionaryAsync(d => d.DependencyId, d => d.Name);
            var seriesNameMap = await context.Series
                .Where(s => allSeriesIds.Contains(s.SeriesId))
                .ToDictionaryAsync(s => s.SeriesId, s => s.SeriesName);
            var releaseStateNameMap = await context.ReleaseStates
                .Where(rs => allReleaseIds.Contains(rs.ReleaseStateId))
                .ToDictionaryAsync(rs => rs.ReleaseStateId, rs => rs.ReleaseStateName);
            var vanillaNameMap = await context.VanillaChars
                .Where(vc => allVanillaNames.Contains(vc.VanillaCharInternalName))
                .ToDictionaryAsync(vc => vc.VanillaCharInternalName, vc => vc.DisplayName);
            var hookLabelMap = await context.Hooks
                .Where(h => allHookIds.Contains(h.HookId))
                .ToDictionaryAsync(h => h.HookId, h => $"0x{h.Offset} ({h.Description})");
            var articleLabelMap = await context.Articles
                .Where(a => allArticleIds.Contains(a.ArticleId))
                .ToDictionaryAsync(a => a.ArticleId, a => $"{a.VanillaCharInternalName}_{a.ArticleName}");

            string ResolveInt(Dictionary<int, string> map, int? key) =>
                key.HasValue && map.TryGetValue(key.Value, out var v) ? v : key?.ToString() ?? "";
            string ResolveStr(Dictionary<string, string> map, string? key) =>
                key != null && map.TryGetValue(key, out var v) ? v : key ?? "";

            // Snapshot before mutation for diff
            var snap = new
            {
                moveset.ModdedCharName,
                VanillaChar   = ResolveStr(vanillaNameMap, moveset.VanillaCharInternalName),
                Series        = ResolveInt(seriesNameMap, moveset.SeriesId),
                moveset.SlottedId, moveset.ReplacementId, moveset.SlotsStart, moveset.SlotsEnd,
                Availability  = ResolveInt(releaseStateNameMap, moveset.ReleaseStateId),
                moveset.HasGlobalOpff, moveset.HasCharacterOpff,
                moveset.HasAgentInit, moveset.HasGlobalOnLinePre, moveset.HasGlobalOnLineEnd,
                moveset.ModPageUrl, moveset.GamebananaWipId, moveset.ThumbhImageUrl,
                moveset.MovesetHeroImageUrl, moveset.BackgroundColor, moveset.ModsWikiLink,
                ReleaseDate   = moveset.ReleaseDate?.ToString("yyyy-MM-dd"),
                moveset.ModpackName, moveset.SourceCode, moveset.PrivateMoveset, moveset.PrivateModder,
                moveset.IsJokeMoveset, moveset.Subtitle,
                Modders       = string.Join(", ", moveset.MovesetModders
                    .Select(m => modderNameMap.TryGetValue(m.ModderId, out var n) ? n : m.ModderId.ToString())
                    .OrderBy(x => x)),
                Dependencies  = string.Join(", ", moveset.MovesetDependencies
                    .Select(d => depNameMap.TryGetValue(d.DependencyId, out var n) ? n : d.DependencyId.ToString())
                    .OrderBy(x => x)),
                Hooks         = string.Join(", ", moveset.MovesetHooks
                    .Select(h => hookLabelMap.TryGetValue(h.HookId, out var l) ? $"{l}: {h.Description}" : h.Description)
                    .OrderBy(x => x)),
                Articles      = string.Join(", ", moveset.MovesetArticles
                    .Select(a => articleLabelMap.TryGetValue(a.ArticleId, out var l) ? $"{l} as {a.ModdedName}" : a.ModdedName)
                    .OrderBy(x => x)),
            };

            var newModders = string.Join(", ", (dto.ModderIds ?? new())
                .Select(mid => modderNameMap.TryGetValue(mid, out var n) ? n : mid.ToString())
                .OrderBy(x => x));
            var newDeps = string.Join(", ", (dto.DependencyIds ?? new())
                .Select(did => depNameMap.TryGetValue(did, out var n) ? n : did.ToString())
                .OrderBy(x => x));
            var newHooks = string.Join(", ", (dto.Hooks ?? new())
                .Select(h => hookLabelMap.TryGetValue(h.HookId, out var l) ? $"{l}: {h.Description}" : h.Description)
                .OrderBy(x => x));
            var newArticles = string.Join(", ", (dto.Articles ?? new())
                .Select(a => articleLabelMap.TryGetValue(a.ArticleId, out var l) ? $"{l} as {a.ModdedName}" : a.ModdedName)
                .OrderBy(x => x));

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Name",            snap.ModdedCharName,    dto.ModdedCharName),
                ("VanillaChar",     snap.VanillaChar,       ResolveStr(vanillaNameMap, dto.VanillaCharInternalName)),
                ("Series",          snap.Series,            ResolveInt(seriesNameMap, dto.SeriesId)),
                ("SlottedId",       snap.SlottedId,         dto.SlottedId),
                ("ReplacementId",   snap.ReplacementId,     dto.ReplacementId),
                ("SlotsStart",      snap.SlotsStart,        dto.SlotsStart),
                ("SlotsEnd",        snap.SlotsEnd,          dto.SlotsEnd),
                ("Availability",    snap.Availability,      ResolveInt(releaseStateNameMap, dto.ReleaseStateId)),
                ("GlobalOPFF",      snap.HasGlobalOpff,     dto.HasGlobalOpff),
                ("CharacterOPFF",   snap.HasCharacterOpff,  dto.HasCharacterOpff),
                ("AgentInit",       snap.HasAgentInit,      dto.HasAgentInit),
                ("GlobalOnLinePre", snap.HasGlobalOnLinePre, dto.HasGlobalOnLinePre),
                ("GlobalOnLineEnd", snap.HasGlobalOnLineEnd, dto.HasGlobalOnLineEnd),
                ("ModPage",         snap.ModPageUrl,        dto.ModPageUrl),
                ("GBWip",           snap.GamebananaWipId,   dto.GamebananaWipId),
                ("Thumbnail",       snap.ThumbhImageUrl,    dto.ThumbhImageUrl),
                ("HeroImage",       snap.MovesetHeroImageUrl, dto.MovesetHeroImageUrl),
                ("BgColor",         snap.BackgroundColor,   dto.BackgroundColor),
                ("ModsWiki",        snap.ModsWikiLink,      dto.ModsWikiLink),
                ("ReleaseDate",     snap.ReleaseDate,       dto.ReleaseDate?.ToString("yyyy-MM-dd")),
                ("Modpack",         snap.ModpackName,       dto.ModpackName),
                ("SourceCode",      snap.SourceCode,        dto.SourceCode),
                ("Private",         snap.PrivateMoveset,    dto.PrivateMoveset),
                ("PrivateModder",   snap.PrivateModder,     dto.PrivateModder),
                ("JokeMoveset",     snap.IsJokeMoveset,     dto.IsJokeMoveset),
                ("Subtitle",        snap.Subtitle,          dto.Subtitle),
                ("Modders",         snap.Modders,           newModders),
                ("Dependencies",    snap.Dependencies,      newDeps),
                ("Hooks",           snap.Hooks,             newHooks),
                ("Articles",        snap.Articles,          newArticles),
            });

            return diff;
        }
    }
}
