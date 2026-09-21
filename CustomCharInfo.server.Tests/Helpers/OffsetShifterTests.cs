using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models;
using Xunit;

namespace CustomCharInfo.server.Tests.Helpers
{
    public class OffsetShifterTests
    {
        // 13.0.4 to 13.0.5 table from PhazoGanon
        private const string Table = @"
0x0          0x169c3bf    +0x0
0x169ef70    0x178b563    -0x1a0
0x178cfe0    0x17999b7    -0xe0
0x1799c80    0x183965f    -0x40
0x183ad64    0x230feeb    +0x0
0x2314650    0x263dd2f    +0x450
0x263e028    0x264e03b    +0x494
0x264efb8    0x2b4d507    +0x490
0x2b4d8d0    0x2b50f97    +0x584
0x2b51ec0    0x2d335df    +0x590
0x2d3aba4    0x39c7e8f    +0x5b0

0x39c8000    0x4f47fff    +0x1000
0x4f48000    0x52a856f    +0x1000

0x52a9000    0x7446100    +0x1000
";

        [Fact]
        public void Parse_RealTable_ReadsEveryRangeSortedByStart()
        {
            var ranges = OffsetShifter.Parse(Table);

            Assert.Equal(14, ranges.Count);
            Assert.Equal(0x0, ranges[0].Start);
            Assert.Equal(0x169c3bf, ranges[0].End);
            Assert.Equal(-0x1a0, ranges[1].Delta);
            Assert.Equal(0x1000, ranges[^1].Delta);
            Assert.Equal(0x7446100, ranges[^1].End);
        }

        [Theory]
        [InlineData("2000000", "2000000", OffsetStates.Generated)]
        [InlineData("6000000", "6001000", OffsetStates.Generated)]
        [InlineData("2B4D8D0", "2B4DE54", OffsetStates.Generated)]
        [InlineData("169EF70", "169EDD0", OffsetStates.Generated)]
        [InlineData("169C3C0", "169C3C0", OffsetStates.CarriedForward)]
        [InlineData("7446101", "7446101", OffsetStates.CarriedForward)]
        public void Shift_WorkedExamples(string before, string expectedAfter, int expectedState)
        {
            var ranges = OffsetShifter.Parse(Table);

            var (address, state) = OffsetShifter.Shift(OffsetFormat.ToLong(before), ranges);

            Assert.Equal(expectedAfter, OffsetFormat.FromLong(address));
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void Parse_AcceptsLowercaseAndMissingPrefix()
        {
            var ranges = OffsetShifter.Parse("abc\tdef\t-10");

            var range = Assert.Single(ranges);
            Assert.Equal(0xabc, range.Start);
            Assert.Equal(0xdef, range.End);
            Assert.Equal(-0x10, range.Delta);
        }

        [Fact]
        public void Parse_WrongColumnCount_NamesTheLine()
        {
            var ex = Assert.Throws<ShiftTableParseException>(() => OffsetShifter.Parse("0x0 0x10 +0x0\n0x20 0x30"));

            Assert.Equal(2, ex.LineNumber);
            Assert.Contains("Line 2", ex.Message);
        }

        [Fact]
        public void Parse_BadHex_NamesTheLine()
        {
            var ex = Assert.Throws<ShiftTableParseException>(() => OffsetShifter.Parse("0x0 0xZZ +0x0"));

            Assert.Equal(1, ex.LineNumber);
        }

        [Fact]
        public void Parse_UnsignedDelta_IsAccepted()
        {
            var ranges = OffsetShifter.Parse("0x0 0x10 0x1000");

            Assert.Equal(0x1000, Assert.Single(ranges).Delta);
        }

        [Fact]
        public void Parse_EndBeforeStart_Throws()
        {
            var ex = Assert.Throws<ShiftTableParseException>(() => OffsetShifter.Parse("0x10 0x0 +0x0"));

            Assert.Contains("ends before", ex.Message);
        }

        [Fact]
        public void Parse_OverlappingRanges_Throws()
        {
            var ex = Assert.Throws<ShiftTableParseException>(() => OffsetShifter.Parse("0x0 0x10 +0x0\n0x10 0x20 +0x0"));

            Assert.Equal(2, ex.LineNumber);
            Assert.Contains("overlaps", ex.Message);
        }

        [Fact]
        public void Parse_Empty_Throws()
        {
            Assert.Throws<ShiftTableParseException>(() => OffsetShifter.Parse("  \n\n"));
        }
    }
}
