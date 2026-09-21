namespace CustomCharInfo.server.Models
{
    // One address range that moved between two consecutive game versions.
    // An address in [RangeStart, RangeEnd] of the From version sits at address + Delta in the To version.
    public class GameVersionShift
    {
        public int GameVersionShiftId { get; set; }

        public int FromGameVersionId { get; set; }
        public GameVersion FromGameVersion { get; set; }

        public int ToGameVersionId { get; set; }
        public GameVersion ToGameVersion { get; set; }

        public long RangeStart { get; set; }

        public long RangeEnd { get; set; }

        public long Delta { get; set; }
    }
}
