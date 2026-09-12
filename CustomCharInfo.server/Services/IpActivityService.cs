using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomCharInfo.server.Services
{
    public class IpActivityService
    {
        private readonly AppDbContext _context;

        public IpActivityService(AppDbContext context)
        {
            _context = context;
        }

        // Upserts the caller's IP for this user and bumps LastActiveAt.
        // Does not call SaveChangesAsync; callers already save as part of their own request handling.
        public async Task Track(string userId, string? ipAddress)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(ipAddress))
                return;

            var now = DateTime.UtcNow;

            var existing = await _context.UserIpAddresses
                .FirstOrDefaultAsync(uip => uip.UserId == userId && uip.IpAddress == ipAddress);

            if (existing != null)
            {
                existing.LastSeenAt = now;
            }
            else
            {
                _context.UserIpAddresses.Add(new UserIpAddress
                {
                    UserId = userId,
                    IpAddress = ipAddress,
                    LastSeenAt = now
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                user.LastActiveAt = now;
        }
    }
}
