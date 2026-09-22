using NUnit.Framework;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class ItemCategoryNavigationViewTests
    {
        [Test]
        public void ResolveUpAction_WithParentAndNoModifier_ReturnsParent()
        {
            ItemCategoryNavigationUpAction action = ItemCategoryNavigationDecision.ResolveUpAction(
                hasParentTarget: true,
                hasContextItem: true,
                hasRootAction: true,
                rootModifierDown: false);

            Assert.That(action, Is.EqualTo(ItemCategoryNavigationUpAction.Parent));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResolveUpAction_WithParentAndRootModifier_ReturnsRoot(bool hasContextItem)
        {
            ItemCategoryNavigationUpAction action = ItemCategoryNavigationDecision.ResolveUpAction(
                hasParentTarget: true,
                hasContextItem: hasContextItem,
                hasRootAction: true,
                rootModifierDown: true);

            Assert.That(action, Is.EqualTo(ItemCategoryNavigationUpAction.Root));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResolveUpAction_AtCategoryRootWithContext_ReturnsRoot(bool rootModifierDown)
        {
            ItemCategoryNavigationUpAction action = ItemCategoryNavigationDecision.ResolveUpAction(
                hasParentTarget: false,
                hasContextItem: true,
                hasRootAction: true,
                rootModifierDown: rootModifierDown);

            Assert.That(action, Is.EqualTo(ItemCategoryNavigationUpAction.Root));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResolveUpAction_AtGlobalRootWithoutContext_ReturnsNone(bool rootModifierDown)
        {
            ItemCategoryNavigationUpAction action = ItemCategoryNavigationDecision.ResolveUpAction(
                hasParentTarget: false,
                hasContextItem: false,
                hasRootAction: true,
                rootModifierDown: rootModifierDown);

            Assert.That(action, Is.EqualTo(ItemCategoryNavigationUpAction.None));
        }

        [Test]
        public void ResolveUpAction_WithoutRootAction_PreservesParentOnlySemantics()
        {
            ItemCategoryNavigationUpAction parentAction = ItemCategoryNavigationDecision.ResolveUpAction(
                hasParentTarget: true,
                hasContextItem: false,
                hasRootAction: false,
                rootModifierDown: true);
            ItemCategoryNavigationUpAction contextOnlyAction = ItemCategoryNavigationDecision.ResolveUpAction(
                hasParentTarget: false,
                hasContextItem: true,
                hasRootAction: false,
                rootModifierDown: true);

            Assert.Multiple(() =>
            {
                Assert.That(parentAction, Is.EqualTo(ItemCategoryNavigationUpAction.Parent));
                Assert.That(contextOnlyAction, Is.EqualTo(ItemCategoryNavigationUpAction.None));
            });
        }
    }
}