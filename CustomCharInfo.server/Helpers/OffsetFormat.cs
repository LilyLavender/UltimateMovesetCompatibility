using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CustomCharInfo.server.Helpers
{
    // One spelling for a hook address: uppercase hex, no 0x prefix, no leading zeros.
    // Every offset is normalized before it is stored or compared, so "0x0abc", "0ABC", and "abc" are the same hook.
    public static class OffsetFormat
    {
        private const int MaxHexDigits = 8;

        public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var text = input.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];

            if (text.Length == 0 || !text.All(Uri.IsHexDigit))
                return false;

            text = text.TrimStart('0');
            if (text.Length == 0)
                text = "0";
            if (text.Length > MaxHexDigits)
                return false;

            normalized = text.ToUpperInvariant();
            return true;
        }

        public static long ToLong(string normalized) =>
            long.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        public static string FromLong(long value) =>
            value.ToString("X", CultureInfo.InvariantCulture);
    }
}
