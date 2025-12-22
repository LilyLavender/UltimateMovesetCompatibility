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
    [Route("api/series")]
    public class SeriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public SeriesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetSeries()
        {
            var userId = _userManager.GetUserId(User);

            var modderId = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.ModderId)
                .FirstOrDefaultAsync();

            // All series user has a moveset from
            var movesetSeriesIds = modderId != null
                ? await _context.MovesetModders
                    .Where(mm => mm.ModderId == modderId)
                    .Select(mm => mm.Moveset.SeriesId)
                    .Distinct()
                    .ToListAsync()
                : new List<int?>();

            // Get the most recent ActionLog for each series
            var latestLogs = await _context.ActionLogs
                .Where(log =>
                    log.ItemTypeId == 3 &&
                    movesetSeriesIds.Contains(log.ItemId))
                .GroupBy(log => log.ItemId)
                .Select(g => g.OrderByDescending(l => l.CreatedAt).First())
                .ToListAsync();

            // Only allow edit if latest log for series is pending user action
            var editableSeriesIds = latestLogs
                .Where(log =>
                    log.AcceptanceStateId == 3 || log.AcceptanceStateId == 4)
                .Select(log => log.ItemId)
                .ToHashSet();

            var seriesList = await _context.Series
                .Select(s => new
                {
                    s.SeriesId,
                    s.SeriesName,
                    s.SeriesIconUrl,
                    MovesetCount = _context.Movesets.Count(m => m.SeriesId == s.SeriesId),
                    CanEdit = editableSeriesIds.Contains(s.SeriesId)
                })
                .ToListAsync();

            return Ok(seriesList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReturnSeriesDto>> GetOneSeries(int id)
        {
            var series = await _context.Series
                .Where(s => s.SeriesId == id)
                .Select(s => new ReturnSeriesDto
                {
                    SeriesId = s.SeriesId,
                    SeriesName = s.SeriesName,
                    SeriesIconUrl = s.SeriesIconUrl,
                    MovesetCount = _context.Movesets.Count(m => m.SeriesId == s.SeriesId)
                })
                .FirstOrDefaultAsync();

            if (series == null)
                return NotFound();

            return Ok(series);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSeries([FromBody] UpdateSeriesDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.SeriesName))
                return BadRequest("Series name is required.");

            var series = new Series
            {
                SeriesName = dto.SeriesName,
                SeriesIconUrl = dto.SeriesIconUrl
            };

            _context.Series.Add(series);
            await _context.SaveChangesAsync();

            // Log action
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();
            int newState = user?.UserTypeId == 3 ? 7 : 2;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 3,
                ItemId = series.SeriesId,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSeries), new { id = series.SeriesId }, series);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSeries(int id, UpdateSeriesDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            var existingSeries = await _context.Series.FindAsync(id);
            if (existingSeries == null)
                return NotFound();

            // Find latest log for this series
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 3 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            // Only allow editing if latest state is 3 or 4
            if (latestLog is null || (latestLog.AcceptanceStateId != 3 && latestLog.AcceptanceStateId != 4))
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.SeriesName))
                return BadRequest("SeriesName is required.");

            existingSeries.SeriesName = dto.SeriesName;
            existingSeries.SeriesIconUrl = dto.SeriesIconUrl;

            // Calculate new acceptance state
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            int newState;
            if (user?.UserTypeId == 3) { newState = 7; }
            else if (latestLog.AcceptanceStateId == 3) { newState = 1; }
            else if (latestLog.AcceptanceStateId == 4) { newState = 2; }
            else { return Forbid(); }

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 3,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSeries(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
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
