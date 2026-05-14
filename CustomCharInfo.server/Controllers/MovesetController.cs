using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Helpers;

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
            {
                query = query.Where(x => x.Moveset.SeriesId == seriesId);
                query = query.Where(x => x.Moveset.PrivateMoveset != true || x.IsOwner);
            }

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
                    x.LatestLog == null
                    || !blockedStates.Contains(x.LatestLog.AcceptanceStateId)
                    || x.IsOwner
                    || (modderId.HasValue && x.Moveset.MovesetModders.Any(mm => mm.ModderId == modderId))
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

                "alpha" => query
                    .OrderBy(x => x.Moveset.PrivateMoveset)
                    .ThenBy(x => x.Moveset.PrivateMoveset == true
                        ? x.Moveset.MovesetModders
                            .OrderBy(mm => mm.SortOrder)
                            .Select(mm => mm.Modder.User.UserName ?? mm.Modder.Name)
                            .FirstOrDefault()
                        : x.Moveset.ModdedCharName),

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
                                .Where(mm => mm.Modder.User == null || mm.Modder.User.Problematic != true)
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

        [HttpGet("{idOrSlottedId}")]
        public async Task<ActionResult<MovesetDetailDto>> GetMoveset(string idOrSlottedId)
        {
            var userId = _userManager.GetUserId(User);
            var user = userId != null
                ? await _context.Users
                    .Select(u => new { u.Id, u.UserTypeId, u.ModderId })
                    .FirstOrDefaultAsync(u => u.Id == userId)
                : null;

            bool isNumericId = int.TryParse(idOrSlottedId, out int movesetId);

            var moveset = await _context.Movesets
                        .Where(m =>
                           isNumericId
                               ? m.MovesetId == movesetId
                               : m.SlottedId == idOrSlottedId
                        )
                .Select(m => new MovesetDetailDto
                {
                    MovesetId = m.MovesetId,
                    ModdedCharName = m.ModdedCharName,
                    VanillaCharInternalName = m.VanillaCharInternalName,
                    SeriesId = m.SeriesId,
                    SlottedId = m.SlottedId,
                    ReplacementId = m.ReplacementId,
                    SlotsStart = m.SlotsStart,
                    SlotsEnd = m.SlotsEnd,
                    ReleaseStateId = m.ReleaseStateId,

                    HasGlobalOpff = m.HasGlobalOpff,
                    HasCharacterOpff = m.HasCharacterOpff,
                    HasAgentInit = m.HasAgentInit,
                    HasGlobalOnLinePre = m.HasGlobalOnLinePre,
                    HasGlobalOnLineEnd = m.HasGlobalOnLineEnd,

                    ModPageUrl = m.ModPageUrl,
                    GamebananaWipId = m.GamebananaWipId,
                    BackgroundColor = m.BackgroundColor,
                    ModsWikiLink = m.ModsWikiLink,
                    ReleaseDate = m.ReleaseDate,
                    ModpackName = m.ModpackName,
                    SourceCode = m.SourceCode,

                    AdminPick = m.AdminPick,
                    PrivateMoveset = m.PrivateMoveset,
                    PrivateModder = m.PrivateModder,

                    ThumbhImageUrl = m.ThumbhImageUrl,
                    MovesetHeroImageUrl = m.MovesetHeroImageUrl,

                    VanillaChar = m.VanillaChar == null ? null : new VanillaCharDto2
                    {
                        VanillaCharInternalName = m.VanillaChar.VanillaCharInternalName,
                        DisplayName = m.VanillaChar.DisplayName
                    },

                    ReleaseState = m.ReleaseState == null ? null : new ReleaseStateDto2
                    {
                        ReleaseStateId = m.ReleaseState.ReleaseStateId,
                        ReleaseStateName = m.ReleaseState.ReleaseStateName
                    },

                    Series = m.Series == null ? null : new SeriesDto2
                    {
                        SeriesId = m.Series.SeriesId,
                        SeriesName = m.Series.SeriesName,
                        SeriesIconUrl = m.Series.SeriesIconUrl
                    },

                    MovesetDependencies = m.MovesetDependencies
                        .Select(md => new MovesetDependencyDto2
                        {
                            Dependency = new DependencyDto2
                            {
                                DependencyId = md.Dependency.DependencyId,
                                Name = md.Dependency.Name,
                                DownloadLink = md.Dependency.DownloadLink
                            }
                        }).ToList(),

                    MovesetModders = m.MovesetModders
                        .OrderBy(mm => mm.SortOrder)
                        .Select(mm => new MovesetModderDto2
                        {
                            SortOrder = mm.SortOrder,
                            Modder = new ModderDto2
                            {
                                ModderId = mm.Modder.ModderId,
                                Name = mm.Modder.Name,
                                Bio = mm.Modder.Bio,
                                GamebananaId = mm.Modder.GamebananaId,
                                DiscordUsername = mm.Modder.DiscordUsername,
                                UserId = mm.Modder.UserId
                            }
                        }).ToList(),

                    MovesetArticles = m.MovesetArticles
                        .Select(ma => new MovesetArticleDto2
                        {
                            ModdedName = ma.ModdedName,
                            Description = ma.Description,
                            Article = new ArticleDto2
                            {
                                ArticleId = ma.Article.ArticleId,
                                VanillaCharInternalName = ma.Article.VanillaCharInternalName,
                                ArticleName = ma.Article.ArticleName
                            }
                        }).ToList(),

                    MovesetHooks = m.MovesetHooks
                        .Select(mh => new MovesetHookDto2
                        {
                            Description = mh.Description,
                            Hook = new HookDto2
                            {
                                HookId = mh.Hook.HookId,
                                Offset = mh.Hook.Offset,
                                Description = mh.Hook.Description,
                                HookableStatusId = mh.Hook.HookableStatusId
                            }
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (moveset == null)
                return NotFound();

            // Sort MovesetModders
            moveset.MovesetModders = moveset.MovesetModders
                .OrderBy(mm => mm.SortOrder)
                .ToList();

            // Check ownership
            bool isOwner =
                user != null
                && user.ModderId != null
                && moveset.MovesetModders.Any(mm => mm.Modder.ModderId == user.ModderId);

            // Hide if private
            if ((bool)moveset.PrivateMoveset && user?.UserTypeId != 3 && !isOwner)
                return NotFound();

            // Find latest log
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 1 && a.ItemId == moveset.MovesetId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            var blockedStates = new[] { 2, 4, 6 };

            // Enforce rules
            if (user?.UserTypeId != 3)
            {
                if (
                    latestLog != null
                    && blockedStates.Contains(latestLog.AcceptanceStateId)
                    && !isOwner
                )
                {
                    return Forbid();
                }
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
                ModPageUrl = dto.ModPageUrl,
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
                Notes = dto.Notes ?? "",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMoveset), new { idOrSlottedId = moveset.MovesetId }, moveset);
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

            // Batch lookups for human-readable diff values
            var allModderIds = moveset.MovesetModders.Select(m => m.ModderId)
                .Concat(dto.ModderIds ?? new()).Distinct().ToList();
            var allDepIds = moveset.MovesetDependencies.Select(d => d.DependencyId)
                .Concat(dto.DependencyIds ?? new()).Distinct().ToList();
            var allSeriesIds = new[] { moveset.SeriesId, (int?)dto.SeriesId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var allReleaseIds = new[] { moveset.ReleaseStateId, (int?)dto.ReleaseStateId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var allVanillaNames = new[] { moveset.VanillaCharInternalName, dto.VanillaCharInternalName }
                .Where(x => x != null).Distinct().ToList();
            var allHookIds = moveset.MovesetHooks.Select(h => h.HookId)
                .Concat((dto.Hooks ?? new()).Select(h => h.HookId)).Distinct().ToList();
            var allArticleIds = moveset.MovesetArticles.Select(a => a.ArticleId)
                .Concat((dto.Articles ?? new()).Select(a => a.ArticleId)).Distinct().ToList();

            var modderNameMap = await _context.Modders
                .Where(m => allModderIds.Contains(m.ModderId))
                .ToDictionaryAsync(m => m.ModderId, m => m.Name ?? m.ModderId.ToString());
            var depNameMap = await _context.Dependencies
                .Where(d => allDepIds.Contains(d.DependencyId))
                .ToDictionaryAsync(d => d.DependencyId, d => d.Name);
            var seriesNameMap = await _context.Series
                .Where(s => allSeriesIds.Contains(s.SeriesId))
                .ToDictionaryAsync(s => s.SeriesId, s => s.SeriesName);
            var releaseStateNameMap = await _context.ReleaseStates
                .Where(rs => allReleaseIds.Contains(rs.ReleaseStateId))
                .ToDictionaryAsync(rs => rs.ReleaseStateId, rs => rs.ReleaseStateName);
            var vanillaNameMap = await _context.VanillaChars
                .Where(vc => allVanillaNames.Contains(vc.VanillaCharInternalName))
                .ToDictionaryAsync(vc => vc.VanillaCharInternalName, vc => vc.DisplayName);
            var hookLabelMap = await _context.Hooks
                .Where(h => allHookIds.Contains(h.HookId))
                .ToDictionaryAsync(h => h.HookId, h => $"0x{h.Offset} ({h.Description})");
            var articleLabelMap = await _context.Articles
                .Where(a => allArticleIds.Contains(a.ArticleId))
                .ToDictionaryAsync(a => a.ArticleId, a => $"{a.VanillaCharInternalName}_{a.ArticleName}");

            string ResolveInt(Dictionary<int, string> map, int? key) =>
                key.HasValue && map.TryGetValue(key.Value, out var v) ? v : key?.ToString() ?? "";
            string ResolveStr(Dictionary<string, string> map, string? key) =>
                key != null && map.TryGetValue(key, out var v) ? v : key ?? "";

            // Snapshot before mutation for diff
            var snap = new
            {
                moveset.ModdedCharName,
                VanillaChar   = ResolveStr(vanillaNameMap, moveset.VanillaCharInternalName),
                Series        = ResolveInt(seriesNameMap, moveset.SeriesId),
                moveset.SlottedId, moveset.ReplacementId, moveset.SlotsStart, moveset.SlotsEnd,
                Availability  = ResolveInt(releaseStateNameMap, moveset.ReleaseStateId),
                moveset.HasGlobalOpff, moveset.HasCharacterOpff,
                moveset.HasAgentInit, moveset.HasGlobalOnLinePre, moveset.HasGlobalOnLineEnd,
                moveset.ModPageUrl, moveset.GamebananaWipId, moveset.ThumbhImageUrl,
                moveset.MovesetHeroImageUrl, moveset.BackgroundColor, moveset.ModsWikiLink,
                ReleaseDate   = moveset.ReleaseDate?.ToString("yyyy-MM-dd"),
                moveset.ModpackName, moveset.SourceCode, moveset.PrivateMoveset, moveset.PrivateModder,
                Modders       = string.Join(", ", moveset.MovesetModders
                    .Select(m => modderNameMap.TryGetValue(m.ModderId, out var n) ? n : m.ModderId.ToString())
                    .OrderBy(x => x)),
                Dependencies  = string.Join(", ", moveset.MovesetDependencies
                    .Select(d => depNameMap.TryGetValue(d.DependencyId, out var n) ? n : d.DependencyId.ToString())
                    .OrderBy(x => x)),
                Hooks         = string.Join(", ", moveset.MovesetHooks
                    .Select(h => hookLabelMap.TryGetValue(h.HookId, out var l) ? $"{l}: {h.Description}" : h.Description)
                    .OrderBy(x => x)),
                Articles      = string.Join(", ", moveset.MovesetArticles
                    .Select(a => articleLabelMap.TryGetValue(a.ArticleId, out var l) ? $"{l} as {a.ModdedName}" : a.ModdedName)
                    .OrderBy(x => x)),
            };

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

            var newModders = string.Join(", ", (dto.ModderIds ?? new())
                .Select(mid => modderNameMap.TryGetValue(mid, out var n) ? n : mid.ToString())
                .OrderBy(x => x));
            var newDeps = string.Join(", ", (dto.DependencyIds ?? new())
                .Select(did => depNameMap.TryGetValue(did, out var n) ? n : did.ToString())
                .OrderBy(x => x));
            var newHooks = string.Join(", ", (dto.Hooks ?? new())
                .Select(h => hookLabelMap.TryGetValue(h.HookId, out var l) ? $"{l}: {h.Description}" : h.Description)
                .OrderBy(x => x));
            var newArticles = string.Join(", ", (dto.Articles ?? new())
                .Select(a => articleLabelMap.TryGetValue(a.ArticleId, out var l) ? $"{l} as {a.ModdedName}" : a.ModdedName)
                .OrderBy(x => x));

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Name",            snap.ModdedCharName,    dto.ModdedCharName),
                ("VanillaChar",     snap.VanillaChar,       ResolveStr(vanillaNameMap, dto.VanillaCharInternalName)),
                ("Series",          snap.Series,            ResolveInt(seriesNameMap, dto.SeriesId)),
                ("SlottedId",       snap.SlottedId,         dto.SlottedId),
                ("ReplacementId",   snap.ReplacementId,     dto.ReplacementId),
                ("SlotsStart",      snap.SlotsStart,        dto.SlotsStart),
                ("SlotsEnd",        snap.SlotsEnd,          dto.SlotsEnd),
                ("Availability",    snap.Availability,      ResolveInt(releaseStateNameMap, dto.ReleaseStateId)),
                ("GlobalOPFF",      snap.HasGlobalOpff,     dto.HasGlobalOpff),
                ("CharacterOPFF",   snap.HasCharacterOpff,  dto.HasCharacterOpff),
                ("AgentInit",       snap.HasAgentInit,      dto.HasAgentInit),
                ("GlobalOnLinePre", snap.HasGlobalOnLinePre, dto.HasGlobalOnLinePre),
                ("GlobalOnLineEnd", snap.HasGlobalOnLineEnd, dto.HasGlobalOnLineEnd),
                ("ModPage",         snap.ModPageUrl,        dto.ModPageUrl),
                ("GBWip",           snap.GamebananaWipId,   dto.GamebananaWipId),
                ("Thumbnail",       snap.ThumbhImageUrl,    dto.ThumbhImageUrl),
                ("HeroImage",       snap.MovesetHeroImageUrl, dto.MovesetHeroImageUrl),
                ("BgColor",         snap.BackgroundColor,   dto.BackgroundColor),
                ("ModsWiki",        snap.ModsWikiLink,      dto.ModsWikiLink),
                ("ReleaseDate",     snap.ReleaseDate,       dto.ReleaseDate?.ToString("yyyy-MM-dd")),
                ("Modpack",         snap.ModpackName,       dto.ModpackName),
                ("SourceCode",      snap.SourceCode,        dto.SourceCode),
                ("Private",         snap.PrivateMoveset,    dto.PrivateMoveset),
                ("PrivateModder",   snap.PrivateModder,     dto.PrivateModder),
                ("Modders",         snap.Modders,           newModders),
                ("Dependencies",    snap.Dependencies,      newDeps),
                ("Hooks",           snap.Hooks,             newHooks),
                ("Articles",        snap.Articles,          newArticles),
            });

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 1,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = dto.Notes ?? "",
                Diff = diff,
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
