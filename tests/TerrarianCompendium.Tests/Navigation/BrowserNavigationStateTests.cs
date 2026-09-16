using System;
using NUnit.Framework;
using TerrarianCompendium.Navigation;

namespace TerrarianCompendium.Tests.Navigation
{
    [TestFixture]
    public sealed class BrowserNavigationStateTests
    {
        [Test]
        public void NewState_StartsAtItemsRootWithZeroRevision()
        {
            var state = new BrowserNavigationState();

            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CurrentDestination.IsSectionRoot, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Items));
            Assert.That(state.CanGoBack, Is.False);
            Assert.That(state.CanGoForward, Is.False);
            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void Navigate_ToRecipesRoot_UpdatesDestination()
        {
            AssertSectionRootNavigation(BrowserSection.Recipes, expectedChanged: true);
        }

        [Test]
        public void Navigate_ToBestiaryRoot_UpdatesDestination()
        {
            AssertSectionRootNavigation(BrowserSection.Bestiary, expectedChanged: true);
        }

        [Test]
        public void Navigate_ToInitialItemsRoot_DoesNotChangeDestination()
        {
            AssertSectionRootNavigation(BrowserSection.Items, expectedChanged: false);
        }

        [Test]
        public void Navigate_ToItem_StoresItemDestinationInItemsSection()
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForItem(123));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsItem, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Items));
            Assert.That(state.CurrentDestination.ItemId, Is.EqualTo(123));
        }

        [Test]
        public void Navigate_ToRecipe_StoresRecipeDestinationInRecipesSection()
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForRecipe(0));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsRecipe, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.Zero);
        }

        [Test]
        public void Navigate_ToRecipeQuery_StoresQueryDestinationInRecipesSection()
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForRecipeQuery(123));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsRecipeQuery, Is.True);
            Assert.That(state.CurrentDestination.HasRecipeQuery, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(123));
        }

        [Test]
        public void Navigate_ToRecipeWithQuery_StoresRecipeAndQueryContext()
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForRecipe(7, 123));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsRecipe, Is.True);
            Assert.That(state.CurrentDestination.HasRecipeQuery, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(7));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(123));
        }

        [Test]
        public void Navigate_FromRecipesRootToItem_ChangesEffectiveSectionToItems()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForSection(BrowserSection.Recipes));

            state.Navigate(BrowserDestination.ForItem(123));

            Assert.That(state.CurrentDestination.IsItem, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Items));
        }

        [Test]
        public void Navigate_FromItemToRecipe_ChangesEffectiveSectionToRecipes()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));

            state.Navigate(BrowserDestination.ForRecipe(7));

            Assert.That(state.CurrentDestination.IsRecipe, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(7));
        }

        [Test]
        public void Navigate_FromItemToRecipeQuery_ChangesEffectiveSectionToRecipes()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));

            state.Navigate(BrowserDestination.ForRecipeQuery(123));

            Assert.That(state.CurrentDestination.IsRecipeQuery, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
        }

        [Test]
        public void Navigate_FromRecipeQueryToRecipeWithSameContext_RemainsInRecipesSection()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipeQuery(123));

            state.Navigate(BrowserDestination.ForRecipe(7, 123));

            Assert.That(state.CurrentDestination.IsRecipe, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(123));
        }

        [Test]
        public void Navigate_FromRecipeWithQueryToItem_ChangesEffectiveSectionToItems()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7, 123));

            state.Navigate(BrowserDestination.ForItem(456));

            Assert.That(state.CurrentDestination.IsItem, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Items));
            Assert.That(state.CurrentDestination.ItemId, Is.EqualTo(456));
        }

        [Test]
        public void Navigate_FromItemToItemsRoot_ClearsItemDestination()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));

            bool changed = state.Navigate(BrowserDestination.ForSection(BrowserSection.Items));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsSectionRoot, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Items));
            Assert.That(state.CurrentDestination.ItemId, Is.Zero);
        }

        [Test]
        public void Navigate_FromRecipeToRecipesRoot_ClearsRecipeDestination()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7));

            bool changed = state.Navigate(BrowserDestination.ForSection(BrowserSection.Recipes));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsSectionRoot, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.Zero);
        }

        [Test]
        public void Navigate_ToSameDestination_DoesNotIncrementRevision()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForItem(123);
            state.Navigate(destination);
            long revision = state.Revision;

            bool changed = state.Navigate(destination);

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void Navigate_ToSameRecipe_DoesNotIncrementRevision()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForRecipe(7);
            state.Navigate(destination);
            long revision = state.Revision;

            bool changed = state.Navigate(destination);

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void Navigate_ToSameRecipeQuery_DoesNotIncrementRevision()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForRecipeQuery(123);
            state.Navigate(destination);
            long revision = state.Revision;

            bool changed = state.Navigate(destination);

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void Navigate_ToRecipeQueryWithDifferentItem_IncrementsRevisionOnce()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipeQuery(123));
            long revision = state.Revision;

            bool changed = state.Navigate(BrowserDestination.ForRecipeQuery(456));

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(456));
        }

        [Test]
        public void Navigate_ToSameRecipeWithDifferentQueryItem_IncrementsRevisionOnce()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7, 123));
            long revision = state.Revision;

            bool changed = state.Navigate(BrowserDestination.ForRecipe(7, 456));

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(7));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(456));
        }

        [Test]
        public void Navigate_ToSameRecipeWithSameQueryItem_DoesNotIncrementRevision()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForRecipe(7, 123);
            state.Navigate(destination);
            long revision = state.Revision;

            bool changed = state.Navigate(destination);

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void Navigate_ToDifferentItem_IncrementsRevisionOnce()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));
            long revision = state.Revision;

            bool changed = state.Navigate(BrowserDestination.ForItem(456));

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
            Assert.That(state.CurrentDestination.ItemId, Is.EqualTo(456));
        }

        [Test]
        public void Navigate_ToDifferentRecipe_IncrementsRevisionOnce()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForRecipe(7));
            long revision = state.Revision;

            bool changed = state.Navigate(BrowserDestination.ForRecipe(8));

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(8));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ForItem_WithNonPositiveItemId_Throws(int itemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BrowserDestination.ForItem(itemId)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ForRecipeQuery_WithNonPositiveItemId_Throws(int itemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BrowserDestination.ForRecipeQuery(itemId)));
        }

        [Test]
        public void ForRecipe_WithZeroRuntimeIndex_IsValid()
        {
            var destination = BrowserDestination.ForRecipe(0);

            Assert.That(destination.IsRecipe, Is.True);
            Assert.That(destination.RecipeRuntimeIndex, Is.Zero);
        }

        [Test]
        public void ForRecipe_WithoutQuery_DoesNotHaveRecipeQueryContext()
        {
            var destination = BrowserDestination.ForRecipe(7);

            Assert.That(destination.IsRecipe, Is.True);
            Assert.That(destination.HasRecipeQuery, Is.False);
            Assert.That(destination.RecipeQueryItemId, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ForRecipeWithQuery_WithNonPositiveQueryItemId_Throws(int itemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BrowserDestination.ForRecipe(7, itemId)));
        }

        [Test]
        public void ForRecipe_WithNegativeRuntimeIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BrowserDestination.ForRecipe(-1)));
        }

        [Test]
        public void ForRecipeWithQuery_WithNegativeRuntimeIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BrowserDestination.ForRecipe(-1, 123)));
        }

        [Test]
        public void ForSection_WithUnsupportedSection_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => BrowserDestination.ForSection((BrowserSection)999)));
        }

        [Test]
        public void DestinationEquality_UsesKindSectionEntityAndRecipeQueryIdentity()
        {
            var itemsRootA = BrowserDestination.ForSection(BrowserSection.Items);
            var itemsRootB = BrowserDestination.ForSection(BrowserSection.Items);
            var recipesRoot = BrowserDestination.ForSection(BrowserSection.Recipes);
            var itemA = BrowserDestination.ForItem(123);
            var itemB = BrowserDestination.ForItem(123);
            var otherItem = BrowserDestination.ForItem(456);
            var recipeA = BrowserDestination.ForRecipe(7);
            var recipeB = BrowserDestination.ForRecipe(7);
            var otherRecipe = BrowserDestination.ForRecipe(8);
            var queryA = BrowserDestination.ForRecipeQuery(123);
            var queryB = BrowserDestination.ForRecipeQuery(123);
            var queryOtherItem = BrowserDestination.ForRecipeQuery(456);
            var recipeWithQueryA = BrowserDestination.ForRecipe(7, 123);
            var recipeWithQueryB = BrowserDestination.ForRecipe(7, 123);
            var recipeWithOtherQueryItem = BrowserDestination.ForRecipe(7, 456);

            Assert.That(itemsRootA, Is.EqualTo(itemsRootB));
            Assert.That(itemsRootA == itemsRootB, Is.True);
            Assert.That(itemsRootA, Is.Not.EqualTo(recipesRoot));
            Assert.That(itemsRootA, Is.Not.EqualTo(itemA));
            Assert.That(itemA, Is.EqualTo(itemB));
            Assert.That(itemA == itemB, Is.True);
            Assert.That(itemA != otherItem, Is.True);
            Assert.That(recipeA, Is.EqualTo(recipeB));
            Assert.That(recipeA == recipeB, Is.True);
            Assert.That(recipeA != otherRecipe, Is.True);
            Assert.That(recipeA, Is.Not.EqualTo(recipesRoot));
            Assert.That(recipeA, Is.Not.EqualTo(itemA));
            Assert.That(queryA, Is.EqualTo(queryB));
            Assert.That(queryA == queryB, Is.True);
            Assert.That(queryA, Is.Not.EqualTo(queryOtherItem));
            Assert.That(queryA, Is.Not.EqualTo(recipesRoot));
            Assert.That(recipeWithQueryA, Is.EqualTo(recipeWithQueryB));
            Assert.That(recipeWithQueryA == recipeWithQueryB, Is.True);
            Assert.That(recipeWithQueryA, Is.Not.EqualTo(recipeA));
            Assert.That(recipeWithQueryA, Is.Not.EqualTo(recipeWithOtherQueryItem));
        }

        [Test]
        public void EqualDestinations_HaveEqualHashCodes()
        {
            var itemA = BrowserDestination.ForItem(123);
            var itemB = BrowserDestination.ForItem(123);
            var recipeA = BrowserDestination.ForRecipe(7);
            var recipeB = BrowserDestination.ForRecipe(7);
            var queryA = BrowserDestination.ForRecipeQuery(123);
            var queryB = BrowserDestination.ForRecipeQuery(123);
            var recipeWithQueryA = BrowserDestination.ForRecipe(7, 123);
            var recipeWithQueryB = BrowserDestination.ForRecipe(7, 123);

            Assert.That(itemA.GetHashCode(), Is.EqualTo(itemB.GetHashCode()));
            Assert.That(recipeA.GetHashCode(), Is.EqualTo(recipeB.GetHashCode()));
            Assert.That(queryA.GetHashCode(), Is.EqualTo(queryB.GetHashCode()));
            Assert.That(recipeWithQueryA.GetHashCode(), Is.EqualTo(recipeWithQueryB.GetHashCode()));
        }

        [Test]
        public void Navigate_ToNewDestination_EnablesBackAndKeepsForwardUnavailable()
        {
            var state = new BrowserNavigationState();

            state.Navigate(BrowserDestination.ForItem(123));

            Assert.That(state.CanGoBack, Is.True);
            Assert.That(state.CanGoForward, Is.False);
        }

        [Test]
        public void GoBack_MovesToPreviousDestinationAndEnablesForward()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));
            state.Navigate(BrowserDestination.ForItem(456));
            long revision = state.Revision;

            bool changed = state.GoBack();

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(123)));
            Assert.That(state.CanGoBack, Is.True);
            Assert.That(state.CanGoForward, Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void GoForward_MovesToNextDestinationAndDisablesForwardAtHistoryEnd()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));
            state.Navigate(BrowserDestination.ForItem(456));
            state.GoBack();
            long revision = state.Revision;

            bool changed = state.GoForward();

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(456)));
            Assert.That(state.CanGoBack, Is.True);
            Assert.That(state.CanGoForward, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void GoBack_AtHistoryStart_DoesNotChangeStateOrRevision()
        {
            var state = new BrowserNavigationState();
            long revision = state.Revision;

            bool changed = state.GoBack();

            Assert.That(changed, Is.False);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CanGoBack, Is.False);
            Assert.That(state.CanGoForward, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void GoForward_AtHistoryEnd_DoesNotChangeStateOrRevision()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));
            long revision = state.Revision;

            bool changed = state.GoForward();

            Assert.That(changed, Is.False);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForItem(123)));
            Assert.That(state.CanGoForward, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void Navigate_ToSameDestination_DoesNotCreateDuplicateHistoryEntry()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForItem(123);
            state.Navigate(destination);

            state.Navigate(destination);
            bool changed = state.GoBack();

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CanGoBack, Is.False);
        }

        [Test]
        public void Navigate_AfterBack_ReplacesForwardBranch()
        {
            var state = new BrowserNavigationState();
            var destinationA = BrowserDestination.ForItem(123);
            var destinationB = BrowserDestination.ForItem(456);
            var destinationC = BrowserDestination.ForItem(789);
            var replacement = BrowserDestination.ForRecipeQuery(123);
            state.Navigate(destinationA);
            state.Navigate(destinationB);
            state.Navigate(destinationC);
            state.GoBack();

            bool changed = state.Navigate(replacement);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(replacement));
            Assert.That(state.CanGoBack, Is.True);
            Assert.That(state.CanGoForward, Is.False);
            Assert.That(state.GoForward(), Is.False);
        }

        [Test]
        public void Navigate_ToCurrentDestinationAfterBack_PreservesForwardBranch()
        {
            var state = new BrowserNavigationState();
            var destinationA = BrowserDestination.ForItem(123);
            var destinationB = BrowserDestination.ForItem(456);
            var destinationC = BrowserDestination.ForItem(789);
            state.Navigate(destinationA);
            state.Navigate(destinationB);
            state.Navigate(destinationC);
            state.GoBack();
            long revision = state.Revision;

            bool navigateChanged = state.Navigate(destinationB);
            bool forwardChanged = state.GoForward();

            Assert.That(navigateChanged, Is.False);
            Assert.That(forwardChanged, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(destinationC));
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void History_RestoresExactCrossDomainRecipeQueryPath()
        {
            var state = new BrowserNavigationState();
            var itemA = BrowserDestination.ForItem(100);
            var query = BrowserDestination.ForRecipeQuery(100);
            var recipe = BrowserDestination.ForRecipe(7, 100);
            var itemB = BrowserDestination.ForItem(200);
            state.Navigate(itemA);
            state.Navigate(query);
            state.Navigate(recipe);
            state.Navigate(itemB);

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(recipe));
            Assert.That(state.CurrentDestination.IsRecipe, Is.True);
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(7));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(100));
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Recipes));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(query));
            Assert.That(state.CurrentDestination.IsRecipeQuery, Is.True);
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(100));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(itemA));
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Items));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CanGoBack, Is.False);
            Assert.That(state.CanGoForward, Is.True);
        }

        [Test]
        public void History_DistinguishesSameRecipeWithDifferentQueryItemContexts()
        {
            var state = new BrowserNavigationState();
            var first = BrowserDestination.ForRecipe(7, 123);
            var second = BrowserDestination.ForRecipe(7, 456);
            state.Navigate(first);
            state.Navigate(second);

            bool changed = state.GoBack();

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(first));
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(7));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(123));
        }

        [Test]
        public void History_RestoresRecipeQueryAndRecipesRootAfterDeselectionNavigation()
        {
            var state = new BrowserNavigationState();
            var query = BrowserDestination.ForRecipeQuery(123);
            var root = BrowserDestination.ForSection(BrowserSection.Recipes);
            state.Navigate(query);
            state.Navigate(root);

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(query));
            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(root));
        }

        [Test]
        public void SuccessfulNavigateBackAndForward_IncrementRevisionExactlyOnceEach()
        {
            var state = new BrowserNavigationState();

            Assert.That(state.Navigate(BrowserDestination.ForItem(123)), Is.True);
            Assert.That(state.Revision, Is.EqualTo(1));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.Revision, Is.EqualTo(2));

            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.Revision, Is.EqualTo(3));
        }

        [Test]
        public void ReplaceCurrent_ReplacesDestinationWithoutPushingHistoryEntry()
        {
            var state = new BrowserNavigationState();
            var item = BrowserDestination.ForItem(123);
            var replacement = BrowserDestination.ForRecipeQuery(123);
            state.Navigate(item);

            bool changed = state.ReplaceCurrent(replacement);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(replacement));
            Assert.That(state.CanGoBack, Is.True);
            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CanGoBack, Is.False);
        }

        [Test]
        public void ReplaceCurrent_WithSameDestination_DoesNotChangeRevision()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForItem(123);
            state.Navigate(destination);
            long revision = state.Revision;

            bool changed = state.ReplaceCurrent(destination);

            Assert.That(changed, Is.False);
            Assert.That(state.CurrentDestination, Is.EqualTo(destination));
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void ReplaceCurrent_IncrementsRevisionExactlyOnce()
        {
            var state = new BrowserNavigationState();
            state.Navigate(BrowserDestination.ForItem(123));
            long revision = state.Revision;

            bool changed = state.ReplaceCurrent(BrowserDestination.ForSection(BrowserSection.Recipes));

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void ReplaceCurrent_FromRecipeQueryToSelectedVariant_PreservesVariantOnBack()
        {
            var state = new BrowserNavigationState();
            var recipesRoot = BrowserDestination.ForSection(BrowserSection.Recipes);
            var selectedVariant = BrowserDestination.ForRecipe(7, 123);
            state.Navigate(recipesRoot);
            state.Navigate(BrowserDestination.ForRecipeQuery(123));
            state.ReplaceCurrent(selectedVariant);
            state.Navigate(BrowserDestination.ForRecipeQuery(456));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(selectedVariant));
            Assert.That(state.CurrentDestination.IsRecipe, Is.True);
            Assert.That(state.CurrentDestination.HasRecipeQuery, Is.True);
            Assert.That(state.CurrentDestination.RecipeRuntimeIndex, Is.EqualTo(7));
            Assert.That(state.CurrentDestination.RecipeQueryItemId, Is.EqualTo(123));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(recipesRoot));
        }

        [Test]
        public void ReplaceCurrent_BetweenRecipeVariants_DoesNotCreateHistoryEntries()
        {
            var state = new BrowserNavigationState();
            var recipesRoot = BrowserDestination.ForSection(BrowserSection.Recipes);
            var finalVariant = BrowserDestination.ForRecipe(9, 123);
            state.Navigate(recipesRoot);
            state.Navigate(BrowserDestination.ForRecipeQuery(123));
            state.ReplaceCurrent(BrowserDestination.ForRecipe(7, 123));
            state.ReplaceCurrent(BrowserDestination.ForRecipe(8, 123));
            state.ReplaceCurrent(finalVariant);
            state.Navigate(BrowserDestination.ForRecipeQuery(456));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(finalVariant));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(recipesRoot));
        }

        [Test]
        public void ReplaceCurrent_SelectedRecipeVariant_PreservesForwardNavigation()
        {
            var state = new BrowserNavigationState();
            var selectedVariant = BrowserDestination.ForRecipe(7, 123);
            var nextDestination = BrowserDestination.ForRecipeQuery(456);
            state.Navigate(BrowserDestination.ForRecipeQuery(123));
            state.ReplaceCurrent(selectedVariant);
            state.Navigate(nextDestination);

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(selectedVariant));
            Assert.That(state.CanGoForward, Is.True);

            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(nextDestination));
            Assert.That(state.CanGoForward, Is.False);
        }

        [Test]
        public void ReplaceCurrent_PreservesNonDuplicateForwardBranch()
        {
            var state = new BrowserNavigationState();
            var destinationA = BrowserDestination.ForItem(123);
            var destinationB = BrowserDestination.ForItem(456);
            var destinationC = BrowserDestination.ForItem(789);
            var replacement = BrowserDestination.ForRecipeQuery(123);
            state.Navigate(destinationA);
            state.Navigate(destinationB);
            state.Navigate(destinationC);
            state.GoBack();

            bool changed = state.ReplaceCurrent(replacement);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(replacement));
            Assert.That(state.CanGoForward, Is.True);
            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(destinationC));
        }

        [Test]
        public void ReplaceCurrent_CoalescesIdenticalNeighboringDestinations()
        {
            var state = new BrowserNavigationState();
            var destinationA = BrowserDestination.ForItem(123);
            var destinationB = BrowserDestination.ForItem(456);
            state.Navigate(destinationA);
            state.Navigate(destinationB);
            state.Navigate(destinationA);
            state.GoBack();

            bool changed = state.ReplaceCurrent(destinationA);

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(destinationA));
            Assert.That(state.CanGoForward, Is.False);
            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(BrowserSection.Items)));
            Assert.That(state.CanGoBack, Is.False);
        }

        [TestCase(123)]
        [TestCase(0)]
        [TestCase(-65)]
        public void Navigate_ToNpc_StoresNpcDestinationInBestiarySection(int npcNetId)
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForNpc(npcNetId));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsNpc, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Bestiary));
            Assert.That(state.CurrentDestination.NpcNetId, Is.EqualTo(npcNetId));
        }

        [Test]
        public void NpcDestinations_WithDifferentNetIds_AreNotEqual()
        {
            var first = BrowserDestination.ForNpc(-1);
            var second = BrowserDestination.ForNpc(0);

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void History_RestoresNpcDestinationThroughBackAndForward()
        {
            var state = new BrowserNavigationState();
            var npc = BrowserDestination.ForNpc(-65);
            var item = BrowserDestination.ForItem(123);
            state.Navigate(npc);
            state.Navigate(item);

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(npc));
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.Bestiary));

            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(item));
        }

        [Test]
        public void Navigate_ToArmorSetsRoot_UpdatesDestination()
        {
            AssertSectionRootNavigation(BrowserSection.ArmorSets, expectedChanged: true);
        }

        [Test]
        public void Navigate_ToArmorSet_StoresArmorSetDestinationInArmorSetsSection()
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForArmorSet(7));

            Assert.That(changed, Is.True);
            Assert.That(state.CurrentDestination.IsArmorSet, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.ArmorSets));
            Assert.That(state.CurrentDestination.ArmorSetId, Is.EqualTo(7));
        }

        [Test]
        public void Navigate_ToSameArmorSet_DoesNotIncrementRevision()
        {
            var state = new BrowserNavigationState();
            var destination = BrowserDestination.ForArmorSet(7);
            state.Navigate(destination);
            long revision = state.Revision;

            bool changed = state.Navigate(destination);

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void History_ItemArmorSetItem_RestoresCrossDomainPath()
        {
            var state = new BrowserNavigationState();
            var firstItem = BrowserDestination.ForItem(10);
            var armorSet = BrowserDestination.ForArmorSet(3);
            var secondItem = BrowserDestination.ForItem(20);
            state.Navigate(firstItem);
            state.Navigate(armorSet);
            state.Navigate(secondItem);

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(armorSet));
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(BrowserSection.ArmorSets));

            Assert.That(state.GoBack(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(firstItem));

            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(armorSet));

            Assert.That(state.GoForward(), Is.True);
            Assert.That(state.CurrentDestination, Is.EqualTo(secondItem));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ForArmorSet_WithNonPositiveId_Throws(int armorSetId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BrowserDestination.ForArmorSet(armorSetId));
        }

        private static void AssertSectionRootNavigation(BrowserSection section, bool expectedChanged)
        {
            var state = new BrowserNavigationState();

            bool changed = state.Navigate(BrowserDestination.ForSection(section));

            Assert.That(changed, Is.EqualTo(expectedChanged));
            Assert.That(state.CurrentDestination, Is.EqualTo(BrowserDestination.ForSection(section)));
            Assert.That(state.CurrentDestination.IsSectionRoot, Is.True);
            Assert.That(state.CurrentDestination.Section, Is.EqualTo(section));
        }
    }
}