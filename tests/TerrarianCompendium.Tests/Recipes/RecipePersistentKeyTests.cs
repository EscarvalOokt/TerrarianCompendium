using System;
using System.Globalization;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipePersistentKeyTests
    {
        [Test]
        public void Create_WithNullRecipe_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { RecipePersistentKey.Create(null); }));
        }

        [Test]
        public void Create_ProducesExpectedCanonicalValue()
        {
            RecipeCatalogEntry recipe = CreateEntry(
                runtimeIndex: 42,
                resultItemId: 100,
                resultStack: 2,
                requiredTileId: 17,
                requiresWater: true,
                requiresHoney: false,
                requiresLava: true,
                requiresSnowBiome: false,
                requiresGraveyardBiome: true,
                requiresMechdusa: false,
                requiresTorchGodsFavor: true,
                isAlchemy: true,
                ingredients:
                [
                    CreateItemIngredient(10, 3, 10),
                    CreateGroupIngredient(20, 4, 99, 30, 20, 30)
                ]);

            var key = RecipePersistentKey.Create(recipe);

            Assert.That(key.Value, Is.EqualTo("v1|r:100:2|n:2|x:i:10:3:10|x:g:20:4:20,30|t:17|e:1010101|a:1"));
            Assert.That(key.ToString(), Is.EqualTo(key.Value));
        }

        [Test]
        public void Create_IgnoresRuntimeIndex()
        {
            var first = RecipePersistentKey.Create(CreateEntry(runtimeIndex: 1));
            var second = RecipePersistentKey.Create(CreateEntry(runtimeIndex: 987654321));

            Assert.That(second, Is.EqualTo(first));
            Assert.That(second.GetHashCode(), Is.EqualTo(first.GetHashCode()));
        }

        [Test]
        public void Create_WithEquivalentRecipeGroupIdentityIds_ProducesSameKey()
        {
            RecipeCatalogEntry first = CreateEntry(
                runtimeIndex: 1,
                ingredients: [CreateGroupIngredient(10, 5, 30, 10, 20)]);
            RecipeCatalogEntry second = CreateEntry(
                runtimeIndex: 2,
                ingredients: [CreateGroupIngredient(10, 5, 900, 10, 20)]);

            Assert.That(RecipePersistentKey.Create(second), Is.EqualTo(RecipePersistentKey.Create(first)));
        }

        [Test]
        public void Create_WithSameRecipeGroupMembersInDifferentOrder_ProducesSameKey()
        {
            RecipeCatalogEntry first = CreateEntry(ingredients: [CreateGroupIngredient(10, 5, 30, 10, 20)]);
            RecipeCatalogEntry second = CreateEntry(ingredients: [CreateGroupIngredient(10, 5, 30, 20, 10)]);

            Assert.That(RecipePersistentKey.Create(second), Is.EqualTo(RecipePersistentKey.Create(first)));
        }

        [Test]
        public void Create_WithDifferentResultItem_ProducesDifferentKey()
        {
            var first = RecipePersistentKey.Create(CreateEntry(resultItemId: 100));
            var second = RecipePersistentKey.Create(CreateEntry(resultItemId: 101));

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Create_WithDifferentResultStack_ProducesDifferentKey()
        {
            var first = RecipePersistentKey.Create(CreateEntry(resultStack: 1));
            var second = RecipePersistentKey.Create(CreateEntry(resultStack: 2));

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Create_WithDifferentIngredientDisplayItem_ProducesDifferentKey()
        {
            RecipeCatalogEntry first = CreateEntry(ingredients: [CreateItemIngredient(10, 1, 15)]);
            RecipeCatalogEntry second = CreateEntry(ingredients: [CreateItemIngredient(11, 1, 15)]);

            Assert.That(RecipePersistentKey.Create(second), Is.Not.EqualTo(RecipePersistentKey.Create(first)));
        }

        [Test]
        public void Create_WithDifferentIngredientStack_ProducesDifferentKey()
        {
            RecipeCatalogEntry first = CreateEntry(ingredients: [CreateItemIngredient(10, 1, 10)]);
            RecipeCatalogEntry second = CreateEntry(ingredients: [CreateItemIngredient(10, 2, 10)]);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Create_WithDifferentOrdinaryRequirementItem_ProducesDifferentKey()
        {
            RecipeCatalogEntry first = CreateEntry(ingredients: [CreateItemIngredient(10, 1, 15)]);
            RecipeCatalogEntry second = CreateEntry(ingredients: [CreateItemIngredient(10, 1, 16)]);

            Assert.That(RecipePersistentKey.Create(second), Is.Not.EqualTo(RecipePersistentKey.Create(first)));
        }

        [Test]
        public void Create_WithDifferentRecipeGroupMembership_ProducesDifferentKey()
        {
            RecipeCatalogEntry first = CreateEntry(ingredients: [CreateGroupIngredient(10, 5, 10, 20, 30)]);
            RecipeCatalogEntry second = CreateEntry(ingredients: [CreateGroupIngredient(10, 5, 10, 20, 40)]);

            Assert.That(RecipePersistentKey.Create(second), Is.Not.EqualTo(RecipePersistentKey.Create(first)));
        }

        [Test]
        public void Create_WithDifferentIngredientOrder_ProducesDifferentKey()
        {
            RecipeCatalogEntry first = CreateEntry(
                ingredients:
                [
                    CreateItemIngredient(10, 1, 10),
                    CreateItemIngredient(20, 1, 20)
                ]);
            RecipeCatalogEntry second = CreateEntry(
                ingredients:
                [
                    CreateItemIngredient(20, 1, 20),
                    CreateItemIngredient(10, 1, 10)
                ]);

            Assert.That(RecipePersistentKey.Create(second), Is.Not.EqualTo(RecipePersistentKey.Create(first)));
        }

        [Test]
        public void Create_WithDuplicateIngredientSlot_ProducesDifferentKey()
        {
            RecipeIngredient ingredient = CreateItemIngredient(10, 1, 10);
            RecipeCatalogEntry first = CreateEntry(ingredients: [ingredient]);
            RecipeCatalogEntry second = CreateEntry(ingredients: [ingredient, ingredient]);

            var firstKey = RecipePersistentKey.Create(first);
            var secondKey = RecipePersistentKey.Create(second);

            Assert.That(secondKey, Is.Not.EqualTo(firstKey));
            Assert.That(secondKey.Value, Does.Contain("|n:2|"));
        }

        [Test]
        public void Create_WithDifferentRequiredTile_ProducesDifferentKey()
        {
            var first = RecipePersistentKey.Create(CreateEntry(requiredTileId: 10));
            var second = RecipePersistentKey.Create(CreateEntry(requiredTileId: 11));

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Create_WithDifferentEnvironmentRequirement_ProducesDifferentKey(int requirementIndex)
        {
            var baseline = RecipePersistentKey.Create(CreateEntry());
            var changed = RecipePersistentKey.Create(CreateEntryWithEnvironmentRequirement(requirementIndex));

            Assert.That(changed, Is.Not.EqualTo(baseline));
        }

        [Test]
        public void Create_WithDifferentAlchemyFlag_ProducesDifferentKey()
        {
            var first = RecipePersistentKey.Create(CreateEntry(isAlchemy: false));
            var second = RecipePersistentKey.Create(CreateEntry(isAlchemy: true));

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Create_UsesInvariantNumericFormatting()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;

            try
            {
                RecipeCatalogEntry recipe = CreateEntry(
                    runtimeIndex: 1234,
                    resultItemId: 5678,
                    resultStack: 90,
                    requiredTileId: 12,
                    ingredients: [CreateItemIngredient(34, 56, 78)]);

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                string english = RecipePersistentKey.Create(recipe).Value;

                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                string french = RecipePersistentKey.Create(recipe).Value;

                Assert.That(french, Is.EqualTo(english));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void Create_UsesCurrentFormatVersionPrefix()
        {
            var key = RecipePersistentKey.Create(CreateEntry());

            Assert.That(RecipePersistentKey.CurrentFormatVersion, Is.EqualTo(1));
            Assert.That(key.Value, Does.StartWith("v1|"));
        }

        [Test]
        public void TryParse_WithCanonicalValue_RoundTripsKey()
        {
            var original = RecipePersistentKey.Create(
                CreateEntry(
                    resultItemId: 100,
                    resultStack: 2,
                    requiredTileId: 17,
                    requiresWater: true,
                    isAlchemy: true,
                    ingredients: [CreateGroupIngredient(20, 4, 99, 30, 20)]));

            bool parsed = RecipePersistentKey.TryParse(original.Value, out RecipePersistentKey restored);

            Assert.That(parsed, Is.True);
            Assert.That(restored, Is.EqualTo(original));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("v2|r:100:1|n:0|t:-|e:0000000|a:0")]
        [TestCase("v1|r:100:1|n:1|t:-|e:0000000|a:0")]
        [TestCase("v1|r:0100:1|n:0|t:-|e:0000000|a:0")]
        [TestCase("v1|r:100:1|n:0|t:-|e:000000|a:0")]
        public void TryParse_WithUnsupportedOrMalformedValue_ReturnsFalse(string value)
        {
            bool parsed = RecipePersistentKey.TryParse(value, out RecipePersistentKey key);

            Assert.That(parsed, Is.False);
            Assert.That(key, Is.Null);
        }

        [Test]
        public void Equality_UsesCanonicalValue()
        {
            var first = RecipePersistentKey.Create(CreateEntry(runtimeIndex: 1));
            var second = RecipePersistentKey.Create(CreateEntry(runtimeIndex: 2));
            var different = RecipePersistentKey.Create(CreateEntry(resultItemId: 101));

            Assert.That(first.Equals(second), Is.True);
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.Equals(different), Is.False);
        }

        private static RecipeCatalogEntry CreateEntry(
            int runtimeIndex = 0,
            int resultItemId = 100,
            int resultStack = 1,
            int? requiredTileId = null,
            bool requiresWater = false,
            bool requiresHoney = false,
            bool requiresLava = false,
            bool requiresSnowBiome = false,
            bool requiresGraveyardBiome = false,
            bool requiresMechdusa = false,
            bool requiresTorchGodsFavor = false,
            bool isAlchemy = false,
            params RecipeIngredient[] ingredients)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                resultStack,
                ingredients ?? [],
                new RecipeEnvironmentRequirements(
                    requiredTileId,
                    requiresWater,
                    requiresHoney,
                    requiresLava,
                    requiresSnowBiome,
                    requiresGraveyardBiome,
                    requiresMechdusa,
                    requiresTorchGodsFavor),
                isAlchemy);
        }

        private static RecipeCatalogEntry CreateEntryWithEnvironmentRequirement(int requirementIndex)
        {
            return CreateEntry(
                requiresWater: requirementIndex == 0,
                requiresHoney: requirementIndex == 1,
                requiresLava: requirementIndex == 2,
                requiresSnowBiome: requirementIndex == 3,
                requiresGraveyardBiome: requirementIndex == 4,
                requiresMechdusa: requirementIndex == 5,
                requiresTorchGodsFavor: requirementIndex == 6);
        }

        private static RecipeIngredient CreateItemIngredient(int displayItemId, int stack, int requiredItemId)
        {
            return new RecipeIngredient(displayItemId, stack, RecipeIngredientRequirement.ForItem(requiredItemId));
        }

        private static RecipeIngredient CreateGroupIngredient(
            int displayItemId,
            int stack,
            int recipeGroupId,
            params int[] validItemIds)
        {
            return new RecipeIngredient(
                displayItemId,
                stack,
                RecipeIngredientRequirement.ForRecipeGroup(recipeGroupId, validItemIds));
        }
    }
}