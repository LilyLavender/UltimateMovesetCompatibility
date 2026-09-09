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
                new ReleaseState { ReleaseStateId = 1, ReleaseStateName = "Released" },
                new ReleaseState { ReleaseStateId = 4, ReleaseStateName = "Beta" }
            );

            context.ItemTypes.AddRange(
                new ItemType { ItemTypeId = 1, ItemTypeName = "Moveset" },
                new ItemType { ItemTypeId = 2, ItemTypeName = "Modder" },
                new ItemType { ItemTypeId = 3, ItemTypeName = "Series" },
                new ItemType { ItemTypeId = 4, ItemTypeName = "Hook" }
            );

            context.Set<UserType>().AddRange(
                new UserType { UserTypeId = 1, UserTypeName = "User" },
                new UserType { UserTypeId = 2, UserTypeName = "Modder" },
                new UserType { UserTypeId = 3, UserTypeName = "Admin" }
            );

            context.VanillaChars.Add(new VanillaChar { VanillaCharInternalName = "mario", DisplayName = "Mario" });

            context.SaveChanges();

            // Generic actor for action logs that don't need to attribute a specific user.
            AddUser(context, "log-author", userTypeId: 1);

            context.AcceptanceStates.AddRange(
                new AcceptanceState { AcceptanceStateId = 1, AcceptanceStateName = "Submitted" },
                new AcceptanceState { AcceptanceStateId = 2, AcceptanceStateName = "Pending" },
                new AcceptanceState { AcceptanceStateId = 3, AcceptanceStateName = "Accepted" },
                new AcceptanceState { AcceptanceStateId = 4, AcceptanceStateName = "PendingRejected" },
                new AcceptanceState { AcceptanceStateId = 5, AcceptanceStateName = "Approved" },
                new AcceptanceState { AcceptanceStateId = 6, AcceptanceStateName = "Rejected" },
                new AcceptanceState { AcceptanceStateId = 7, AcceptanceStateName = "AdminApproved" }
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

        public static ActionLog AddActionLog(AppDbContext context, int itemId, int acceptanceStateId, DateTime createdAt, string userId, int itemTypeId = 1)
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
