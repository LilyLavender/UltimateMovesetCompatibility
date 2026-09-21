using CustomCharInfo.server.Helpers;
using Xunit;

namespace CustomCharInfo.server.Tests.Helpers
{
    public class OffsetFormatTests
    {
        [Theory]
        [InlineData("0x0abc", "ABC")]
        [InlineData("0ABC", "ABC")]
        [InlineData("abc", "ABC")]
        [InlineData("  0X2B4D8D0 ", "2B4D8D0")]
        [InlineData("0x0", "0")]
        [InlineData("00000000", "0")]
        [InlineData("00000000FFFFFFFF", "FFFFFFFF")]
        public void TryNormalize_AcceptsEquivalentSpellings(string input, string expected)
        {
            Assert.True(OffsetFormat.TryNormalize(input, out var normalized));
            Assert.Equal(expected, normalized);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0x")]
        [InlineData("xyz")]
        [InlineData("0x12G4")]
        [InlineData("123456789")]
        [InlineData("-1")]
        public void TryNormalize_RejectsNonAddresses(string input)
        {
            Assert.False(OffsetFormat.TryNormalize(input, out _));
        }

        [Fact]
        public void ToLong_AndFromLong_RoundTrip()
        {
            Assert.Equal(0x2B4D8D0, OffsetFormat.ToLong("2B4D8D0"));
            Assert.Equal("2B4D8D0", OffsetFormat.FromLong(0x2B4D8D0));
        }
    }
}
