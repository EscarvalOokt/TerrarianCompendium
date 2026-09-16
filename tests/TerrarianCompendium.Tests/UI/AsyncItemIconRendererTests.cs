using NUnit.Framework;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class AsyncItemIconRendererTests
    {
        [TestCase(16, 16, 32, 32)]
        [TestCase(32, 24, 32, 32)]
        [TestCase(32, 32, 32, 40)]
        public void CalculateScale_FrameWithinSizeLimit_DoesNotUpscale(
            int frameWidth,
            int frameHeight,
            int width,
            int height)
        {
            float scale = AsyncItemIconRenderer.CalculateScale(frameWidth, frameHeight, width, height);

            Assert.That(scale, Is.EqualTo(1f));
        }

        [Test]
        public void CalculateScale_WideFrame_DownscalesByWidth()
        {
            float scale = AsyncItemIconRenderer.CalculateScale(64, 32, 32, 32);

            Assert.That(scale, Is.EqualTo(0.5f));
        }

        [Test]
        public void CalculateScale_TallFrame_DownscalesByHeight()
        {
            float scale = AsyncItemIconRenderer.CalculateScale(32, 80, 40, 40);

            Assert.That(scale, Is.EqualTo(0.5f));
        }

        [Test]
        public void CalculateScale_RectangularBounds_UsesSmallerDimensionAsVanillaSizeLimit()
        {
            float scale = AsyncItemIconRenderer.CalculateScale(60, 30, 80, 40);

            Assert.That(scale, Is.EqualTo(40f / 60f).Within(0.0001f));
        }

        [TestCase(0, 16, 32, 32)]
        [TestCase(16, 0, 32, 32)]
        [TestCase(16, 16, 0, 32)]
        [TestCase(16, 16, 32, 0)]
        [TestCase(-1, 16, 32, 32)]
        public void CalculateScale_InvalidDimensions_ReturnsZero(int frameWidth, int frameHeight, int width, int height)
        {
            float scale = AsyncItemIconRenderer.CalculateScale(frameWidth, frameHeight, width, height);

            Assert.That(scale, Is.Zero);
        }
    }
}