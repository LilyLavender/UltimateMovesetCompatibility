using CustomCharInfo.server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CustomCharInfo.server.Tests.TestHelpers
{
    /// <summary>
    /// AccountController takes a SignInManager&lt;ApplicationUser&gt; dependency it never actually
    /// calls in the paths we test, so this just satisfies the constructor with harmless mocks.
    /// </summary>
    public static class MockSignInManagerFactory
    {
        public static SignInManager<ApplicationUser> Create(UserManager<ApplicationUser> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            return new SignInManager<ApplicationUser>(
                userManager,
                contextAccessor.Object,
                claimsFactory.Object,
                Options.Create(new IdentityOptions()),
                new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
                new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<ApplicationUser>>().Object);
        }
    }
}
