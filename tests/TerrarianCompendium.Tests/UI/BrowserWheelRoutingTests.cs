using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class BrowserWheelRoutingTests
    {
        private static readonly BrowserWheelBounds _root = new(100, 100, 800, 500);
        private static readonly BrowserWheelBounds _section = new(108, 167, 538, 420);
        private static readonly BrowserWheelBounds _details = new(654, 170, 230, 416);

        [TestCase(99, 200)]
        [TestCase(900, 200)]
        [TestCase(200, 99)]
        [TestCase(200, 600)]
        public void OutsideRoot_PassesThrough(int x, int y)
        {
            Assert.That(Classify(x, y), Is.EqualTo(BrowserWheelRegion.PassThrough));
        }

        [TestCase(108, 167)]
        [TestCase(645, 586)]
        [TestCase(654, 170)]
        [TestCase(883, 585)]
        public void SectionAndDetailsContent_Deliver(int x, int y)
        {
            Assert.That(Classify(x, y), Is.EqualTo(BrowserWheelRegion.Deliver));
        }

        [TestCase(100, 100)]
        [TestCase(200, 130)]
        [TestCase(200, 150)]
        [TestCase(650, 300)]
        [TestCase(646, 300)]
        [TestCase(884, 300)]
        [TestCase(108, 587)]
        public void RootChromeAndContentGaps_CaptureWithoutDelivery(int x, int y)
        {
            Assert.That(Classify(x, y), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
        }

        [Test]
        public void SectionPopover_PanelDeliversAndBackdropCaptures()
        {
            BrowserWheelPopupGeometry popup = SectionPopup();

            Assert.That(Classify(500, 250, sectionPopup: popup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(480, 205, sectionPopup: popup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(639, 404, sectionPopup: popup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(640, 250, sectionPopup: popup), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
            Assert.That(Classify(200, 300, sectionPopup: popup), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
        }

        [Test]
        public void DetailsPopover_PanelDeliversAndBackdropCaptures()
        {
            BrowserWheelPopupGeometry popup = DetailsPopup();

            Assert.That(Classify(670, 270, detailsPopup: popup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(864, 509, detailsPopup: popup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(865, 350, detailsPopup: popup), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
            Assert.That(Classify(700, 200, detailsPopup: popup), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
        }

        [Test]
        public void PopupOverlay_LeavesNeighboringContentIndependentlyScrollable()
        {
            Assert.That(Classify(700, 350, sectionPopup: SectionPopup()), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(300, 350, detailsPopup: DetailsPopup()), Is.EqualTo(BrowserWheelRegion.Deliver));
        }

        [Test]
        public void SimultaneousSectionAndDetailsPopovers_UseTheirOwnPanelAndBackdrop()
        {
            BrowserWheelPopupGeometry sectionPopup = SectionPopup();
            BrowserWheelPopupGeometry detailsPopup = DetailsPopup();

            Assert.That(Classify(500, 250, sectionPopup, detailsPopup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(200, 300, sectionPopup, detailsPopup), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
            Assert.That(Classify(670, 270, sectionPopup, detailsPopup), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(700, 200, sectionPopup, detailsPopup), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
        }

        [Test]
        public void NoOpenPopover_ContentStillDelivers()
        {
            Assert.That(Classify(200, 300), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(700, 300), Is.EqualTo(BrowserWheelRegion.Deliver));
        }

        [Test]
        public void WithoutDetails_UsesFullWidthSectionAndDoesNotInferHiddenDetails()
        {
            BrowserWheelBounds fullWidthSection = new(108, 167, 776, 420);

            Assert.That(
                BrowserWheelRouting.Classify(700, 300, _root, fullWidthSection, null, null, null),
                Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(
                BrowserWheelRouting.Classify(700, 150, _root, fullWidthSection, null, null, null),
                Is.EqualTo(BrowserWheelRegion.CaptureOnly));
        }

        [Test]
        public void HalfOpenBounds_AreUsedForEveryRegion()
        {
            Assert.That(Classify(899, 599), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
            Assert.That(Classify(900, 599), Is.EqualTo(BrowserWheelRegion.PassThrough));
            Assert.That(Classify(645, 586), Is.EqualTo(BrowserWheelRegion.Deliver));
            Assert.That(Classify(646, 586), Is.EqualTo(BrowserWheelRegion.CaptureOnly));
        }

        private static BrowserWheelRegion Classify(
            int x,
            int y,
            BrowserWheelPopupGeometry? sectionPopup = null,
            BrowserWheelPopupGeometry? detailsPopup = null)
        {
            return BrowserWheelRouting.Classify(x, y, _root, _section, _details, sectionPopup, detailsPopup);
        }

        private static BrowserWheelPopupGeometry SectionPopup()
        {
            return new BrowserWheelPopupGeometry(_section, new BrowserWheelBounds(480, 205, 160, 200));
        }

        private static BrowserWheelPopupGeometry DetailsPopup()
        {
            return new BrowserWheelPopupGeometry(_details, new BrowserWheelBounds(660, 260, 205, 250));
        }
    }
}