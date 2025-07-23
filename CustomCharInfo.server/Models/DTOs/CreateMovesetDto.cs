using System.ComponentModel.DataAnnotations;

namespace CustomCharInfo.server.Models.DTOs
{
    public class CreateMovesetDto
    {
        [Required]
        public string ModdedCharName { get; set; }
        [Required]
        public string VanillaCharInternalName { get; set; }
        [Required]
        public int SeriesId { get; set; }
        [Required]
        public string SlottedId { get; set; }
        [Required]
        public string ReplacementId { get; set; }
        public int? SlotsStart { get; set; }
        public int? SlotsEnd { get; set; }
        [Required]
        public int ReleaseStateId { get; set; }
        public bool? HasGlobalOpff { get; set; }
        public bool? HasCharacterOpff { get; set; }
        public bool? HasAgentInit { get; set; }
        public bool? HasGlobalOnLinePre { get; set; }
        public bool? HasGlobalOnLineEnd { get; set; }
        public int? GamebananaPageId { get; set; }
        public int? GamebananaWipId { get; set; }
        public string? ThumbhImageUrl { get; set; }
        public string? MovesetHeroImageUrl { get; set; }
        public string? BackgroundColor { get; set; }
        public string? ModsWikiLink { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? ModpackName { get; set; }
        public string? SourceCode { get; set; }
        public bool? PrivateMoveset { get; set; }
        public bool? PrivateModder { get; set; }

        [Required]
        public List<int>? ModderIds { get; set; }
        public List<int>? DependencyIds { get; set; }
        public List<MovesetHookDto>? Hooks { get; set; }
        public List<MovesetArticleDto>? Articles { get; set; }
    }
}

