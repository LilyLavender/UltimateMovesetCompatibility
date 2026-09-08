using CustomCharInfo.server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CustomCharInfo.server.Tests.TestHelpers
{
    /// <summary>
    /// UserManager&lt;ApplicationUser&gt; has no interface, so controllers depend on the concrete
    /// class directly. Moq can still mock it (its members are virtual) as long as we satisfy the
    /// constructor with harmless stand-ins for the store/options/hashers it never actually needs
    /// in these tests.
    /// </summary>
    public static class MockUserManagerFactory
    {
        public static Mock<UserManager<ApplicationUser>> Create(string? currentUserId = null, IQueryable<ApplicationUser>? users = null)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(o => o.Value).Returns(new IdentityOptions());

            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                options.Object,
                new PasswordHasher<ApplicationUser>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object);

            if (currentUserId != null)
            {
                mgr.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                    .Returns(currentUserId);
            }

            // UserManager.Users normally proxies to an IQueryableUserStore; our bare store mock
            // doesn't implement that, so controllers reading `_userManager.Users` need this set up
            // explicitly or they NullReferenceException.
            mgr.Setup(m => m.Users).Returns(users ?? Enumerable.Empty<ApplicationUser>().AsQueryable());

            return mgr;
        }
    }
}
