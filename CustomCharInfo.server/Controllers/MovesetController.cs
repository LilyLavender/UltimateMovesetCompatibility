using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Helpers;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/movesets")]
    public partial class MovesetController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public MovesetController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
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
            [FromQuery] string? likedByUserId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = int.MaxValue
        )
        {
            var user = await _userManager.GetRequesterSummaryAsync(_context, User);

            
            bool isAdmin = user?.UserTypeId == UserTypes.Admin;
            int? currentModderId = user?.ModderId;

            var query = _context.Movesets
                .AsNoTracking()
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.MovesetModders)
                    .ThenInclude(mm => mm.Modder)
                        .ThenInclude(modder => modder.User)
                .Include(m => m.MovesetArticles)
                    .ThenInclude(ma => ma.Article)
                .Include(m => m.VanillaChar)
                .Select(m => new
                {
                    Moveset = m,

                    LatestLog = _context.ActionLogs
                        .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == m.MovesetId)
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

            if (!string.IsNullOrEmpty(likedByUserId))
                query = query.Where(x =>
                    _context.MovesetLikes.Any(ml => ml.MovesetId == x.Moveset.MovesetId && ml.UserId == likedByUserId));

            if (privateOnly.HasValue)
                query = query.Where(x => x.Moveset.PrivateMoveset == privateOnly.Value);

            if (adminPickOnly == true)
                query = query.Where(x => (bool)x.Moveset.AdminPick);

            if (betaOnly == true)
                query = query.Where(x => x.Moveset.ReleaseStateId == ReleaseStates.OpenBeta);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (upcomingOnly == true)
                query = query.Where(x => x.Moveset.ReleaseDate > today);

            if (recentOnly == true)
                query = query.Where(x => x.Moveset.ReleaseDate <= today);

            // Visibility
            if (!isAdmin)
            {
                query = query.Where(x =>
                    x.LatestLog == null
                    || !AcceptanceStates.Blocked.Contains(x.LatestLog.AcceptanceStateId)
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

                "popularity" => query
                    .OrderByDescending(x => _context.MovesetLikes.Count(ml => ml.MovesetId == x.Moveset.MovesetId))
                    .ThenBy(x => x.Moveset.ModdedCharName),

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
                    x.Moveset.IsJokeMoveset,

                    Subtitle =
                        x.Moveset.PrivateMoveset == true && !(isAdmin || x.IsOwner)
                            ? null
                            : x.Moveset.Subtitle,

                    LikeCount = _context.MovesetLikes.Count(ml => ml.MovesetId == x.Moveset.MovesetId),

                    VanillaCharName = x.Moveset.VanillaCharInternalName,
                    VanillaCharDisplayName = x.Moveset.VanillaChar != null
                        ? x.Moveset.VanillaChar.DisplayName
                        : x.Moveset.VanillaCharInternalName,
                    SeriesName = x.Moveset.PrivateMoveset == true && !(isAdmin || x.IsOwner)
                        ? null
                        : (x.Moveset.Series != null ? x.Moveset.Series.SeriesName : null),
                    ArticleNames = x.Moveset.MovesetArticles
                        .Select(ma => $"{ma.Article.VanillaCharInternalName}_{ma.Article.ArticleName}"),
                    HasSourceCode = x.Moveset.SourceCode != null && x.Moveset.SourceCode != "",
                    HasModsWikiLink = x.Moveset.ModsWikiLink != null && x.Moveset.ModsWikiLink != "",
                })
                .ToListAsync();

            return Ok(movesets);
        }

        private const int MaxSearchResults = 20;

        // Minimal-shape autocomplete: SlottedId + MovesetId + name,
        // so results plug directly into compatibility/plugins/predict without a second lookup.
        [HttpGet("search")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public-heavy")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<IEnumerable<object>>> SearchMovesets([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(Array.Empty<object>());

            var user = await _userManager.GetRequesterSummaryAsync(_context, User);

            bool isAdmin = user?.UserTypeId == UserTypes.Admin;
            int? currentModderId = user?.ModderId;
                        var lowered = q.Trim().ToLower();

            var query = _context.Movesets
                .AsNoTracking()
                .Where(m => m.ModdedCharName.ToLower().Contains(lowered))
                .Select(m => new
                {
                    m.MovesetId,
                    m.SlottedId,
                    m.ModdedCharName,
                    m.PrivateMoveset,
                    IsOwner = currentModderId != null && m.MovesetModders.Any(mm => mm.ModderId == currentModderId),
                    LatestLogState = _context.ActionLogs
                        .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == m.MovesetId)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => (int?)a.AcceptanceStateId)
                        .FirstOrDefault()
                });

            if (!isAdmin)
            {
                query = query.Where(x =>
                    (x.PrivateMoveset != true || x.IsOwner)
                    && (x.LatestLogState == null || !AcceptanceStates.Blocked.Contains(x.LatestLogState.Value) || x.IsOwner));
            }

            var results = await query
                .OrderBy(x => x.ModdedCharName)
                .Take(MaxSearchResults)
                .Select(x => new { x.MovesetId, x.SlottedId, Name = x.ModdedCharName })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("{idOrSlottedId}")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<MovesetDetailDto>> GetMoveset(string idOrSlottedId)
        {
            var user = await _userManager.GetRequesterSummaryAsync(_context, User);

            bool isNumericId = int.TryParse(idOrSlottedId, out int movesetId);
            var loweredSlottedId = idOrSlottedId.ToLower();

            var moveset = await _context.Movesets
                        .Where(m =>
                           isNumericId
                               ? m.MovesetId == movesetId
                               : m.SlottedId.ToLower() == loweredSlottedId
                        )
                .Select(MovesetDetailProjection.FromMoveset)
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
            if ((bool)moveset.PrivateMoveset && user?.UserTypeId != UserTypes.Admin && !isOwner)
                return NotFound();

            // Find latest log
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == moveset.MovesetId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            
            // Enforce rules
            if (user?.UserTypeId != UserTypes.Admin)
            {
                if (
                    latestLog != null
                    && AcceptanceStates.Blocked.Contains(latestLog.AcceptanceStateId)
                    && !isOwner
                )
                {
                    return Forbid();
                }
            }

            moveset.LikeCount = await _context.MovesetLikes.CountAsync(ml => ml.MovesetId == moveset.MovesetId);
            moveset.UserLiked = user != null && await _context.MovesetLikes.AnyAsync(ml => ml.MovesetId == moveset.MovesetId && ml.UserId == user.Id);

            return Ok(moveset);
        }
    }
}
