using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;

using SixLabors.ImageSharp;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/")]
    public class LookupController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public LookupController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dependencies")]
        public async Task<IActionResult> GetDependencies()
        {
            var deps = await _context.Dependencies.Select(d => new
            {
                d.DependencyId,
                d.Name,
                d.DownloadLink
            }).ToListAsync();
            return Ok(deps);
        }

        [HttpGet("vanillachars")]
        public async Task<IActionResult> GetVanillaChars()
        {
            var chars = await _context.VanillaChars.ToListAsync();
            return Ok(chars);
        }

        [HttpGet("articles")]
        public async Task<IActionResult> GetArticles()
        {
            var articles = await _context.Articles.Select(a => new
            {
                a.ArticleId,
                a.VanillaCharInternalName,
                a.ArticleName
            }).ToListAsync();
            return Ok(articles);
        }

        [HttpGet("hookablestatuses")]
        public async Task<IActionResult> GetHookableStatuses()
        {
            var statuses = await _context.HookableStatuses.Select(hs => new
            {
                hs.HookableStatusId,
                hs.Name
            }).ToListAsync();
            return Ok(statuses);
        }

        [HttpGet("releasestates")]
        public async Task<IActionResult> GetReleaseStates()
        {
            var states = await _context.ReleaseStates.ToListAsync();
            return Ok(states);
        }    
    }
}
