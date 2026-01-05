using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    // TODO putting "2" at the end of everything is bad and evil and must be fixed

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

        public int? GamebananaPageId { get; set; }
        public int? GamebananaWipId { get; set; }

        public string BackgroundColor { get; set; }
        public string ModsWikiLink { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string ModpackName { get; set; }
        public string SourceCode { get; set; }

        public bool? AdminPick { get; set; }
        public bool? PrivateMoveset { get; set; }
        public bool? PrivateModder { get; set; }

        public VanillaCharDto2 VanillaChar { get; set; }
        public ReleaseStateDto2 ReleaseState { get; set; }
        public SeriesDto2 Series { get; set; }

        public List<MovesetDependencyDto2> MovesetDependencies { get; set; }
        public List<MovesetModderDto2> MovesetModders { get; set; }
        public List<MovesetArticleDto2> MovesetArticles { get; set; }
        public List<MovesetHookDto2> MovesetHooks { get; set; }

        public string ThumbhImageUrl { get; set; }
        public string MovesetHeroImageUrl { get; set; }
    }

    public class VanillaCharDto2
    {
        public string VanillaCharInternalName { get; set; }
        public string DisplayName { get; set; }
    }

    public class ReleaseStateDto2
    {
        public int? ReleaseStateId { get; set; }
        public string ReleaseStateName { get; set; }
    }

    public class SeriesDto2
    {
        public int? SeriesId { get; set; }
        public string SeriesName { get; set; }
        public string SeriesIconUrl { get; set; }
    }

    public class MovesetDependencyDto2
    {
        public DependencyDto2 Dependency { get; set; }
    }

    public class DependencyDto2
    {
        public int? DependencyId { get; set; }
        public string Name { get; set; }
        public string DownloadLink { get; set; }
    }

    public class MovesetModderDto2
    {
        public ModderDto2 Modder { get; set; }
        public int? SortOrder { get; set; }
    }

    public class ModderDto2
    {
        public int ModderId { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public int? GamebananaId { get; set; }
        public string? DiscordUsername { get; set; }
        public string? UserId { get; set; }
    }

    public class MovesetArticleDto2
    {
        public ArticleDto2 Article { get; set; }
        public string ModdedName { get; set; }
        public string Description { get; set; }
    }

    public class ArticleDto2
    {
        public int? ArticleId { get; set; }
        public string VanillaCharInternalName { get; set; }
        public string ArticleName { get; set; }
    }

    public class MovesetHookDto2
    {
        public HookDto2 Hook { get; set; }
        public string Description { get; set; }
    }

    public class HookDto2
    {
        public int? HookId { get; set; }
        public string Offset { get; set; }
        public string Description { get; set; }
        public int? HookableStatusId { get; set; }
    }
}