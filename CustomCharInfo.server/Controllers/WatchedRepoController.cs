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
    // The page itself calls GitHub from the browser; the server only remembers which repos to check.
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

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<WatchedRepoDto>>> GetWatchedRepos()
        {
            if (await GetAdminUserAsync() == null) return Forbid();

            var repos = await _context.WatchedRepos
                .AsNoTracking()
                .OrderBy(w => w.Owner.ToLower())
                .ThenBy(w => w.Repo.ToLower())
                .Select(w => new WatchedRepoDto
                {
                    WatchedRepoId = w.WatchedRepoId,
                    Owner = w.Owner,
                    Repo = w.Repo,
                    AddedByUsername = w.AddedBy != null ? w.AddedBy.UserName : null,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return Ok(repos);
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

            return CreatedAtAction(nameof(GetWatchedRepos), null, new WatchedRepoDto
            {
                WatchedRepoId = watched.WatchedRepoId,
                Owner = watched.Owner,
                Repo = watched.Repo,
                AddedByUsername = user.UserName,
                CreatedAt = watched.CreatedAt
            });
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
