using System.Text.Json;

namespace CustomCharInfo.server.Helpers
{
    public static class DiffHelper
    {
        public static string? Build(IEnumerable<(string Field, object? OldVal, object? NewVal)> fields)
        {
            var changes = fields
                .Where(f => Normalize(f.OldVal) != Normalize(f.NewVal))
                .Select(f => new { field = f.Field, old = f.OldVal, @new = f.NewVal })
                .ToList();

            return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
        }

        private static string Normalize(object? v) => v?.ToString() ?? "";
    }
}
