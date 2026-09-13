using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using System.Text.RegularExpressions;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/plugins")]
    public class PluginController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Same set used in MovesetController to decide whether an item's latest ActionLog state means "not publicly visible yet".
        private static readonly int[] BlockedStates = { 2, 4, 6 };

        private static readonly Regex HashPattern = new("^[0-9a-f]{64}$", RegexOptions.Compiled);

        // Matches a run of 2+ dot-separated numeric groups anywhere in a string.
        // "3.0.2" in "3.0.2 (standalone)", or just the "4.0.10" lead-in of "4.0.10-beta.0.1".
        private static readonly Regex VersionNumberPattern = new(@"\d+(?:\.\d+)+", RegexOptions.Compiled);
        // Same pattern, anchored - used only to decide whether a label gets a "v" prefix when displayed.
        private static readonly Regex VersionNumberAtStartPattern = new(@"^\d+(?:\.\d+)+", RegexOptions.Compiled);

        public PluginController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(ApplicationUser? user, bool isAdmin)> GetRequesterAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return (null, false);
            var user = await _context.Users.FindAsync(userId);
            return (user, user?.UserTypeId == 3);
        }

        // Version labels are free text ("Beta 2.0", "3.0.2 (standalone)", "4.0.10-beta.0.1" are all valid).
        // This finds a numeric-dotted run anywhere in the label to compare on,
        // missing trailing segments treated as 0 (e.g. "1.2" == "1.2.0").
        // Returns null if none is found.
        private static int[]? ParseVersion(string label)
        {
            var match = VersionNumberPattern.Match(label);
            if (!match.Success) return null;

            var parts = match.Value.Split('.');
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var n) || n < 0) return null;
                result[i] = n;
            }
            return result;
        }

        // Strip "v" or "version" so the stored/displayed value is just "1.2.3"/"2.0".
        private static string NormalizeVersionLabel(string label)
        {
            var result = label.TrimStart();
            if (result.StartsWith("version", StringComparison.OrdinalIgnoreCase))
                result = result.Substring("version".Length);
            else if (result.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                result = result.Substring(1);
            return result.Trim();
        }

        private static int CompareVersions(int[] a, int[] b)
        {
            var len = Math.Max(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                var av = i < a.Length ? a[i] : 0;
                var bv = i < b.Length ? b[i] : 0;
                if (av != bv) return av.CompareTo(bv);
            }
            return 0;
        }

        // Resolves every version in the given set that counts as "current": all of them whose own
        // version number matches the highest one found in the set, so tied labels like
        // "2.0 (standalone)" and "2.0 (bundled)" are current together, not just one arbitrarily.
        // If nothing in the set has a parseable version number, falls back to the single
        // most-recently-submitted version. Used both to recompute the cached IsCurrent flag and
        // by the public identify endpoint (over only publicly-visible versions).
        private static List<PluginVersion> ResolveCurrentSet(IEnumerable<PluginVersion> versions)
        {
            var list = versions as IList<PluginVersion> ?? versions.ToList();

            int[]? max = null;
            foreach (var v in list)
            {
                var parsed = ParseVersion(v.VersionLabel);
                if (parsed != null && (max == null || CompareVersions(parsed, max) > 0)) max = parsed;
            }

            if (max == null)
            {
                var mostRecent = list.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                return mostRecent != null ? new List<PluginVersion> { mostRecent } : new List<PluginVersion>();
            }

            return list.Where(v =>
            {
                var parsed = ParseVersion(v.VersionLabel);
                return parsed != null && CompareVersions(parsed, max) == 0;
            }).ToList();
        }

        private void RecomputeIsCurrent(Plugin plugin)
        {
            var currentSet = ResolveCurrentSet(plugin.PluginVersions);
            foreach (var v in plugin.PluginVersions)
                v.IsCurrent = currentSet.Contains(v);
        }

        // Moveset plugins don't nest multiple versions under one Plugin - a moveset just has
        // several flat Plugin rows, each one specific version, so "current" has to be resolved
        // across all of a moveset's Plugins rather than within a single Plugin's own versions.
        private async Task RecomputeCurrentForMovesetAsync(int movesetId)
        {
            var siblingPlugins = await _context.Plugins
                .Include(p => p.PluginVersions)
                .Where(p => p.MovesetId == movesetId)
                .ToListAsync();

            var allVersions = siblingPlugins.SelectMany(p => p.PluginVersions).ToList();
            var currentSet = ResolveCurrentSet(allVersions);
            foreach (var v in allVersions)
                v.IsCurrent = currentSet.Contains(v);
        }

        private bool IsMovesetModder(Moveset moveset, int? modderId) =>
            modderId != null && moveset.MovesetModders.Any(mm => mm.ModderId == modderId);

        private async Task<int?> LatestActionLogStateAsync(int itemTypeId, int itemId)
        {
            return await _context.ActionLogs
                .Where(a => a.ItemTypeId == itemTypeId && a.ItemId == itemId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => (int?)a.AcceptanceStateId)
                .FirstOrDefaultAsync();
        }

        private void LogPluginVersionAction(string userId, int pluginVersionId, bool isAdmin, string notes)
        {
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = 5,
                ItemId = pluginVersionId,
                AcceptanceStateId = isAdmin ? 7 : 2,
                Notes = notes ?? "",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Public listing for a moveset's own (case-1) plugins - shown on the moveset's edit/detail pages.
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<PluginDto>>> GetPlugins([FromQuery] int? movesetId)
        {
            if (movesetId == null) return BadRequest("movesetId is required.");

            var plugins = await _context.Plugins
                .Include(p => p.PluginVersions)
                .Include(p => p.Moveset)
                .Include(p => p.Dependency)
                .Where(p => p.MovesetId == movesetId)
                .ToListAsync();

            var result = new List<PluginDto>();
            foreach (var p in plugins)
                result.Add(await ToDtoAsync(p));

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PluginDto>> GetPlugin(int id)
        {
            var plugin = await _context.Plugins
                .Include(p => p.PluginVersions)
                .Include(p => p.Moveset)
                .Include(p => p.Dependency)
                .FirstOrDefaultAsync(p => p.PluginId == id);

            if (plugin == null) return NotFound();

            return Ok(await ToDtoAsync(plugin));
        }

        private async Task<PluginDto> ToDtoAsync(Plugin plugin)
        {
            var dto = new PluginDto
            {
                PluginId = plugin.PluginId,
                Name = plugin.Name,
                Description = plugin.Description,
                DefaultLearnMoreUrl = plugin.DefaultLearnMoreUrl,
                MovesetId = plugin.MovesetId,
                MovesetName = plugin.Moveset?.ModdedCharName,
                DependencyId = plugin.DependencyId,
                DependencyName = plugin.Dependency?.Name,
                OwnerModderId = plugin.OwnerModderId
            };

            var isCase1 = plugin.MovesetId != null;

            foreach (var v in plugin.PluginVersions.OrderByDescending(v => v.CreatedAt))
            {
                var versionDto = new PluginVersionDto
                {
                    PluginVersionId = v.PluginVersionId,
                    VersionLabel = v.VersionLabel,
                    Hash = v.Hash,
                    LearnMoreUrl = v.LearnMoreUrl,
                    IsCurrent = v.IsCurrent,
                    CreatedAt = v.CreatedAt
                };

                if (!isCase1)
                {
                    var log = await _context.ActionLogs
                        .Include(a => a.AcceptanceState)
                        .Where(a => a.ItemTypeId == 5 && a.ItemId == v.PluginVersionId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();
                    versionDto.AcceptanceStateId = log?.AcceptanceStateId;
                    versionDto.AcceptanceStateName = log?.AcceptanceState?.AcceptanceStateName;
                }

                dto.Versions.Add(versionDto);
            }

            return dto;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PluginDto>> CreatePlugin(CreatePluginDto dto)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null || user.ModderId == null) return Forbid();

            if (dto.MovesetId != null && dto.DependencyId != null)
                return BadRequest("A plugin cannot be attached to both a moveset and a dependency.");

            var hash = dto.Hash?.Trim().ToLowerInvariant();
            if (hash == null || !HashPattern.IsMatch(hash))
                return BadRequest("Hash must be a 64-character hex SHA-256 digest.");

            dto.VersionLabel = NormalizeVersionLabel(dto.VersionLabel);
            if (string.IsNullOrWhiteSpace(dto.VersionLabel))
                return BadRequest("VersionLabel is required.");

            Moveset? moveset = null;
            if (dto.MovesetId != null)
            {
                moveset = await _context.Movesets.Include(m => m.MovesetModders).FirstOrDefaultAsync(m => m.MovesetId == dto.MovesetId);
                if (moveset == null) return NotFound("Moveset not found.");
                if (!isAdmin && !IsMovesetModder(moveset, user.ModderId))
                    return Forbid();
            }
            else if (dto.DependencyId != null)
            {
                var dependency = await _context.Dependencies.FindAsync(dto.DependencyId);
                if (dependency == null) return NotFound("Dependency not found.");
                // Any modder may attach a plugin identity to an existing Dependency; gating happens on the version below.
            }

            if (await _context.PluginVersions.AnyAsync(v => v.Hash == hash))
                return Conflict("A plugin version with this hash already exists.");

            // Description only applies to standalone ("other") plugins - a moveset/dependency
            // plugin's identity is already fully described by what it's attached to.
            var isOther = dto.MovesetId == null && dto.DependencyId == null;

            var plugin = new Plugin
            {
                Name = dto.Name,
                Description = isOther ? dto.Description : null,
                DefaultLearnMoreUrl = dto.DefaultLearnMoreUrl,
                MovesetId = dto.MovesetId,
                DependencyId = dto.DependencyId,
                OwnerModderId = user.ModderId.Value,
                CreatedAt = DateTime.UtcNow,
                PluginVersions = new List<PluginVersion>()
            };

            var version = new PluginVersion
            {
                VersionLabel = dto.VersionLabel,
                Hash = hash,
                LearnMoreUrl = dto.LearnMoreUrl,
                SubmittedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsCurrent = true
            };
            plugin.PluginVersions.Add(version);

            _context.Plugins.Add(plugin);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict("A plugin version with this hash already exists.");
            }

            if (dto.MovesetId != null)
            {
                // A moveset's plugins are flat, one-version-each entries competing for "current"
                // against each other, not against a nested versions list of their own.
                await RecomputeCurrentForMovesetAsync(dto.MovesetId.Value);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Case 1 (moveset-attached) is free metadata, never logged. Cases 2/3 need review.
                LogPluginVersionAction(user.Id, version.PluginVersionId, isAdmin, dto.Notes);
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetPlugin), new { id = plugin.PluginId }, await ToDtoAsync(plugin));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePlugin(int id, UpdatePluginDto dto)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null) return Forbid();

            var plugin = await _context.Plugins.Include(p => p.Moveset).ThenInclude(m => m.MovesetModders).FirstOrDefaultAsync(p => p.PluginId == id);
            if (plugin == null) return NotFound();

            bool canEdit = isAdmin ||
                (plugin.MovesetId != null && IsMovesetModder(plugin.Moveset!, user.ModderId)) ||
                (plugin.MovesetId == null && plugin.OwnerModderId == user.ModderId);

            if (!canEdit) return Forbid();

            var isOther = plugin.MovesetId == null && plugin.DependencyId == null;

            if (dto.Name != null) plugin.Name = dto.Name;
            if (dto.Description != null && isOther) plugin.Description = dto.Description;
            if (dto.DefaultLearnMoreUrl != null) plugin.DefaultLearnMoreUrl = dto.DefaultLearnMoreUrl;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePlugin(int id)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null) return Forbid();

            var plugin = await _context.Plugins.Include(p => p.Moveset).ThenInclude(m => m.MovesetModders).FirstOrDefaultAsync(p => p.PluginId == id);
            if (plugin == null) return NotFound();

            bool canDelete = isAdmin ||
                (plugin.MovesetId != null && IsMovesetModder(plugin.Moveset!, user.ModderId)) ||
                (plugin.MovesetId == null && plugin.OwnerModderId == user.ModderId);

            if (!canDelete) return Forbid();

            var movesetId = plugin.MovesetId;
            _context.Plugins.Remove(plugin);
            await _context.SaveChangesAsync();

            if (movesetId != null)
            {
                await RecomputeCurrentForMovesetAsync(movesetId.Value);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPost("{id}/versions")]
        [Authorize]
        public async Task<ActionResult<PluginVersionDto>> AddPluginVersion(int id, CreatePluginVersionDto dto)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null || user.ModderId == null) return Forbid();

            var plugin = await _context.Plugins
                .Include(p => p.Moveset).ThenInclude(m => m.MovesetModders)
                .Include(p => p.PluginVersions)
                .FirstOrDefaultAsync(p => p.PluginId == id);
            if (plugin == null) return NotFound();

            if (plugin.MovesetId != null)
                return BadRequest("Moveset plugins don't support adding a version - submit a new plugin entry instead.");

            // Any modder may add a version to a dependency/other plugin; gating happens via review below.
            var hash = dto.Hash?.Trim().ToLowerInvariant();
            if (hash == null || !HashPattern.IsMatch(hash))
                return BadRequest("Hash must be a 64-character hex SHA-256 digest.");

            dto.VersionLabel = NormalizeVersionLabel(dto.VersionLabel);
            if (string.IsNullOrWhiteSpace(dto.VersionLabel))
                return BadRequest("VersionLabel is required.");

            var version = new PluginVersion
            {
                PluginId = plugin.PluginId,
                VersionLabel = dto.VersionLabel,
                Hash = hash,
                LearnMoreUrl = dto.LearnMoreUrl,
                SubmittedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            plugin.PluginVersions.Add(version);
            RecomputeIsCurrent(plugin);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict("A plugin version with this hash already exists.");
            }

            LogPluginVersionAction(user.Id, version.PluginVersionId, isAdmin, dto.Notes);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlugin), new { id = plugin.PluginId }, new PluginVersionDto
            {
                PluginVersionId = version.PluginVersionId,
                VersionLabel = version.VersionLabel,
                Hash = version.Hash,
                LearnMoreUrl = version.LearnMoreUrl,
                IsCurrent = version.IsCurrent,
                CreatedAt = version.CreatedAt
            });
        }

        [HttpPut("{id}/versions/{versionId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePluginVersion(int id, int versionId, UpdatePluginVersionDto dto)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null) return Forbid();

            var plugin = await _context.Plugins
                .Include(p => p.Moveset).ThenInclude(m => m.MovesetModders)
                .Include(p => p.PluginVersions)
                .FirstOrDefaultAsync(p => p.PluginId == id);
            if (plugin == null) return NotFound();

            var version = plugin.PluginVersions.FirstOrDefault(v => v.PluginVersionId == versionId);
            if (version == null) return NotFound();

            if (plugin.MovesetId != null)
                return BadRequest("Moveset plugins don't support editing a version - delete and resubmit instead.");

            // Admin-only - see plan doc's "re-review on relabel" rule for why regular modders
            // can't touch an existing version's claims once submitted.
            if (!isAdmin) return Forbid();

            if (dto.VersionLabel != null)
            {
                var normalized = NormalizeVersionLabel(dto.VersionLabel);
                if (string.IsNullOrWhiteSpace(normalized))
                    return BadRequest("VersionLabel is required.");
                version.VersionLabel = normalized;
            }
            if (dto.LearnMoreUrl != null) version.LearnMoreUrl = dto.LearnMoreUrl;

            RecomputeIsCurrent(plugin);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}/versions/{versionId}")]
        [Authorize]
        public async Task<IActionResult> DeletePluginVersion(int id, int versionId)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null) return Forbid();

            var plugin = await _context.Plugins
                .Include(p => p.Moveset).ThenInclude(m => m.MovesetModders)
                .Include(p => p.PluginVersions)
                .FirstOrDefaultAsync(p => p.PluginId == id);
            if (plugin == null) return NotFound();

            var version = plugin.PluginVersions.FirstOrDefault(v => v.PluginVersionId == versionId);
            if (version == null) return NotFound();

            if (plugin.MovesetId != null)
                return BadRequest("Moveset plugins don't support deleting a single version - delete the whole plugin entry instead.");

            if (!isAdmin) return Forbid();

            plugin.PluginVersions.Remove(version);
            _context.PluginVersions.Remove(version);
            RecomputeIsCurrent(plugin);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Public, unauthenticated. Never reveals pending/rejected entries, even to the submitter -
        // status on those is checked via the My Plugins page instead.
        [HttpGet("identify")]
        [AllowAnonymous]
        public async Task<ActionResult<IdentifyPluginResultDto>> Identify([FromQuery] string hash)
        {
            var normalized = hash?.Trim().ToLowerInvariant();
            if (normalized == null || !HashPattern.IsMatch(normalized))
                return BadRequest("Hash must be a 64-character hex SHA-256 digest.");

            var version = await _context.PluginVersions
                .Include(v => v.Plugin).ThenInclude(p => p.Moveset)
                .Include(v => v.Plugin).ThenInclude(p => p.Dependency)
                .Include(v => v.Plugin).ThenInclude(p => p.PluginVersions)
                .FirstOrDefaultAsync(v => v.Hash == normalized);

            if (version == null) return NotFound();

            var plugin = version.Plugin;
            bool isCase1 = plugin.MovesetId != null;

            if (isCase1)
            {
                var movesetState = await LatestActionLogStateAsync(1, plugin.MovesetId!.Value);
                if ((movesetState != null && BlockedStates.Contains(movesetState.Value)) || plugin.Moveset!.PrivateMoveset == true)
                    return NotFound();
            }
            else
            {
                var versionState = await LatestActionLogStateAsync(5, version.PluginVersionId);
                if (versionState == null || BlockedStates.Contains(versionState.Value))
                    return NotFound();
            }

            // For cases 2/3, "current" must only be resolved among publicly-visible versions,
            // so a still-pending newer version can't be revealed via the outdated-check.
            IEnumerable<PluginVersion> visibleVersions = plugin.PluginVersions;
            if (!isCase1)
            {
                var visibleIds = new List<int>();
                foreach (var v in plugin.PluginVersions)
                {
                    var state = await LatestActionLogStateAsync(5, v.PluginVersionId);
                    if (state != null && !BlockedStates.Contains(state.Value)) visibleIds.Add(v.PluginVersionId);
                }
                visibleVersions = plugin.PluginVersions.Where(v => visibleIds.Contains(v.PluginVersionId));
            }

            var currentSet = ResolveCurrentSet(visibleVersions);

            return Ok(new IdentifyPluginResultDto
            {
                AttachmentType = plugin.MovesetId != null ? "Moveset" : plugin.DependencyId != null ? "Dependency" : "Other",
                PluginName = plugin.Name,
                PluginDescription = plugin.Description,
                MovesetId = plugin.MovesetId,
                MovesetName = plugin.Moveset?.ModdedCharName,
                DependencyId = plugin.DependencyId,
                DependencyName = plugin.Dependency?.Name,
                MatchedVersionLabel = version.VersionLabel,
                IsCurrent = currentSet.Contains(version),
                CurrentVersionLabel = currentSet.FirstOrDefault()?.VersionLabel,
                LearnMoreUrl = version.LearnMoreUrl ?? plugin.DefaultLearnMoreUrl
            });
        }

        // Lists every plugin the requester can see the status of: case-1 plugins on movesets
        // they modder, plus case-2/3 plugins they own.
        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<List<PluginDto>>> GetMyPlugins()
        {
            var (user, _) = await GetRequesterAsync();
            if (user == null || user.ModderId == null) return Forbid();

            var modderMovesetIds = await _context.MovesetModders
                .Where(mm => mm.ModderId == user.ModderId)
                .Select(mm => mm.MovesetId)
                .ToListAsync();

            var plugins = await _context.Plugins
                .Include(p => p.PluginVersions)
                .Include(p => p.Moveset)
                .Include(p => p.Dependency)
                .Where(p => (p.MovesetId != null && modderMovesetIds.Contains(p.MovesetId.Value)) || p.OwnerModderId == user.ModderId)
                .ToListAsync();

            var result = new List<PluginDto>();
            foreach (var p in plugins)
                result.Add(await ToDtoAsync(p));

            return Ok(result);
        }

        // Lets the submission form offer "add a version to an existing plugin" instead of always
        // creating a new one - e.g. someone uploading v1.1 of an overhaul mod that's already
        // registered can attach it to that same plugin rather than creating a duplicate identity.
        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<List<PluginSearchResultDto>>> SearchPlugins([FromQuery] int? dependencyId, [FromQuery] bool standalone = false)
        {
            if (dependencyId == null && !standalone)
                return BadRequest("Specify dependencyId or standalone=true.");

            var query = dependencyId != null
                ? _context.Plugins.Where(p => p.DependencyId == dependencyId)
                : _context.Plugins.Where(p => p.MovesetId == null && p.DependencyId == null);

            var plugins = await query
                .Select(p => new PluginSearchResultDto
                {
                    PluginId = p.PluginId,
                    Name = p.Name,
                    Description = p.Description,
                    DefaultLearnMoreUrl = p.DefaultLearnMoreUrl
                })
                .ToListAsync();

            return Ok(plugins);
        }
    }
}
