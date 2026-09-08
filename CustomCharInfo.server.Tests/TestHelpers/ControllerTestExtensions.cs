using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CustomCharInfo.server.Tests.TestHelpers
{
    public static class ControllerTestExtensions
    {
        /// <summary>
        /// Gives a controller an HttpContext with a bare ClaimsPrincipal so ControllerBase.User
        /// doesn't throw. The mocked UserManager.GetUserId ignores the principal's actual claims,
        /// so this identity only needs to exist, not carry real claim values.
        /// </summary>
        public static void SetFakeUser(this ControllerBase controller)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        /// <summary>
        /// Sets a NameIdentifier claim on the controller's User, for code (e.g.
        /// ActionLogsController.CreateActionLog) that reads the claim directly instead of going
        /// through UserManager.GetUserId.
        /// </summary>
        public static void SetFakeUser(this ControllerBase controller, string userId)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
