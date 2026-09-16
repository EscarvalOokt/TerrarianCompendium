using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeIndexTests
    {
        [Test]
        public void Create_WithNullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { RecipeIndex.Create(null); }));
        }

        [Test]
        public void Create_WithEmptyCatalog_ReturnsEmptyRelations()
        {
            var index = RecipeIndex.Create(RecipeCatalog.Create(Array.Empty<RecipeCatalogEntry>()));

            Assert.That(index.GetRecipesProducing(1), Is.Empty);
            Assert.That(index.GetRecipesUsing(1), Is.Empty);
            Assert.That(index.HasRecipeProducing(1), Is.False);
        }

        [Test]
        public void GetRecipesProducing_ReturnsAllRecipesForResultItem()
        {
            RecipeCatalogEntry first = CreateEntry(0, 100);
            RecipeCatalogEntry second = CreateEntry(1, 200);
            RecipeCatalogEntry third = CreateEntry(2, 100);
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { first, second, third }));

            IReadOnlyList<RecipeCatalogEntry> recipes = index.GetRecipesProducing(100);

            Assert.That(recipes, Is.EqualTo(new[] { first, third }));
        }

        [Test]
        public void HasRecipeProducing_ReturnsExpectedResult()
        {
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { CreateEntry(0, 100) }));

            Assert.That(index.HasRecipeProducing(100), Is.True);
            Assert.That(index.HasRecipeProducing(200), Is.False);
        }

        [Test]
        public void GetRecipesUsing_WithOrdinaryRequirement_ReturnsRecipeForConcreteItem()
        {
            RecipeCatalogEntry recipe = CreateEntry(0, 100, CreateItemIngredient(10));
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { recipe }));

            Assert.That(index.GetRecipesUsing(10), Is.EqualTo(new[] { recipe }));
            Assert.That(index.GetRecipesUsing(20), Is.Empty);
        }

        [Test]
        public void GetRecipesUsing_WithRecipeGroupRequirement_ReturnsRecipeForEveryValidMember()
        {
            RecipeCatalogEntry recipe = CreateEntry(0, 100, CreateGroupIngredient(10, 5, 10, 20, 30));
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { recipe }));

            Assert.That(index.GetRecipesUsing(10), Is.EqualTo(new[] { recipe }));
            Assert.That(index.GetRecipesUsing(20), Is.EqualTo(new[] { recipe }));
            Assert.That(index.GetRecipesUsing(30), Is.EqualTo(new[] { recipe }));
        }

        [Test]
        public void GetRecipesUsing_WhenItemMatchesMultipleRequirements_ReturnsRecipeOnce()
        {
            RecipeCatalogEntry recipe = CreateEntry(
                0,
                100,
                CreateItemIngredient(10),
                CreateGroupIngredient(20, 5, 10, 20),
                CreateGroupIngredient(30, 6, 10, 30));
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { recipe }));

            IReadOnlyList<RecipeCatalogEntry> recipes = index.GetRecipesUsing(10);

            Assert.That(recipes, Has.Count.EqualTo(1));
            Assert.That(recipes[0], Is.SameAs(recipe));
        }

        [Test]
        public void GetRecipesUsing_ReturnsAllRecipesThatAcceptItem()
        {
            RecipeCatalogEntry first = CreateEntry(0, 100, CreateItemIngredient(10));
            RecipeCatalogEntry second = CreateEntry(1, 200, CreateGroupIngredient(20, 5, 10, 20));
            RecipeCatalogEntry third = CreateEntry(2, 300, CreateItemIngredient(30));
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { first, second, third }));

            Assert.That(index.GetRecipesUsing(10), Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void Relations_FollowRuntimeIndexOrder()
        {
            RecipeCatalogEntry third = CreateEntry(2, 100, CreateItemIngredient(10));
            RecipeCatalogEntry first = CreateEntry(0, 100, CreateItemIngredient(10));
            RecipeCatalogEntry second = CreateEntry(1, 100, CreateItemIngredient(10));
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { third, first, second }));

            Assert.That(GetRuntimeIndices(index.GetRecipesProducing(100)), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(GetRuntimeIndices(index.GetRecipesUsing(10)), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(999)]
        public void Queries_WithMissingOrInvalidItemId_ReturnEmptyResults(int itemId)
        {
            var index = RecipeIndex.Create(
                RecipeCatalog.Create(
                    new[]
                    {
                        CreateEntry(0, 100, CreateItemIngredient(10))
                    }));

            Assert.That(index.GetRecipesProducing(itemId), Is.Empty);
            Assert.That(index.GetRecipesUsing(itemId), Is.Empty);
            Assert.That(index.HasRecipeProducing(itemId), Is.False);
        }

        [Test]
        public void RelationResults_AreReadOnly()
        {
            RecipeCatalogEntry recipe = CreateEntry(0, 100, CreateItemIngredient(10));
            var index = RecipeIndex.Create(RecipeCatalog.Create(new[] { recipe }));
            var producingRecipes = (IList<RecipeCatalogEntry>)index.GetRecipesProducing(100);
            var usingRecipes = (IList<RecipeCatalogEntry>)index.GetRecipesUsing(10);

            Assert.Throws<NotSupportedException>((Action)(() => { producingRecipes.Add(CreateEntry(1, 100)); }));
            Assert.Throws<NotSupportedException>(
                (Action)(() => { usingRecipes.Add(CreateEntry(1, 200, CreateItemIngredient(10))); }));
        }

        [Test]
        public void Relations_ReuseCatalogEntries()
        {
            RecipeCatalogEntry recipe = CreateEntry(0, 100, CreateItemIngredient(10));
            var catalog = RecipeCatalog.Create(new[] { recipe });
            var index = RecipeIndex.Create(catalog);

            Assert.That(index.GetRecipesProducing(100)[0], Is.SameAs(recipe));
            Assert.That(index.GetRecipesUsing(10)[0], Is.SameAs(recipe));
        }

        private static RecipeCatalogEntry CreateEntry(
            int runtimeIndex,
            int resultItemId,
            params RecipeIngredient[] ingredients)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                ingredients,
                new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                isAlchemy: false);
        }

        private static RecipeIngredient CreateItemIngredient(int itemId)
        {
            return new RecipeIngredient(itemId, 1, RecipeIngredientRequirement.ForItem(itemId));
        }

        private static RecipeIngredient CreateGroupIngredient(
            int displayItemId,
            int recipeGroupId,
            params int[] validItemIds)
        {
            return new RecipeIngredient(
                displayItemId,
                1,
                RecipeIngredientRequirement.ForRecipeGroup(recipeGroupId, validItemIds));
        }

        private static int[] GetRuntimeIndices(IReadOnlyList<RecipeCatalogEntry> recipes)
        {
            var runtimeIndices = new int[recipes.Count];

            for (var index = 0; index < recipes.Count; index++)
                runtimeIndices[index] = recipes[index].RuntimeIndex;

            return runtimeIndices;
        }
    }
}