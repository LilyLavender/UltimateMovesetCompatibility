using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/")]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public ApiController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region Dependencies

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

        #endregion Dependencies

        #region Vanilla Chars

        [HttpGet("vanillachars")]
        public async Task<IActionResult> GetVanillaChars()
        {
            var chars = await _context.VanillaChars.ToListAsync();
            return Ok(chars);
        }

        #endregion Vanilla Chars

        #region Articles

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

        #endregion Articles

        #region Hookable Statuses

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

        #endregion Hookable Statuses

        #region Release States

        [HttpGet("releasestates")]
        public async Task<IActionResult> GetReleaseStates()
        {
            var states = await _context.ReleaseStates.ToListAsync();
            return Ok(states);
        }

        #endregion Release States

        #region Series

        [HttpGet("series")]
        public async Task<IActionResult> GetSeries()
        {
            var userId = _userManager.GetUserId(User);

            var modderId = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.ModderId)
                .FirstOrDefaultAsync();

            // All series user has a moveset from
            var movesetSeriesIds = modderId != null
                ? await _context.MovesetModders
                    .Where(mm => mm.ModderId == modderId)
                    .Select(mm => mm.Moveset.SeriesId)
                    .Distinct()
                    .ToListAsync()
                : new List<int?>();

            // Get the most recent ActionLog for each series
            var latestLogs = await _context.ActionLogs
                .Where(log =>
                    log.ItemTypeId == 3 &&
                    movesetSeriesIds.Contains(log.ItemId))
                .GroupBy(log => log.ItemId)
                .Select(g => g.OrderByDescending(l => l.CreatedAt).First())
                .ToListAsync();

            // Only allow edit if latest log for series is pending user action
            var editableSeriesIds = latestLogs
                .Where(log =>
                    log.AcceptanceStateId == 3 || log.AcceptanceStateId == 4)
                .Select(log => log.ItemId)
                .ToHashSet();

            var seriesList = await _context.Series
                .Select(s => new
                {
                    s.SeriesId,
                    s.SeriesName,
                    s.SeriesIconUrl,
                    MovesetCount = _context.Movesets.Count(m => m.SeriesId == s.SeriesId),
                    CanEdit = editableSeriesIds.Contains(s.SeriesId)
                })
                .ToListAsync();

            return Ok(seriesList);
        }

        [HttpGet("series/{id}")]
        public async Task<ActionResult<ReturnSeriesDto>> GetOneSeries(int id)
        {
            var series = await _context.Series
                .Where(s => s.SeriesId == id)
                .Select(s => new ReturnSeriesDto
                {
                    SeriesId = s.SeriesId,
                    SeriesName = s.SeriesName,
                    SeriesIconUrl = s.SeriesIconUrl,
                    MovesetCount = _context.Movesets.Count(m => m.SeriesId == s.SeriesId)
                })
                .FirstOrDefaultAsync();

            if (series == null)
                return NotFound();

            return Ok(series);
        }

        [HttpPost("series")]
        [Authorize]
        public async Task<IActionResult> CreateSeries([FromBody] UpdateSeriesDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.SeriesName))
                return BadRequest("Series name is required.");

            var series = new Series
            {
                SeriesName = dto.SeriesName,
                SeriesIconUrl = dto.SeriesIconUrl
            };

            _context.Series.Add(series);
            await _context.SaveChangesAsync();

            // Log action
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();
            int newState = user?.UserTypeId == 3 ? 7 : 2;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 3,
                ItemId = series.SeriesId,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetSeries), new { id = series.SeriesId }, series);
        }

        [HttpPut("series/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSeries(int id, UpdateSeriesDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            var existingSeries = await _context.Series.FindAsync(id);
            if (existingSeries == null)
                return NotFound();

            // Find latest log for this series
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 3 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            // Only allow editing if latest state is 3 or 4
            if (latestLog is null || (latestLog.AcceptanceStateId != 3 && latestLog.AcceptanceStateId != 4))
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.SeriesName))
                return BadRequest("SeriesName is required.");

            existingSeries.SeriesName = dto.SeriesName;
            existingSeries.SeriesIconUrl = dto.SeriesIconUrl;

            // Calculate new acceptance state
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            int newState;
            if (user?.UserTypeId == 3) { newState = 7; }
            else if (latestLog.AcceptanceStateId == 3) { newState = 1; }
            else if (latestLog.AcceptanceStateId == 4) { newState = 2; }
            else { return Forbid(); }

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 3,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("series/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSeries(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var series = await _context.Series.FindAsync(id);
            if (series == null)
                return NotFound();

            _context.Series.Remove(series);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        #endregion Series

        #region Hooks

        [HttpGet("hooks")]
        public async Task<ActionResult<IEnumerable<HookDto>>> GetHooks()
        {
            var hooks = await _context.Hooks
                .Include(h => h.HookableStatus)
                .Select(h => new HookDto
                {
                    HookId = h.HookId,
                    Offset = h.Offset,
                    Description = h.Description,
                    HookableStatusId = h.HookableStatusId,
                    HookableStatus = h.HookableStatus.Name
                })
                .ToListAsync();

            return Ok(hooks);
        }

        [HttpGet("hooks/{id}")]
        public async Task<ActionResult<HookDto>> GetHook(int id)
        {
            var hook = await _context.Hooks
                .Include(h => h.HookableStatus)
                .Where(h => h.HookId == id)
                .Select(h => new HookDto
                {
                    HookId = h.HookId,
                    Offset = h.Offset,
                    Description = h.Description,
                    HookableStatusId = h.HookableStatusId,
                    HookableStatus = h.HookableStatus.Name
                })
                .FirstOrDefaultAsync();

            if (hook == null)
                return NotFound();

            return Ok(hook);
        }

        [HttpPost("hooks")]
        [Authorize]
        public async Task<ActionResult<CreateHookDto>> CreateHook(CreateHookDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            var hook = new Hook
            {
                Offset = dto.Offset,
                Description = dto.Description,
                HookableStatusId = dto.HookableStatusId
            };

            _context.Hooks.Add(hook);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHook), new { id = hook.HookId }, hook);
        }

        [HttpPut("hooks/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateHook(int id, UpdateHookDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            var hook = await _context.Hooks.FindAsync(id);
            if (hook == null)
                return NotFound();

            if (dto.Offset != null)
                hook.Offset = dto.Offset;

            if (dto.Description != null)
                hook.Description = dto.Description;

            if (dto.HookableStatusId.HasValue)
                hook.HookableStatusId = dto.HookableStatusId.Value;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("hooks/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteHook(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var hook = await _context.Hooks.FindAsync(id);
            if (hook == null)
                return NotFound();

            _context.Hooks.Remove(hook);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion Hooks

        #region Modders

        [HttpGet("modders")]
        public async Task<ActionResult> GetModders()
        {
            var modders = await _context.Modders
                .Include(m => m.User)
                .Select(m => new
                {
                    m.ModderId,
                    Name = m.User != null ? m.User.UserName : m.Name,
                    m.Bio,
                    m.GamebananaId,
                    m.DiscordUsername,
                }).ToListAsync();

            return Ok(modders);
        }

        [HttpGet("modders/{id}")]
        public async Task<ActionResult> GetModder(int id)
        {
            var modder = await _context.Modders
                .Include(m => m.User)
                .Where(m => m.ModderId == id)
                .Select(m => new
                {
                    m.ModderId,
                    Name = m.User != null ? m.User.UserName : m.Name,
                    m.Bio,
                    m.GamebananaId,
                    m.DiscordUsername,
                }).FirstOrDefaultAsync();

            if (modder == null)
                return NotFound();

            return Ok(modder);
        }

        [Authorize]
        [HttpPost("modders")]
        public async Task<ActionResult<CreateModderDto>> CreateModder(CreateModderDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId, u.UserName })
                .SingleOrDefaultAsync();

            if (user == null || user.ModderId != null)
                return Unauthorized();

            var modder = new Modder
            {
                Name = user?.UserName,
                Bio = dto.Bio,
                GamebananaId = dto.GamebananaId,
                DiscordUsername = dto.DiscordUsername
            };

            _context.Modders.Add(modder);
            await _context.SaveChangesAsync();

            // Log action
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 2,
                ItemId = modder.ModderId,
                AcceptanceStateId = 2,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetModder), new { id = modder.ModderId }, modder);
        }

        [Authorize]
        [HttpPut("modders/{id}")]
        public async Task<IActionResult> UpdateModder(int id, UpdateModderDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            if (user == null)
                return Forbid();

            bool isOwner = user.ModderId == id;

            bool isOriginalSubmitter = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 2 && a.ItemId == id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync() == userId;

            if (!(isOwner || isOriginalSubmitter || user.UserTypeId == 3))
                return Forbid("You are not authorized to edit this modder profile.");

            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 2 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestLog?.AcceptanceStateId == 6)
                return Forbid("This modder profile has been rejected and cannot be edited.");

            var modder = await _context.Modders.FindAsync(id);
            if (modder == null)
                return NotFound();

            if (dto.Bio != null)
                modder.Bio = dto.Bio;

            if (dto.GamebananaId.HasValue)
                modder.GamebananaId = dto.GamebananaId.Value;

            if (dto.DiscordUsername != null)
                modder.DiscordUsername = dto.DiscordUsername;

            int newState = user.UserTypeId == 3
                ? 7
                : (latestLog?.AcceptanceStateId == 2 || latestLog?.AcceptanceStateId == 4) ? 2 : 1;

            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 2,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("modders/{id}")]
        public async Task<IActionResult> DeleteModder(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var modder = await _context.Modders.FindAsync(id);
            if (modder == null)
                return NotFound();

            _context.Modders.Remove(modder);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion Modders

        #region Movesets

        [HttpGet("movesets")]
        public async Task<ActionResult<IEnumerable<object>>> GetMovesets(
            [FromQuery] int? seriesId,
            [FromQuery] int? releaseStateId,
            [FromQuery] int? modderId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Movesets
                .AsNoTracking()
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.MovesetModders)
                    .ThenInclude(mm => mm.Modder)
                        .ThenInclude(modder => modder.User)
                .AsQueryable();

            if (seriesId.HasValue)
                query = query.Where(m => m.SeriesId == seriesId);

            if (releaseStateId.HasValue)
                query = query.Where(m => m.ReleaseStateId == releaseStateId);

            if (modderId.HasValue)
                query = query.Where(m =>
                    m.MovesetModders.Any(mm => mm.Modder.ModderId == modderId.Value));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.ModdedCharName.Contains(search));

            var movesets = await query
                .OrderBy(m => m.ModdedCharName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.MovesetId,
                    m.ModdedCharName,
                    SeriesIconUrl = m.Series.SeriesIconUrl,
                    BackgroundColor = m.BackgroundColor,
                    ThumbhImageUrl = m.ThumbhImageUrl,
                    ReleaseState = m.ReleaseState.ReleaseStateName,
                    Modders = m.MovesetModders
                        .Select(mm => mm.Modder.User.UserName ?? mm.Modder.Name)
                        .ToList(),
                    ReleaseDate = m.ReleaseDate,
                    AdminPick = m.AdminPick,
                    PrivateMoveset = m.PrivateMoveset,
                    PrivateModder = m.PrivateModder,
                })
                .ToListAsync();

            return Ok(movesets);
        }

        [HttpGet("movesets/{id}")]
        public async Task<ActionResult<Moveset>> GetMoveset(int id)
        {
            var moveset = await _context.Movesets
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.VanillaChar)
                .Include(m => m.MovesetDependencies).ThenInclude(md => md.Dependency)
                .Include(m => m.MovesetModders).ThenInclude(mm => mm.Modder).ThenInclude(modder => modder.User)
                .Include(m => m.MovesetArticles).ThenInclude(ma => ma.Article)
                .Include(m => m.MovesetHooks).ThenInclude(mh => mh.Hook).ThenInclude(h => h.HookableStatus)
                .FirstOrDefaultAsync(m => m.MovesetId == id);

            if (moveset == null)
                return NotFound();

            return Ok(moveset);
        }

        [HttpGet("movesets/by-internal-id/{slottedId}")]
        public async Task<ActionResult<Moveset>> GetMovesetBySlottedId(string slottedId)
        {
            var moveset = await _context.Movesets
                .Include(m => m.Series)
                .Include(m => m.ReleaseState)
                .Include(m => m.VanillaChar)
                .Include(m => m.MovesetDependencies).ThenInclude(md => md.Dependency)
                .Include(m => m.MovesetModders).ThenInclude(mm => mm.Modder).ThenInclude(modder => modder.User)
                .Include(m => m.MovesetArticles).ThenInclude(ma => ma.Article)
                .Include(m => m.MovesetHooks).ThenInclude(mh => mh.Hook).ThenInclude(h => h.HookableStatus)
                .FirstOrDefaultAsync(m => m.SlottedId == slottedId);

            if (moveset == null)
                return NotFound();

            return Ok(moveset);
        }

        [HttpPost("movesets")]
        public async Task<ActionResult<Moveset>> PostMoveset(CreateMovesetDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var userFromId = await _context.Users.FindAsync(userId);
            if (userFromId == null || userFromId.UserTypeId < 2)
                return Forbid();

            if (dto.ModderIds == null || !dto.ModderIds.Any())
                return BadRequest("At least one ModderId is required.");

            var moveset = new Moveset
            {
                ModdedCharName = dto.ModdedCharName,
                VanillaCharInternalName = dto.VanillaCharInternalName,
                SeriesId = dto.SeriesId,
                SlottedId = dto.SlottedId,
                ReplacementId = dto.ReplacementId,
                SlotsStart = dto.SlotsStart ?? 0,
                SlotsEnd = dto.SlotsEnd ?? 0,
                ReleaseStateId = dto.ReleaseStateId,
                HasGlobalOpff = dto.HasGlobalOpff,
                HasCharacterOpff = dto.HasCharacterOpff,
                HasAgentInit = dto.HasAgentInit,
                HasGlobalOnLinePre = dto.HasGlobalOnLinePre,
                HasGlobalOnLineEnd = dto.HasGlobalOnLineEnd,
                GamebananaPageId = dto.GamebananaPageId ?? 0,
                GamebananaWipId = dto.GamebananaWipId ?? 0,
                ThumbhImageUrl = dto.ThumbhImageUrl,
                MovesetHeroImageUrl = dto.MovesetHeroImageUrl,
                BackgroundColor = dto.BackgroundColor,
                ModsWikiLink = dto.ModsWikiLink,
                ReleaseDate = dto.ReleaseDate,
                ModpackName = dto.ModpackName,
                SourceCode = dto.SourceCode,
                PrivateMoveset = dto.PrivateMoveset,
                PrivateModder = dto.PrivateModder,
                MovesetModders = dto.ModderIds.Select(id => new MovesetModder { ModderId = id }).ToList(),
                MovesetDependencies = dto.DependencyIds?.Select(id => new MovesetDependency { DependencyId = id }).ToList() ?? new List<MovesetDependency>(),
                MovesetHooks = dto.Hooks?.Select(h => new MovesetHook
                {
                    HookId = h.HookId,
                    Description = h.Description
                }).ToList() ?? new List<MovesetHook>(),
                MovesetArticles = dto.Articles?.Select(a => new MovesetArticle
                {
                    ArticleId = a.ArticleId,
                    ModdedName = a.ModdedName,
                    Description = a.Description
                }).ToList() ?? new List<MovesetArticle>()
            };

            _context.Movesets.Add(moveset);
            await _context.SaveChangesAsync();

            // Log action
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();
            int newState = user?.UserTypeId == 3 ? 7 : 2;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 1,
                ItemId = moveset.MovesetId,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetMoveset), new { id = moveset.MovesetId }, moveset);
        }

        [Authorize]
        [HttpPut("movesets/{id}")]
        public async Task<IActionResult> PutMoveset(int id, CreateMovesetDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ModderId, u.UserTypeId })
                .SingleOrDefaultAsync();

            if (user == null || user.ModderId == null)
                return Forbid();

            var moveset = await _context.Movesets
                .Include(m => m.MovesetModders)
                .Include(m => m.MovesetDependencies)
                .Include(m => m.MovesetHooks)
                .Include(m => m.MovesetArticles)
                .FirstOrDefaultAsync(m => m.MovesetId == id);

            if (moveset == null)
                return NotFound();

            // Make sure current user is a modder on the moveset
            bool isModder = moveset.MovesetModders.Any(mm => mm.ModderId == user.ModderId);
            if (!isModder)
                return Forbid();

            // Find latest log; Don't allow editing if last acceptance state isn't what's expected.
            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == 2 && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            if (latestLog?.AcceptanceStateId == 6)
                return Forbid();

            if (dto.ModderIds == null || !dto.ModderIds.Any())
            {
                return BadRequest("At least one ModderId is required.");
            }

            if (dto.DependencyIds == null || dto.Hooks == null || dto.Articles == null)
            {
                return BadRequest("DependencyIds, Hooks, and Articles must be present (even if empty).");
            }

            moveset.ModdedCharName = dto.ModdedCharName;
            moveset.VanillaCharInternalName = dto.VanillaCharInternalName;
            moveset.SeriesId = dto.SeriesId;
            moveset.SlottedId = dto.SlottedId;
            moveset.ReplacementId = dto.ReplacementId;
            moveset.SlotsStart = dto.SlotsStart ?? 0;
            moveset.SlotsEnd = dto.SlotsEnd ?? 0;
            moveset.ReleaseStateId = dto.ReleaseStateId;
            moveset.HasGlobalOpff = dto.HasGlobalOpff;
            moveset.HasCharacterOpff = dto.HasCharacterOpff;
            moveset.HasAgentInit = dto.HasAgentInit;
            moveset.HasGlobalOnLinePre = dto.HasGlobalOnLinePre;
            moveset.HasGlobalOnLineEnd = dto.HasGlobalOnLineEnd;
            moveset.GamebananaPageId = dto.GamebananaPageId ?? 0;
            moveset.GamebananaWipId = dto.GamebananaWipId ?? 0;
            moveset.ThumbhImageUrl = dto.ThumbhImageUrl;
            moveset.MovesetHeroImageUrl = dto.MovesetHeroImageUrl;
            moveset.BackgroundColor = dto.BackgroundColor;
            moveset.ModsWikiLink = dto.ModsWikiLink;
            moveset.ReleaseDate = dto.ReleaseDate;
            moveset.ModpackName = dto.ModpackName;
            moveset.SourceCode = dto.SourceCode;
            moveset.PrivateMoveset = dto.PrivateMoveset;
            moveset.PrivateModder = dto.PrivateModder;

            // Sync (i know that guy!!) Modders
            _context.MovesetModders.RemoveRange(moveset.MovesetModders);
            moveset.MovesetModders = dto.ModderIds
                .Select(mid => new MovesetModder { MovesetId = id, ModderId = mid })
                .ToList();

            // Sync Dependencies
            _context.MovesetDependencies.RemoveRange(moveset.MovesetDependencies);
            moveset.MovesetDependencies = dto.DependencyIds
                .Select(did => new MovesetDependency { MovesetId = id, DependencyId = did })
                .ToList();

            // Sync Hooks
            _context.MovesetHooks.RemoveRange(moveset.MovesetHooks);
            moveset.MovesetHooks = dto.Hooks
                .Select(h => new MovesetHook
                {
                    MovesetId = id,
                    HookId = h.HookId,
                    Description = h.Description
                })
                .ToList();

            // Sync Articles
            _context.MovesetArticles.RemoveRange(moveset.MovesetArticles);
            moveset.MovesetArticles = dto.Articles
                .Select(a => new MovesetArticle
                {
                    MovesetId = id,
                    ArticleId = a.ArticleId,
                    ModdedName = a.ModdedName,
                    Description = a.Description
                })
                .ToList();

            // Log action
            int newState = user?.UserTypeId == 3
            ? 7
            : latestLog != null
            ? 2
            : (latestLog?.AcceptanceStateId == 2 || latestLog?.AcceptanceStateId == 4) ? 2 : 1;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 1,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = "", // todo allow notes
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("movesets/{id}")]
        public async Task<IActionResult> DeleteMoveset(int id)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var moveset = await _context.Movesets.FindAsync(id);
            if (moveset == null)
                return NotFound();

            _context.Movesets.Remove(moveset);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion Movesets

        #region Blog

        [HttpGet("blog")]
        public async Task<ActionResult<IEnumerable<BlogPostDto>>> GetBlogPosts()
        {
            var posts = await _context.BlogPosts
                .Include(p => p.User)
                .OrderByDescending(p => p.PostedDate)
                .Select(p => new BlogPostDto
                {
                    BlogPostId = p.BlogPostId,
                    BlogTitle = p.BlogTitle,
                    BlogText = p.BlogText,
                    BlogImageUrl = p.BlogImageUrl,
                    PostedDate = p.PostedDate,
                    AuthorUserName = p.User.UserName
                })
                .ToListAsync();

            return Ok(posts);
        }

        [Authorize]
        [HttpPost("blog")]
        public async Task<IActionResult> CreateBlogPost(CreateBlogPostDto dto)
        {
            // Make sure user is admin
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            var blogPost = new BlogPost
            {
                BlogTitle = dto.BlogTitle,
                BlogText = dto.BlogText,
                BlogImageUrl = dto.BlogImageUrl,
                UserId = userId,
                PostedDate = DateTime.UtcNow
            };

            _context.BlogPosts.Add(blogPost);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlogPosts), new { id = blogPost.BlogPostId }, blogPost);
        }

        #endregion

        #region Uploads

        [HttpPost("upload/moveset-image")]
        [Authorize]
        public async Task<IActionResult> UploadMovesetImage([FromForm] ImageUploadDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId < 2)
                return Forbid();

            if (dto.File == null || string.IsNullOrWhiteSpace(dto.Type) || string.IsNullOrWhiteSpace(dto.ItemName))
                return BadRequest("Missing file, type, or itemName");

            var allowedTypes = new Dictionary<string, (int width, int height)>
            {
                { "thumb_h", (340, 82) },
                { "moveset_hero", (1200, 1200) },
                { "series_icon", (800, 800) },
            };

            if (!allowedTypes.TryGetValue(dto.Type, out var expectedSize))
                return BadRequest("Invalid type. Must be one of thumb_h, moveset_hero, series_icon");

            using var imageStream = dto.File.OpenReadStream();
            using var image = Image.Load(imageStream);
            if (image.Width != expectedSize.width || image.Height != expectedSize.height)
                return BadRequest($"Invalid image dimensions. {dto.Type} must be {expectedSize.width}x{expectedSize.height}");

            var rawName = Path.GetFileNameWithoutExtension(dto.ItemName);
            var safeName = Regex.Replace(rawName, "[^a-zA-Z0-9]", "");

            if (dto.Type != "series_icon")
            {
                safeName = safeName.ToLower();
            }

            string fileName;
            string uploadDir;

            if (dto.Type == "series_icon")
            {
                fileName = $"{safeName}SeriesIcon{Path.GetExtension(dto.File.FileName)}";
                uploadDir = Path.Combine("wwwroot", "uploads", "series-icons");
            }
            else
            {
                fileName = $"{dto.Type}_{safeName}{Path.GetExtension(dto.File.FileName)}";
                uploadDir = Path.Combine("wwwroot", "uploads", "moveset-ui");
            }

            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, fileName);

            using var saveStream = System.IO.File.Create(filePath);
            await dto.File.CopyToAsync(saveStream);

            var relativeUrl = dto.Type == "series_icon"
                ? $"/uploads/series-icons/{fileName}"
                : $"/uploads/moveset-ui/{fileName}";

            return Ok(new { url = relativeUrl });
        }
        
        [HttpPost("upload/blog-image")]
        [Authorize]
        public async Task<IActionResult> UploadBlogImage([FromForm] BlogImageUploadDto dto)
        {
            // Make sure user is modder
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId < 2)
                return Forbid();

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("File is missing.");

            string uploadDir = Path.Combine("wwwroot", "uploads", "blog-images");
            Directory.CreateDirectory(uploadDir);

            string fileExt = Path.GetExtension(dto.File.FileName);
            string fileName;
            string filePath;

            do
            {
                fileName = $"{Guid.NewGuid()}{fileExt}";
                filePath = Path.Combine(uploadDir, fileName);
            }
            while (System.IO.File.Exists(filePath));

            using var saveStream = System.IO.File.Create(filePath);
            await dto.File.CopyToAsync(saveStream);

            var relativeUrl = $"/uploads/blog-images/{fileName}";

            return Ok(new { url = relativeUrl });
        }

        #endregion

    }
}
