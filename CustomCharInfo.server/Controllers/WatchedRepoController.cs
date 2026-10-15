using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

namespace CustomCharInfo.server.Controllers
{
    // The list of GitHub repositories admins recheck on the Repo Releases page.
    // The page itself calls GitHub from the browser; the server remembers which repos to check
    // and what the last check found, so the list can say which repos have a release newer than
    // anything registered from them.
    [ApiController]
    [Route("api/watched-repos")]
    public class WatchedRepoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WatchedRepoController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<ApplicationUser?> GetAdminUserAsync()
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            return user.IsAdmin() ? user : null;
        }

        // A repo needs an update when nothing is attributed to it yet, or its newest release
        // was published after the newest version registered from it.
        internal static string ResolveStatus(WatchedRepo watched, int attributedPluginCount, DateTime? newestVersionAt)
        {
            if (watched.LastCheckedAt == null) return WatchedRepoStatus.Unchecked;
            if (watched.LastReleaseAt == null) return WatchedRepoStatus.NoReleases;
            if (attributedPluginCount == 0 || newestVersionAt == null) return WatchedRepoStatus.NeedsUpdate;
            return watched.LastReleaseAt > newestVersionAt ? WatchedRepoStatus.NeedsUpdate : WatchedRepoStatus.UpToDate;
        }

        private async Task<WatchedRepoDto> ToDtoAsync(WatchedRepo watched)
        {
            var repoUrl = RepoAttribution.RepoUrl(watched.Owner, watched.Repo);
            var attributed = await _context.Plugins
                .Where(RepoAttribution.MatchesRepo(repoUrl))
                .Select(p => p.PluginVersions.Max(v => (DateTime?)v.CreatedAt))
                .ToListAsync();
            var newestVersionAt = attributed.Count == 0 ? null : attributed.Max();

            return new WatchedRepoDto
            {
                WatchedRepoId = watched.WatchedRepoId,
                Owner = watched.Owner,
                Repo = watched.Repo,
                CreatedAt = watched.CreatedAt,
                LastCheckedAt = watched.LastCheckedAt,
                LastReleaseAt = watched.LastReleaseAt,
                LastReleaseTag = watched.LastReleaseTag,
                LastReleaseUrl = watched.LastReleaseUrl,
                AttributedPluginCount = attributed.Count,
                Status = ResolveStatus(watched, attributed.Count, newestVersionAt)
            };
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<WatchedRepoDto>>> GetWatchedRepos()
        {
            if (await GetAdminUserAsync() == null) return Forbid();

            var repos = await _context.WatchedRepos
                .AsNoTracking()
                .OrderBy(w => w.Owner.ToLower())
                .ThenBy(w => w.Repo.ToLower())
                .ToListAsync();

            var result = new List<WatchedRepoDto>();
            foreach (var watched in repos)
                result.Add(await ToDtoAsync(watched));

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<WatchedRepoDto>> AddWatchedRepo(AddWatchedRepoDto dto)
        {
            var user = await GetAdminUserAsync();
            if (user == null) return Forbid();

            if (!GitHubRepoRef.TryParse(dto.Input, out var owner, out var repo))
                return BadRequest("Enter a GitHub repository as owner/repo or a github.com URL.");

            var ownerLower = owner.ToLower();
            var repoLower = repo.ToLower();
            if (await _context.WatchedRepos.AnyAsync(w => w.Owner.ToLower() == ownerLower && w.Repo.ToLower() == repoLower))
                return Conflict("That repository is already in the list.");

            var watched = new WatchedRepo
            {
                Owner = owner,
                Repo = repo,
                AddedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.WatchedRepos.Add(watched);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWatchedRepos), null, await ToDtoAsync(watched));
        }

        // The page reports what a check found: the newest release, or nothing when the repo has none.
        [HttpPost("{id}/check-result")]
        [Authorize]
        public async Task<ActionResult<WatchedRepoDto>> ReportCheckResult(int id, WatchedRepoCheckResultDto dto)
        {
            if (await GetAdminUserAsync() == null) return Forbid();

            var watched = await _context.WatchedRepos.FindAsync(id);
            if (watched == null) return NotFound();

            watched.LastCheckedAt = DateTime.UtcNow;
            watched.LastReleaseAt = dto.LastReleaseAt;
            watched.LastReleaseTag = dto.LastReleaseTag;
            watched.LastReleaseUrl = dto.LastReleaseUrl;
            await _context.SaveChangesAsync();

            return Ok(await ToDtoAsync(watched));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteWatchedRepo(int id)
        {
            if (await GetAdminUserAsync() == null) return Forbid();

            var watched = await _context.WatchedRepos.FindAsync(id);
            if (watched == null) return NotFound();

            _context.WatchedRepos.Remove(watched);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
