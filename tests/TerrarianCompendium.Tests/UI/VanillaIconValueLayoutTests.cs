using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class VanillaIconValueLayoutTests
    {
        [Test]
        public void CalculateHeight_NoItems_ReturnsZero()
        {
            Assert.That(VanillaIconValueLayout.CalculateHeight(0, 300), Is.EqualTo(0));
        }

        [Test]
        public void CalculateColumnCount_WideArea_UsesTwoColumns()
        {
            int width = VanillaIconValueLayout.MinimumCellWidth * 2 + VanillaIconValueLayout.ColumnGap;

            Assert.That(VanillaIconValueLayout.CalculateColumnCount(2, width), Is.EqualTo(2));
        }

        [Test]
        public void CalculateColumnCount_NarrowPositiveArea_UsesOneColumn()
        {
            Assert.That(VanillaIconValueLayout.CalculateColumnCount(3, 1), Is.EqualTo(1));
        }

        [Test]
        public void CalculateHeight_OddItemCount_UsesPartialFinalRow()
        {
            int width = VanillaIconValueLayout.MinimumCellWidth * 2 + VanillaIconValueLayout.ColumnGap;
            int expected = VanillaIconValueLayout.RowHeight * 2 + VanillaIconValueLayout.RowGap;

            Assert.That(VanillaIconValueLayout.CalculateHeight(3, width), Is.EqualTo(expected));
        }

        [Test]
        public void CalculateCellBounds_TwoColumns_FillAvailableWidthWithoutOverlap()
        {
            const int width = 205;
            VanillaIconValueCellBounds first = VanillaIconValueLayout.CalculateCellBounds(0, 2, width);
            VanillaIconValueCellBounds second = VanillaIconValueLayout.CalculateCellBounds(1, 2, width);

            Assert.Multiple(() =>
            {
                Assert.That(first.X, Is.EqualTo(0));
                Assert.That(first.Y, Is.EqualTo(0));
                Assert.That(second.Y, Is.EqualTo(0));
                Assert.That(second.X, Is.EqualTo(first.Width + VanillaIconValueLayout.ColumnGap));
                Assert.That(second.Right, Is.EqualTo(width));
                Assert.That(first.Height, Is.EqualTo(VanillaIconValueLayout.RowHeight));
                Assert.That(second.Height, Is.EqualTo(VanillaIconValueLayout.RowHeight));
            });
        }
    }
}