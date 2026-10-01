using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;

namespace CustomCharInfo.server.Helpers
{
    // One credited modder: id, sorted distinct role ids, card flag.
    public record CreditSpec(int ModderId, List<int> RoleIds, bool ShowOnCard);

    // Reads the credited modders out of a moveset request, whichever of the two shapes it uses.
    public static class MovesetCredits
    {
        // In request order, deduplicated by modder. The first entry for a modder wins its position.
        // A later duplicate merges its roles in and keeps the credit on the card if either said so.
        public static List<CreditSpec> FromDto(CreateMovesetDto dto)
        {
            var specs = new List<CreditSpec>();
            var byModder = new Dictionary<int, int>();

            void Add(int modderId, IEnumerable<int>? roleIds, bool showOnCard)
            {
                var roles = (roleIds ?? Enumerable.Empty<int>()).Distinct().OrderBy(r => r).ToList();
                if (byModder.TryGetValue(modderId, out var index))
                {
                    var existing = specs[index];
                    specs[index] = existing with
                    {
                        RoleIds = existing.RoleIds.Concat(roles).Distinct().OrderBy(r => r).ToList(),
                        ShowOnCard = existing.ShowOnCard || showOnCard
                    };
                    return;
                }
                byModder[modderId] = specs.Count;
                specs.Add(new CreditSpec(modderId, roles, showOnCard));
            }

            if (dto.Modders != null)
            {
                foreach (var m in dto.Modders) Add(m.ModderId, m.RoleIds, m.ShowOnCard);
            }
            else
            {
                foreach (var id in dto.ModderIds ?? new List<int>()) Add(id, null, true);
            }

            return specs;
        }

        // Null when valid, else the 400 message.
        public static string? Validate(List<CreditSpec> credits)
        {
            if (credits.Count == 0)
                return "At least one credited modder is required.";
            if (credits.Any(c => c.RoleIds.Any(r => !ContributionRoles.All.Contains(r))))
                return "Unknown contribution role.";
            if (!credits.Any(c => c.ShowOnCard))
                return "At least one credited modder must be shown on the card.";
            return null;
        }

        public static List<MovesetModder> ToEntities(List<CreditSpec> credits, int movesetId = 0) =>
            credits.Select((c, i) => new MovesetModder
            {
                MovesetId = movesetId,
                ModderId = c.ModderId,
                SortOrder = i,
                ShowOnCard = c.ShowOnCard,
                Roles = c.RoleIds.Select(r => new MovesetModderRole
                {
                    MovesetId = movesetId,
                    ModderId = c.ModderId,
                    ContributionRoleId = r
                }).ToList()
            }).ToList();

        // What a partial editor may not change: who is credited, their roles, and their card flag.
        public static bool SameMembers(IEnumerable<MovesetModder> stored, List<CreditSpec> requested)
        {
            var before = stored
                .Select(mm => (mm.ModderId, mm.ShowOnCard, string.Join(",", mm.Roles.Select(r => r.ContributionRoleId).OrderBy(r => r))))
                .ToHashSet();
            var after = requested
                .Select(c => (c.ModderId, c.ShowOnCard, string.Join(",", c.RoleIds)))
                .ToHashSet();
            return before.SetEquals(after);
        }

        // "Name (Coding, Animation) (hidden from card)" for the edit diff.
        public static string Label(string name, IEnumerable<int> roleIds, bool showOnCard, Dictionary<int, string> roleNames)
        {
            var roles = roleIds
                .OrderBy(r => r)
                .Select(r => roleNames.TryGetValue(r, out var n) ? n : r.ToString())
                .ToList();
            var label = roles.Count > 0 ? $"{name} ({string.Join(", ", roles)})" : name;
            return showOnCard ? label : $"{label} (hidden from card)";
        }
    }
}
