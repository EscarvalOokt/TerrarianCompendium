using NUnit.Framework;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class BrowserShellLayoutTests
    {
        [TestCase(1000)]
        [TestCase(642)]
        [TestCase(230)]
        public void CalculateDetailsWidth_WithEnoughSpace_ReturnsFixedWidth(int availableWidth)
        {
            int result = BrowserShell.CalculateDetailsWidth(availableWidth);

            Assert.That(result, Is.EqualTo(230));
        }

        [TestCase(229, 229)]
        [TestCase(100, 100)]
        [TestCase(1, 1)]
        public void CalculateDetailsWidth_WhenViewportIsNarrowerThanDetails_UsesAvailableWidth(
            int availableWidth,
            int expectedWidth)
        {
            int result = BrowserShell.CalculateDetailsWidth(availableWidth);

            Assert.That(result, Is.EqualTo(expectedWidth));
        }

        [TestCase(0)]
        [TestCase(-100)]
        public void CalculateDetailsWidth_WithoutPositiveSpace_ReturnsZero(int availableWidth)
        {
            int result = BrowserShell.CalculateDetailsWidth(availableWidth);

            Assert.That(result, Is.Zero);
        }
    }
}