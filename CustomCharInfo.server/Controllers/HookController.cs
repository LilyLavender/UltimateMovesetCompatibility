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

        [HttpGet("{id}")]
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

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CreateHookDto>> CreateHook(CreateHookDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            var hook = new Hook
            {
                Offset = dto.Offset,
                Description = dto.Description,
                HookableStatusId = dto.HookableStatusId
            };

            _context.Hooks.Add(hook);
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

            if (dto.Offset != null)
                hook.Offset = dto.Offset;

            if (dto.Description != null)
                hook.Description = dto.Description;

            if (dto.HookableStatusId.HasValue)
                hook.HookableStatusId = dto.HookableStatusId.Value;

            await _context.SaveChangesAsync();

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

            _context.Hooks.Remove(hook);
            await _context.SaveChangesAsync();

            return NoContent();
        }
       }
}
