using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class SupplementalTooltipTests
    {
        [Test]
        public void WrapText_TextFits_ReturnsSingleLine()
        {
            IReadOnlyList<string> lines = SupplementalTooltip.WrapText("short text", 10, MeasureCharacters);

            Assert.That(lines, Is.EqualTo(["short text"]));
        }

        [Test]
        public void WrapText_MultipleWords_WrapsAtWordBoundary()
        {
            IReadOnlyList<string> lines = SupplementalTooltip.WrapText("alpha beta gamma", 10, MeasureCharacters);

            Assert.That(lines, Is.EqualTo(["alpha beta", "gamma"]));
        }

        [Test]
        public void WrapText_OversizedToken_SplitsToFitMaximumWidth()
        {
            IReadOnlyList<string> lines = SupplementalTooltip.WrapText("abcdefghij", 4, MeasureCharacters);

            Assert.Multiple(() =>
            {
                Assert.That(lines, Is.EqualTo(["abcd", "efgh", "ij"]));
                foreach (string line in lines)
                    Assert.That(MeasureCharacters(line), Is.LessThanOrEqualTo(4));
            });
        }

        [Test]
        public void WrapText_OversizedTokenBetweenWords_PreservesFragmentOrder()
        {
            IReadOnlyList<string> lines = SupplementalTooltip.WrapText("one abcdefgh two", 5, MeasureCharacters);

            Assert.That(lines, Is.EqualTo(["one", "abcde", "fgh", "two"]));
        }

        [Test]
        public void WrapText_WhitespaceOnly_ReturnsNoVisualLines()
        {
            IReadOnlyList<string> lines = SupplementalTooltip.WrapText("   ", 10, MeasureCharacters);

            Assert.That(lines, Is.Empty);
        }

        [Test]
        public void CalculateVisualRows_TokensFit_ReturnsSingleRow()
        {
            SupplementalTooltip.VisualToken[] tokens =
            [
                CreateVisualToken(10, 8),
                CreateVisualToken(12, 9),
                CreateVisualToken(8, 7)
            ];

            IReadOnlyList<SupplementalTooltip.VisualRowLayout> rows =
                SupplementalTooltip.CalculateVisualRows(tokens, 40, 2);

            Assert.Multiple(() =>
            {
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].StartIndex, Is.EqualTo(0));
                Assert.That(rows[0].Count, Is.EqualTo(3));
                Assert.That(rows[0].Width, Is.EqualTo(34));
                Assert.That(rows[0].Height, Is.EqualTo(9));
            });
        }

        [Test]
        public void CalculateVisualRows_Overflow_WrapsWithoutReordering()
        {
            SupplementalTooltip.VisualToken[] tokens =
            [
                CreateVisualToken(10, 8),
                CreateVisualToken(12, 9),
                CreateVisualToken(8, 7)
            ];

            IReadOnlyList<SupplementalTooltip.VisualRowLayout> rows =
                SupplementalTooltip.CalculateVisualRows(tokens, 24, 2);

            Assert.Multiple(() =>
            {
                Assert.That(rows.Count, Is.EqualTo(2));
                Assert.That(rows[0].StartIndex, Is.EqualTo(0));
                Assert.That(rows[0].Count, Is.EqualTo(2));
                Assert.That(rows[0].Width, Is.EqualTo(24));
                Assert.That(rows[1].StartIndex, Is.EqualTo(2));
                Assert.That(rows[1].Count, Is.EqualTo(1));
                Assert.That(rows[1].Width, Is.EqualTo(8));
            });
        }

        [Test]
        public void CalculateVisualRows_OversizedToken_ConstrainsMeasuredRowWidth()
        {
            SupplementalTooltip.VisualToken[] tokens =
            [
                CreateVisualToken(50, 8)
            ];

            IReadOnlyList<SupplementalTooltip.VisualRowLayout> rows =
                SupplementalTooltip.CalculateVisualRows(tokens, 20, 2);

            Assert.That(rows[0].Width, Is.EqualTo(20));
        }

        private static SupplementalTooltip.VisualToken CreateVisualToken(int width, int height)
        {
            return new SupplementalTooltip.VisualToken(width, height, (_, _, _, _) => { });
        }

        private static int MeasureCharacters(string value)
        {
            return value?.Length ?? 0;
        }
    }
}