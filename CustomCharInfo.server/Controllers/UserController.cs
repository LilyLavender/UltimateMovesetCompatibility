using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;

using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null || currentUser.UserTypeId != 3)
                return Forbid();

            // Users with a matching Modder row, joined in SQL rather than loading both tables
            // into memory and joining with LINQ-to-Objects.
            var inBoth = await (
                from u in _context.Users
                join m in _context.Modders on u.ModderId equals (int?)m.ModderId
                select new
                {
                    User = new { u.Id, u.ModderId, u.UserName, u.Email, u.UserTypeId },
                    Modder = new { m.ModderId, m.Name, m.Bio, m.GamebananaId, m.UserId, m.DiscordUsername }
                }
            ).ToListAsync();

            // Users with no matching Modder row
            var onlyUsers = await _context.Users
                .Where(u => !_context.Modders.Any(m => m.ModderId == u.ModderId))
                .Select(u => new { u.Id, u.ModderId, u.UserName, u.Email, u.UserTypeId })
                .ToListAsync();

            // Modders with no matching user row
            var onlyModders = await _context.Modders
                .Where(m => !_context.Users.Any(u => u.ModderId == m.ModderId))
                .Select(m => new { m.ModderId, m.Name, m.Bio, m.GamebananaId, m.UserId, m.DiscordUsername })
                .ToListAsync();

            return Ok(new
            {
                onlyUsers,
                onlyModders,
                inBoth
            });
        }
    }
}
