using NUnit.Framework;
using TerrarianCompendium.Navigation;

namespace TerrarianCompendium.Tests.Navigation
{
    [TestFixture]
    public sealed class InventoryItemNavigationTargetTests
    {
        [TestCase(false, true, true, false, false, (int)InventoryItemNavigationTarget.Items)]
        [TestCase(false, true, true, true, false, (int)InventoryItemNavigationTarget.Recipes)]
        [TestCase(true, true, true, false, false, (int)InventoryItemNavigationTarget.Items)]
        public void ResolveTarget_AcceptedRightClickChords_SelectOnlyOneDestination(
            bool leftClick,
            bool rightClick,
            bool altHeld,
            bool ctrlHeld,
            bool shiftHeld,
            int expectedTarget)
        {
            InventoryItemNavigationTarget target = InventoryItemNavigationHandler.ResolveTarget(
                leftClick,
                rightClick,
                altHeld,
                ctrlHeld,
                shiftHeld);

            Assert.That(target, Is.EqualTo((InventoryItemNavigationTarget)expectedTarget));
        }

        [TestCase(false, false, false, false, false)]
        [TestCase(true, false, false, false, false)]
        [TestCase(false, true, false, false, false)]
        [TestCase(false, true, false, true, false)]
        [TestCase(true, false, true, false, false)]
        [TestCase(true, false, true, true, false)]
        [TestCase(false, true, true, false, true)]
        [TestCase(false, true, true, true, true)]
        [TestCase(true, true, true, true, false)]
        [TestCase(false, false, true, true, false)]
        public void ResolveTarget_UnacceptedGesturesRemainVanillaOwned(
            bool leftClick,
            bool rightClick,
            bool altHeld,
            bool ctrlHeld,
            bool shiftHeld)
        {
            InventoryItemNavigationTarget target = InventoryItemNavigationHandler.ResolveTarget(
                leftClick,
                rightClick,
                altHeld,
                ctrlHeld,
                shiftHeld);

            Assert.That(target, Is.EqualTo(InventoryItemNavigationTarget.None));
        }
    }
}