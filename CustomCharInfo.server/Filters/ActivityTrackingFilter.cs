using CustomCharInfo.server.Data;
using CustomCharInfo.server.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustomCharInfo.server.Filters
{
    // Records the caller's IP and last-active time for any authenticated POST/PUT.
    // Login is handled separately in AccountController since that request isn't authenticated yet.
    public class ActivityTrackingFilter : IAsyncActionFilter
    {
        private readonly IpActivityService _ipActivityService;
        private readonly AppDbContext _context;

        public ActivityTrackingFilter(IpActivityService ipActivityService, AppDbContext context)
        {
            _ipActivityService = ipActivityService;
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            var httpContext = context.HttpContext;
            var method = httpContext.Request.Method;
            if (method != HttpMethods.Post && method != HttpMethods.Put)
                return;

            if (httpContext.User.Identity?.IsAuthenticated != true)
                return;

            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            await _ipActivityService.Track(userId, ip);
            await _context.SaveChangesAsync();
        }
    }
}
