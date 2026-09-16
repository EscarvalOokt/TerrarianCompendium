using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class VanillaPopoverTests
    {
        [Test]
        public void CalculateContentSizedPanelBounds_AddsChromePaddingToNaturalContentSize()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 600,
                hostHeight: 400,
                anchorX: 450,
                anchorY: 20,
                anchorWidth: 120,
                anchorHeight: 24,
                contentWidth: 100,
                contentHeight: 40,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(454));
            Assert.That(bounds.Y, Is.EqualTo(48));
            Assert.That(bounds.Width, Is.EqualTo(116));
            Assert.That(bounds.Height, Is.EqualTo(56));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_ShortContentFitsBelow_StaysBelowAnchor()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 400,
                hostHeight: 400,
                anchorX: 200,
                anchorY: 300,
                anchorWidth: 40,
                anchorHeight: 24,
                contentWidth: 80,
                contentHeight: 20,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(144));
            Assert.That(bounds.Y, Is.EqualTo(328));
            Assert.That(bounds.Width, Is.EqualTo(96));
            Assert.That(bounds.Height, Is.EqualTo(36));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_WhenBelowCannotFitAndAboveCan_OpensAboveAnchor()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 400,
                hostHeight: 300,
                anchorX: 200,
                anchorY: 240,
                anchorWidth: 100,
                anchorHeight: 24,
                contentWidth: 100,
                contentHeight: 60,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(184));
            Assert.That(bounds.Y, Is.EqualTo(160));
            Assert.That(bounds.Width, Is.EqualTo(116));
            Assert.That(bounds.Height, Is.EqualTo(76));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_WhenNeitherSideCanFit_UsesLargerAvailableSide()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 300,
                hostHeight: 180,
                anchorX: 260,
                anchorY: 70,
                anchorWidth: 40,
                anchorHeight: 24,
                contentWidth: 444,
                contentHeight: 344,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(0));
            Assert.That(bounds.Y, Is.EqualTo(98));
            Assert.That(bounds.Width, Is.EqualTo(300));
            Assert.That(bounds.Height, Is.EqualTo(82));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_ClampsWidthToHost()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 300,
                hostHeight: 400,
                anchorX: 260,
                anchorY: 20,
                anchorWidth: 40,
                anchorHeight: 24,
                contentWidth: 444,
                contentHeight: 284,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(0));
            Assert.That(bounds.Y, Is.EqualTo(48));
            Assert.That(bounds.Width, Is.EqualTo(300));
            Assert.That(bounds.Height, Is.EqualTo(300));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_RightEdgeAnchor_ClampsHorizontalPosition()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 300,
                hostHeight: 200,
                anchorX: 280,
                anchorY: 20,
                anchorWidth: 40,
                anchorHeight: 24,
                contentWidth: 100,
                contentHeight: 40,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(184));
            Assert.That(bounds.Y, Is.EqualTo(48));
            Assert.That(bounds.Width, Is.EqualTo(116));
            Assert.That(bounds.Height, Is.EqualTo(56));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_LeftEdgeAnchor_ClampsHorizontalPosition()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 300,
                hostHeight: 200,
                anchorX: -30,
                anchorY: 20,
                anchorWidth: 20,
                anchorHeight: 24,
                contentWidth: 100,
                contentHeight: 40,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(0));
            Assert.That(bounds.Y, Is.EqualTo(48));
            Assert.That(bounds.Width, Is.EqualTo(116));
            Assert.That(bounds.Height, Is.EqualTo(56));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_TinyHost_KeepsBoundsInsideViewport()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 10,
                hostHeight: 10,
                anchorX: 0,
                anchorY: 0,
                anchorWidth: 0,
                anchorHeight: 0,
                contentWidth: 100,
                contentHeight: 100,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(0));
            Assert.That(bounds.Y, Is.EqualTo(4));
            Assert.That(bounds.Width, Is.EqualTo(10));
            Assert.That(bounds.Height, Is.EqualTo(6));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_NegativeContentDimensions_NormalizesBeforeAddingChrome()
        {
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 100,
                hostHeight: 100,
                anchorX: 10,
                anchorY: 10,
                anchorWidth: 10,
                anchorHeight: 10,
                contentWidth: -5,
                contentHeight: -7,
                panelPadding: 8,
                anchorGap: 4);

            Assert.That(bounds.X, Is.EqualTo(4));
            Assert.That(bounds.Y, Is.EqualTo(24));
            Assert.That(bounds.Width, Is.EqualTo(16));
            Assert.That(bounds.Height, Is.EqualTo(16));
        }

        [Test]
        public void CalculateContentSizedPanelBounds_WhenContentSizeChanges_RecalculatesBounds()
        {
            VanillaPopoverBounds initialBounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 400,
                hostHeight: 300,
                anchorX: 200,
                anchorY: 20,
                anchorWidth: 40,
                anchorHeight: 24,
                contentWidth: 80,
                contentHeight: 20,
                panelPadding: 8,
                anchorGap: 4);

            VanillaPopoverBounds updatedBounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth: 400,
                hostHeight: 300,
                anchorX: 200,
                anchorY: 20,
                anchorWidth: 40,
                anchorHeight: 24,
                contentWidth: 180,
                contentHeight: 100,
                panelPadding: 8,
                anchorGap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(initialBounds.X, Is.EqualTo(144));
                Assert.That(initialBounds.Y, Is.EqualTo(48));
                Assert.That(initialBounds.Width, Is.EqualTo(96));
                Assert.That(initialBounds.Height, Is.EqualTo(36));

                Assert.That(updatedBounds.X, Is.EqualTo(44));
                Assert.That(updatedBounds.Y, Is.EqualTo(48));
                Assert.That(updatedBounds.Width, Is.EqualTo(196));
                Assert.That(updatedBounds.Height, Is.EqualTo(116));
            });
        }
    }
}