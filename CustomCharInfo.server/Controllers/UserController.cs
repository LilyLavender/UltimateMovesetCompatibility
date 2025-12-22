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

            // Load all users
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.ModderId,
                    u.UserName,
                    u.Email,
                    u.UserTypeId
                })
                .ToListAsync();
            var modders = await _context.Modders
                .Select(m => new
                {
                    m.ModderId,
                    Name = m.Name,
                    m.Bio,
                    m.GamebananaId,
                    m.UserId,
                    m.DiscordUsername
                })
                .ToListAsync();

            // Users in both tables
            var inBoth = users
                .Where(u => u.ModderId.HasValue && modders.Any(m => m.ModderId == u.ModderId.Value))
                .Select(u =>
                {
                    var m = modders.First(m => m.ModderId == u.ModderId.Value);
                    return new
                    {
                        User = u,
                        Modder = m
                    };
                })
                .ToList();

            // Users only in ApplicationUser
            var onlyUsers = users
                .Where(u => !u.ModderId.HasValue || !modders.Any(m => m.ModderId == u.ModderId.Value))
                .ToList();

            // Users only in Modders
            var onlyModders = modders
                .Where(m => !users.Any(u => u.ModderId == m.ModderId))
                .ToList();

            return Ok(new
            {
                onlyUsers,
                onlyModders,
                inBoth
            });
        }
    }
}
