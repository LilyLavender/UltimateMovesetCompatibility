using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using Microsoft.AspNetCore.Identity;

namespace CustomCharInfo.server.Helpers
{
    // Shared "who is calling and what may they do" helpers.
    // Roles are read from the database on every request rather than from the JWT,
    // so promoting or demoting a user takes effect immediately (see ADR 005).
    public static class RequesterExtensions
    {
        // The signed-in user's row, or null when anonymous or the row no longer exists.
        public static async Task<ApplicationUser?> GetRequesterAsync(
            this UserManager<ApplicationUser> userManager, AppDbContext context, ClaimsPrincipal principal)
        {
            var userId = userManager.GetUserId(principal);
            return userId == null ? null : await context.Users.FindAsync(userId);
        }

        public static bool IsAdmin([NotNullWhen(true)] this ApplicationUser? user) =>
            user?.UserTypeId == UserTypes.Admin;

        // Admins count as modders everywhere a modder is required.
        public static bool IsModder([NotNullWhen(true)] this ApplicationUser? user) =>
            user != null && user.UserTypeId >= UserTypes.Modder;
    }
}
