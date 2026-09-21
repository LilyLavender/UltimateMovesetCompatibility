using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Services;

using Microsoft.AspNetCore.Authorization;
using CustomCharInfo.server.Helpers;
using Npgsql;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/hooks")]
    public class HookController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly HookOffsetService _offsets;

        public HookController(AppDbContext context, UserManager<ApplicationUser> userManager, HookOffsetService offsets)
        {
            _context = context;
            _userManager = userManager;
            _offsets = offsets;
        }

        private IQueryable<HookDto> ProjectHooks(IQueryable<Hook> hooks, GameVersion? latest)
        {
            var latestId = latest?.GameVersionId ?? 0;
            var latestName = latest?.Name;
            return hooks.Select(h => new HookDto
            {
                HookId = h.HookId,
                Offset = h.Offset,
                GameVersion = latestName,
                OffsetStateId = h.HookOffsets
                    .Where(o => o.GameVersionId == latestId)
                    .Select(o => (int?)o.OffsetStateId)
                    .FirstOrDefault(),
                Offsets = h.HookOffsets
                    .OrderByDescending(o => o.GameVersion.SortOrder)
                    .Select(o => new HookOffsetDto
                    {
                        GameVersionId = o.GameVersionId,
                        GameVersion = o.GameVersion.Name,
                        Offset = o.Offset,
                        OffsetStateId = o.OffsetStateId,
                        OffsetState = o.OffsetState.Name,
                        UpdatedAt = o.UpdatedAt
                    })
                    .ToList(),
                Description = h.Description,
                HookableStatusId = h.HookableStatusId,
                HookableStatus = h.HookableStatus.Name
            });
        }

        [HttpGet]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<IEnumerable<HookDto>>> GetHooks()
        {
            var latest = await _offsets.GetLatestVersionAsync();
            var hooks = await ProjectHooks(_context.Hooks.AsNoTracking(), latest).ToListAsync();
            return Ok(hooks);
        }

        private const int MaxSearchResults = 20;

        [HttpGet("search")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public-heavy")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<IEnumerable<object>>> SearchHooks([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(Array.Empty<object>());

            var lowered = q.Trim().ToLower();
            var results = await _context.Hooks
                .AsNoTracking()
                .Where(h => h.Description.ToLower().Contains(lowered)
                    || h.Offset.ToLower().Contains(lowered)
                    || h.HookOffsets.Any(o => o.Offset.ToLower().Contains(lowered)))
                .OrderBy(h => h.Description)
                .Take(MaxSearchResults)
                .Select(h => new { Id = h.HookId, Name = h.Description })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("{id}")]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<HookDto>> GetHook(int id)
        {
            var latest = await _offsets.GetLatestVersionAsync();
            var hook = await ProjectHooks(_context.Hooks.AsNoTracking().Where(h => h.HookId == id), latest)
                .FirstOrDefaultAsync();

            if (hook == null)
                return NotFound();

            return Ok(hook);
        }

        // Hooks have no owner and no hard review gate.
        // Edits take effect immediately for any modder.
        // Create/update is still logged as pending admin soft.
        private void LogHookAction(string userId, int hookId, int acceptanceStateId, string notes, string? diff)
        {
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = ItemTypes.Hook,
                ItemId = hookId,
                AcceptanceStateId = acceptanceStateId,
                Notes = notes,
                Diff = diff,
                CreatedAt = DateTime.UtcNow
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CreateHookDto>> CreateHook(CreateHookDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            var latest = await _offsets.GetLatestVersionAsync();
            if (latest == null)
                return BadRequest("No game version exists yet.");

            var version = dto.GameVersionId.HasValue
                ? await _context.GameVersions.FindAsync(dto.GameVersionId.Value)
                : latest;
            if (version == null)
                return BadRequest("Unknown game version.");

            if (!OffsetFormat.TryNormalize(dto.Offset, out var offset))
                return BadRequest("Offset must be a hex address of up to eight digits.");

            if (await _context.HookOffsets.AnyAsync(o => o.GameVersionId == version.GameVersionId && o.Offset == offset))
                return Conflict($"A hook with offset 0x{offset} already exists in {version.Name}.");

            var hook = new Hook
            {
                Offset = offset,
                Description = dto.Description,
                HookableStatusId = dto.HookableStatusId
            };
            _context.Hooks.Add(hook);

            try
            {
                await _offsets.SetOffsetAsync(hook, version, offset, OffsetStates.Confirmed, userFromId.Id);
                await _offsets.DeriveForwardAsync(hook, version, offset);
                await _context.SaveChangesAsync();
            }
            catch (HookOffsetConflictException ex)
            {
                return Conflict(ex.Message);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The AnyAsync check above is check-then-act; this catches a genuine race against
                // the unique index on (GameVersionId, Offset) as a backstop.
                return Conflict($"A hook with offset 0x{offset} already exists in {version.Name}.");
            }

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ($"Offset ({version.Name})", null, hook.Offset),
                ("Description", null, hook.Description),
                ("HookableStatusId", null, hook.HookableStatusId),
            });

            LogHookAction(userFromId.Id, hook.HookId, acceptanceStateId: AcceptanceStates.PendingAdminSoft, dto.Notes ?? "", diff);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHook), new { id = hook.HookId }, hook);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateHook(int id, UpdateHookDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            var hook = await _context.Hooks.FindAsync(id);
            if (hook == null)
                return NotFound();

            var snapDescription = hook.Description;
            var snapHookableStatusId = hook.HookableStatusId;

            if (dto.Description != null)
                hook.Description = dto.Description;

            if (dto.HookableStatusId.HasValue)
                hook.HookableStatusId = dto.HookableStatusId.Value;

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Description", snapDescription, hook.Description),
                ("HookableStatusId", snapHookableStatusId, hook.HookableStatusId),
            });

            LogHookAction(userFromId.Id, hook.HookId, acceptanceStateId: AcceptanceStates.PendingAdminSoft, dto.Notes ?? "", diff);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Confirms or overrides a hook's offset for one game version.
        // Without an offset in the body, the existing row is marked confirmed as is.
        [HttpPut("{id}/offsets/{gameVersionId}")]
        [Authorize]
        public async Task<ActionResult<HookOffsetDto>> SetHookOffset(int id, int gameVersionId, ConfirmHookOffsetDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            var hook = await _context.Hooks.FindAsync(id);
            if (hook == null)
                return NotFound();

            var version = await _context.GameVersions.FindAsync(gameVersionId);
            if (version == null)
                return NotFound();

            var existing = await _context.HookOffsets
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.HookId == id && o.GameVersionId == gameVersionId);

            string? offset;
            if (dto.Offset != null)
            {
                if (!OffsetFormat.TryNormalize(dto.Offset, out offset))
                    return BadRequest("Offset must be a hex address of up to eight digits.");
            }
            else if (existing != null)
            {
                offset = existing.Offset;
            }
            else
            {
                return BadRequest($"This hook has no offset for {version.Name} yet; provide one.");
            }

            var takenBy = await _context.HookOffsets
                .Where(o => o.GameVersionId == gameVersionId && o.Offset == offset && o.HookId != id)
                .Select(o => o.Hook.Description)
                .FirstOrDefaultAsync();
            if (takenBy != null)
                return Conflict($"Offset 0x{offset} already belongs to \"{takenBy}\" in {version.Name}.");

            var row = await _offsets.SetOffsetAsync(hook, version, offset, OffsetStates.Confirmed, userFromId.Id);

            var stateNames = await _context.OffsetStates.ToDictionaryAsync(s => s.OffsetStateId, s => s.Name);
            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ($"Offset ({version.Name})", existing?.Offset, offset),
                ($"Offset state ({version.Name})", existing == null ? null : stateNames[existing.OffsetStateId], stateNames[OffsetStates.Confirmed]),
            });

            LogHookAction(userFromId.Id, hook.HookId, acceptanceStateId: AcceptanceStates.PendingAdminSoft, dto.Notes ?? "", diff);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict($"Offset 0x{offset} already belongs to another hook in {version.Name}.");
            }

            return Ok(new HookOffsetDto
            {
                GameVersionId = version.GameVersionId,
                GameVersion = version.Name,
                Offset = row.Offset,
                OffsetStateId = row.OffsetStateId,
                OffsetState = stateNames[row.OffsetStateId],
                UpdatedAt = row.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteHook(int id)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            var hook = await _context.Hooks.FindAsync(id);
            if (hook == null)
                return NotFound();

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Offset", hook.Offset, null),
                ("Description", hook.Description, null),
                ("HookableStatusId", hook.HookableStatusId, null),
            });

            LogHookAction(user.Id, hook.HookId, acceptanceStateId: AcceptanceStates.Accepted, "Deleted hook", diff);
            _context.Hooks.Remove(hook);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
