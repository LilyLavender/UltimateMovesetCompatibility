using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Helpers;

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
        
        [HttpGet("public")]
        public async Task<ActionResult> GetPublicModders()
        {
            var modders = await _context.Modders
                .Where(m =>
                    // Not problematic
                    m.User.Problematic != true &&
                    // Has been accepted at some point
                    _context.ActionLogs.Any(a =>
                        a.ItemTypeId == ItemTypes.Modder && a.ItemId == m.ModderId &&
                        AcceptanceStates.AnyAccepted.Contains(a.AcceptanceStateId)
                    ) &&
                    // Latest modder log is not blocked
                    !AcceptanceStates.Blocked.Contains(
                        _context.ActionLogs
                            .Where(a => a.ItemTypeId == ItemTypes.Modder && a.ItemId == m.ModderId)
                            .OrderByDescending(a => a.CreatedAt)
                            .Select(a => a.AcceptanceStateId)
                            .FirstOrDefault()
                    ) &&
                    // Has at least one public, non-hardheld moveset
                    m.MovesetModders.Any(mm =>
                        mm.Moveset.PrivateMoveset != true &&
                        !AcceptanceStates.Blocked.Contains(
                            _context.ActionLogs
                                .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == mm.Moveset.MovesetId)
                                .OrderByDescending(a => a.CreatedAt)
                                .Select(a => a.AcceptanceStateId)
                                .FirstOrDefault()
                        )
                    )
                )
                .Select(m => new
                {
                    m.ModderId,
                    Name = m.User != null ? m.User.UserName : m.Name,
                    m.Bio,
                    m.GamebananaId,
                    m.PfpUrl,
                    IsAdmin = m.User != null && m.User.UserTypeId == UserTypes.Admin,
                    MovesetCount = m.MovesetModders.Count(mm =>
                        mm.Moveset.PrivateMoveset != true &&
                        !AcceptanceStates.Blocked.Contains(
                            _context.ActionLogs
                                .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == mm.Moveset.MovesetId)
                                .OrderByDescending(a => a.CreatedAt)
                                .Select(a => a.AcceptanceStateId)
                                .FirstOrDefault()
                        )
                    )
                })
                .OrderBy(m => m.Name.ToLower())
                .ToListAsync();

            return Ok(modders);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetModders()
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Unauthorized();
        
                    
            var moddersQuery = _context.Modders
                .Include(m => m.User)
                .Select(m => new
                {
                    Modder = m,
                    LatestLog = _context.ActionLogs
                        .Where(l => l.ItemTypeId == ItemTypes.Modder && l.ItemId == m.ModderId)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefault()
                });
        
            // Filter out blocked modders
            if (userFromId.UserTypeId != UserTypes.Admin)
            {
                moddersQuery = moddersQuery
                    .Where(x => x.LatestLog == null || !AcceptanceStates.Blocked.Contains(x.LatestLog.AcceptanceStateId));
            }
        
            var modders = await moddersQuery
                .Select(x => new
                {
                    x.Modder.ModderId,
                    Name = x.Modder.User != null ? x.Modder.User.UserName : x.Modder.Name,
                    x.Modder.Bio,
                    x.Modder.GamebananaId,
                    x.Modder.DiscordUsername,
                    x.Modder.PfpUrl,
                    x.Modder.TwitterUsername,
                    x.Modder.BlueskyHandle,
                    x.Modder.GithubUsername,
                    Problematic = x.Modder.User != null && x.Modder.User.Problematic == true
                })
                .ToListAsync();

            return Ok(modders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetModder(int id)
        {
            var userId = _userManager.GetUserId(User);
            var userFromId = userId != null
                ? await _context.Users.FindAsync(userId)
                : null;

            
            var modderQuery = _context.Modders
                .Include(m => m.User)
                .Where(m => m.ModderId == id)
                .Select(m => new
                {
                    Modder = m,
                    LatestLog = _context.ActionLogs
                        .Where(l => l.ItemTypeId == ItemTypes.Modder && l.ItemId == m.ModderId)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefault()
                });

            // Filter out blocked modders
            if (userFromId?.UserTypeId != UserTypes.Admin)
            {
                modderQuery = modderQuery.Where(x =>
                    x.LatestLog == null ||
                    !AcceptanceStates.Blocked.Contains(x.LatestLog.AcceptanceStateId)
                );
            }

            var result = await modderQuery
                .Select(x => new
                {
                    x.Modder.ModderId,
                    Name = x.Modder.User != null ? x.Modder.User.UserName : x.Modder.Name,
                    x.Modder.Bio,
                    x.Modder.GamebananaId,
                    x.Modder.DiscordUsername,
                    x.Modder.PfpUrl,
                    x.Modder.TwitterUsername,
                    x.Modder.BlueskyHandle,
                    x.Modder.GithubUsername,
                    Problematic = x.Modder.User != null && x.Modder.User.Problematic == true
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

            bool isAdmin = user.UserTypeId == UserTypes.Admin;
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

            if (user == null)
                return Unauthorized();

            // Already modder
            if (user.ModderId != null)
                return BadRequest("User is already a modder.");

            // Check for existing modder application
            var hasPendingApplication = await _context.ActionLogs.AnyAsync(a =>
                a.UserId == userId &&
                a.ItemTypeId == ItemTypes.Modder &&
                AcceptanceStates.Hard.Contains(a.AcceptanceStateId)
            );

            if (hasPendingApplication)
                return Conflict("User already has a pending modder application.");

            var modder = new Modder
            {
                Name = user?.UserName,
                Bio = dto.Bio,
                GamebananaId = dto.GamebananaId,
                DiscordUsername = dto.DiscordUsername,
                PfpUrl = dto.PfpUrl,
                TwitterUsername = dto.TwitterUsername,
                BlueskyHandle = dto.BlueskyHandle,
                GithubUsername = dto.GithubUsername,
            };

            _context.Modders.Add(modder);
            await _context.SaveChangesAsync();

            // Log action
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = ItemTypes.Modder,
                ItemId = modder.ModderId,
                AcceptanceStateId = AcceptanceStates.PendingAdminHard,
                Notes = dto.Notes ?? "",
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
                .Where(a => a.ItemTypeId == ItemTypes.Modder && a.ItemId == id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync() == userId;

            if (!(isOwner || isOriginalSubmitter || user.UserTypeId == UserTypes.Admin))
                return Forbid("You are not authorized to edit this modder profile.");

            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == ItemTypes.Modder && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestLog?.AcceptanceStateId == AcceptanceStates.Rejected)
                return Forbid("This modder profile has been rejected and cannot be edited.");

            var modder = await _context.Modders.FindAsync(id);
            if (modder == null)
                return NotFound();

            var snapBio = modder.Bio;
            var snapGbId = modder.GamebananaId;
            var snapDiscord = modder.DiscordUsername;
            var snapPfpUrl = modder.PfpUrl;
            var snapTwitter = modder.TwitterUsername;
            var snapBluesky = modder.BlueskyHandle;
            var snapGithub = modder.GithubUsername;

            if (dto.Bio != null)
                modder.Bio = dto.Bio;

            if (dto.GamebananaId.HasValue)
                modder.GamebananaId = dto.GamebananaId.Value;

            if (dto.DiscordUsername != null)
                modder.DiscordUsername = dto.DiscordUsername;

            modder.PfpUrl = dto.PfpUrl;
            modder.TwitterUsername = dto.TwitterUsername;
            modder.BlueskyHandle = dto.BlueskyHandle;
            modder.GithubUsername = dto.GithubUsername;

            var diff = DiffHelper.Build(new (string, object?, object?)[]
            {
                ("Bio",             snapBio,     dto.Bio),
                ("GamebananaId",    snapGbId,    dto.GamebananaId),
                ("DiscordUsername", snapDiscord, dto.DiscordUsername),
                ("PfpUrl",         snapPfpUrl,  dto.PfpUrl),
                ("TwitterUsername", snapTwitter,  dto.TwitterUsername),
                ("BlueskyHandle",   snapBluesky,  dto.BlueskyHandle),
                ("GithubUsername",  snapGithub,   dto.GithubUsername),
            });

            int newState = user.UserTypeId == UserTypes.Admin
                ? AcceptanceStates.AutoAccepted
                : latestLog != null && AcceptanceStates.Hard.Contains(latestLog.AcceptanceStateId)
                    ? AcceptanceStates.PendingAdminHard
                    : AcceptanceStates.PendingAdminSoft;

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = ItemTypes.Modder,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = dto.Notes ?? "",
                Diff = diff,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteModder(int id)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
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
