using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomCharInfo.server.Helpers
{
    // Who may edit a moveset: its credited modders and its editors.
    // Admins are NOT allowed to edit movesets they're not modders or editors on.
    // The list endpoints repeat the same rule inline as EF expressions, since a method call cannot be translated inside a projection.
    public static class MovesetAccess
    {
        // Credited modder or any editor. Requires MovesetModders and MovesetEditors to be loaded.
        public static bool CanEdit(Moveset moveset, int? modderId) =>
            modderId != null
            && (moveset.MovesetModders.Any(mm => mm.ModderId == modderId)
                || moveset.MovesetEditors.Any(me => me.ModderId == modderId));

        // Credited modder or full-access editor. Requires MovesetModders and MovesetEditors to be loaded.
        public static bool CanManageMembers(Moveset moveset, int? modderId) =>
            modderId != null
            && (moveset.MovesetModders.Any(mm => mm.ModderId == modderId)
                || moveset.MovesetEditors.Any(me => me.ModderId == modderId && me.FullAccess));

        // Same as CanEdit for callers that only have ids.
        public static async Task<bool> CanEditAsync(AppDbContext context, int movesetId, int? modderId)
        {
            if (modderId == null) return false;
            return await context.MovesetModders.AnyAsync(mm => mm.MovesetId == movesetId && mm.ModderId == modderId)
                || await context.MovesetEditors.AnyAsync(me => me.MovesetId == movesetId && me.ModderId == modderId);
        }

        // Every moveset the modder is credited on or edits.
        public static async Task<List<int>> EditableMovesetIdsAsync(AppDbContext context, int? modderId)
        {
            if (modderId == null) return new List<int>();
            var credited = context.MovesetModders.Where(mm => mm.ModderId == modderId).Select(mm => mm.MovesetId);
            var edited = context.MovesetEditors.Where(me => me.ModderId == modderId).Select(me => me.MovesetId);
            return await credited.Union(edited).ToListAsync();
        }
    }
}
