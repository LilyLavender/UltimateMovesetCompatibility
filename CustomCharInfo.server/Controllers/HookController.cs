using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

using SixLabors.ImageSharp;
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

        public HookController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [EnableCors("PublicApi")]
        [EnableRateLimiting("public")]
        [ApiExplorerSettings(GroupName = "public")]
        public async Task<ActionResult<IEnumerable<HookDto>>> GetHooks()
        {
            var hooks = await _context.Hooks
                .Include(h => h.HookableStatus)
                .Select(h => new HookDto
                {
                    HookId = h.HookId,
                    Offset = h.Offset,
                    Description = h.Description,
                    HookableStatusId = h.HookableStatusId,
                    HookableStatus = h.HookableStatus.Name
                })
                .ToListAsync();

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
                .Where(h => h.Description.ToLower().Contains(lowered) || h.Offset.ToLower().Contains(lowered))
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
            var hook = await _context.Hooks
                .Include(h => h.HookableStatus)
                .Where(h => h.HookId == id)
                .Select(h => new HookDto
                {
                    HookId = h.HookId,
                    Offset = h.Offset,
                    Description = h.Description,
                    HookableStatusId = h.HookableStatusId,
                    HookableStatus = h.HookableStatus.Name
                })
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
                ItemTypeId = 4,
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
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            if (await _context.Hooks.AnyAsync(h => h.Offset == dto.Offset))
                return Conflict("A hook with this offset already exists.");

            var hook = new Hook
            {
                Offset = dto.Offset,
                Description = dto.Description,
                HookableStatusId = dto.HookableStatusId
            };

            _context.Hooks.Add(hook);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The AnyAsync check above is check-then-act; this catches a genuine race against
                // the unique index on Offset as a backstop.
                return Conflict("A hook with this offset already exists.");
            }

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Offset", null, hook.Offset),
                ("Description", null, hook.Description),
                ("HookableStatusId", null, hook.HookableStatusId),
            });

            LogHookAction(userId, hook.HookId, acceptanceStateId: 1, dto.Notes ?? "", diff);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHook), new { id = hook.HookId }, hook);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateHook(int id, UpdateHookDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            var hook = await _context.Hooks.FindAsync(id);
            if (hook == null)
                return NotFound();

            var snapOffset = hook.Offset;
            var snapDescription = hook.Description;
            var snapHookableStatusId = hook.HookableStatusId;

            if (dto.Offset != null)
                hook.Offset = dto.Offset;

            if (dto.Description != null)
                hook.Description = dto.Description;

            if (dto.HookableStatusId.HasValue)
                hook.HookableStatusId = dto.HookableStatusId.Value;

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Offset", snapOffset, hook.Offset),
                ("Description", snapDescription, hook.Description),
                ("HookableStatusId", snapHookableStatusId, hook.HookableStatusId),
            });

            LogHookAction(userId, hook.HookId, acceptanceStateId: 1, dto.Notes ?? "", diff);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict("A hook with this offset already exists.");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteHook(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
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

            LogHookAction(userId, hook.HookId, acceptanceStateId: 5, "Deleted hook", diff);
            _context.Hooks.Remove(hook);
            await _context.SaveChangesAsync();

            return NoContent();
        }
       }
}
