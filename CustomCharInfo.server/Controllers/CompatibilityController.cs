using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/compatibility")]
    public class CompatibilityController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompatibilityController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] int movesetId1, [FromQuery] int movesetId2)
        {
            var (lo, hi) = movesetId1 < movesetId2
                ? (movesetId1, movesetId2)
                : (movesetId2, movesetId1);

            var reports = await _context.CompatibilityReports
                .Where(r => r.MovesetId1 == lo && r.MovesetId2 == hi)
                .ToListAsync();

            var userId = _userManager.GetUserId(User);
            var userReport = userId != null
                ? reports.FirstOrDefault(r => r.UserId == userId)
                : null;

            return Ok(new
            {
                CompatibleCount = reports.Count(r => r.IsCompatible),
                IncompatibleCount = reports.Count(r => !r.IsCompatible),
                UserVote = userReport == null ? (bool?)null : userReport.IsCompatible
            });
        }

        // Returns all pairs that involve movesetId, with vote counts per partner
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummaryForMoveset([FromQuery] int movesetId)
        {
            var reports = await _context.CompatibilityReports
                .Where(r => r.MovesetId1 == movesetId || r.MovesetId2 == movesetId)
                .ToListAsync();

            var result = reports
                .GroupBy(r => r.MovesetId1 == movesetId ? r.MovesetId2 : r.MovesetId1)
                .Select(g => new
                {
                    MovesetId = g.Key,
                    CompatibleCount = g.Count(r => r.IsCompatible),
                    IncompatibleCount = g.Count(r => !r.IsCompatible)
                });

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitReport([FromBody] CompatibilityReportDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var (lo, hi) = dto.MovesetId1 < dto.MovesetId2
                ? (dto.MovesetId1, dto.MovesetId2)
                : (dto.MovesetId2, dto.MovesetId1);

            var existing = await _context.CompatibilityReports
                .FirstOrDefaultAsync(r => r.MovesetId1 == lo && r.MovesetId2 == hi && r.UserId == userId);

            if (existing != null)
            {
                if (existing.IsCompatible == dto.IsCompatible)
                {
                    _context.CompatibilityReports.Remove(existing);
                }
                else
                {
                    existing.IsCompatible = dto.IsCompatible;
                    existing.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                _context.CompatibilityReports.Add(new CompatibilityReport
                {
                    MovesetId1 = lo,
                    MovesetId2 = hi,
                    UserId = userId,
                    IsCompatible = dto.IsCompatible,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var reports = await _context.CompatibilityReports
                .Where(r => r.MovesetId1 == lo && r.MovesetId2 == hi)
                .ToListAsync();

            var userReport = reports.FirstOrDefault(r => r.UserId == userId);

            return Ok(new
            {
                CompatibleCount = reports.Count(r => r.IsCompatible),
                IncompatibleCount = reports.Count(r => !r.IsCompatible),
                UserVote = userReport == null ? (bool?)null : userReport.IsCompatible
            });
        }
    }

    public class CompatibilityReportDto
    {
        public int MovesetId1 { get; set; }
        public int MovesetId2 { get; set; }
        public bool IsCompatible { get; set; }
    }
}
