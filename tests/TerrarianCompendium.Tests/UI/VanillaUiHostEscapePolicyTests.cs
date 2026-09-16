using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class VanillaUiHostEscapePolicyTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ResolveEscapeAction_WhenWritingText_DoesNothing(bool hasOpenTransientSurface)
        {
            BrowserEscapeAction result = VanillaUiHost.ResolveEscapeAction(
                isWritingText: true,
                hasOpenTransientSurface: hasOpenTransientSurface);

            Assert.That(result, Is.EqualTo(BrowserEscapeAction.None));
        }

        [Test]
        public void ResolveEscapeAction_WithOpenTransientSurface_ClosesTransientSurface()
        {
            BrowserEscapeAction result = VanillaUiHost.ResolveEscapeAction(
                isWritingText: false,
                hasOpenTransientSurface: true);

            Assert.That(result, Is.EqualTo(BrowserEscapeAction.CloseTransientSurface));
        }

        [Test]
        public void ResolveEscapeAction_WithoutTextOrTransientSurface_ClosesBrowser()
        {
            BrowserEscapeAction result = VanillaUiHost.ResolveEscapeAction(
                isWritingText: false,
                hasOpenTransientSurface: false);

            Assert.That(result, Is.EqualTo(BrowserEscapeAction.CloseBrowser));
        }
    }
}