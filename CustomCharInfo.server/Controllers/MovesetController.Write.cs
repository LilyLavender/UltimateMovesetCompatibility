// Create, update, delete, and per-user actions on movesets.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Npgsql;

namespace CustomCharInfo.server.Controllers
{
    public partial class MovesetController
    {
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Moveset>> PostMoveset(CreateMovesetDto dto)
        {
            var userFromId = await _userManager.GetRequesterAsync(_context, User);
            if (!userFromId.IsModder())
                return Forbid();

            if (dto.ModderIds == null || !dto.ModderIds.Any())
                return BadRequest("At least one ModderId is required.");

            if (string.IsNullOrWhiteSpace(dto.SlottedId) || dto.SlottedId.Any(char.IsDigit))
                return BadRequest("SlottedId is required and cannot contain digits.");

            var normalizedSlottedId = dto.SlottedId.Trim();
            var slottedIdTaken = await _context.Movesets
                .AnyAsync(m => m.SlottedId.ToLower() == normalizedSlottedId.ToLower());
            if (slottedIdTaken)
                return Conflict("A moveset with this SlottedId already exists.");

            var moveset = new Moveset
            {
                MovesetModders = dto.ModderIds.Select(id => new MovesetModder { ModderId = id }).ToList(),
                MovesetEditors = BuildEditors(dto),
                MovesetDependencies = dto.DependencyIds?.Select(id => new MovesetDependency { DependencyId = id }).ToList() ?? new List<MovesetDependency>(),
                MovesetHooks = dto.Hooks?.Select((h, i) => new MovesetHook
                {
                    HookId = h.HookId,
                    Description = h.Description,
                    SortOrder = i
                }).ToList() ?? new List<MovesetHook>(),
                MovesetArticles = dto.Articles?.Select((a, i) => new MovesetArticle
                {
                    ArticleId = a.ArticleId,
                    ModdedName = a.ModdedName,
                    Description = a.Description,
                    SortOrder = i
                }).ToList() ?? new List<MovesetArticle>()
            };

            MovesetMapper.ApplyScalars(moveset, dto);

            _context.Movesets.Add(moveset);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The AnyAsync check above is check-then-act; this catches a genuine race against
                // the unique index on SlottedId as a backstop.
                return Conflict("A moveset with this SlottedId already exists.");
            }

            // Log action
            int newState = userFromId.IsAdmin() ? AcceptanceStates.AutoAccepted : AcceptanceStates.PendingAdminHard;
            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userFromId.Id,
                ItemTypeId = ItemTypes.Moveset,
                ItemId = moveset.MovesetId,
                AcceptanceStateId = newState,
                Notes = dto.Notes ?? "",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMoveset), new { idOrSlottedId = moveset.MovesetId }, moveset);
        }

        // Attaches an image uploaded just after a create, without writing an ActionLog entry or affecting review state.
        // Completes the create->upload->attach sequence started by PostMoveset.
        // Only fills the field if it's still empty,
        // so it can't be reused to swap an existing image without going through the normal reviewed edit path.
        [Authorize]
        [HttpPatch("{id}/images")]
        public async Task<IActionResult> PatchMovesetImages(int id, [FromBody] MovesetImagesDto dto)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (user == null)
                return Forbid();

            var moveset = await _context.Movesets
                .Include(m => m.MovesetModders)
                .Include(m => m.MovesetEditors)
                .FirstOrDefaultAsync(m => m.MovesetId == id);
            if (moveset == null)
                return NotFound();

            if (!MovesetAccess.CanEdit(moveset, user.ModderId))
                return Forbid();

            if (dto.ThumbhImageUrl != null)
            {
                if (!string.IsNullOrEmpty(moveset.ThumbhImageUrl))
                    return Conflict("ThumbhImageUrl is already set; use the full update endpoint to change it.");
                moveset.ThumbhImageUrl = dto.ThumbhImageUrl;
            }

            if (dto.MovesetHeroImageUrl != null)
            {
                if (!string.IsNullOrEmpty(moveset.MovesetHeroImageUrl))
                    return Conflict("MovesetHeroImageUrl is already set; use the full update endpoint to change it.");
                moveset.MovesetHeroImageUrl = dto.MovesetHeroImageUrl;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpPost("set-admin-picks")]
        public async Task<IActionResult> SetAdminPicks([FromBody] List<int> adminPickIds)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            adminPickIds ??= new List<int>();

            // Fetch all movesets
            var movesets = await _context.Movesets.ToListAsync();
            foreach (var moveset in movesets)
            {
                moveset.AdminPick = adminPickIds.Contains(moveset.MovesetId);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Admin picks updated successfully",
                adminPickIds
            });
        }

        [Authorize]
        [HttpPut("{id}")]
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
                .Include(m => m.MovesetEditors)
                .Include(m => m.MovesetDependencies)
                .Include(m => m.MovesetHooks)
                .Include(m => m.MovesetArticles)
                .FirstOrDefaultAsync(m => m.MovesetId == id);

            if (moveset == null)
                return NotFound();

            // Credited modders and editors may edit; only credited modders and full-access editors may change who is on it.
            if (!MovesetAccess.CanEdit(moveset, user.ModderId))
                return Forbid();

            var newEditors = BuildEditors(dto);
            if (!MovesetAccess.CanManageMembers(moveset, user.ModderId))
            {
                bool moddersChanged = !moveset.MovesetModders.Select(mm => mm.ModderId).ToHashSet()
                    .SetEquals(dto.ModderIds ?? new List<int>());
                bool editorsChanged = !moveset.MovesetEditors.Select(me => (me.ModderId, me.FullAccess)).ToHashSet()
                    .SetEquals(newEditors.Select(me => (me.ModderId, me.FullAccess)));
                if (moddersChanged || editorsChanged)
                    return Forbid();
            }

            var latestLog = await _context.ActionLogs
                .Where(a => a.ItemTypeId == ItemTypes.Moveset && a.ItemId == id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (dto.ModderIds == null || !dto.ModderIds.Any())
            {
                return BadRequest("At least one ModderId is required.");
            }

            if (dto.DependencyIds == null || dto.Hooks == null || dto.Articles == null)
            {
                return BadRequest("DependencyIds, Hooks, and Articles must be present (even if empty).");
            }

            if (string.IsNullOrWhiteSpace(dto.SlottedId) || dto.SlottedId.Any(char.IsDigit))
                return BadRequest("SlottedId is required and cannot contain digits.");

            var normalizedSlottedId = dto.SlottedId.Trim();
            var slottedIdTaken = await _context.Movesets
                .AnyAsync(m => m.MovesetId != id && m.SlottedId.ToLower() == normalizedSlottedId.ToLower());
            if (slottedIdTaken)
                return Conflict("A moveset with this SlottedId already exists.");

            bool keyDetailsChanged =
                moveset.ModdedCharName != dto.ModdedCharName ||
                moveset.SlottedId != dto.SlottedId ||
                moveset.ReplacementId != dto.ReplacementId;

            var diff = await MovesetDiffBuilder.BuildAsync(_context, moveset, dto);

            MovesetMapper.ApplyScalars(moveset, dto);

            // Sync (i know that guy!!) Modders
            _context.MovesetModders.RemoveRange(moveset.MovesetModders);
            moveset.MovesetModders = dto.ModderIds
            .Select((mid, index) => new MovesetModder 
            { 
                MovesetId = id, 
                ModderId = mid, 
                SortOrder = index 
            })
            .ToList();

            // Sync Editors
            _context.MovesetEditors.RemoveRange(moveset.MovesetEditors);
            foreach (var editor in newEditors) editor.MovesetId = id;
            moveset.MovesetEditors = newEditors;

            // Sync Dependencies
            _context.MovesetDependencies.RemoveRange(moveset.MovesetDependencies);
            moveset.MovesetDependencies = dto.DependencyIds
                .Select(did => new MovesetDependency { MovesetId = id, DependencyId = did })
                .ToList();

            // Sync Hooks
            _context.MovesetHooks.RemoveRange(moveset.MovesetHooks);
            moveset.MovesetHooks = dto.Hooks
                .Select((h, i) => new MovesetHook
                {
                    MovesetId = id,
                    HookId = h.HookId,
                    Description = h.Description,
                    SortOrder = i
                })
                .ToList();

            // Sync Articles
            _context.MovesetArticles.RemoveRange(moveset.MovesetArticles);
            moveset.MovesetArticles = dto.Articles
                .Select((a, i) => new MovesetArticle
                {
                    MovesetId = id,
                    ArticleId = a.ArticleId,
                    ModdedName = a.ModdedName,
                    Description = a.Description,
                    SortOrder = i
                })
                .ToList();

            // Log action
            // Rejected stays rejected until an admin acts; admins auto-accept; key-field edits and already-hidden items go hard; everything else is soft.
            int newState =
                latestLog?.AcceptanceStateId == AcceptanceStates.Rejected
                    ? AcceptanceStates.Rejected
                    : user?.UserTypeId == UserTypes.Admin
                        ? AcceptanceStates.AutoAccepted
                        : keyDetailsChanged
                            ? AcceptanceStates.PendingAdminHard
                            : latestLog != null && AcceptanceStates.Hard.Contains(latestLog.AcceptanceStateId)
                                ? AcceptanceStates.PendingAdminHard
                                : AcceptanceStates.PendingAdminSoft;


            _context.ActionLogs.Add(new ActionLog
            {
                UserId = userId,
                ItemTypeId = ItemTypes.Moveset,
                ItemId = id,
                AcceptanceStateId = newState,
                Notes = dto.Notes ?? "",
                Diff = diff,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The AnyAsync check above is check-then-act; this catches a genuine race against
                // the unique index on SlottedId as a backstop.
                return Conflict("A moveset with this SlottedId already exists.");
            }

            return NoContent();
        }

        // Editors from the request, minus anyone who is also a credited modder, deduplicated by modder.
        private static List<MovesetEditor> BuildEditors(CreateMovesetDto dto)
        {
            var credited = (dto.ModderIds ?? new List<int>()).ToHashSet();
            return (dto.Editors ?? new List<MovesetEditorDto>())
                .Where(e => !credited.Contains(e.ModderId))
                .GroupBy(e => e.ModderId)
                .Select(g => new MovesetEditor { ModderId = g.Key, FullAccess = g.Any(e => e.FullAccess) })
                .ToList();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMoveset(int id)
        {
            var user = await _userManager.GetRequesterAsync(_context, User);
            if (!user.IsAdmin())
                return Forbid();

            var moveset = await _context.Movesets.FindAsync(id);
            if (moveset == null)
                return NotFound();

            // CompatibilityReports have a Restrict FK to Moveset (AppDbContext.cs),
            // so they'd block this delete with a DbUpdateException unless removed first.
            // Everything else (MovesetLikes/Modders/Hooks/Articles/Dependencies) cascades via EF's default convention,
            // confirmed in CascadeDeleteTests. Plugins (case 1, moveset-attached) cascade too, via an
            // explicit OnDelete(Cascade) in AppDbContext since Plugin.MovesetId is nullable and EF's
            // default for an optional FK is SetNull, not cascade.
            // ActionLogs referencing this moveset are left in place as an audit trail;
            // the frontend already tolerates a missing item there.
            using var transaction = await _context.Database.BeginTransactionAsync();

            var reports = await _context.CompatibilityReports
                .Where(cr => cr.MovesetId1 == id || cr.MovesetId2 == id)
                .ToListAsync();
            _context.CompatibilityReports.RemoveRange(reports);

            _context.Movesets.Remove(moveset);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();

            var moveset = await _context.Movesets.FindAsync(id);
            if (moveset == null) return NotFound();

            var existing = await _context.MovesetLikes
                .FirstOrDefaultAsync(ml => ml.MovesetId == id && ml.UserId == userId);

            if (existing != null)
                _context.MovesetLikes.Remove(existing);
            else
                _context.MovesetLikes.Add(new MovesetLike { MovesetId = id, UserId = userId, CreatedAt = DateTime.UtcNow });

            await _context.SaveChangesAsync();

            var likeCount = await _context.MovesetLikes.CountAsync(ml => ml.MovesetId == id);
            return Ok(new { likeCount, userLiked = existing == null });
        }
    }
}
