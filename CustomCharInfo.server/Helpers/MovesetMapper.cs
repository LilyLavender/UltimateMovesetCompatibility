using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

namespace CustomCharInfo.server.Helpers
{
    public static class MovesetMapper
    {
        // Copies every scalar field from the submitted DTO onto the entity.
        // Relationship collections (modders, dependencies, hooks, articles) are synced by the caller.
        public static void ApplyScalars(Moveset moveset, CreateMovesetDto dto)
        {
            moveset.ModdedCharName = dto.ModdedCharName;
            moveset.VanillaCharInternalName = dto.VanillaCharInternalName;
            moveset.SeriesId = dto.SeriesId;
            moveset.SlottedId = dto.SlottedId;
            moveset.ReplacementId = dto.ReplacementId;
            moveset.SlotsStart = dto.SlotsStart ?? 0;
            moveset.SlotsEnd = dto.SlotsEnd ?? 0;
            moveset.ReleaseStateId = dto.ReleaseStateId;
            moveset.HasGlobalOpff = dto.HasGlobalOpff;
            moveset.HasCharacterOpff = dto.HasCharacterOpff;
            moveset.HasAgentInit = dto.HasAgentInit;
            moveset.HasGlobalOnLinePre = dto.HasGlobalOnLinePre;
            moveset.HasGlobalOnLineEnd = dto.HasGlobalOnLineEnd;
            moveset.ModPageUrl = dto.ModPageUrl;
            moveset.GamebananaWipId = dto.GamebananaWipId ?? 0;
            moveset.ThumbhImageUrl = dto.ThumbhImageUrl;
            moveset.MovesetHeroImageUrl = dto.MovesetHeroImageUrl;
            moveset.BackgroundColor = dto.BackgroundColor;
            moveset.ModsWikiLink = dto.ModsWikiLink;
            moveset.ReleaseDate = dto.ReleaseDate;
            moveset.ModpackName = dto.ModpackName;
            moveset.SourceCode = dto.SourceCode;
            moveset.PrivateMoveset = dto.PrivateMoveset;
            moveset.PrivateModder = dto.PrivateModder;
            moveset.IsJokeMoveset = dto.IsJokeMoveset;
            moveset.Subtitle = dto.Subtitle;
        }
    }
}
