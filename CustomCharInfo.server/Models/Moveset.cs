using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomCharInfo.server.Models
{
    public class Moveset
    {
        [Key]
        public int MovesetId { get; set; }
        
        [Required, MaxLength(32)]
        public string ModdedCharName { get; set; }

        [Required, MaxLength(12)]
        public string VanillaCharInternalName { get; set; }

        public int? SeriesId { get; set; }

        [MaxLength(32)]
        public string? SlottedId { get; set; }

        [MaxLength(32)]
        public string? ReplacementId { get; set; }

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
        [MaxLength(6)]
        public string? BackgroundColor { get; set; }

        [MaxLength(32)]
        public string? ModsWikiLink { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? ModpackName { get; set; }
        public string? SourceCode { get; set; }
        public bool? AdminPick { get; set; }
        public bool? PrivateMoveset { get; set; }
        public bool? PrivateModder { get; set; }

        // Navigation
        public VanillaChar VanillaChar { get; set; }
        public ReleaseState ReleaseState { get; set; }
        public Series Series { get; set; }
        public ICollection<MovesetDependency> MovesetDependencies { get; set; }
        public ICollection<MovesetModder> MovesetModders { get; set; }
        public ICollection<MovesetArticle> MovesetArticles { get; set; }
        public ICollection<MovesetHook> MovesetHooks { get; set; }

        // Images
        [MaxLength(255)]
        public string? ThumbhImageUrl { get; set; }
        [MaxLength(255)]
        public string? MovesetHeroImageUrl { get; set; }
    }
}
