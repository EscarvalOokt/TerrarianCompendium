using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Crafting
{
    [TestFixture]
    public sealed class ItemCraftingAvailabilityTests
    {
        [Test]
        public void Queries_WithInvalidItemId_Throw()
        {
            CreateContext(
                [CreateRecipe(0, 100)],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => ItemCraftingAvailability.IsCraftableNow(0, recipeIndex, availabilityState)));
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => ItemCraftingAvailability.GetCraftableRecipes(0, recipeIndex, availabilityState)));
        }

        [Test]
        public void Queries_WithNullDependencies_Throw()
        {
            CreateContext(
                [CreateRecipe(0, 100)],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);

            Assert.Throws<ArgumentNullException>(
                (Action)(() => ItemCraftingAvailability.IsCraftableNow(100, null, availabilityState)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => ItemCraftingAvailability.IsCraftableNow(100, recipeIndex, null)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => ItemCraftingAvailability.GetCraftableRecipes(100, null, availabilityState)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => ItemCraftingAvailability.GetCraftableRecipes(100, recipeIndex, null)));
        }

        [Test]
        public void GetCraftableRecipes_WhenItemHasNoProducingRecipes_ReturnsEmpty()
        {
            CreateContext(
                [CreateRecipe(0, 100)],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);
            availabilityState.ReplaceSnapshot([0]);

            IReadOnlyList<RecipeCatalogEntry> recipes =
                ItemCraftingAvailability.GetCraftableRecipes(200, recipeIndex, availabilityState);

            Assert.That(recipes, Is.Empty);
            Assert.That(ItemCraftingAvailability.IsCraftableNow(200, recipeIndex, availabilityState), Is.False);
        }

        [Test]
        public void GetCraftableRecipes_WhenProducingRecipesAreUnavailable_ReturnsEmpty()
        {
            CreateContext(
                [CreateRecipe(0, 100), CreateRecipe(1, 100)],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);

            IReadOnlyList<RecipeCatalogEntry> recipes =
                ItemCraftingAvailability.GetCraftableRecipes(100, recipeIndex, availabilityState);

            Assert.That(recipes, Is.Empty);
            Assert.That(ItemCraftingAvailability.IsCraftableNow(100, recipeIndex, availabilityState), Is.False);
        }

        [Test]
        public void GetCraftableRecipes_ReturnsOnlyAvailableProducingRecipes()
        {
            RecipeCatalogEntry first = CreateRecipe(0, 100);
            RecipeCatalogEntry second = CreateRecipe(1, 100);
            RecipeCatalogEntry unrelated = CreateRecipe(2, 200);
            CreateContext(
                [first, second, unrelated],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);
            availabilityState.ReplaceSnapshot([1, 2]);

            IReadOnlyList<RecipeCatalogEntry> recipes =
                ItemCraftingAvailability.GetCraftableRecipes(100, recipeIndex, availabilityState);

            Assert.That(recipes, Is.EqualTo([second]));
            Assert.That(ItemCraftingAvailability.IsCraftableNow(100, recipeIndex, availabilityState), Is.True);
        }

        [Test]
        public void GetCraftableRecipes_PreservesRecipeIndexOrder()
        {
            RecipeCatalogEntry third = CreateRecipe(7, 100);
            RecipeCatalogEntry first = CreateRecipe(1, 100);
            RecipeCatalogEntry second = CreateRecipe(4, 100);
            CreateContext(
                [third, first, second],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);
            availabilityState.ReplaceSnapshot([7, 4, 1]);

            IReadOnlyList<RecipeCatalogEntry> recipes =
                ItemCraftingAvailability.GetCraftableRecipes(100, recipeIndex, availabilityState);

            Assert.That(GetRuntimeIndices(recipes), Is.EqualTo([1, 4, 7]));
        }

        [Test]
        public void GetCraftableRecipes_ExcludesAvailableRecipesForOtherResults()
        {
            RecipeCatalogEntry target = CreateRecipe(0, 100);
            RecipeCatalogEntry unrelated = CreateRecipe(1, 200);
            CreateContext(
                [target, unrelated],
                out RecipeIndex recipeIndex,
                out CraftingAvailabilityState availabilityState);
            availabilityState.ReplaceSnapshot([1]);

            IReadOnlyList<RecipeCatalogEntry> recipes =
                ItemCraftingAvailability.GetCraftableRecipes(100, recipeIndex, availabilityState);

            Assert.That(recipes, Is.Empty);
            Assert.That(ItemCraftingAvailability.IsCraftableNow(100, recipeIndex, availabilityState), Is.False);
        }

        [Test]
        public void GetCraftableRecipes_ReturnsReadOnlySnapshot()
        {
            RecipeCatalogEntry recipe = CreateRecipe(0, 100);
            CreateContext([recipe], out RecipeIndex recipeIndex, out CraftingAvailabilityState availabilityState);
            availabilityState.ReplaceSnapshot([0]);

            var recipes = (IList<RecipeCatalogEntry>)ItemCraftingAvailability.GetCraftableRecipes(
                100,
                recipeIndex,
                availabilityState);

            Assert.Throws<NotSupportedException>((Action)(() => recipes.Add(CreateRecipe(1, 100))));
        }

        private static void CreateContext(
            IEnumerable<RecipeCatalogEntry> recipes,
            out RecipeIndex recipeIndex,
            out CraftingAvailabilityState availabilityState)
        {
            var catalog = RecipeCatalog.Create(recipes);
            recipeIndex = RecipeIndex.Create(catalog);
            availabilityState = new CraftingAvailabilityState(catalog);
        }

        private static RecipeCatalogEntry CreateRecipe(int runtimeIndex, int resultItemId)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                [],
                new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                isAlchemy: false);
        }

        private static int[] GetRuntimeIndices(IReadOnlyList<RecipeCatalogEntry> recipes)
        {
            var result = new int[recipes.Count];

            for (var index = 0; index < recipes.Count; index++)
                result[index] = recipes[index].RuntimeIndex;

            return result;
        }
    }
}