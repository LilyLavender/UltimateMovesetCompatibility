using System.Text.RegularExpressions;

namespace CustomCharInfo.server.Helpers
{
    // Turns the ways someone might paste a GitHub repository into an owner and repo pair.
    // Accepts "owner/repo", "github.com/owner/repo/anything", and full https URLs with or without ".git".
    public static class GitHubRepoRef
    {
        private static readonly Regex Pattern = new(
            @"^(?:https?://)?(?:www\.)?(?:github\.com/)?(?<owner>[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)/(?<repo>[A-Za-z0-9_.-]+?)(?:\.git)?(?:/.*)?$",
            RegexOptions.Compiled);

        private static readonly Regex ReleaseAssetPath = new(
            @"^/[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?/[A-Za-z0-9_.-]+/releases/download/[^/]+/[^/]+$",
            RegexOptions.Compiled);

        // Only a github.com release download link may be handed to the server-side hasher,
        // so the endpoint can never be pointed at anything else.
        public static bool TryParseReleaseAssetUrl(string? input, out Uri uri)
        {
            uri = null!;
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var parsed)) return false;
            if (parsed.Scheme != Uri.UriSchemeHttps) return false;
            if (!parsed.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                && !parsed.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase)) return false;
            if (!ReleaseAssetPath.IsMatch(parsed.AbsolutePath)) return false;
            uri = parsed;
            return true;
        }

        public static bool TryParse(string? input, out string owner, out string repo)
        {
            owner = "";
            repo = "";
            if (string.IsNullOrWhiteSpace(input)) return false;

            var match = Pattern.Match(input.Trim());
            if (!match.Success) return false;

            owner = match.Groups["owner"].Value;
            repo = match.Groups["repo"].Value;
            if (repo.Length == 0 || repo == "." || repo == "..") return false;
            return true;
        }
    }
}
