using NUnit.Framework;
using TerrarianCompendium.Navigation;

namespace TerrarianCompendium.Tests.Navigation
{
    [TestFixture]
    public sealed class BrowserSelectionNavigationTests
    {
        [Test]
        public void ToggleItem_FromItemsRoot_SelectsItem()
        {
            var state = new BrowserNavigationState();

            bool changed = BrowserSelectionNavigation.ToggleItem(state, 123);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(123)));
            Assert.That(BrowserSelectionNavigation.IsItemSelected(state.CurrentDestination, 123), Is.True);
        }

        [Test]
        public void ToggleItem_SelectedItem_DeselectsToItemsRoot()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));

            bool changed = BrowserSelectionNavigation.ToggleItem(state, 123);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(BrowserSelectionNavigation.IsItemSelected(state.CurrentDestination, 123), Is.False);
        }

        [Test]
        public void ToggleItem_DifferentSelectedItem_SelectsClickedItem()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));

            BrowserSelectionNavigation.ToggleItem(state, 456);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(456)));
        }

        [Test]
        public void ToggleArmorSet_FromOtherSection_SelectsArmorSet()
        {
            var state = new BrowserNavigationState();

            bool changed = BrowserSelectionNavigation.ToggleArmorSet(state, 5);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForArmorSet(5)));
            Assert.That(BrowserSelectionNavigation.IsArmorSetSelected(state.CurrentDestination, 5), Is.True);
        }

        [Test]
        public void ToggleArmorSet_SelectedArmorSet_DeselectsToArmorSetsRoot()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForArmorSet(5));

            BrowserSelectionNavigation.ToggleArmorSet(state, 5);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.ArmorSets)));
            Assert.That(BrowserSelectionNavigation.IsArmorSetSelected(state.CurrentDestination, 5), Is.False);
        }

        [Test]
        public void ToggleArmorSet_DifferentSelectedArmorSet_SelectsClickedArmorSet()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForArmorSet(5));

            BrowserSelectionNavigation.ToggleArmorSet(state, 6);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForArmorSet(6)));
        }

        [Test]
        public void ToggleRecipeResult_FromRecipesRoot_SelectsRecipeQuery()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForSection(BrowserSection.Recipes));

            bool changed = BrowserSelectionNavigation.ToggleRecipeResult(state, 123);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForRecipeQuery(123)));
            Assert.That(BrowserSelectionNavigation.IsRecipeResultSelected(state.CurrentDestination, 123), Is.True);
        }

        [Test]
        public void ToggleRecipeResult_SelectedQuery_DeselectsToRecipesRoot()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipeQuery(123));

            BrowserSelectionNavigation.ToggleRecipeResult(state, 123);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Recipes)));
        }

        [Test]
        public void ToggleRecipeResult_ExactRecipeWithSameQuery_DeselectsToRecipesRoot()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7, 123));

            BrowserSelectionNavigation.ToggleRecipeResult(state, 123);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Recipes)));
        }

        [Test]
        public void ToggleRecipeResult_ExactRecipeWithDifferentQuery_SelectsClickedQuery()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7, 123));

            BrowserSelectionNavigation.ToggleRecipeResult(state, 456);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForRecipeQuery(456)));
        }

        [Test]
        public void ToggleRecipeResult_ExactRecipeWithoutQuery_SelectsClickedQuery()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7));

            BrowserSelectionNavigation.ToggleRecipeResult(state, 123);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForRecipeQuery(123)));
        }

        [Test]
        public void IsRecipeResultSelected_ExactRecipeWithQuery_UsesQueryItemIdentity()
        {
            var destination = BrowserDestination.ForRecipe(7, 123);

            Assert.That(BrowserSelectionNavigation.IsRecipeResultSelected(destination, 123), Is.True);
            Assert.That(BrowserSelectionNavigation.IsRecipeResultSelected(destination, 456), Is.False);
        }

        [TestCase(25)]
        [TestCase(0)]
        [TestCase(-25)]
        public void ToggleNpc_FromOtherSection_SelectsNpc(int npcNetId)
        {
            var state = new BrowserNavigationState();

            bool changed = BrowserSelectionNavigation.ToggleNpc(state, npcNetId);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForNpc(npcNetId)));
            Assert.That(BrowserSelectionNavigation.IsNpcSelected(state.CurrentDestination, npcNetId), Is.True);
        }

        [TestCase(25)]
        [TestCase(0)]
        [TestCase(-25)]
        public void ToggleNpc_SelectedNpc_DeselectsToBestiaryRoot(int npcNetId)
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForNpc(npcNetId));

            BrowserSelectionNavigation.ToggleNpc(state, npcNetId);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Bestiary)));
            Assert.That(BrowserSelectionNavigation.IsNpcSelected(state.CurrentDestination, npcNetId), Is.False);
        }

        [Test]
        public void ToggleNpc_DifferentSelectedNpc_SelectsClickedNpc()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForNpc(25));

            BrowserSelectionNavigation.ToggleNpc(state, 26);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForNpc(26)));
        }

        [Test]
        public void ToggleSelection_MatchingNumericIdInDifferentDomain_SelectsClickedDomainInsteadOfDeselecting()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForArmorSet(123));

            BrowserSelectionNavigation.ToggleItem(state, 123);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(123)));
        }

        [Test]
        public void ToggleItem_SelectThenDeselect_PreservesNormalBackForwardHistory()
        {
            var state = new BrowserNavigationState();

            BrowserSelectionNavigation.ToggleItem(state, 123);
            BrowserSelectionNavigation.ToggleItem(state, 123);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CanGoBack, Is.True);

            bool wentBack = state.GoBack();

            Assert.That(wentBack, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(123)));
            Assert.That(state.CanGoForward, Is.True);

            bool wentForward = state.GoForward();

            Assert.That(wentForward, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
        }

        [Test]
        public void ToggleRecipeResult_SelectThenDeselect_PreservesNormalBackForwardHistory()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForSection(BrowserSection.Recipes));

            BrowserSelectionNavigation.ToggleRecipeResult(state, 123);
            BrowserSelectionNavigation.ToggleRecipeResult(state, 123);

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Recipes)));

            state.GoBack();

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForRecipeQuery(123)));

            state.GoForward();

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Recipes)));
        }
    }
}