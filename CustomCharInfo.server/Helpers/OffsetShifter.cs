using System.Globalization;
using CustomCharInfo.server.Models;

namespace CustomCharInfo.server.Helpers
{
    // One line of a shift table: addresses in [Start, End] of the old version moved by Delta in the new one.
    public sealed record ShiftRange(long Start, long End, long Delta);

    public class ShiftTableParseException : Exception
    {
        public int LineNumber { get; }

        public ShiftTableParseException(int lineNumber, string reason)
            : base($"Line {lineNumber}: {reason}")
        {
            LineNumber = lineNumber;
        }
    }

    // Parses the shift table that describes a game update and moves one address through it.
    // The table format is one range per line, "start end delta", hex with or without 0x, delta signed, blank lines ignored.
    public static class OffsetShifter
    {
        public static List<ShiftRange> Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ShiftTableParseException(1, "The shift table is empty.");

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var parsed = new List<(ShiftRange Range, int Line)>();

            for (var i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var line = lines[i].Trim();
                if (line.Length == 0)
                    continue;

                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                    throw new ShiftTableParseException(lineNumber, "Expected three values: start, end, and delta.");

                if (!TryParseHex(parts[0], out var start))
                    throw new ShiftTableParseException(lineNumber, $"\"{parts[0]}\" is not a hex address.");
                if (!TryParseHex(parts[1], out var end))
                    throw new ShiftTableParseException(lineNumber, $"\"{parts[1]}\" is not a hex address.");
                if (!TryParseSignedHex(parts[2], out var delta))
                    throw new ShiftTableParseException(lineNumber, $"\"{parts[2]}\" is not a signed hex delta such as +0x450 or -0x1a0.");
                if (end < start)
                    throw new ShiftTableParseException(lineNumber, "The range ends before it starts.");

                parsed.Add((new ShiftRange(start, end, delta), lineNumber));
            }

            if (parsed.Count == 0)
                throw new ShiftTableParseException(1, "The shift table is empty.");

            parsed.Sort((a, b) => a.Range.Start.CompareTo(b.Range.Start));
            for (var i = 1; i < parsed.Count; i++)
            {
                if (parsed[i].Range.Start <= parsed[i - 1].Range.End)
                    throw new ShiftTableParseException(parsed[i].Line, $"This range overlaps the one on line {parsed[i - 1].Line}.");
            }

            return parsed.Select(p => p.Range).ToList();
        }

        // The address in the new version and how it was produced.
        // An address no range covers is assumed to be unchanged and comes back as CarriedForward.
        public static (long Address, int OffsetStateId) Shift(long address, IReadOnlyList<ShiftRange> ranges)
        {
            foreach (var range in ranges)
            {
                if (address < range.Start || address > range.End)
                    continue;
                var shifted = address + range.Delta;
                if (shifted < 0)
                    break;
                return (shifted, OffsetStates.Generated);
            }

            return (address, OffsetStates.CarriedForward);
        }

        private static bool TryParseHex(string text, out long value)
        {
            value = 0;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];
            if (text.Length == 0 || text.Length > 15)
                return false;
            return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseSignedHex(string text, out long value)
        {
            value = 0;
            var negative = false;
            if (text.StartsWith('+'))
                text = text[1..];
            else if (text.StartsWith('-'))
            {
                negative = true;
                text = text[1..];
            }

            if (!TryParseHex(text, out var magnitude))
                return false;

            value = negative ? -magnitude : magnitude;
            return true;
        }
    }
}
