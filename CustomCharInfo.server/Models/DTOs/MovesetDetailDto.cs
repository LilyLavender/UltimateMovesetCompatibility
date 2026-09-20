using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    // Response shape for GET /api/movesets/{idOrSlottedId}.
    // The nested *SummaryDto types are the trimmed views of related rows this response embeds.

    public class MovesetDetailDto
    {
        public int MovesetId { get; set; }
        public string ModdedCharName { get; set; }
        public string VanillaCharInternalName { get; set; }
        public int? SeriesId { get; set; }
        public string SlottedId { get; set; }
        public string ReplacementId { get; set; }
        public int? SlotsStart { get; set; }
        public int? SlotsEnd { get; set; }
        public int? ReleaseStateId { get; set; }

        public bool? HasGlobalOpff { get; set; }
        public bool? HasCharacterOpff { get; set; }
        public bool? HasAgentInit { get; set; }
        public bool? HasGlobalOnLinePre { get; set; }
        public bool? HasGlobalOnLineEnd { get; set; }

        public string? ModPageUrl { get; set; }
        public int? GamebananaWipId { get; set; }

        public string BackgroundColor { get; set; }
        public string ModsWikiLink { get; set; }
        public DateOnly? ReleaseDate { get; set; }
        public string ModpackName { get; set; }
        public string SourceCode { get; set; }

        public bool? AdminPick { get; set; }
        public bool? PrivateMoveset { get; set; }
        public bool? PrivateModder { get; set; }
        public bool? IsJokeMoveset { get; set; }
        public string? Subtitle { get; set; }

        public VanillaCharSummaryDto VanillaChar { get; set; }
        public ReleaseStateSummaryDto ReleaseState { get; set; }
        public SeriesSummaryDto Series { get; set; }

        public List<MovesetDependencyDetailDto> MovesetDependencies { get; set; }
        public List<MovesetModderDetailDto> MovesetModders { get; set; }
        public List<MovesetArticleDetailDto> MovesetArticles { get; set; }
        public List<MovesetHookDetailDto> MovesetHooks { get; set; }

        public string ThumbhImageUrl { get; set; }
        public string MovesetHeroImageUrl { get; set; }

        public int LikeCount { get; set; }
        public bool UserLiked { get; set; }

        // Whether the requester can edit this moveset (modder or editor)
        public bool CanEdit { get; set; }
        public bool CanManageMembers { get; set; }

        // Only returned when CanEdit is true. Editors are never shown to the public.
        public List<MovesetEditorDetailDto>? MovesetEditors { get; set; }
    }

    public class MovesetEditorDetailDto
    {
        public ModderSummaryDto Modder { get; set; }
        public bool FullAccess { get; set; }
    }

    public class VanillaCharSummaryDto
    {
        public string VanillaCharInternalName { get; set; }
        public string DisplayName { get; set; }
    }

    public class ReleaseStateSummaryDto
    {
        public int? ReleaseStateId { get; set; }
        public string ReleaseStateName { get; set; }
    }

    public class SeriesSummaryDto
    {
        public int? SeriesId { get; set; }
        public string SeriesName { get; set; }
        public string SeriesIconUrl { get; set; }
    }

    public class MovesetDependencyDetailDto
    {
        public DependencySummaryDto Dependency { get; set; }
    }

    public class DependencySummaryDto
    {
        public int? DependencyId { get; set; }
        public string Name { get; set; }
        public string DownloadLink { get; set; }
    }

    public class MovesetModderDetailDto
    {
        public ModderSummaryDto Modder { get; set; }
        public int? SortOrder { get; set; }
    }

    public class ModderSummaryDto
    {
        public int ModderId { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public int? GamebananaId { get; set; }
        public string? DiscordUsername { get; set; }
        public string? UserId { get; set; }
    }

    public class MovesetArticleDetailDto
    {
        public ArticleSummaryDto Article { get; set; }
        public string ModdedName { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
    }

    public class ArticleSummaryDto
    {
        public int? ArticleId { get; set; }
        public string VanillaCharInternalName { get; set; }
        public string ArticleName { get; set; }
    }

    public class MovesetHookDetailDto
    {
        public HookSummaryDto Hook { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
    }

    public class HookSummaryDto
    {
        public int? HookId { get; set; }
        public string Offset { get; set; }
        public string Description { get; set; }
        public int? HookableStatusId { get; set; }
    }
}