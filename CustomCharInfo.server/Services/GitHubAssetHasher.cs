using System.Security.Cryptography;

namespace CustomCharInfo.server.Services
{
    public record AssetHashResult(string Hash, long Size);

    public class AssetTooLargeException : Exception
    {
        public AssetTooLargeException(long maxBytes)
            : base($"Asset is larger than the {maxBytes / (1024 * 1024)} MB limit.") { }
    }

    // Downloads a release asset and returns its SHA-256, for assets that predate GitHub's own digests.
    // Needed because GitHub's asset host sends no CORS headers, so the browser cannot do this itself.
    public interface IReleaseAssetHasher
    {
        Task<AssetHashResult> HashAsync(Uri url, CancellationToken cancellationToken);
    }

    public class GitHubAssetHasher : IReleaseAssetHasher
    {
        public const long MaxBytes = 50L * 1024 * 1024;

        private readonly HttpClient _http;

        public GitHubAssetHasher(HttpClient http)
        {
            _http = http;
        }

        public async Task<AssetHashResult> HashAsync(Uri url, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > MaxBytes)
                throw new AssetTooLargeException(MaxBytes);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxBytes) throw new AssetTooLargeException(MaxBytes);
                sha.AppendData(buffer, 0, read);
            }

            return new AssetHashResult(Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant(), total);
        }
    }
}
