using CustomCharInfo.server.Data;
using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomCharInfo.server.Services
{
    // Thrown when a derived or submitted offset already belongs to another hook in the same game version.
    public class HookOffsetConflictException : Exception
    {
        public HookOffsetConflictException(string message) : base(message) { }
    }

    public sealed record OffsetPreviewRow(int HookId, string Description, string OldOffset, string NewOffset, int OffsetStateId);

    public sealed record OffsetApplyResult(int Generated, int CarriedForward, List<OffsetPreviewRow> Rows);

    // The only writer of HookOffset rows.
    // Every write that touches the newest game version also copies the value onto Hook.Offset, so the two never drift.
    // Methods stage changes on the context; the caller saves.
    public class HookOffsetService
    {
        private readonly AppDbContext _context;

        public HookOffsetService(AppDbContext context)
        {
            _context = context;
        }

        public Task<GameVersion?> GetLatestVersionAsync() =>
            _context.GameVersions.OrderByDescending(v => v.SortOrder).FirstOrDefaultAsync();

        // Upserts the row for one hook and version. The offset must already be normalized.
        public async Task<HookOffset> SetOffsetAsync(Hook hook, GameVersion version, string offset, int offsetStateId, string? userId)
        {
            var row = hook.HookId == 0
                ? null
                : await _context.HookOffsets.FindAsync(hook.HookId, version.GameVersionId);

            if (row == null)
            {
                row = new HookOffset { Hook = hook, GameVersion = version };
                _context.HookOffsets.Add(row);
            }

            row.Offset = offset;
            row.OffsetStateId = offsetStateId;
            row.SetByUserId = userId;
            row.UpdatedAt = DateTime.UtcNow;

            var latest = await GetLatestVersionAsync();
            if (latest == null || latest.GameVersionId == version.GameVersionId)
                hook.Offset = offset;

            return row;
        }

        // Walks the stored shift tables from the given version to the newest, writing one row per later version.
        // Throws HookOffsetConflictException when a derived address already belongs to another hook.
        public async Task DeriveForwardAsync(Hook hook, GameVersion from, string fromOffset)
        {
            var later = await _context.GameVersions
                .Where(v => v.SortOrder > from.SortOrder)
                .OrderBy(v => v.SortOrder)
                .ToListAsync();

            var previous = from;
            var address = OffsetFormat.ToLong(fromOffset);
            foreach (var next in later)
            {
                var ranges = await LoadRangesAsync(previous.GameVersionId, next.GameVersionId);
                var (shifted, stateId) = OffsetShifter.Shift(address, ranges);
                var offset = OffsetFormat.FromLong(shifted);

                var takenBy = await _context.HookOffsets
                    .Where(o => o.GameVersionId == next.GameVersionId && o.Offset == offset && o.HookId != hook.HookId)
                    .Select(o => o.Hook.Description)
                    .FirstOrDefaultAsync();
                if (takenBy != null)
                    throw new HookOffsetConflictException($"In {next.Name} this offset becomes 0x{offset}, which already belongs to \"{takenBy}\".");

                await SetOffsetAsync(hook, next, offset, stateId, null);
                previous = next;
                address = shifted;
            }
        }

        // Computes every hook's offset in a new version from its offset in the previous one, without writing.
        public async Task<OffsetApplyResult> PreviewAsync(GameVersion previous, IReadOnlyList<ShiftRange> ranges)
        {
            var rows = await ComputeAsync(previous, ranges);
            return Summarize(rows);
        }

        // Writes the computed rows for a version that has just been added and makes it the current offset everywhere.
        public async Task<OffsetApplyResult> ApplyVersionAsync(GameVersion previous, GameVersion added, IReadOnlyList<ShiftRange> ranges)
        {
            var rows = await ComputeAsync(previous, ranges);
            var hooks = await _context.Hooks
                .Where(h => rows.Select(r => r.HookId).Contains(h.HookId))
                .ToDictionaryAsync(h => h.HookId);

            foreach (var row in rows)
            {
                var hook = hooks[row.HookId];
                _context.HookOffsets.Add(new HookOffset
                {
                    Hook = hook,
                    GameVersion = added,
                    Offset = row.NewOffset,
                    OffsetStateId = row.OffsetStateId,
                    SetByUserId = null,
                    UpdatedAt = DateTime.UtcNow
                });
                hook.Offset = row.NewOffset;
            }

            return Summarize(rows);
        }

        // After the newest version is removed, points Hook.Offset back at the version that is now newest.
        public async Task ResyncCurrentAsync(GameVersion latest)
        {
            var offsets = await _context.HookOffsets
                .Where(o => o.GameVersionId == latest.GameVersionId)
                .ToDictionaryAsync(o => o.HookId, o => o.Offset);

            var hooks = await _context.Hooks.ToListAsync();
            foreach (var hook in hooks)
            {
                if (offsets.TryGetValue(hook.HookId, out var offset))
                    hook.Offset = offset;
            }
        }

        private async Task<List<OffsetPreviewRow>> ComputeAsync(GameVersion previous, IReadOnlyList<ShiftRange> ranges)
        {
            var current = await _context.HookOffsets
                .AsNoTracking()
                .Where(o => o.GameVersionId == previous.GameVersionId)
                .OrderBy(o => o.HookId)
                .Select(o => new { o.HookId, o.Hook.Description, o.Offset })
                .ToListAsync();

            var rows = current.Select(c =>
            {
                var (shifted, stateId) = OffsetShifter.Shift(OffsetFormat.ToLong(c.Offset), ranges);
                return new OffsetPreviewRow(c.HookId, c.Description, c.Offset, OffsetFormat.FromLong(shifted), stateId);
            }).ToList();

            var collision = rows.GroupBy(r => r.NewOffset).FirstOrDefault(g => g.Count() > 1);
            if (collision != null)
            {
                var names = string.Join("\", \"", collision.Select(r => r.Description));
                throw new HookOffsetConflictException($"The shift table sends more than one hook to 0x{collision.Key}: \"{names}\".");
            }

            return rows;
        }

        private async Task<List<ShiftRange>> LoadRangesAsync(int fromId, int toId) =>
            await _context.GameVersionShifts
                .AsNoTracking()
                .Where(s => s.FromGameVersionId == fromId && s.ToGameVersionId == toId)
                .OrderBy(s => s.RangeStart)
                .Select(s => new ShiftRange(s.RangeStart, s.RangeEnd, s.Delta))
                .ToListAsync();

        private static OffsetApplyResult Summarize(List<OffsetPreviewRow> rows) =>
            new(
                rows.Count(r => r.OffsetStateId == OffsetStates.Generated),
                rows.Count(r => r.OffsetStateId == OffsetStates.CarriedForward),
                rows);
    }
}
