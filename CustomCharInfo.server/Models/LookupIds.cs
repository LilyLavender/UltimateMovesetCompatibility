namespace CustomCharInfo.server.Models
{
    // IDs of the seeded lookup rows, mirrored in the frontend's globals.js.
    // The rows themselves live in the database; these just name them so code never compares against a bare number.

    public static class UserTypes
    {
        public const int User = 1;
        public const int Modder = 2;
        public const int Admin = 3;
    }

    // Soft edits stay visible while pending; hard edits are hidden until an admin acts.
    public static class AcceptanceStates
    {
        public const int PendingAdminSoft = 1;
        public const int PendingAdminHard = 2;
        public const int PendingUserSoft = 3;
        public const int PendingUserHard = 4;
        public const int Accepted = 5;
        public const int Rejected = 6;
        public const int AutoAccepted = 7;

        // Groups for "is it in one of these" checks. Static arrays translate to SQL IN (...) inside EF queries.
        public static readonly int[] PendingAdmin = { PendingAdminSoft, PendingAdminHard };
        public static readonly int[] PendingUser = { PendingUserSoft, PendingUserHard };
        public static readonly int[] Soft = { PendingAdminSoft, PendingUserSoft };
        public static readonly int[] Hard = { PendingAdminHard, PendingUserHard };
        public static readonly int[] AnyAccepted = { Accepted, AutoAccepted };

        // States under which an item is hidden from everyone but its owner and admins.
        public static readonly int[] Blocked = { PendingAdminHard, PendingUserHard, Rejected };
    }

    // What ActionLog.ItemId refers to.
    public static class ItemTypes
    {
        public const int Moveset = 1;
        public const int Modder = 2;
        public const int Series = 3;
        public const int Hook = 4;
        public const int Plugin = 5;
    }

    public static class ReleaseStates
    {
        public const int Released = 1;
        public const int Upcoming = 2;
        public const int PendingUpdate = 3;
        public const int OpenBeta = 4;
        public const int Deprecated = 5;
    }

    // Whether a Skyline hook offset tolerates being hooked by more than one plugin.
    // Drives predicted compatibility: once-only hooks conflict outright, untested ones are a predicted conflict.
    public static class HookableStatuses
    {
        public const int Untested = 1;
        public const int OnlyOnce = 2;
        public const int MoreThanOnce = 3;
    }
}
