using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Services;

namespace CustomCharInfo.server.Controllers
{
    // Game versions that hook offsets are recorded against.
    // Adding one takes the shift table for the update and derives every hook's new offset in one step.
    [ApiController]
    [Route("api/game-versions")]
    public class GameVersionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HookOffsetService _offsets;

        public GameVersionController(AppDbContext context, UserManager<ApplicationUser> userManager, HookOffsetService offsets)
        {
            _context = context;
            _userManager = userManager;
            _offsets = offsets;
        }

        [HttpGet]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<List<GameVersionDto>>> GetGameVersions()
        {
            var versions = await _context.GameVersions
                .AsNoTracking()
                .OrderByDescending(v => v.SortOrder)
                .ToListAsync();

            var latestId = versions.FirstOrDefault()?.GameVersionId;
            return Ok(versions.Select(v => ToDto(v, latestId)).ToList());
        }

        // Same validation and computation as the real call without writing anything.
        [HttpPost("preview")]
        [Authorize]
        public async Task<ActionResult<GameVersionApplyResultDto>> PreviewGameVersion(CreateGameVersionDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin()) return Forbid();

            var validation = await ValidateAsync(dto);
            if (validation.Error != null) return validation.Error;

            try
            {
                var result = await _offsets.PreviewAsync(validation.Previous!, validation.Ranges!);
                return Ok(ToDto(null, result));
            }
            catch (HookOffsetConflictException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GameVersionApplyResultDto>> CreateGameVersion(CreateGameVersionDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin()) return Forbid();

            var validation = await ValidateAsync(dto);
            if (validation.Error != null) return validation.Error;
            var previous = validation.Previous!;
            var ranges = validation.Ranges!;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var added = new GameVersion
            {
                Name = dto.Name.Trim(),
                SortOrder = previous.SortOrder + 1,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.GameVersions.Add(added);

            foreach (var range in ranges)
            {
                _context.GameVersionShifts.Add(new GameVersionShift
                {
                    FromGameVersion = previous,
                    ToGameVersion = added,
                    RangeStart = range.Start,
                    RangeEnd = range.End,
                    Delta = range.Delta
                });
            }

            OffsetApplyResult result;
            try
            {
                result = await _offsets.ApplyVersionAsync(previous, added, ranges);
            }
            catch (HookOffsetConflictException ex)
            {
                return Conflict(ex.Message);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetGameVersions), null, ToDto(ToDto(added, added.GameVersionId), result));
        }

        // Only the newest version can go and never the last one. This is the undo for a bad shift table.
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteGameVersion(int id)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin()) return Forbid();

            var version = await _context.GameVersions.FindAsync(id);
            if (version == null) return NotFound();

            var latest = await _offsets.GetLatestVersionAsync();
            if (latest == null || latest.GameVersionId != id)
                return BadRequest("Only the newest game version can be deleted.");

            var remaining = await _context.GameVersions
                .Where(v => v.GameVersionId != id)
                .OrderByDescending(v => v.SortOrder)
                .FirstOrDefaultAsync();
            if (remaining == null)
                return BadRequest("The only game version cannot be deleted.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Removed explicitly rather than trusting cascade, so the tracked hooks see the change before the resync.
            _context.HookOffsets.RemoveRange(_context.HookOffsets.Where(o => o.GameVersionId == id));
            _context.GameVersionShifts.RemoveRange(_context.GameVersionShifts.Where(s => s.ToGameVersionId == id || s.FromGameVersionId == id));
            _context.GameVersions.Remove(version);
            await _context.SaveChangesAsync();

            await _offsets.ResyncCurrentAsync(remaining);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }

        private sealed record Validation(ActionResult? Error, GameVersion? Previous, List<ShiftRange>? Ranges);

        private async Task<Validation> ValidateAsync(CreateGameVersionDto dto)
        {
            var name = dto.Name?.Trim() ?? "";
            if (name.Length == 0)
                return new Validation(BadRequest("A version name is required."), null, null);

            var lowered = name.ToLower();
            if (await _context.GameVersions.AnyAsync(v => v.Name.ToLower() == lowered))
                return new Validation(Conflict($"Game version {name} already exists."), null, null);

            var previous = await _offsets.GetLatestVersionAsync();
            if (previous == null)
                return new Validation(BadRequest("No game version exists to shift from."), null, null);

            try
            {
                return new Validation(null, previous, OffsetShifter.Parse(dto.ShiftTable));
            }
            catch (ShiftTableParseException ex)
            {
                return new Validation(BadRequest(ex.Message), null, null);
            }
        }

        private static GameVersionDto ToDto(GameVersion v, int? latestId) => new()
        {
            GameVersionId = v.GameVersionId,
            Name = v.Name,
            SortOrder = v.SortOrder,
            IsLatest = v.GameVersionId == latestId,
            CreatedAt = v.CreatedAt
        };

        private static GameVersionApplyResultDto ToDto(GameVersionDto? version, OffsetApplyResult result) => new()
        {
            Version = version,
            Generated = result.Generated,
            CarriedForward = result.CarriedForward,
            Rows = result.Rows.Select(r => new OffsetPreviewRowDto
            {
                HookId = r.HookId,
                Description = r.Description,
                OldOffset = r.OldOffset,
                NewOffset = r.NewOffset,
                OffsetStateId = r.OffsetStateId
            }).ToList()
        };
    }
}
