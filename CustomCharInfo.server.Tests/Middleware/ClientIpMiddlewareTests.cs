using CustomCharInfo.server.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CustomCharInfo.server.Tests.Middleware
{
    public class ClientIpMiddlewareTests
    {
        private static IHeaderDictionary Headers(params (string Name, string Value)[] entries)
        {
            var headers = new HeaderDictionary();
            foreach (var (name, value) in entries)
                headers[name] = value;
            return headers;
        }

        [Fact]
        public void PrefersCloudflareHeaderOverForwardedFor()
        {
            var headers = Headers(("CF-Connecting-IP", "203.0.113.5"), ("X-Forwarded-For", "198.51.100.9, 10.25.0.1"));

            Assert.Equal("203.0.113.5", ClientIpMiddleware.ResolveClientIp(headers)?.ToString());
        }

        [Fact]
        public void FallsBackToTrueClientIp()
        {
            var headers = Headers(("True-Client-IP", "203.0.113.7"), ("X-Forwarded-For", "198.51.100.9, 10.25.0.1"));

            Assert.Equal("203.0.113.7", ClientIpMiddleware.ResolveClientIp(headers)?.ToString());
        }

        [Fact]
        public void TakesLeftmostForwardedForEntry()
        {
            var headers = Headers(("X-Forwarded-For", "198.51.100.9, 10.25.0.1, 10.31.4.2"));

            Assert.Equal("198.51.100.9", ClientIpMiddleware.ResolveClientIp(headers)?.ToString());
        }

        [Fact]
        public void ReturnsNullWithoutProxyHeaders()
        {
            Assert.Null(ClientIpMiddleware.ResolveClientIp(Headers()));
        }

        [Fact]
        public void SkipsUnparseableValues()
        {
            var headers = Headers(("CF-Connecting-IP", "not-an-ip"), ("X-Forwarded-For", "2001:db8::1"));

            Assert.Equal("2001:db8::1", ClientIpMiddleware.ResolveClientIp(headers)?.ToString());
        }

        [Fact]
        public async Task InvokeOverridesConnectionAddressOnlyWhenHeaderPresent()
        {
            var middleware = new ClientIpMiddleware(_ => Task.CompletedTask);

            var withHeader = new DefaultHttpContext();
            withHeader.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.25.0.1");
            withHeader.Request.Headers["CF-Connecting-IP"] = "203.0.113.5";
            await middleware.InvokeAsync(withHeader);
            Assert.Equal("203.0.113.5", withHeader.Connection.RemoteIpAddress?.ToString());

            var without = new DefaultHttpContext();
            without.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            await middleware.InvokeAsync(without);
            Assert.Equal(System.Net.IPAddress.Loopback, without.Connection.RemoteIpAddress);
        }
    }
}
