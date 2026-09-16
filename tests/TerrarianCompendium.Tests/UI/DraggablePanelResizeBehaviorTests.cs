using NUnit.Framework;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class DraggablePanelResizeBehaviorTests
    {
        [Test]
        public void ClampDimension_WithValueInsideBounds_ReturnsValue()
        {
            int result = DraggablePanelResizeBehavior.ClampDimension(900, 800, 1200);

            Assert.That(result, Is.EqualTo(900));
        }

        [Test]
        public void ClampDimension_WithValueBelowMinimum_ReturnsMinimum()
        {
            int result = DraggablePanelResizeBehavior.ClampDimension(700, 800, 1200);

            Assert.That(result, Is.EqualTo(800));
        }

        [Test]
        public void ClampDimension_WithValueAboveScreen_ReturnsScreenSize()
        {
            int result = DraggablePanelResizeBehavior.ClampDimension(1400, 800, 1200);

            Assert.That(result, Is.EqualTo(1200));
        }

        [Test]
        public void ClampDimension_WithScreenBelowMinimum_ReturnsScreenSize()
        {
            int result = DraggablePanelResizeBehavior.ClampDimension(800, 800, 640);

            Assert.That(result, Is.EqualTo(640));
        }

        [TestCase(800, 800)]
        [TestCase(1200, 1200)]
        public void ClampDimension_WithExactBoundary_ReturnsBoundary(int value, int expected)
        {
            int result = DraggablePanelResizeBehavior.ClampDimension(value, 800, 1200);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TryUpdate_AfterBegin_AppliesPointerDelta()
        {
            var behavior = new DraggablePanelResizeBehavior(800, 540);
            behavior.Begin(100, 200, 900, 600);

            bool updated = behavior.TryUpdate(150, 240, true, false, 1400, 900, out int width, out int height);

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(width, Is.EqualTo(950));
                Assert.That(height, Is.EqualTo(640));
                Assert.That(behavior.IsResizing, Is.True);
            });
        }

        [Test]
        public void TryUpdate_WhenRequestedSizeIsBelowMinimum_ClampsToMinimum()
        {
            var behavior = new DraggablePanelResizeBehavior(800, 540);
            behavior.Begin(200, 200, 900, 600);

            bool updated = behavior.TryUpdate(0, 0, true, false, 1400, 900, out int width, out int height);

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(width, Is.EqualTo(800));
                Assert.That(height, Is.EqualTo(540));
            });
        }

        [Test]
        public void TryUpdate_WhenRequestedSizeExceedsViewport_ClampsToViewport()
        {
            var behavior = new DraggablePanelResizeBehavior(800, 540);
            behavior.Begin(100, 100, 900, 600);

            bool updated = behavior.TryUpdate(900, 900, true, false, 1200, 700, out int width, out int height);

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(width, Is.EqualTo(1200));
                Assert.That(height, Is.EqualTo(700));
            });
        }

        [Test]
        public void TryUpdate_WhenPointerReleased_CancelsResize()
        {
            var behavior = new DraggablePanelResizeBehavior(800, 540);
            behavior.Begin(100, 100, 900, 600);

            bool updated = behavior.TryUpdate(120, 120, false, false, 1200, 800, out _, out _);

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.False);
                Assert.That(behavior.IsResizing, Is.False);
            });
        }

        [Test]
        public void TryUpdate_WhenExternalInputIsBlocked_CancelsResize()
        {
            var behavior = new DraggablePanelResizeBehavior(800, 540);
            behavior.Begin(100, 100, 900, 600);

            bool updated = behavior.TryUpdate(120, 120, true, true, 1200, 800, out _, out _);

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.False);
                Assert.That(behavior.IsResizing, Is.False);
            });
        }

        [Test]
        public void Cancel_WhenResizing_StopsResize()
        {
            var behavior = new DraggablePanelResizeBehavior(800, 540);
            behavior.Begin(100, 100, 900, 600);

            behavior.Cancel();

            Assert.That(behavior.IsResizing, Is.False);
        }
    }
}