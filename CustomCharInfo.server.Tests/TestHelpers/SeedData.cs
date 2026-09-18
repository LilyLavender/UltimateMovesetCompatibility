using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;

namespace CustomCharInfo.server.Tests.TestHelpers
{
    /// <summary>
    /// Common lookup rows most controller tests need (release states, item/acceptance state
    /// vocab for the ActionLog approval workflow, user types). Call once per test context.
    /// </summary>
    public static class SeedData
    {
        public static void SeedLookups(AppDbContext context)
        {
            context.ReleaseStates.AddRange(
                new ReleaseState { ReleaseStateId = ReleaseStates.Released, ReleaseStateName = "Released" },
                new ReleaseState { ReleaseStateId = ReleaseStates.OpenBeta, ReleaseStateName = "Open Beta" }
            );

            context.ItemTypes.AddRange(
                new ItemType { ItemTypeId = ItemTypes.Moveset, ItemTypeName = "Moveset" },
                new ItemType { ItemTypeId = ItemTypes.Modder, ItemTypeName = "Modder" },
                new ItemType { ItemTypeId = ItemTypes.Series, ItemTypeName = "Series" },
                new ItemType { ItemTypeId = ItemTypes.Hook, ItemTypeName = "Hook" }
            );

            context.Set<UserType>().AddRange(
                new UserType { UserTypeId = UserTypes.User, UserTypeName = "User" },
                new UserType { UserTypeId = UserTypes.Modder, UserTypeName = "Modder" },
                new UserType { UserTypeId = UserTypes.Admin, UserTypeName = "Admin" }
            );

            context.VanillaChars.Add(new VanillaChar { VanillaCharInternalName = "mario", DisplayName = "Mario" });

            context.SaveChanges();

            // Generic actor for action logs that don't need to attribute a specific user.
            AddUser(context, "log-author", userTypeId: UserTypes.User);

            context.AcceptanceStates.AddRange(
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.PendingAdminSoft, AcceptanceStateName = "Pending Admin Action (Soft)" },
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.PendingAdminHard, AcceptanceStateName = "Pending Admin Action (Hard)" },
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.PendingUserSoft, AcceptanceStateName = "Pending User Action (Soft)" },
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.PendingUserHard, AcceptanceStateName = "Pending User Action (Hard)" },
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.Accepted, AcceptanceStateName = "Accepted" },
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.Rejected, AcceptanceStateName = "Rejected" },
                new AcceptanceState { AcceptanceStateId = AcceptanceStates.AutoAccepted, AcceptanceStateName = "Auto-Accepted" }
            );

            context.SaveChanges();
        }

        public static ApplicationUser AddUser(AppDbContext context, string id, int userTypeId, int? modderId = null)
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = $"user-{id}",
                UserTypeId = userTypeId,
                ModderId = modderId
            };
            context.Users.Add(user);
            context.SaveChanges();
            return user;
        }

        public static Modder AddModder(AppDbContext context, int modderId, string userId, string name)
        {
            var modder = new Modder { ModderId = modderId, UserId = userId, Name = name };
            context.Modders.Add(modder);
            context.SaveChanges();
            return modder;
        }

        public static ActionLog AddActionLog(AppDbContext context, int itemId, int acceptanceStateId, DateTime createdAt, string userId, int itemTypeId = ItemTypes.Moveset)
        {
            var log = new ActionLog
            {
                ItemTypeId = itemTypeId,
                ItemId = itemId,
                AcceptanceStateId = acceptanceStateId,
                UserId = userId,
                Notes = "",
                CreatedAt = createdAt
            };
            context.ActionLogs.Add(log);
            context.SaveChanges();
            return log;
        }
    }
}
