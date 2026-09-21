using System.Net;

namespace CustomCharInfo.server.Middleware
{
    // Render sits behind Cloudflare and appends its own proxies to X-Forwarded-For.
    // The forwarded-headers middleware reads one hop from the right, which lands on a Render address.
    // This runs after it and prefers the Cloudflare headers, which a client cannot forge.
    // Failing those it takes the leftmost forwarded entry.
    // With none of those headers present the connection address is left alone, so local development is unchanged.
    public class ClientIpMiddleware
    {
        public static readonly string[] HeaderPreference = { "CF-Connecting-IP", "True-Client-IP", "X-Forwarded-For" };

        private readonly RequestDelegate _next;

        public ClientIpMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var clientIp = ResolveClientIp(context.Request.Headers);
            if (clientIp != null)
                context.Connection.RemoteIpAddress = clientIp;

            return _next(context);
        }

        // Returns the first parseable address in header preference order, or null when no header names one.
        public static IPAddress? ResolveClientIp(IHeaderDictionary headers)
        {
            foreach (var name in HeaderPreference)
            {
                var first = headers[name].ToString().Split(',')[0].Trim();
                if (first.Length > 0 && IPAddress.TryParse(first, out var ip))
                    return ip;
            }

            return null;
        }
    }
}
