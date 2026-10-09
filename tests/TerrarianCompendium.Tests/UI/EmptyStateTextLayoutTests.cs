using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class EmptyStateTextLayoutTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(24, 1)]
        [TestCase(100, 76)]
        public void CalculateContentWidth_AppliesHorizontalPaddingWithoutLosingPositiveWidth(int width, int expected)
        {
            Assert.That(EmptyStateTextLayout.CalculateContentWidth(width), Is.EqualTo(expected));
        }

        [Test]
        public void CalculateBlockTop_MultipleLines_CentersWholeBlock()
        {
            int top = EmptyStateTextLayout.CalculateBlockTop(height: 100, lineCount: 2);

            Assert.That(top, Is.EqualTo(32));
        }

        [Test]
        public void CalculateBlockTop_BlockTallerThanViewport_ClampsToTop()
        {
            int top = EmptyStateTextLayout.CalculateBlockTop(height: 20, lineCount: 2);

            Assert.That(top, Is.Zero);
        }

        [Test]
        public void CalculateLineLeft_CentersLine()
        {
            Assert.That(EmptyStateTextLayout.CalculateLineLeft(width: 100, lineWidth: 40), Is.EqualTo(30));
        }

        [Test]
        public void CalculateLineTop_UsesLineSpacingWithinCenteredBlock()
        {
            int first = EmptyStateTextLayout.CalculateLineTop(blockTop: 32, lineIndex: 0);
            int second = EmptyStateTextLayout.CalculateLineTop(blockTop: 32, lineIndex: 1);

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(33));
                Assert.That(second, Is.EqualTo(51));
                Assert.That(second - first, Is.EqualTo(EmptyStateTextLayout.LineHeight));
            });
        }
    }
}