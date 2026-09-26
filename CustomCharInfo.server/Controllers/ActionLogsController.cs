using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{

    [ApiController]
    [Route("api/logs")]
    public class ActionLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public ActionLogsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Determines whether the requester may view logs for the given item:
        // admins may view anything;
        // hooks are shared/unowned so any authenticated user may view them;
        // everyone else must be the modder that owns the modder/moveset/series in question.
        private async Task<bool> CanViewItemLogsAsync(string requesterId, int itemTypeId, int itemId)
        {
            var requester = await _context.Users
                .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                .FirstOrDefaultAsync(u => u.Id == requesterId);

            if (requester == null)
                return false;

            if (requester.UserTypeId == UserTypes.Admin)
                return true;

            if (itemTypeId == ItemTypes.Hook)
                return true;

            if (requester.ModderId == null)
                return false;

            switch (itemTypeId)
            {
                case ItemTypes.Modder:
                    return requester.ModderId == itemId;
                case ItemTypes.Moveset:
                    return await MovesetAccess.CanEditAsync(_context, itemId, requester.ModderId);
                case ItemTypes.Series:
                    return await _context.Movesets
                        .AnyAsync(m => m.SeriesId == itemId &&
                            (_context.MovesetModders.Any(mm => mm.ModderId == requester.ModderId && mm.MovesetId == m.MovesetId)
                             || _context.MovesetEditors.Any(me => me.ModderId == requester.ModderId && me.MovesetId == m.MovesetId)));
                case ItemTypes.Plugin:
                    return await _context.PluginVersions
                        .AnyAsync(v => v.PluginVersionId == itemId && v.Plugin.OwnerModderId == requester.ModderId);
                default:
                    return false;
            }
        }

        // The columns the log DTOs need, projected so no full entity row (least of all the Identity user) leaves the database.
        private sealed class LogRow
        {
            public int ActionLogId { get; set; }
            public string UserId { get; set; } = "";
            public string? UserName { get; set; }
            public string? Email { get; set; }
            public int ItemTypeId { get; set; }
            public string ItemTypeName { get; set; } = "";
            public int ItemId { get; set; }
            public int AcceptanceStateId { get; set; }
            public string AcceptanceStateName { get; set; } = "";
            public string Notes { get; set; } = "";
            public string? Diff { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private static readonly Expression<Func<ActionLog, LogRow>> ToRow = a => new LogRow
        {
            ActionLogId = a.ActionLogId,
            UserId = a.UserId,
            UserName = a.User.UserName,
            Email = a.User.Email,
            ItemTypeId = a.ItemTypeId,
            ItemTypeName = a.ItemType.ItemTypeName,
            ItemId = a.ItemId,
            AcceptanceStateId = a.AcceptanceStateId,
            AcceptanceStateName = a.AcceptanceState.AcceptanceStateName,
            Notes = a.Notes,
            Diff = a.Diff,
            CreatedAt = a.CreatedAt
        };

        private class ItemLookups
        {
            public Dictionary<int, (int Id, string Name)> Movesets { get; set; } = new();
            public Dictionary<int, (int Id, string Name)> Modders { get; set; } = new();
            public Dictionary<int, (int Id, string Name)> Series { get; set; } = new();
            public Dictionary<int, (int Id, string Offset)> Hooks { get; set; } = new();
            public Dictionary<int, (int Id, string Label)> PluginVersions { get; set; } = new();
        }

        // Batches the item-detail lookups (moveset/modder/series/hook names) needed to
        // render a set of action logs, so callers avoid N+1 queries per log.
        private async Task<ItemLookups> BuildItemLookupsAsync(IEnumerable<LogRow> logs)
        {
            var modderIds = logs.Where(l => l.ItemTypeId == ItemTypes.Modder).Select(l => l.ItemId).Distinct().ToList();
            var movesetIds = logs.Where(l => l.ItemTypeId == ItemTypes.Moveset).Select(l => l.ItemId).Distinct().ToList();
            var seriesIds = logs.Where(l => l.ItemTypeId == ItemTypes.Series).Select(l => l.ItemId).Distinct().ToList();
            var hookIds = logs.Where(l => l.ItemTypeId == ItemTypes.Hook).Select(l => l.ItemId).Distinct().ToList();

            var modders = await _context.Modders
                .Where(m => modderIds.Contains(m.ModderId))
                .Select(m => new { m.ModderId, UserName = m.User.UserName ?? m.Name })
                .ToDictionaryAsync(m => m.ModderId, m => (m.ModderId, m.UserName));

            var movesets = await _context.Movesets
                .Where(m => movesetIds.Contains(m.MovesetId))
                .Select(m => new { m.MovesetId, m.ModdedCharName })
                .ToDictionaryAsync(m => m.MovesetId, m => (m.MovesetId, m.ModdedCharName));

            var series = await _context.Series
                .Where(s => seriesIds.Contains(s.SeriesId))
                .Select(s => new { s.SeriesId, s.SeriesName })
                .ToDictionaryAsync(s => s.SeriesId, s => (s.SeriesId, s.SeriesName));

            var hooks = await _context.Hooks
                .Where(h => hookIds.Contains(h.HookId))
                .Select(h => new { h.HookId, h.Offset })
                .ToDictionaryAsync(h => h.HookId, h => (h.HookId, h.Offset));

            var pluginVersionIds = logs.Where(l => l.ItemTypeId == ItemTypes.Plugin).Select(l => l.ItemId).Distinct().ToList();
            var pluginVersions = await _context.PluginVersions
                .Where(v => pluginVersionIds.Contains(v.PluginVersionId))
                .Select(v => new { v.PluginVersionId, Label = v.Plugin.Name + " v" + v.VersionLabel })
                .ToDictionaryAsync(v => v.PluginVersionId, v => (v.PluginVersionId, v.Label));

            return new ItemLookups { Movesets = movesets, Modders = modders, Series = series, Hooks = hooks, PluginVersions = pluginVersions };
        }

        private static object? BuildItemDetails(LogRow a, ItemLookups lookups)
        {
            return a.ItemTypeId switch
            {
                1 when lookups.Movesets.TryGetValue(a.ItemId, out var moveset) => new
                {
                    MovesetId = moveset.Id,
                    ModdedCharName = moveset.Name
                },
                2 when lookups.Modders.TryGetValue(a.ItemId, out var modder) => new
                {
                    ModderId = modder.Id,
                    Name = modder.Name
                },
                3 when lookups.Series.TryGetValue(a.ItemId, out var s) => new
                {
                    SeriesId = s.Id,
                    SeriesName = s.Name
                },
                4 when lookups.Hooks.TryGetValue(a.ItemId, out var hook) => new
                {
                    HookId = hook.Id,
                    Offset = hook.Offset
                },
                5 when lookups.PluginVersions.TryGetValue(a.ItemId, out var pluginVersion) => new
                {
                    PluginVersionId = pluginVersion.Id,
                    Label = pluginVersion.Label
                },
                _ => null
            };
        }

        private static GetActionLogDto ToDto(LogRow a, ItemLookups lookups, bool isAdmin)
        {
            return new GetActionLogDto
            {
                ActionLogId = a.ActionLogId,
                User = new UserSummaryDto
                {
                    Id = a.UserId,
                    UserName = a.UserName,
                    Email = isAdmin ? a.Email : null
                },
                ItemType = new ItemTypeDto
                {
                    ItemTypeId = a.ItemTypeId,
                    ItemTypeName = a.ItemTypeName
                },
                Item = BuildItemDetails(a, lookups),
                AcceptanceState = new AcceptanceStateDto
                {
                    AcceptanceStateId = a.AcceptanceStateId,
                    AcceptanceStateName = a.AcceptanceStateName
                },
                Notes = a.Notes,
                Diff = a.Diff,
                CreatedAt = a.CreatedAt
            };
        }

        // Who is asking and whose logs the request is about.
        private sealed record LogScope(bool IsAdmin, string EffectiveUserId, int? ModderId);

        // Applies the shared rules of the list endpoints: only admins may view everything or another user's logs.
        private async Task<(LogScope? Scope, ActionResult? Error)> ResolveScopeAsync(bool viewAll, string? targetUserId)
        {
            var requesterId = _userManager.GetUserId(User);
            if (requesterId == null)
                return (null, Forbid());

            var requester = await _context.Users
                .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                .FirstOrDefaultAsync(u => u.Id == requesterId);

            if (requester == null)
                return (null, Forbid());

            bool isAdmin = requester.UserTypeId == UserTypes.Admin;

            // Only admins may view all logs
            if (viewAll && !isAdmin)
                return (null, Forbid());

            // Only admins may query another user's logs
            if (targetUserId != null && !isAdmin)
                return (null, Forbid());

            // Determine user being queried for
            var effectiveUserId = targetUserId ?? requesterId;

            var user = await _context.Users
                .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                .FirstOrDefaultAsync(u => u.Id == effectiveUserId);

            if (user == null)
                return (null, NotFound("Target user not found."));

            return (new LogScope(isAdmin, effectiveUserId, user.ModderId), null);
        }

        // Narrows logs to the items the effective user may see: their modder row, movesets they are credited on or edit,
        // those movesets' series, hooks they logged on or their movesets use, and plugin versions they own.
        private async Task<IQueryable<ActionLog>> ScopeToUserAsync(IQueryable<ActionLog> query, LogScope scope)
        {
            var effectiveUserId = scope.EffectiveUserId;
            var modderId = scope.ModderId;
            var extraModderItemIds = new List<int>();

            if (modderId == null)
            {
                extraModderItemIds = await _context.ActionLogs
                    .Where(log => log.UserId == effectiveUserId && log.ItemTypeId == ItemTypes.Modder)
                    .Select(log => log.ItemId)
                    .Distinct()
                    .ToListAsync();
            }

            // Hooks are shared/unowned - a user can see a hook's logs if they've submitted a
            // log entry for it before (i.e. they've created or edited it at some point).
            var editedHookIds = await _context.ActionLogs
                .Where(log => log.UserId == effectiveUserId && log.ItemTypeId == ItemTypes.Hook)
                .Select(log => log.ItemId)
                .Distinct()
                .ToListAsync();

            if (modderId != null)
            {
                // Movesets the user is credited on or edits
                var userMovesetIds = await MovesetAccess.EditableMovesetIdsAsync(_context, modderId);

                // Get seriesIds from movesets
                var seriesIdsFromMovesets = await _context.Movesets
                    .Where(m => userMovesetIds.Contains(m.MovesetId))
                    .Select(m => m.SeriesId)
                    .Distinct()
                    .ToListAsync();

                // Dependency/standalone plugins this modder owns (case 1 plugins are never logged)
                var ownedPluginVersionIds = await _context.PluginVersions
                    .Where(v => v.Plugin.OwnerModderId == modderId)
                    .Select(v => v.PluginVersionId)
                    .ToListAsync();

                // Hooks any of those movesets use, so offset changes reach the modders they affect.
                var usedHookIds = await _context.MovesetHooks
                    .Where(mh => userMovesetIds.Contains(mh.MovesetId))
                    .Select(mh => mh.HookId)
                    .Distinct()
                    .ToListAsync();
                var visibleHookIds = editedHookIds.Union(usedHookIds).ToList();

                return query.Where(a =>
                    (a.ItemTypeId == ItemTypes.Modder && a.ItemId == modderId) ||
                    (a.ItemTypeId == ItemTypes.Moveset && userMovesetIds.Contains(a.ItemId)) ||
                    (a.ItemTypeId == ItemTypes.Series && seriesIdsFromMovesets.Contains(a.ItemId)) ||
                    (a.ItemTypeId == ItemTypes.Hook && visibleHookIds.Contains(a.ItemId)) ||
                    (a.ItemTypeId == ItemTypes.Plugin && ownedPluginVersionIds.Contains(a.ItemId))
                );
            }

            return query.Where(a =>
                (a.ItemTypeId == ItemTypes.Modder && extraModderItemIds.Contains(a.ItemId)) ||
                (a.ItemTypeId == ItemTypes.Hook && editedHookIds.Contains(a.ItemId))
            );
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetActionLogDto>>> GetActionLogs(
            [FromQuery] bool viewAll = false,
            [FromQuery] string? targetUserId = null,
            int page = 1,
            int pageSize = int.MaxValue,
            [FromQuery] int[]? acceptanceStates = null,
            [FromQuery] int[]? itemTypes = null
        ) {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0.");

            var (scope, error) = await ResolveScopeAsync(viewAll, targetUserId);
            if (error != null)
                return error;

            var query = _context.ActionLogs.AsNoTracking();

            // Restrict scope
            if (!viewAll)
                query = await ScopeToUserAsync(query, scope!);

            // AcceptanceState filter
            if (acceptanceStates != null && acceptanceStates.Any())
            {
                query = query.Where(a => acceptanceStates.Contains(a.AcceptanceStateId));
            }

            // ItemType filter
            if (itemTypes != null && itemTypes.Any())
            {
                query = query.Where(a => itemTypes.Contains(a.ItemTypeId));
            }

            var rows = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToRow)
                .ToListAsync();

            var lookups = await BuildItemLookupsAsync(rows);
            var logs = rows.Select(a => ToDto(a, lookups, scope!.IsAdmin)).ToList();

            return Ok(logs);
        }

        // The newest log per item, reduced to its state, for the pages that only need to know where an item sits in review.
        // Same scope rules as the list; the acceptance-state filter applies to the newest log, not to the history.
        [Authorize]
        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<LatestStateDto>>> GetLatestStates(
            [FromQuery] bool viewAll = false,
            [FromQuery] string? targetUserId = null,
            [FromQuery] int[]? acceptanceStates = null,
            [FromQuery] int[]? itemTypes = null
        ) {
            var (scope, error) = await ResolveScopeAsync(viewAll, targetUserId);
            if (error != null)
                return error;

            var query = _context.ActionLogs.AsNoTracking();

            if (!viewAll)
                query = await ScopeToUserAsync(query, scope!);

            if (itemTypes != null && itemTypes.Any())
            {
                query = query.Where(a => itemTypes.Contains(a.ItemTypeId));
            }

            var latest = query.LatestPerItem(_context);

            if (acceptanceStates != null && acceptanceStates.Any())
            {
                latest = latest.Where(a => acceptanceStates.Contains(a.AcceptanceStateId));
            }

            var rows = await latest
                .Select(a => new LatestStateDto
                {
                    ItemTypeId = a.ItemTypeId,
                    ItemId = a.ItemId,
                    AcceptanceStateId = a.AcceptanceStateId,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(rows);
        }

        [Authorize]
        [HttpGet("{itemTypeId}-{itemId}")]
        public async Task<ActionResult<IEnumerable<GetActionLogDto>>> GetActionLogsByItem(
            int itemTypeId,
            int itemId)
        {
            var requesterId = _userManager.GetUserId(User);
            if (requesterId == null)
                return Forbid();

            var canView = await CanViewItemLogsAsync(requesterId, itemTypeId, itemId);
            if (!canView)
                return Forbid();

            var isAdmin = await _context.Users
                .Where(u => u.Id == requesterId)
                .Select(u => u.UserTypeId == UserTypes.Admin)
                .FirstOrDefaultAsync();

            var rows = await _context.ActionLogs
                .AsNoTracking()
                .Where(a => a.ItemTypeId == itemTypeId && a.ItemId == itemId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(ToRow)
                .ToListAsync();

            if (!rows.Any())
                return Ok(Array.Empty<GetActionLogDto>());

            var lookups = await BuildItemLookupsAsync(rows);
            var logs = rows.Select(a => ToDto(a, lookups, isAdmin)).ToList();

            return Ok(logs);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ActionLogDto>> CreateActionLog(ActionLogDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            // Send ActionLog
            var log = new ActionLog
            {
                UserId = dto.UserId,
                ItemTypeId = dto.ItemTypeId,
                ItemId = dto.ItemId,
                AcceptanceStateId = dto.AcceptanceStateId,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActionLogs.Add(log);

            // Associate user to modder IF NOT ALREADY
            var originalSubmitterId = await _context.ActionLogs
                    .Where(a => a.ItemTypeId == ItemTypes.Modder && a.ItemId == dto.ItemId)
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => a.UserId)
                    .FirstOrDefaultAsync();
            var originalUser = await _context.Users.FindAsync(originalSubmitterId);
            if (
                dto.ItemTypeId == ItemTypes.Modder
                && dto.AcceptanceStateId == AcceptanceStates.Accepted
                && !string.IsNullOrEmpty(originalSubmitterId) // If original submitter exists
                && originalUser?.UserTypeId == UserTypes.User
            ) {
                var modder = await _context.Modders.FindAsync(dto.ItemId);

                if (originalUser != null && modder != null)
                {
                    // ApplicationUser.ModderId is a denormalized cache of Modder.UserId,
                    // kept for fast user->modder lookups without a join.
                    // This is the only place that sets it.
                    // Any other code path that links a user to a modder must update both sides here or the two will silently desync.
                    if (originalUser.ModderId == null)
                    {
                        originalUser.ModderId = dto.ItemId;
                    }

                    if (originalUser.UserTypeId != UserTypes.Modder)
                    {
                        originalUser.UserTypeId = UserTypes.Modder;
                    }

                    _context.Users.Update(originalUser);

                    if (string.IsNullOrEmpty(modder.UserId))
                    {
                        modder.UserId = originalSubmitterId;
                        _context.Modders.Update(modder);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActionLog), new { id = log.ActionLogId }, log);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<GetActionLogDto>> GetActionLog(int id)
        {
            var row = await _context.ActionLogs
                .AsNoTracking()
                .Where(a => a.ActionLogId == id)
                .Select(ToRow)
                .SingleOrDefaultAsync();

            if (row == null)
                return NotFound();

            var requesterId = _userManager.GetUserId(User);
            if (requesterId == null)
                return Forbid();

            var canView = await CanViewItemLogsAsync(requesterId, row.ItemTypeId, row.ItemId);
            if (!canView)
                return Forbid();

            var isAdmin = await _context.Users
                .Where(u => u.Id == requesterId)
                .Select(u => u.UserTypeId == UserTypes.Admin)
                .FirstOrDefaultAsync();

            var lookups = await BuildItemLookupsAsync(new[] { row });
            var logDto = ToDto(row, lookups, isAdmin);

            return Ok(logDto);
        }
    }
}
