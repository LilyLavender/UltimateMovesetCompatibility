using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/movesets")]
    public class MovesetController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public MovesetController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMovesets(
            [FromQuery] int? seriesId,
            [FromQuery] int? releaseStateId,
            [FromQuery] int? modderId,
            [FromQuery] string? sort,
            [FromQuery] bool? privateOnly,
            [FromQuery] bool? adminPickOnly,
            [FromQuery] bool? upcomingOnly,
            [FromQuery] bool? recentOnly,
            [FromQuery] bool? betaOnly,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = int.MaxValue
        )
        {
            var userId = _userManager.GetUserId(User);
            var user = userId != null
                ? await _context.Users
                    .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                    .FirstOrDefaultAsync(u => u.Id == userId)
                : null;

            var blockedStates = new[] { 2, 4, 6 };

            bool isAdmin = user?.UserTypeId == 3;
            int? currentModderId = user?.ModderId;

            var query = _context.Movesets
                .AsNoTracking()
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.MovesetModders)
                    .ThenInclude(mm => mm.Modder)
                        .ThenInclude(modder => modder.User)
                .Select(m => new
                {
                    Moveset = m,

                    LatestLog = _context.ActionLogs
                        .Where(a => a.ItemTypeId == 1 && a.ItemId == m.MovesetId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault(),

                    IsOwner = currentModderId != null &&
                        m.MovesetModders.Any(mm => mm.ModderId == currentModderId)
                })
                .AsQueryable();

            // Filters
            if (seriesId.HasValue)
                query = query.Where(x => x.Moveset.SeriesId == seriesId);

            if (releaseStateId.HasValue)
                query = query.Where(x => x.Moveset.ReleaseStateId == releaseStateId);

            if (modderId.HasValue)
                query = query.Where(x =>
                    x.Moveset.MovesetModders.Any(mm => mm.ModderId == modderId));

            if (privateOnly.HasValue)
                query = query.Where(x => x.Moveset.PrivateMoveset == privateOnly.Value);

            if (adminPickOnly == true)
                query = query.Where(x => (bool)x.Moveset.AdminPick);

            if (betaOnly == true)
                query = query.Where(x => x.Moveset.ReleaseStateId == 4);

            if (upcomingOnly == true)
                query = query.Where(x => x.Moveset.ReleaseDate > DateTime.UtcNow);

            if (recentOnly == true)
                query = query.Where(x => x.Moveset.ReleaseDate <= DateTime.UtcNow);

            // Visibility
            if (!isAdmin)
            {
                query = query.Where(x =>
                    x.LatestLog == null ||
                    !blockedStates.Contains(x.LatestLog.AcceptanceStateId) ||
                    x.IsOwner
                );
            }

            // Sort
            query = sort switch
            {
                "releaseDate" => query
                    .OrderBy(x => x.Moveset.PrivateMoveset)
                    .ThenBy(x => x.Moveset.ReleaseDate == null)
                    .ThenByDescending(x => x.Moveset.ReleaseDate),

                "releaseDateAsc" => query
                    .OrderBy(x => x.Moveset.PrivateMoveset)
                    .ThenBy(x => x.Moveset.ReleaseDate == null)
                    .ThenBy(x => x.Moveset.ReleaseDate),

                _ => query
                    .OrderBy(x => x.Moveset.PrivateMoveset)
                    .ThenBy(x => x.Moveset.ModdedCharName)
            };

            // Projection
            var movesets = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Moveset.MovesetId,

                    ModdedCharName =
                        x.Moveset.PrivateMoveset == true && !(isAdmin || x.IsOwner)
                            ? "???"
                            : x.Moveset.ModdedCharName,

                    SeriesIconUrl =
                        x.Moveset.PrivateMoveset == true && !(isAdmin || x.IsOwner)
                            ? null
                            : x.Moveset.Series.SeriesIconUrl,

                    BackgroundColor = x.Moveset.BackgroundColor,

                    ThumbhImageUrl =
                        x.Moveset.PrivateMoveset == true && !(isAdmin || x.IsOwner)
                            ? null
                            : x.Moveset.ThumbhImageUrl,

                    ReleaseState = x.Moveset.ReleaseState.ReleaseStateName,

                    Modders =
                        x.Moveset.PrivateModder == true && !(isAdmin || x.IsOwner)
                            ? new List<string> { "???" }
                            : x.Moveset.MovesetModders
                                .OrderBy(mm => mm.SortOrder)
                                .Select(mm => mm.Modder.User.UserName ?? mm.Modder.Name)
                                .ToList(),

                    x.Moveset.ReleaseDate,
                    x.Moveset.AdminPick,
                    x.Moveset.PrivateMoveset,
                })
                .ToListAsync();

            return Ok(movesets);
        }

        [HttpGet("report")]
        public async Task<ActionResult<IEnumerable<object>>> GetMovesetsReport()
        {
            var userId = _userManager.GetUserId(User);
            var user = userId != null
                ? await _context.Users
                    .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                    .FirstOrDefaultAsync(u => u.Id == userId)
                : null;

            var blockedStates = new[] { 2, 4, 6 };

            var isAdmin = user?.UserTypeId == 3;
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
                        .Where(a => a.ItemTypeId == 1 && a.ItemId == m.MovesetId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault(),

                    IsOwner = userModderId != null &&
                        m.MovesetModders.Any(mm => mm.ModderId == userModderId)
                })
                .AsQueryable();

            if (user == null || user.UserTypeId != 3)
            {
                query = query.Where(x =>
                    x.LatestLog == null ||
                    !blockedStates.Contains(x.LatestLog.AcceptanceStateId) ||
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
                        Original = $"{ma.Article.VanillaCharInternalName}-{ma.Article.ArticleName}",
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

        [HttpGet("{id}")]
        public async Task<ActionResult<Moveset>> GetMoveset(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = userId != null
                ? await _context.Users
                    .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                    .FirstOrDefaultAsync(u => u.Id == userId)
                : null;

            var moveset = await _context.Movesets
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.VanillaChar)
                .Include(m => m.MovesetDependencies).ThenInclude(md => md.Dependency)
                .Include(m => m.MovesetModders).ThenInclude(mm => mm.Modder).ThenInclude(modder => modder.User)
                .Include(m => m.MovesetArticles).ThenInclude(ma => ma.Article)
                .Include(m => m.MovesetHooks).ThenInclude(mh => mh.Hook).ThenInclude(h => h.HookableStatus)
                .FirstOrDefaultAsync(m => m.MovesetId == id);

            if (moveset == null)
                return NotFound();

            // Sort MovesetModders
            moveset.MovesetModders = moveset.MovesetModders
                .OrderBy(mm => mm.SortOrder)
                .ToList();

            // Check ownership
            bool isOwner =
                user != null &&
                user.ModderId != null &&
                moveset.MovesetModders.Any(mm => mm.ModderId == user.ModderId);

            // Hide if private
            if ((bool)moveset.PrivateMoveset && user?.UserTypeId != 3 && !isOwner)
                return NotFound();

            // Find latest log
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 1 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            var blockedStates = new[] { 2, 4, 6 };

            // Enforce rules
            if (user?.UserTypeId != 3)
            {
                if (
                    latestLog != null &&
                    blockedStates.Contains(latestLog.AcceptanceStateId) &&
                    !isOwner
                )
                {
                    return Forbid();
                }
            }

            return Ok(moveset);
        }

        [HttpGet("by-internal-id/{slottedId}")]
        public async Task<ActionResult<Moveset>> GetMovesetBySlottedId(string slottedId)
        {
            var userId = _userManager.GetUserId(User);
            var user = userId != null
                ? await _context.Users
                    .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                    .FirstOrDefaultAsync(u => u.Id == userId)
                : null;

            var moveset = await _context.Movesets
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.VanillaChar)
                .Include(m => m.MovesetDependencies).ThenInclude(md => md.Dependency)
                .Include(m => m.MovesetModders).ThenInclude(mm => mm.Modder).ThenInclude(modder => modder.User)
                .Include(m => m.MovesetArticles).ThenInclude(ma => ma.Article)
                .Include(m => m.MovesetHooks).ThenInclude(mh => mh.Hook).ThenInclude(h => h.HookableStatus)
                .FirstOrDefaultAsync(m => m.SlottedId == slottedId);

            if (moveset == null)
                return NotFound();

            // Find latest action log
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 1 && a.ItemId == moveset.MovesetId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            var blockedStates = new[] { 2, 4, 6 };

            // Check ownership
            bool isOwner =
                user?.ModderId != null &&
                moveset.MovesetModders.Any(mm => mm.ModderId == user.ModderId);

            // Enforce rules
            if (
                user?.UserTypeId != 3 &&
                !isOwner &&
                latestLog != null &&
                blockedStates.Contains(latestLog.AcceptanceStateId)
            )
            {
                return Forbid();
            }

            return Ok(moveset);
        }

        [HttpPost]
        public async Task<ActionResult<Moveset>> PostMoveset(CreateMovesetDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            if (dto.ModderIds == null || !dto.ModderIds.Any())
                return BadRequest("At least one ModderId is required.");

            var moveset = new Moveset
            {
                ModdedCharName = dto.ModdedCharName,
                VanillaCharInternalName = dto.VanillaCharInternalName,
                SeriesId = dto.SeriesId,
                SlottedId = dto.SlottedId,
                ReplacementId = dto.ReplacementId,
                SlotsStart = dto.SlotsStart ?? 0,
                SlotsEnd = dto.SlotsEnd ?? 0,
                ReleaseStateId = dto.ReleaseStateId,
                HasGlobalOpff = dto.HasGlobalOpff,
                HasCharacterOpff = dto.HasCharacterOpff,
                HasAgentInit = dto.HasAgentInit,
                HasGlobalOnLinePre = dto.HasGlobalOnLinePre,
                HasGlobalOnLineEnd = dto.HasGlobalOnLineEnd,
                GamebananaPageId = dto.GamebananaPageId ?? 0,
                GamebananaWipId = dto.GamebananaWipId ?? 0,
                ThumbhImageUrl = dto.ThumbhImageUrl,
                MovesetHeroImageUrl = dto.MovesetHeroImageUrl,
                BackgroundColor = dto.BackgroundColor,
                ModsWikiLink = dto.ModsWikiLink,
                ReleaseDate = dto.ReleaseDate,
                ModpackName = dto.ModpackName,
                SourceCode = dto.SourceCode,
                PrivateMoveset = dto.PrivateMoveset,
                PrivateModder = dto.PrivateModder,
                MovesetModders = dto.ModderIds.Select(id => new MovesetModder { ModderId = id }).ToList(),
                MovesetDependencies = dto.DependencyIds?.Select(id => new MovesetDependency { DependencyId = id }).ToList() ?? new List<MovesetDependency>(),
                MovesetHooks = dto.Hooks?.Select(h => new MovesetHook
                {
                    HookId = h.HookId,
                    Description = h.Description
                }).ToList() ?? new List<MovesetHook>(),
                MovesetArticles = dto.Articles?.Select(a => new MovesetArticle
                {
                    ArticleId = a.ArticleId,
                    ModdedName = a.ModdedName,
                    Description = a.Description
                }).ToList() ?? new List<MovesetArticle>()
            };

            _context.Movesets.Add(moveset);
            await _context.SaveChangesAsync();

            // Log action
            int newState = userFromId.UserTypeId == 3 ? 7 : 2;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 1,
                ItemId = moveset.MovesetId,
                AcceptanceStateId = 2,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMoveset), new { id = moveset.MovesetId }, moveset);
        }

        [Authorize]
        [HttpPost("set-admin-picks")]
        public async Task<IActionResult> SetAdminPicks([FromBody] List<int> adminPickIds)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            adminPickIds ??= new List<int>();

            // Fetch all movesets
            var movesets = await _context.Movesets.ToListAsync();
            foreach (var moveset in movesets)
            {
                moveset.AdminPick = adminPickIds.Contains(moveset.MovesetId);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Admin picks updated successfully",
                adminPickIds
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMoveset(int id, CreateMovesetDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            if (user == null || user.ModderId == null)
                return Forbid();

            var moveset = await _context.Movesets
                .Include(m => m.MovesetModders)
                .Include(m => m.MovesetDependencies)
                .Include(m => m.MovesetHooks)
                .Include(m => m.MovesetArticles)
                .FirstOrDefaultAsync(m => m.MovesetId == id);

            if (moveset == null)
                return NotFound();

            // Make sure current user is a modder on the moveset
            bool isModder = moveset.MovesetModders.Any(mm => mm.ModderId == user.ModderId);
            if (!isModder)
                return Forbid();

            // Find latest log; Don't allow editing if last acceptance state isn't what's expected.
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 1 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            if (latestLog?.AcceptanceStateId == 6)
                return Forbid();

            if (dto.ModderIds == null || !dto.ModderIds.Any())
            {
                return BadRequest("At least one ModderId is required.");
            }

            if (dto.DependencyIds == null || dto.Hooks == null || dto.Articles == null)
            {
                return BadRequest("DependencyIds, Hooks, and Articles must be present (even if empty).");
            }

            bool keyDetailsChanged =
                moveset.ModdedCharName != dto.ModdedCharName ||
                moveset.SlottedId != dto.SlottedId ||
                moveset.ReplacementId != dto.ReplacementId;

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
            moveset.GamebananaPageId = dto.GamebananaPageId ?? 0;
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

            // Sync (i know that guy!!) Modders
            _context.MovesetModders.RemoveRange(moveset.MovesetModders);
            moveset.MovesetModders = dto.ModderIds
            .Select((mid, index) => new MovesetModder 
            { 
                MovesetId = id, 
                ModderId = mid, 
                SortOrder = index 
            })
            .ToList();

            // Sync Dependencies
            _context.MovesetDependencies.RemoveRange(moveset.MovesetDependencies);
            moveset.MovesetDependencies = dto.DependencyIds
                .Select(did => new MovesetDependency { MovesetId = id, DependencyId = did })
                .ToList();

            // Sync Hooks
            _context.MovesetHooks.RemoveRange(moveset.MovesetHooks);
            moveset.MovesetHooks = dto.Hooks
                .Select(h => new MovesetHook
                {
                    MovesetId = id,
                    HookId = h.HookId,
                    Description = h.Description
                })
                .ToList();

            // Sync Articles
            _context.MovesetArticles.RemoveRange(moveset.MovesetArticles);
            moveset.MovesetArticles = dto.Articles
                .Select(a => new MovesetArticle
                {
                    MovesetId = id,
                    ArticleId = a.ArticleId,
                    ModdedName = a.ModdedName,
                    Description = a.Description
                })
                .ToList();

            // Log action
            int newState =
            user?.UserTypeId == 3
                ? 7 // admin auto-accept
                : keyDetailsChanged
                    ? 2 // hard admin
                    : (latestLog?.AcceptanceStateId == 2 || latestLog?.AcceptanceStateId == 4)
                        ? 2 // stay hard
                        : 1; // soft admin
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 1,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMoveset(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var moveset = await _context.Movesets.FindAsync(id);
            if (moveset == null)
                return NotFound();

            _context.Movesets.Remove(moveset);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
