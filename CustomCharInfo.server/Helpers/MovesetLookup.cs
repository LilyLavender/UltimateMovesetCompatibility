using CustomCharInfo.server.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomCharInfo.server.Helpers
{
    // Shared MovesetId/SlottedId resolution for the public API,
    // so every endpoint that accepts a moveset reference behaves the same way as MovesetController.GetMoveset's {idOrSlottedId} route.
    // SlottedId can never be all-digits (enforced by CK_Movesets_SlottedId_NoDigits),
    // so a numeric string always means MovesetId with no ambiguity.
    public static class MovesetLookup
    {
        public static async Task<int?> ResolveMovesetIdAsync(AppDbContext context, string idOrSlottedId)
        {
            if (int.TryParse(idOrSlottedId, out int movesetId))
            {
                return await context.Movesets.AnyAsync(m => m.MovesetId == movesetId)
                    ? movesetId
                    : null;
            }

            var lowered = idOrSlottedId.ToLower();
            var match = await context.Movesets
                .Where(m => m.SlottedId.ToLower() == lowered)
                .Select(m => (int?)m.MovesetId)
                .FirstOrDefaultAsync();

            return match;
        }
    }
}
