using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;

namespace CustomCharInfo.server.Helpers
{
    // Query shapes for "what is this item's current state", kept in SQL so only ids and state ids leave the database.
    public static class ActionLogQueries
    {
        // Keeps only the newest log per item type and item id, decided by a newer-log check against every log.
        // Ties on CreatedAt fall to the higher ActionLogId.
        public static IQueryable<ActionLog> LatestPerItem(this IQueryable<ActionLog> logs, AppDbContext context) =>
            logs.Where(a => !context.ActionLogs.Any(b =>
                b.ItemTypeId == a.ItemTypeId
                && b.ItemId == a.ItemId
                && (b.CreatedAt > a.CreatedAt || (b.CreatedAt == a.CreatedAt && b.ActionLogId > a.ActionLogId))));

        // Ids of movesets whose newest log is a blocked state, usable as a subquery in Contains checks.
        public static IQueryable<int> BlockedMovesetIds(AppDbContext context) =>
            context.ActionLogs
                .Where(a => a.ItemTypeId == ItemTypes.Moveset)
                .LatestPerItem(context)
                .Where(a => AcceptanceStates.Blocked.Contains(a.AcceptanceStateId))
                .Select(a => a.ItemId);
    }
}
