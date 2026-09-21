using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Helpers;

namespace CustomCharInfo.server.Controllers
{
    // Temporary diagnostic so an admin can see which proxy headers reach the app on Render.
    // Remove once ClientIpMiddleware's header preference has been confirmed in production.
    [ApiController]
    [Route("api/admin/request-info")]
    public class RequestInfoController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public RequestInfoController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetRequestInfo()
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin()) return Forbid();

            var headers = Request.Headers;
            return Ok(new
            {
                remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                cfConnectingIp = headers["CF-Connecting-IP"].ToString(),
                trueClientIp = headers["True-Client-IP"].ToString(),
                xForwardedFor = headers["X-Forwarded-For"].ToString(),
                xOriginalFor = headers["X-Original-For"].ToString()
            });
        }
    }
}
