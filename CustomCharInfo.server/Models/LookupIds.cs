namespace CustomCharInfo.server.Models
{
    // IDs of the seeded lookup rows, mirrored in the frontend's globals.js.
    // The rows themselves live in the database; these just name them so code never compares against a bare number.

    public static class UserTypes
    {
        public const int User = 1;
        public const int Modder = 2;
        public const int Admin = 3;
        // Differs from Admin only in seeing personal data (emails and IP addresses); displays as Admin to end users.
        public const int SuperAdmin = 4;
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

    // How a hook's offset for one game version was produced.
    // Confirmed means a person entered or checked it; the other two were derived when a game version was added.
    public static class OffsetStates
    {
        public const int Confirmed = 1;
        public const int Generated = 2;
        public const int CarriedForward = 3;

        public static readonly int[] Unverified = { Generated, CarriedForward };
    }

    // What a credited modder did on a moveset. A credit may hold several or none.
    public static class ContributionRoles
    {
        public const int Coding = 1;
        public const int Animation = 2;
        public const int Modelling = 3;
        public const int Rendering = 4;
        public const int Sounds = 5;
        public const int Effects = 6;
        public const int ConceptDesign = 7;
        public const int Other = 8;

        public static readonly int[] All = { Coding, Animation, Modelling, Rendering, Sounds, Effects, ConceptDesign, Other };

        // Roles that count on a modder's profile. Other is stored and shown on the moveset page only.
        public static readonly int[] ProfileVisible = { Coding, Animation, Modelling, Rendering, Sounds, Effects, ConceptDesign };
    }
}
