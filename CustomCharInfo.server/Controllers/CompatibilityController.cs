using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<IActionResult> GetReports([FromQuery] string moveset1, [FromQuery] string moveset2)
        {
            var movesetId1 = await MovesetLookup.ResolveMovesetIdAsync(_context, moveset1);
            var movesetId2 = await MovesetLookup.ResolveMovesetIdAsync(_context, moveset2);
            if (movesetId1 == null || movesetId2 == null)
                return NotFound("One or both movesets do not exist.");

            var (lo, hi) = movesetId1 < movesetId2
                ? (movesetId1.Value, movesetId2.Value)
                : (movesetId2.Value, movesetId1.Value);

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

        // Returns all pairs that involve moveset, with vote counts per partner
        [HttpGet("summary")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<IActionResult> GetSummaryForMoveset([FromQuery] string moveset)
        {
            var movesetId = await MovesetLookup.ResolveMovesetIdAsync(_context, moveset);
            if (movesetId == null)
                return NotFound("Moveset does not exist.");

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

        private const int MaxPredictMovesets = 10;
        private static readonly string[] SeverityOrder = { "compatible", "warning", "predicted-incompat", "incompatible" };

        // N-way predicted compatibility, ported from CompatibilityCheckPage.vue's runCheck
        [HttpGet("predict")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public-heavy")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<IActionResult> PredictCompatibility([FromQuery] string movesets)
        {
            if (string.IsNullOrWhiteSpace(movesets))
                return BadRequest("movesets is required.");

            var tokens = movesets.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length < 2)
                return BadRequest("At least two movesets are required.");
            if (tokens.Length > MaxPredictMovesets)
                return BadRequest($"At most {MaxPredictMovesets} movesets are allowed per request.");

            var ids = new List<int>();
            foreach (var token in tokens)
            {
                var resolved = await MovesetLookup.ResolveMovesetIdAsync(_context, token);
                if (resolved == null)
                    return NotFound($"Moveset '{token}' does not exist.");
                ids.Add(resolved.Value);
            }
            if (ids.Distinct().Count() != ids.Count)
                return BadRequest("Duplicate movesets in request.");

            var movesetData = await _context.Movesets
                .Where(m => ids.Contains(m.MovesetId))
                .Select(m => new
                {
                    m.MovesetId,
                    m.SlottedId,
                    m.SlotsStart,
                    m.SlotsEnd,
                    m.VanillaCharInternalName,
                    Hooks = m.MovesetHooks.Select(mh => new { mh.HookId, mh.Hook.HookableStatusId }).ToList(),
                    Articles = m.MovesetArticles.Select(ma => new { ma.ArticleId }).ToList()
                })
                .ToListAsync();
            var byId = movesetData.ToDictionary(m => m.MovesetId);

            static bool SlotsOverlap(int? aStart, int? aEnd, int? bStart, int? bEnd)
            {
                int aS = aStart ?? 0, aE = aEnd ?? 0, bS = bStart ?? 0, bE = bEnd ?? 0;
                if (aS == 0 && aE == 0 && bS == 0 && bE == 0) return false;
                return aS <= bE && bS <= aE;
            }

            int SeverityRank(string s) => Array.IndexOf(SeverityOrder, s);

            var pairs = new List<object>();
            var overallSeverity = "compatible";

            for (int i = 0; i < ids.Count; i++)
            {
                for (int j = i + 1; j < ids.Count; j++)
                {
                    var a = byId[ids[i]];
                    var b = byId[ids[j]];

                    var pairSeverity = "compatible";
                    void Escalate(string sev)
                    {
                        if (SeverityRank(sev) > SeverityRank(pairSeverity)) pairSeverity = sev;
                    }

                    var conflictingHookIds = new List<int>();
                    foreach (var hookA in a.Hooks)
                    {
                        if (!b.Hooks.Any(h => h.HookId == hookA.HookId)) continue;
                        conflictingHookIds.Add(hookA.HookId);
                        if (hookA.HookableStatusId == HookableStatuses.MoreThanOnce)
                            Escalate("warning");
                        else if (hookA.HookableStatusId == HookableStatuses.OnlyOnce)
                            Escalate("incompatible");
                        else
                            Escalate("predicted-incompat");
                    }

                    var conflictingArticleIds = new List<int>();
                    bool sameChar = a.VanillaCharInternalName == b.VanillaCharInternalName;
                    bool slotsOverlap = SlotsOverlap(a.SlotsStart, a.SlotsEnd, b.SlotsStart, b.SlotsEnd);
                    foreach (var artA in a.Articles)
                    {
                        if (!b.Articles.Any(x => x.ArticleId == artA.ArticleId)) continue;
                        if (sameChar)
                        {
                            conflictingArticleIds.Add(artA.ArticleId);
                            Escalate("incompatible");
                        }
                        else if (slotsOverlap)
                        {
                            conflictingArticleIds.Add(artA.ArticleId);
                            Escalate("warning");
                        }
                    }

                    pairs.Add(new
                    {
                        Moveset1 = new { MovesetId = a.MovesetId, a.SlottedId },
                        Moveset2 = new { MovesetId = b.MovesetId, b.SlottedId },
                        Severity = pairSeverity,
                        ConflictingHookIds = conflictingHookIds,
                        ConflictingArticleIds = conflictingArticleIds
                    });

                    if (SeverityRank(pairSeverity) > SeverityRank(overallSeverity))
                        overallSeverity = pairSeverity;
                }
            }

            return Ok(new { Pairs = pairs, OverallSeverity = overallSeverity });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitReport([FromBody] CompatibilityReportDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (dto.MovesetId1 == dto.MovesetId2)
                return BadRequest("A moveset cannot be compared against itself.");

            var (lo, hi) = dto.MovesetId1 < dto.MovesetId2
                ? (dto.MovesetId1, dto.MovesetId2)
                : (dto.MovesetId2, dto.MovesetId1);

            var existingMovesetCount = await _context.Movesets
                .CountAsync(m => m.MovesetId == lo || m.MovesetId == hi);
            if (existingMovesetCount != 2)
                return NotFound("One or both movesets do not exist.");

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
