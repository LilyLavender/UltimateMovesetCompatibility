using System.Net;
using System.Text;
using CustomCharInfo.server.Services;
using Xunit;

namespace CustomCharInfo.server.Tests.Services
{
    public class GitHubAssetHasherTests
    {
        private const string HelloSha256 = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_respond(request));
        }

        // A stream that reports no length and yields the requested number of bytes, for the streaming cap.
        private sealed class EndlessStream : Stream
        {
            private long _remaining;
            public EndlessStream(long length) => _remaining = length;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = (int)Math.Min(count, _remaining);
                _remaining -= n;
                return n;
            }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private static GitHubAssetHasher Hasher(Func<HttpRequestMessage, HttpResponseMessage> respond)
            => new(new HttpClient(new StubHandler(respond)));

        [Fact]
        public async Task HashAsync_ReturnsLowercaseSha256AndSize()
        {
            var hasher = Hasher(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.ASCII.GetBytes("hello"))
            });

            var result = await hasher.HashAsync(new Uri("https://github.com/o/r/releases/download/v1/a.nro"), CancellationToken.None);

            Assert.Equal(HelloSha256, result.Hash);
            Assert.Equal(5, result.Size);
        }

        [Fact]
        public async Task HashAsync_RejectsDeclaredOversizedContent()
        {
            var hasher = Hasher(_ =>
            {
                var content = new ByteArrayContent(new byte[1]);
                content.Headers.ContentLength = GitHubAssetHasher.MaxBytes + 1;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            });

            await Assert.ThrowsAsync<AssetTooLargeException>(() =>
                hasher.HashAsync(new Uri("https://github.com/o/r/releases/download/v1/a.nro"), CancellationToken.None));
        }

        [Fact]
        public async Task HashAsync_StopsStreamingPastTheCap()
        {
            var hasher = Hasher(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new EndlessStream(GitHubAssetHasher.MaxBytes + 1))
            });

            await Assert.ThrowsAsync<AssetTooLargeException>(() =>
                hasher.HashAsync(new Uri("https://github.com/o/r/releases/download/v1/a.nro"), CancellationToken.None));
        }

        [Fact]
        public async Task HashAsync_SurfacesHttpFailures()
        {
            var hasher = Hasher(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                hasher.HashAsync(new Uri("https://github.com/o/r/releases/download/v1/a.nro"), CancellationToken.None));
        }
    }
}
