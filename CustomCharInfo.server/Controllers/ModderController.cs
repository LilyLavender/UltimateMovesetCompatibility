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
    [Route("api/modders")]
    public class ModderController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public ModderController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetModders()
        {
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Unauthorized();
        
            var blockedStates = new[] { 2, 4, 6 };
        
            var moddersQuery = _context.Modders
                .Include(m => m.User)
                .Select(m => new
                {
                    Modder = m,
                    LatestLog = _context.ActionLogs
                        .Where(l => l.ItemTypeId == 2 && l.ItemId == m.ModderId)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefault()
                });
        
            // Filter out blocked modders
            if (userFromId.UserTypeId != 3)
            {
                moddersQuery = moddersQuery
                    .Where(x => x.LatestLog == null || !blockedStates.Contains(x.LatestLog.AcceptanceStateId));
            }
        
            var modders = await moddersQuery
                .Select(x => new
                {
                    x.Modder.ModderId,
                    Name = x.Modder.User != null ? x.Modder.User.UserName : x.Modder.Name,
                    x.Modder.Bio,
                    x.Modder.GamebananaId,
                    x.Modder.DiscordUsername
                })
                .ToListAsync();
        
            return Ok(modders);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetModder(int id)
        {
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Unauthorized();

            var blockedStates = new[] { 2, 4, 6 };

            var modderQuery = _context.Modders
                .Include(m => m.User)
                .Where(m => m.ModderId == id)
                .Select(m => new
                {
                    Modder = m,
                    LatestLog = _context.ActionLogs
                        .Where(l => l.ItemTypeId == 2 && l.ItemId == m.ModderId)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefault()
                });

            // Filter out blocked modders
            if (userFromId.UserTypeId != 3)
            {
                modderQuery = modderQuery.Where(x =>
                    x.LatestLog == null ||
                    !blockedStates.Contains(x.LatestLog.AcceptanceStateId)
                );
            }

            var result = await modderQuery
                .Select(x => new
                {
                    x.Modder.ModderId,
                    Name = x.Modder.User != null ? x.Modder.User.UserName : x.Modder.Name,
                    x.Modder.Bio,
                    x.Modder.GamebananaId,
                    x.Modder.DiscordUsername
                })
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("is-admin")]
        public async Task<ActionResult<object>> ModderIsAdmin(int modderId)
        {
            // Find the user associated with this modder
            var user = await _context.Users
                .Where(u => u.ModderId == modderId)
                .Select(u => new { u.UserTypeId })
                .FirstOrDefaultAsync();

            // No user associated
            if (user == null)
            {
                return Ok(new { isAdmin = false });
            }

            bool isAdmin = user.UserTypeId == 3;
            return Ok(new { isAdmin });
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CreateModderDto>> CreateModder(CreateModderDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId, u.UserName })
                .SingleOrDefaultAsync();

            if (user == null || user.ModderId != null)
                return Unauthorized();

            var modder = new Modder
            {
                Name = user?.UserName,
                Bio = dto.Bio,
                GamebananaId = dto.GamebananaId,
                DiscordUsername = dto.DiscordUsername
            };

            _context.Modders.Add(modder);
            await _context.SaveChangesAsync();

            // Log action
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 2,
                ItemId = modder.ModderId,
                AcceptanceStateId = 2,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetModder), new { id = modder.ModderId }, modder);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModder(int id, UpdateModderDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            if (user == null)
                return Forbid();

            bool isOwner = user.ModderId == id;

            bool isOriginalSubmitter = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 2 && a.ItemId == id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync() == userId;

            if (!(isOwner || isOriginalSubmitter || user.UserTypeId == 3))
                return Forbid("You are not authorized to edit this modder profile.");

            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 2 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestLog?.AcceptanceStateId == 6)
                return Forbid("This modder profile has been rejected and cannot be edited.");

            var modder = await _context.Modders.FindAsync(id);
            if (modder == null)
                return NotFound();

            if (dto.Bio != null)
                modder.Bio = dto.Bio;

            if (dto.GamebananaId.HasValue)
                modder.GamebananaId = dto.GamebananaId.Value;

            if (dto.DiscordUsername != null)
                modder.DiscordUsername = dto.DiscordUsername;

            int newState = user.UserTypeId == 3
                ? 7
                : (latestLog?.AcceptanceStateId == 2 || latestLog?.AcceptanceStateId == 4) ? 2 : 1;

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 2,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModder(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var modder = await _context.Modders.FindAsync(id);
            if (modder == null)
                return NotFound();

            _context.Modders.Remove(modder);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
