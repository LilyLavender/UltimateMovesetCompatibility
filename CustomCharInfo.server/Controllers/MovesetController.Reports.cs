// Read-only export shapes: the slot grid and the wide report. Routes stay under api/movesets.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCharInfo.server.Controllers
{
    public partial class MovesetController
    {
        [HttpGet("slot-grid")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult> GetSlotGrid()
        {
            
            var rows = await _context.Movesets
                .AsNoTracking()
                .Select(m => new
                {
                    m.MovesetId,
                    m.ModdedCharName,
                    m.Subtitle,
                    m.VanillaCharInternalName,
                    VanillaDisplayName = m.VanillaChar != null ? m.VanillaChar.DisplayName : m.VanillaCharInternalName,
                    SlotsStart = m.SlotsStart ?? 0,
                    SlotsEnd = m.SlotsEnd ?? 0,
                    IsPrivate = m.PrivateMoveset == true,
                    LatestState = _context.ActionLogs
                        .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == m.MovesetId)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => (int?)a.AcceptanceStateId)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = rows
                .Where(m => m.LatestState == null || !AcceptanceStates.Blocked.Contains(m.LatestState.Value))
                .GroupBy(m => new { m.VanillaCharInternalName, m.VanillaDisplayName })
                .OrderBy(g => g.Key.VanillaDisplayName)
                .Select(g => new
                {
                    VanillaChar = g.Key.VanillaCharInternalName,
                    DisplayName = g.Key.VanillaDisplayName,
                    Movesets = g
                        .OrderBy(m => m.SlotsStart)
                        .Select(m => new
                        {
                            m.MovesetId,
                            Name = m.IsPrivate ? "???" : m.ModdedCharName,
                            Subtitle = m.IsPrivate ? null : m.Subtitle,
                            m.SlotsStart,
                            m.SlotsEnd,
                            m.IsPrivate
                        })
                        .ToList()
                })
                .ToList();

            return Ok(result);
        }

        [HttpGet("report")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public-heavy")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<IEnumerable<object>>> GetMovesetsReport([FromQuery] bool includeHidden = false)
        {
            var user = await _userManager.GetRequesterSummaryAsync(_context, User);

            var seeAll = user?.UserTypeId == UserTypes.Admin && includeHidden;
            var userModderId = user?.ModderId;

            var query = _context.Movesets
                .AsNoTracking()
                .Include(m => m.ReleaseState)
                .Include(m => m.MovesetModders)
                    .ThenInclude(mm => mm.Modder)
                        .ThenInclude(md => md.User)
                .Include(m => m.MovesetArticles)
                    .ThenInclude(ma => ma.Article)
                .Include(m => m.MovesetHooks)
                    .ThenInclude(mh => mh.Hook)
                .Select(m => new
                {
                    Moveset = m,

                    LatestLog = _context.ActionLogs
                        .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == m.MovesetId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault(),

                    IsOwner = userModderId != null &&
                        (m.MovesetModders.Any(mm => mm.ModderId == userModderId)
                         || m.MovesetEditors.Any(me => me.ModderId == userModderId))
                })
                .AsQueryable();

            if (!seeAll)
            {
                query = query.Where(x =>
                    x.LatestLog == null ||
                    !AcceptanceStates.Blocked.Contains(x.LatestLog.AcceptanceStateId) ||
                    x.IsOwner
                );
            }

            var movesets = await query
                // Privacy
                .OrderBy(x => x.Moveset.PrivateModder == true)
                .ThenBy(x =>
                    x.Moveset.PrivateModder == true
                        ? null
                        : x.Moveset.MovesetModders
                            .OrderBy(mm => mm.SortOrder)
                            .Select(mm => mm.Modder.Name)
                            .FirstOrDefault()
                )
                // Release date
                .ThenBy(x => x.Moveset.ReleaseDate == null)
                .ThenBy(x => x.Moveset.ReleaseDate)
                // Select
                .Select(x => new
                {
                    // Modders
                    Modders =
                        x.Moveset.PrivateModder == true
                            ? "???"
                            : string.Join(", ",
                                x.Moveset.MovesetModders
                                    .OrderBy(mm => mm.SortOrder)
                                    .Select(mm => mm.Modder.Name)
                            ),

                    // Main info
                    ModdedCharName =
                        x.Moveset.PrivateMoveset == true
                            ? "???"
                            : x.Moveset.ModdedCharName,

                    Subtitle =
                        x.Moveset.PrivateMoveset == true
                            ? null
                            : x.Moveset.Subtitle,

                    VanillaCharName = x.Moveset.VanillaCharInternalName,

                    SlottedId =
                        x.Moveset.PrivateMoveset == true
                            ? "???"
                            : x.Moveset.SlottedId,

                    ReplacementId =
                        x.Moveset.PrivateMoveset == true
                            ? "???"
                            : x.Moveset.ReplacementId,

                    SlotsRange = $"c{x.Moveset.SlotsStart:D3}-c{x.Moveset.SlotsEnd:D3}",
                    ReleaseState = x.Moveset.ReleaseState.ReleaseStateName,

                    // Flags
                    HasGlobalOpff = x.Moveset.HasGlobalOpff,
                    HasCharacterOpff = x.Moveset.HasCharacterOpff,
                    HasAgentInit = x.Moveset.HasAgentInit,
                    HasGlobalOnLinePre = x.Moveset.HasGlobalOnLinePre,
                    HasGlobalOnLineEnd = x.Moveset.HasGlobalOnLineEnd,

                    // Articles
                    Articles = x.Moveset.MovesetArticles.Select(ma => new
                    {
                        Original = $"{ma.Article.VanillaCharInternalName}_{ma.Article.ArticleName}",
                        Cloned = x.Moveset.PrivateMoveset == true ? "???" : ma.ModdedName
                    }),

                    // Hooks
                    Hooks = x.Moveset.MovesetHooks.Select(mh => new
                    {
                        mh.Hook.Offset,
                        mh.Hook.Description,
                        Usage = x.Moveset.PrivateMoveset == true ? "???" : mh.Description
                    })
                })
                .ToListAsync();

            return Ok(movesets);
        }
    }
}
