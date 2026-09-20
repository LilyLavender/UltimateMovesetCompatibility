using CustomCharInfo.server.Services;

namespace CustomCharInfo.server.Tests.TestHelpers
{
    // Stands in for GitHubAssetHasher so controller tests never touch the network.
    public class FakeAssetHasher : IReleaseAssetHasher
    {
        public List<Uri> Calls { get; } = new();

        public Func<Uri, AssetHashResult> Handler { get; set; } = _ => new AssetHashResult(new string('f', 64), 123);

        public Task<AssetHashResult> HashAsync(Uri url, CancellationToken cancellationToken)
        {
            Calls.Add(url);
            return Task.FromResult(Handler(url));
        }
    }
}
