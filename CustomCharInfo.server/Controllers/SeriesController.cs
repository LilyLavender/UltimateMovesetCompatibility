using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Helpers;

using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/series")]
    public class SeriesController : ControllerBase
    {
        // Series seeded at launch (the base game's own franchises) end at this ID;
        // anything higher was submitted by a modder.
        private const int LastVanillaSeriesId = 41;

        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public SeriesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<IActionResult> GetSeries(
            [FromQuery] bool? inSeriesList = false,
            [FromQuery] bool includeHidden = false
        )
        {
            var userId = _userManager.GetUserId(User);

            var userInfo = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .FirstOrDefaultAsync();

            var modderId = userInfo?.ModderId;
            // Admins count blocked movesets only when asked for hidden content explicitly.
            var seeAll = userInfo?.UserTypeId == UserTypes.Admin && includeHidden;
            
            // All series the user has a moveset in, as a credited modder or an editor
            var ownedMovesetIds = await MovesetAccess.EditableMovesetIdsAsync(_context, modderId);
            var movesetSeriesIds = await _context.Movesets
                .Where(m => ownedMovesetIds.Contains(m.MovesetId))
                .Select(m => m.SeriesId)
                .Distinct()
                .ToListAsync();

            // Get the most recent ActionLog for each series
            var latestLogs = await _context.ActionLogs
                .Where(log =>
                    log.ItemTypeId == ItemTypes.Series &&
                    movesetSeriesIds.Contains(log.ItemId))
                .GroupBy(log => log.ItemId)
                .Select(g => g.OrderByDescending(l => l.CreatedAt).First())
                .ToListAsync();

            // Only allow edit if latest log for series is pending user action
            var editableSeriesIds = latestLogs
                .Where(log =>
                    AcceptanceStates.PendingUser.Contains(log.AcceptanceStateId))
                .Select(log => log.ItemId)
                .ToHashSet();

            var userModderSeriesIds = movesetSeriesIds
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var seriesQuery = _context.Series
                .Select(s => new
                {
                    s.SeriesId,
                    s.SeriesName,
                    s.SeriesIconUrl,

                    // Count only non-private, non-blocked movesets (blocked only counted for admins asking for hidden content)
                    MovesetCount = _context.Movesets.Count(m =>
                        m.SeriesId == s.SeriesId &&
                        m.PrivateMoveset != true &&
                        (seeAll || !AcceptanceStates.Blocked.Contains(
                            _context.ActionLogs
                                .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == m.MovesetId)
                                .OrderByDescending(a => a.CreatedAt)
                                .Select(a => a.AcceptanceStateId)
                                .FirstOrDefault()
                        ))
                    ),

                    // Filtering
                    TotalMovesets = _context.Movesets.Count(m =>
                        m.SeriesId == s.SeriesId
                    ),

                    CanEdit = editableSeriesIds.Contains(s.SeriesId),
                    IsUserModder = userModderSeriesIds.Contains(s.SeriesId)
                });

            // Hide all added series where movesets are private
            if (inSeriesList == true)
            {
                seriesQuery = seriesQuery.Where(s =>
                    s.SeriesId <= LastVanillaSeriesId ||
                    s.TotalMovesets == 0 ||
                    s.MovesetCount > 0
                );
            }

            var seriesList = await seriesQuery.ToListAsync();

            return Ok(seriesList);
        }

        private const int MaxSearchResults = 20;

        [HttpGet("search")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public-heavy")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<IEnumerable<object>>> SearchSeries([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(Array.Empty<object>());

            var lowered = q.Trim().ToLower();
            var results = await _context.Series
                .AsNoTracking()
                .Where(s => s.SeriesName.ToLower().Contains(lowered))
                .OrderBy(s => s.SeriesName)
                .Take(MaxSearchResults)
                .Select(s => new { Id = s.SeriesId, Name = s.SeriesName })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("{id}")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<ReturnSeriesDto>> GetOneSeries(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = userId != null
                ? await _context.Users.FindAsync(userId)
                : null;
            var isAdmin = user.IsAdmin();
            var modderId = user?.ModderId;
            
            // Check if series exists
            var seriesExists = await _context.Series.AnyAsync(s => s.SeriesId == id);
            if (!seriesExists)
                return NotFound();

            // Check if series has any public movesets
            var hasPublicMovesets = await _context.Movesets
                .AnyAsync(m => m.SeriesId == id && m.PrivateMoveset != true);

            // Check if user owns or edits a moveset in the series
            var userOwnsMoveset = modderId != null && await _context.Movesets
                .AnyAsync(m => m.SeriesId == id &&
                    (m.MovesetModders.Any(mm => mm.ModderId == modderId)
                     || m.MovesetEditors.Any(me => me.ModderId == modderId)));

            if (!hasPublicMovesets && !isAdmin && !userOwnsMoveset)
                return Forbid();

            var series = await _context.Series
                .Where(s => s.SeriesId == id)
                .Select(s => new ReturnSeriesDto
                {
                    SeriesId = s.SeriesId,
                    SeriesName = s.SeriesName,
                    SeriesIconUrl = s.SeriesIconUrl,
                    UserOwnsMoveset = userOwnsMoveset,
                    MovesetCount = _context.Movesets.Count(m =>
                        m.SeriesId == s.SeriesId &&
                        m.PrivateMoveset != true &&
                        (isAdmin || !AcceptanceStates.Blocked.Contains(
                            _context.ActionLogs
                                .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == m.MovesetId)
                                .OrderByDescending(a => a.CreatedAt)
                                .Select(a => a.AcceptanceStateId)
                                .FirstOrDefault()
                        )))
                })
                .FirstOrDefaultAsync();

            return Ok(series);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSeries([FromBody] UpdateSeriesDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.SeriesName))
                return BadRequest("Series name is required.");

            var normalizedName = dto.SeriesName.Trim();

            var existingSeries = await _context.Series
                .AnyAsync(s => s.SeriesName.ToLower() == normalizedName.ToLower());

            if (existingSeries)
                return Conflict("A series with this name already exists.");

            var series = new Series
            {
                SeriesName = dto.SeriesName,
                SeriesIconUrl = dto.SeriesIconUrl
            };

            _context.Series.Add(series);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The AnyAsync check above is check-then-act; this catches a genuine race against
                // the unique index on SeriesName as a backstop.
                return Conflict("A series with this name already exists.");
            }

            // Log action
            var user = await _userManager.Users
                .Where(u => u.Id == userFromId.Id)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();
            int newState = user?.UserTypeId == UserTypes.Admin ? AcceptanceStates.AutoAccepted : AcceptanceStates.PendingAdminHard;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userFromId.Id,
                ItemTypeId = ItemTypes.Series,
                ItemId = series.SeriesId,
                AcceptanceStateId = newState,
                Notes = dto.Notes ?? "",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSeries), new { id = series.SeriesId }, series);
        }

        // Attaches an image uploaded just after a create, without writing an ActionLog entry or affecting review state.
        // Completes the create->upload->attach sequence started by CreateSeries.
        // Only fills the field if it's still empty,
        // so it can't be reused to swap an existing icon without going through the normal reviewed edit path.
        [Authorize]
        [HttpPatch("{id}/image")]
        public async Task<IActionResult> PatchSeriesImage(int id, [FromBody] SeriesImageDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            var series = await _context.Series.FindAsync(id);
            if (series == null)
                return NotFound();

            if (!string.IsNullOrEmpty(series.SeriesIconUrl))
                return Conflict("SeriesIconUrl is already set; use the full update endpoint to change it.");

            series.SeriesIconUrl = dto.SeriesIconUrl;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/request-edit")]
        [Authorize]
        public async Task<IActionResult> RequestSeriesEdit(int id, [FromBody] RequestEditSeriesDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            var modderId = userFromId.ModderId;
            if (modderId == null)
                return Forbid();

            var seriesExists = await _context.Series.AnyAsync(s => s.SeriesId == id);
            if (!seriesExists)
                return NotFound("Series not found.");

            var hasMoveset = await _context.Movesets
                .AnyAsync(m => m.SeriesId == id &&
                    (m.MovesetModders.Any(mm => mm.ModderId == modderId)
                     || m.MovesetEditors.Any(me => me.ModderId == modderId)));
            if (!hasMoveset)
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.Notes))
                return BadRequest("Notes are required when requesting a series edit.");

            var log = new ActionLog
            {
                UserId = userFromId.Id,
                ItemTypeId = ItemTypes.Series,
                ItemId = id,
                AcceptanceStateId = AcceptanceStates.PendingAdminSoft,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActionLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { log.ActionLogId });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSeries(int id, UpdateSeriesDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            var existingSeries = await _context.Series.FindAsync(id);
            if (existingSeries == null)
                return NotFound();

            // Find latest log for this series
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == ItemTypes.Series && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            // Edits are only accepted while an admin has handed the series back to the user.
            if (latestLog is null || !AcceptanceStates.PendingUser.Contains(latestLog.AcceptanceStateId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.SeriesName))
                return BadRequest("SeriesName is required.");

            var normalizedName = dto.SeriesName.Trim();

            var duplicateSeries = await _context.Series
                .AnyAsync(s =>
                    s.SeriesId != id &&
                    s.SeriesName.ToLower() == normalizedName.ToLower());

            if (duplicateSeries)
                return Conflict("A series with this name already exists.");

            var snapName = existingSeries.SeriesName;
            var snapIcon = existingSeries.SeriesIconUrl;

            existingSeries.SeriesName = normalizedName;
            existingSeries.SeriesIconUrl = dto.SeriesIconUrl;

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("SeriesName",    snapName, normalizedName),
                ("SeriesIconUrl", snapIcon, dto.SeriesIconUrl),
            });

            // Calculate new acceptance state
            var user = await _userManager.Users
                .Where(u => u.Id == userFromId.Id)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            int newState;
            if (user?.UserTypeId == UserTypes.Admin) { newState = AcceptanceStates.AutoAccepted; }
            else if (latestLog.AcceptanceStateId == AcceptanceStates.PendingUserSoft) { newState = AcceptanceStates.PendingAdminSoft; }
            else if (latestLog.AcceptanceStateId == AcceptanceStates.PendingUserHard) { newState = AcceptanceStates.PendingAdminHard; }
            else { return Forbid(); }

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userFromId.Id,
                ItemTypeId = ItemTypes.Series,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = dto.Notes ?? "",
                Diff = diff,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict("A series with this name already exists.");
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSeries(int id)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            var series = await _context.Series.FindAsync(id);
            if (series == null)
                return NotFound();

            _context.Series.Remove(series);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
