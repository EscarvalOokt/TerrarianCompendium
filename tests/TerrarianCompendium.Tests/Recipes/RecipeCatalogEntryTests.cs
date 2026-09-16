using System;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeCatalogEntryTests
    {
        [Test]
        public void Constructor_WithValidData_PreservesMetadata()
        {
            var ingredient = new RecipeIngredient(9, 3, RecipeIngredientRequirement.ForItem(9));
            var environment = new RecipeEnvironmentRequirements(18, true, true, true, true, true, true, true);

            var entry = new RecipeCatalogEntry(7, 100, 2, new[] { ingredient }, environment, isAlchemy: true);

            Assert.That(entry.RuntimeIndex, Is.EqualTo(7));
            Assert.That(entry.ResultItemId, Is.EqualTo(100));
            Assert.That(entry.ResultStack, Is.EqualTo(2));
            Assert.That(entry.Ingredients, Is.EqualTo(new[] { ingredient }));
            Assert.That(entry.EnvironmentRequirements, Is.SameAs(environment));
            Assert.That(entry.IsAlchemy, Is.True);
        }

        [Test]
        public void Constructor_CopiesIngredientCollection()
        {
            var originalIngredient = new RecipeIngredient(9, 1, RecipeIngredientRequirement.ForItem(9));
            var replacementIngredient = new RecipeIngredient(10, 1, RecipeIngredientRequirement.ForItem(10));
            RecipeIngredient[] ingredients = { originalIngredient };

            RecipeCatalogEntry entry = CreateEntry(ingredients);
            ingredients[0] = replacementIngredient;

            Assert.That(entry.Ingredients[0], Is.SameAs(originalIngredient));
        }

        [Test]
        public void Constructor_WithNullIngredient_Throws()
        {
            RecipeIngredient[] ingredients = { null };

            Assert.Throws<ArgumentException>((Action)(() => { CreateEntry(ingredients); }));
        }

        [TestCase(-1)]
        [TestCase(-10)]
        public void Constructor_WithNegativeRuntimeIndex_Throws(int runtimeIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() =>
                {
                    _ = new RecipeCatalogEntry(
                        runtimeIndex,
                        1,
                        1,
                        Array.Empty<RecipeIngredient>(),
                        CreateEnvironment(),
                        isAlchemy: false);
                }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithNonPositiveResultItemId_Throws(int resultItemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() =>
                {
                    _ = new RecipeCatalogEntry(
                        0,
                        resultItemId,
                        1,
                        Array.Empty<RecipeIngredient>(),
                        CreateEnvironment(),
                        isAlchemy: false);
                }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithNonPositiveResultStack_Throws(int resultStack)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() =>
                {
                    _ = new RecipeCatalogEntry(
                        0,
                        1,
                        resultStack,
                        Array.Empty<RecipeIngredient>(),
                        CreateEnvironment(),
                        isAlchemy: false);
                }));
        }

        [Test]
        public void Constructor_WithNullIngredients_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() => { _ = new RecipeCatalogEntry(0, 1, 1, null, CreateEnvironment(), isAlchemy: false); }));
        }

        [Test]
        public void Constructor_WithNullEnvironmentRequirements_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeCatalogEntry(0, 1, 1, Array.Empty<RecipeIngredient>(), null, isAlchemy: false);
                }));
        }

        [Test]
        public void ItemRequirement_UsesConcreteItemIdentity()
        {
            var requirement = RecipeIngredientRequirement.ForItem(42);

            Assert.That(requirement.Kind, Is.EqualTo(RecipeIngredientRequirementKind.Item));
            Assert.That(requirement.IdentityId, Is.EqualTo(42));
            Assert.That(requirement.ValidItemIds, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void RecipeGroupRequirement_CopiesSortsAndDeduplicatesValidItems()
        {
            int[] validItemIds = { 30, 10, 20, 10 };

            var requirement = RecipeIngredientRequirement.ForRecipeGroup(5, validItemIds);
            validItemIds[0] = 999;

            Assert.That(requirement.Kind, Is.EqualTo(RecipeIngredientRequirementKind.RecipeGroup));
            Assert.That(requirement.IdentityId, Is.EqualTo(5));
            Assert.That(requirement.ValidItemIds, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ItemRequirement_WithNonPositiveItemId_Throws(int itemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { RecipeIngredientRequirement.ForItem(itemId); }));
        }

        [Test]
        public void RecipeGroupRequirement_WithNegativeGroupId_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { RecipeIngredientRequirement.ForRecipeGroup(-1, new[] { 1 }); }));
        }

        [Test]
        public void RecipeGroupRequirement_WithNoValidItems_Throws()
        {
            Assert.Throws<ArgumentException>(
                (Action)(() => { RecipeIngredientRequirement.ForRecipeGroup(0, Array.Empty<int>()); }));
        }

        [Test]
        public void RecipeIngredient_PreservesDisplayAndNormalizedRequirement()
        {
            var requirement = RecipeIngredientRequirement.ForRecipeGroup(3, new[] { 9, 619 });
            var ingredient = new RecipeIngredient(9, 5, requirement);

            Assert.That(ingredient.DisplayItemId, Is.EqualTo(9));
            Assert.That(ingredient.Stack, Is.EqualTo(5));
            Assert.That(ingredient.Requirement, Is.SameAs(requirement));
        }

        [Test]
        public void EnvironmentRequirements_PreserveIndependentFlags()
        {
            var environment = new RecipeEnvironmentRequirements(
                18,
                requiresWater: true,
                requiresHoney: false,
                requiresLava: true,
                requiresSnowBiome: false,
                requiresGraveyardBiome: true,
                requiresMechdusa: false,
                requiresTorchGodsFavor: true);

            Assert.That(environment.RequiredTileId, Is.EqualTo(18));
            Assert.That(environment.RequiresWater, Is.True);
            Assert.That(environment.RequiresHoney, Is.False);
            Assert.That(environment.RequiresLava, Is.True);
            Assert.That(environment.RequiresSnowBiome, Is.False);
            Assert.That(environment.RequiresGraveyardBiome, Is.True);
            Assert.That(environment.RequiresMechdusa, Is.False);
            Assert.That(environment.RequiresTorchGodsFavor, Is.True);
        }

        private static RecipeCatalogEntry CreateEntry(RecipeIngredient[] ingredients)
        {
            return new RecipeCatalogEntry(0, 1, 1, ingredients, CreateEnvironment(), isAlchemy: false);
        }

        private static RecipeEnvironmentRequirements CreateEnvironment()
        {
            return new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false);
        }
    }
}