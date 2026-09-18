using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
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
                    return await _context.MovesetModders
                        .AnyAsync(mm => mm.ModderId == requester.ModderId && mm.MovesetId == itemId);
                case ItemTypes.Series:
                    return await _context.Movesets
                        .AnyAsync(m => m.SeriesId == itemId &&
                            _context.MovesetModders.Any(mm => mm.ModderId == requester.ModderId && mm.MovesetId == m.MovesetId));
                case ItemTypes.Plugin:
                    return await _context.PluginVersions
                        .AnyAsync(v => v.PluginVersionId == itemId && v.Plugin.OwnerModderId == requester.ModderId);
                default:
                    return false;
            }
        }

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
        private async Task<ItemLookups> BuildItemLookupsAsync(IEnumerable<ActionLog> logs)
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

        private static object? BuildItemDetails(ActionLog a, ItemLookups lookups)
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

        private static GetActionLogDto ToDto(ActionLog a, ItemLookups lookups, bool isAdmin)
        {
            return new GetActionLogDto
            {
                ActionLogId = a.ActionLogId,
                User = new UserSummaryDto
                {
                    Id = a.User.Id,
                    UserName = a.User.UserName,
                    Email = isAdmin ? a.User.Email : null
                },
                ItemType = new ItemTypeDto
                {
                    ItemTypeId = a.ItemType.ItemTypeId,
                    ItemTypeName = a.ItemType.ItemTypeName
                },
                Item = BuildItemDetails(a, lookups),
                AcceptanceState = new AcceptanceStateDto
                {
                    AcceptanceStateId = a.AcceptanceState.AcceptanceStateId,
                    AcceptanceStateName = a.AcceptanceState.AcceptanceStateName
                },
                Notes = a.Notes,
                Diff = a.Diff,
                CreatedAt = a.CreatedAt
            };
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

            var requesterId = _userManager.GetUserId(User);
            if (requesterId == null)
                return Forbid();

            var requester = await _context.Users
                .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                .FirstOrDefaultAsync(u => u.Id == requesterId);

            if (requester == null)
                return Forbid();

            bool isAdmin = requester.UserTypeId == UserTypes.Admin;

            // Only admins may view all logs
            if (viewAll && !isAdmin)
                return Forbid();

            // Only admins may query another user's logs
            if (targetUserId != null && !isAdmin)
                return Forbid();

            // Determine user being queried for
            var effectiveUserId = targetUserId ?? requesterId;

            var user = await _context.Users
                .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                .FirstOrDefaultAsync(u => u.Id == effectiveUserId);

            if (user == null)
                return NotFound("Target user not found.");

            var query = _context.ActionLogs
                .Include(a => a.User)
                .Include(a => a.ItemType)
                .Include(a => a.AcceptanceState)
                .OrderByDescending(a => a.CreatedAt)
                .AsQueryable();

            // Restrict scope
            if (!viewAll)
            {
                var modderId = user.ModderId;
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
                    // Get movesetIds user is a modder for
                    var userMovesetIds = await _context.MovesetModders
                        .Where(mm => mm.ModderId == modderId)
                        .Select(mm => mm.MovesetId)
                        .ToListAsync();

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

                    query = query.Where(a =>
                        (a.ItemTypeId == ItemTypes.Modder && a.ItemId == modderId) ||
                        (a.ItemTypeId == ItemTypes.Moveset && userMovesetIds.Contains(a.ItemId)) ||
                        (a.ItemTypeId == ItemTypes.Series && seriesIdsFromMovesets.Contains(a.ItemId)) ||
                        (a.ItemTypeId == ItemTypes.Hook && editedHookIds.Contains(a.ItemId)) ||
                        (a.ItemTypeId == ItemTypes.Plugin && ownedPluginVersionIds.Contains(a.ItemId))
                    );
                }
                else
                {
                    query = query.Where(a =>
                        (a.ItemTypeId == ItemTypes.Modder && extraModderItemIds.Contains(a.ItemId)) ||
                        (a.ItemTypeId == ItemTypes.Hook && editedHookIds.Contains(a.ItemId))
                    );
                }
            }

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

            var logsRaw = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var lookups = await BuildItemLookupsAsync(logsRaw);
            var logs = logsRaw.Select(a => ToDto(a, lookups, isAdmin)).ToList();

            return Ok(logs);
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

            var logsRaw = await _context.ActionLogs
                .Include(a => a.User)
                .Include(a => a.ItemType)
                .Include(a => a.AcceptanceState)
                .Where(a => a.ItemTypeId == itemTypeId && a.ItemId == itemId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            if (!logsRaw.Any())
                return Ok(Array.Empty<GetActionLogDto>());

            var lookups = await BuildItemLookupsAsync(logsRaw);
            var logs = logsRaw.Select(a => ToDto(a, lookups, isAdmin)).ToList();

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
            var actionLog = await _context.ActionLogs
                .Include(a => a.User)
                .Include(a => a.ItemType)
                .Include(a => a.AcceptanceState)
                .SingleOrDefaultAsync(a => a.ActionLogId == id);

            if (actionLog == null)
                return NotFound();

            var requesterId = _userManager.GetUserId(User);
            if (requesterId == null)
                return Forbid();

            var canView = await CanViewItemLogsAsync(requesterId, actionLog.ItemTypeId, actionLog.ItemId);
            if (!canView)
                return Forbid();

            var isAdmin = await _context.Users
                .Where(u => u.Id == requesterId)
                .Select(u => u.UserTypeId == UserTypes.Admin)
                .FirstOrDefaultAsync();

            var lookups = await BuildItemLookupsAsync(new[] { actionLog });
            var logDto = ToDto(actionLog, lookups, isAdmin);

            return Ok(logDto);
        }
    }
}
