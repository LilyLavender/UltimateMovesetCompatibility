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

            // Most recent IP per user, looked up separately since it doesn't fit the SQL joins below cleanly.
            var lastIps = await _context.UserIpAddresses
                .GroupBy(uip => uip.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastIp = g.OrderByDescending(uip => uip.LastSeenAt).Select(uip => uip.IpAddress).First(),
                    IpCount = g.Count()
                })
                .ToDictionaryAsync(x => x.UserId);

            // Users with a matching Modder row, joined in SQL rather than loading both tables
            // into memory and joining with LINQ-to-Objects.
            var inBoth = await (
                from u in _context.Users
                join m in _context.Modders on u.ModderId equals (int?)m.ModderId
                select new
                {
                    User = new { u.Id, u.ModderId, u.UserName, u.Email, u.UserTypeId, u.LastActiveAt },
                    Modder = new { m.ModderId, m.Name, m.Bio, m.GamebananaId, m.UserId, m.DiscordUsername }
                }
            ).ToListAsync();

            // Users with no matching Modder row
            var onlyUsers = await _context.Users
                .Where(u => !_context.Modders.Any(m => m.ModderId == u.ModderId))
                .Select(u => new { u.Id, u.ModderId, u.UserName, u.Email, u.UserTypeId, u.LastActiveAt })
                .ToListAsync();

            // Modders with no matching user row
            var onlyModders = await _context.Modders
                .Where(m => !_context.Users.Any(u => u.ModderId == m.ModderId))
                .Select(m => new { m.ModderId, m.Name, m.Bio, m.GamebananaId, m.UserId, m.DiscordUsername })
                .ToListAsync();

            // Attach last-IP info in memory now that both sides are materialized.
            var onlyUsersWithIp = onlyUsers.Select(u => new
            {
                u.Id,
                u.ModderId,
                u.UserName,
                u.Email,
                u.UserTypeId,
                u.LastActiveAt,
                LastIp = lastIps.TryGetValue(u.Id, out var lu) ? lu.LastIp : null,
                IpCount = lastIps.TryGetValue(u.Id, out var lu2) ? lu2.IpCount : 0
            });

            var inBothWithIp = inBoth.Select(x => new
            {
                User = new
                {
                    x.User.Id,
                    x.User.ModderId,
                    x.User.UserName,
                    x.User.Email,
                    x.User.UserTypeId,
                    x.User.LastActiveAt,
                    LastIp = lastIps.TryGetValue(x.User.Id, out var lb) ? lb.LastIp : null,
                    IpCount = lastIps.TryGetValue(x.User.Id, out var lb2) ? lb2.IpCount : 0
                },
                x.Modder
            });

            return Ok(new
            {
                onlyUsers = onlyUsersWithIp,
                onlyModders,
                inBoth = inBothWithIp
            });
        }
    }
}
