using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Npgsql;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Services;

namespace CustomCharInfo.server.Controllers
{
    // Admin tooling for the Repo Releases page: read-only hash matching and batch registration.
    public partial class PluginController
    {
        private const int MatchHashesMax = 200;
        private const int BatchRegisterMax = 100;

        // Says, for each hash, whether it is a registered version, an unregistered hash users have
        // already looked up, or something nobody has seen. Unlike identify this never bumps check
        // counts or writes unknown rows, and it reports pending versions so admins see the full picture.
        [HttpPost("match-hashes")]
        [Authorize]
        public async Task<ActionResult<List<MatchHashResultDto>>> MatchHashes(MatchHashesRequestDto dto)
        {
            var (_, isAdmin) = await GetRequesterAsync();
            if (!isAdmin) return Forbid();

            if (dto.Hashes == null || dto.Hashes.Count == 0)
                return BadRequest("At least one hash is required.");
            if (dto.Hashes.Count > MatchHashesMax)
                return BadRequest($"At most {MatchHashesMax} hashes per request.");

            var hashes = new List<string>();
            foreach (var raw in dto.Hashes)
            {
                var hash = raw?.Trim().ToLowerInvariant();
                if (hash == null || !HashPattern.IsMatch(hash))
                    return BadRequest("Every hash must be a 64-character hex SHA-256 digest.");
                if (!hashes.Contains(hash)) hashes.Add(hash);
            }

            var versions = await _context.PluginVersions
                .Include(v => v.Plugin).ThenInclude(p => p.Moveset)
                .Include(v => v.Plugin).ThenInclude(p => p.Dependency)
                .Where(v => hashes.Contains(v.Hash))
                .ToListAsync();
            var unknown = await _context.UnknownPluginHashes
                .Where(u => hashes.Contains(u.Hash))
                .ToListAsync();

            var results = new List<MatchHashResultDto>();
            foreach (var hash in hashes)
            {
                var version = versions.FirstOrDefault(v => v.Hash == hash);
                if (version != null)
                {
                    var plugin = version.Plugin;
                    results.Add(new MatchHashResultDto
                    {
                        Hash = hash,
                        Status = MatchHashStatus.Registered,
                        PluginId = plugin.PluginId,
                        PluginName = plugin.Name,
                        AttachmentType = plugin.MovesetId != null ? "Moveset" : plugin.DependencyId != null ? "Dependency" : "Other",
                        MovesetId = plugin.MovesetId,
                        MovesetName = plugin.Moveset?.ModdedCharName,
                        DependencyId = plugin.DependencyId,
                        DependencyName = plugin.Dependency?.Name,
                        VersionLabel = version.VersionLabel,
                        IsCurrent = version.IsCurrent,
                        AcceptanceStateId = plugin.MovesetId != null ? null : await LatestActionLogStateAsync(ItemTypes.Plugin, version.PluginVersionId)
                    });
                    continue;
                }

                var seen = unknown.FirstOrDefault(u => u.Hash == hash);
                results.Add(seen != null
                    ? new MatchHashResultDto
                    {
                        Hash = hash,
                        Status = MatchHashStatus.Unregistered,
                        CheckCount = seen.CheckCount,
                        FirstCheckedAt = seen.FirstCheckedAt,
                        LastCheckedAt = seen.LastCheckedAt
                    }
                    : new MatchHashResultDto { Hash = hash, Status = MatchHashStatus.Unseen });
            }

            return Ok(results);
        }

        // Downloads one release asset and returns its SHA-256, for assets GitHub has no digest for.
        // The browser cannot do this itself because GitHub's asset host sends no CORS headers.
        [HttpGet("hash-asset")]
        [Authorize]
        public async Task<ActionResult<AssetHashDto>> HashAsset([FromQuery] string url, CancellationToken cancellationToken)
        {
            var (_, isAdmin) = await GetRequesterAsync();
            if (!isAdmin) return Forbid();

            if (!GitHubRepoRef.TryParseReleaseAssetUrl(url, out var uri))
                return BadRequest("Only github.com release asset download links can be hashed.");

            try
            {
                var result = await _assetHasher.HashAsync(uri, cancellationToken);
                return Ok(new AssetHashDto { Hash = result.Hash, Size = result.Size });
            }
            catch (AssetTooLargeException ex)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, $"GitHub download failed: {ex.Message}");
            }
        }

        // Registers several versions under one plugin identity in one transaction.
        // Any bad row rejects the whole batch so a half-registered repo never happens.
        [HttpPost("batch")]
        [Authorize]
        public async Task<ActionResult<PluginDto>> BatchRegister(BatchRegisterPluginsDto dto)
        {
            var (user, isAdmin) = await GetRequesterAsync();
            if (user == null || !isAdmin) return Forbid();

            if ((dto.PluginId == null) == (dto.NewPlugin == null))
                return BadRequest("Specify exactly one of pluginId or newPlugin.");
            if (dto.Versions == null || dto.Versions.Count == 0)
                return BadRequest("At least one version is required.");
            if (dto.Versions.Count > BatchRegisterMax)
                return BadRequest($"At most {BatchRegisterMax} versions per batch.");

            var seen = new HashSet<string>();
            var problems = new List<string>();
            for (int i = 0; i < dto.Versions.Count; i++)
            {
                var row = dto.Versions[i];
                var hash = row.Hash?.Trim().ToLowerInvariant();
                if (hash == null || !HashPattern.IsMatch(hash))
                    problems.Add($"Row {i + 1}: hash must be a 64-character hex SHA-256 digest.");
                else if (!seen.Add(hash))
                    problems.Add($"Row {i + 1}: hash appears more than once in this batch.");
                else
                    row.Hash = hash;

                row.VersionLabel = NormalizeVersionLabel(row.VersionLabel ?? "");
                if (string.IsNullOrWhiteSpace(row.VersionLabel))
                    problems.Add($"Row {i + 1}: version label is required.");
            }
            if (problems.Count > 0) return BadRequest(string.Join(" ", problems));

            var hashes = dto.Versions.Select(v => v.Hash).ToList();
            var alreadyRegistered = await _context.PluginVersions
                .Where(v => hashes.Contains(v.Hash))
                .Select(v => v.Hash)
                .ToListAsync();
            if (alreadyRegistered.Count > 0)
                return Conflict("Already registered: " + string.Join(", ", alreadyRegistered));

            Plugin plugin;
            if (dto.PluginId != null)
            {
                var existing = await _context.Plugins
                    .Include(p => p.PluginVersions)
                    .FirstOrDefaultAsync(p => p.PluginId == dto.PluginId);
                if (existing == null) return NotFound("Plugin not found.");
                if (existing.MovesetId != null)
                    return BadRequest("Moveset plugins don't support batch registration.");
                plugin = existing;
            }
            else
            {
                if (user.ModderId == null)
                    return BadRequest("Your account needs a modder profile to own a new plugin.");

                var newPlugin = dto.NewPlugin!;
                if (newPlugin.DependencyId != null)
                {
                    var dependency = await _context.Dependencies.FindAsync(newPlugin.DependencyId);
                    if (dependency == null) return NotFound("Dependency not found.");
                }

                var isOther = newPlugin.DependencyId == null;
                plugin = new Plugin
                {
                    Name = newPlugin.Name,
                    Description = isOther ? newPlugin.Description : null,
                    DefaultLearnMoreUrl = newPlugin.DefaultLearnMoreUrl,
                    DependencyId = newPlugin.DependencyId,
                    OwnerModderId = user.ModderId.Value,
                    CreatedAt = DateTime.UtcNow,
                    PluginVersions = new List<PluginVersion>()
                };
                _context.Plugins.Add(plugin);
            }

            var now = DateTime.UtcNow;
            var created = new List<PluginVersion>();
            foreach (var row in dto.Versions)
            {
                var version = new PluginVersion
                {
                    VersionLabel = row.VersionLabel,
                    Hash = row.Hash,
                    LearnMoreUrl = row.LearnMoreUrl,
                    SubmittedByUserId = user.Id,
                    CreatedAt = now
                };
                plugin.PluginVersions.Add(version);
                await AbsorbUnknownHashAsync(version);
                created.Add(version);
            }
            RecomputeIsCurrent(plugin);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict("A plugin version with one of these hashes already exists.");
            }

            foreach (var version in created)
                LogPluginVersionAction(user.Id, version.PluginVersionId, isAdmin, dto.Notes ?? "");
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = await _context.Plugins
                .Include(p => p.PluginVersions)
                .Include(p => p.Moveset)
                .Include(p => p.Dependency)
                .FirstAsync(p => p.PluginId == plugin.PluginId);
            return CreatedAtAction(nameof(GetPlugin), new { id = plugin.PluginId }, await ToDtoAsync(result));
        }
    }
}
