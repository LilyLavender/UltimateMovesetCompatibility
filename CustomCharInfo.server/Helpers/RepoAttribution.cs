using System.Linq.Expressions;
using CustomCharInfo.server.Models;

namespace CustomCharInfo.server.Helpers
{
    // Ties plugins to a GitHub repository through their mod links, since nothing stores the link directly.
    // A plugin belongs to a repo when its default link is the repo page (or a page under it),
    // or any of its versions links under it. Release pages do, so batch-registered versions count.
    public static class RepoAttribution
    {
        public static string RepoUrl(string owner, string repo) =>
            $"https://github.com/{owner}/{repo}".ToLowerInvariant();

        public static Expression<Func<Plugin, bool>> MatchesRepo(string repoUrl)
        {
            var prefix = repoUrl + "/";
            return p => p.MovesetId == null && (
                (p.DefaultLearnMoreUrl != null && (p.DefaultLearnMoreUrl.ToLower() == repoUrl || p.DefaultLearnMoreUrl.ToLower().StartsWith(prefix)))
                || p.PluginVersions.Any(v => v.LearnMoreUrl != null && v.LearnMoreUrl.ToLower().StartsWith(prefix)));
        }
    }
}
