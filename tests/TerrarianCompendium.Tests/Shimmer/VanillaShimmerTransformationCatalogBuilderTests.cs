using System;
using NUnit.Framework;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class VanillaShimmerTransformationCatalogBuilderTests
    {
        [Test]
        public void ResolveEquivalentItemId_OrdinaryLookupUsesSharedEquivalent()
        {
            int result = VanillaShimmerTransformationCatalogBuilder.ResolveEquivalentItemId(
                inputItemId: 10,
                shimmerEquivalentItemId: 20,
                decraftEquivalentItemId: 30,
                forDecrafting: false);

            Assert.That(result, Is.EqualTo(20));
        }

        [Test]
        public void ResolveEquivalentItemId_DecraftingPrefersDedicatedEquivalent()
        {
            int result = VanillaShimmerTransformationCatalogBuilder.ResolveEquivalentItemId(
                inputItemId: 10,
                shimmerEquivalentItemId: 20,
                decraftEquivalentItemId: 30,
                forDecrafting: true);

            Assert.That(result, Is.EqualTo(30));
        }

        [Test]
        public void ResolveEquivalentItemId_MissingMappingsFallsBackToInput()
        {
            int result = VanillaShimmerTransformationCatalogBuilder.ResolveEquivalentItemId(
                inputItemId: 10,
                shimmerEquivalentItemId: -1,
                decraftEquivalentItemId: -1,
                forDecrafting: true);

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void CreateEntry_DirectTransformationWinsOverDecraftCandidate()
        {
            RecipeCatalog recipes = CreateRecipeCatalog(7);

            ShimmerTransformationEntry entry = VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                inputItemId: 10,
                isCommonCoin: false,
                hasDirectTransformation: true,
                directResultItemId: 20,
                hasDynamicDirectResult: false,
                hasSpecialItemBehavior: false,
                hasNpcBehavior: false,
                baseRecipeRuntimeIndex: 7,
                crimsonRecipeRuntimeIndex: -1,
                corruptionRecipeRuntimeIndex: -1,
                recipeCatalog: recipes);

            Assert.Multiple(() =>
            {
                Assert.That(entry, Is.Not.Null);
                Assert.That(entry.IsDirect, Is.True);
                Assert.That(entry.DirectResultItemId, Is.EqualTo(20));
                Assert.That(entry.BaseRecipeRuntimeIndex, Is.Null);
            });
        }

        [Test]
        public void CreateEntry_DynamicDirectTransformationHasNoStaticResult()
        {
            ShimmerTransformationEntry entry = VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                inputItemId: 10,
                isCommonCoin: false,
                hasDirectTransformation: true,
                directResultItemId: 0,
                hasDynamicDirectResult: true,
                hasSpecialItemBehavior: false,
                hasNpcBehavior: false,
                baseRecipeRuntimeIndex: -1,
                crimsonRecipeRuntimeIndex: -1,
                corruptionRecipeRuntimeIndex: -1,
                recipeCatalog: CreateRecipeCatalog());

            Assert.Multiple(() =>
            {
                Assert.That(entry.IsDirect, Is.True);
                Assert.That(entry.HasDynamicDirectResult, Is.True);
                Assert.That(entry.DirectResultItemId, Is.Null);
            });
        }

        [Test]
        public void CreateEntry_CommonCoinIsOutsideCatalogScope()
        {
            ShimmerTransformationEntry entry = VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                10,
                isCommonCoin: true,
                hasDirectTransformation: true,
                directResultItemId: 20,
                hasDynamicDirectResult: false,
                hasSpecialItemBehavior: false,
                hasNpcBehavior: false,
                baseRecipeRuntimeIndex: 7,
                crimsonRecipeRuntimeIndex: -1,
                corruptionRecipeRuntimeIndex: -1,
                recipeCatalog: CreateRecipeCatalog(7));

            Assert.That(entry, Is.Null);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void CreateEntry_SpecialAndNpcOnlyBehaviorsAreOutsideCatalogScope(
            bool hasSpecialItemBehavior,
            bool hasNpcBehavior)
        {
            ShimmerTransformationEntry entry = VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                10,
                isCommonCoin: false,
                hasDirectTransformation: false,
                directResultItemId: 0,
                hasDynamicDirectResult: false,
                hasSpecialItemBehavior,
                hasNpcBehavior,
                baseRecipeRuntimeIndex: 7,
                crimsonRecipeRuntimeIndex: -1,
                corruptionRecipeRuntimeIndex: -1,
                recipeCatalog: CreateRecipeCatalog(7));

            Assert.That(entry, Is.Null);
        }

        [Test]
        public void CreateEntry_DecraftPreservesBaseAndWorldVariants()
        {
            RecipeCatalog recipes = CreateRecipeCatalog(7, 8, 9);

            ShimmerTransformationEntry entry = VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                10,
                isCommonCoin: false,
                hasDirectTransformation: false,
                directResultItemId: 0,
                hasDynamicDirectResult: false,
                hasSpecialItemBehavior: false,
                hasNpcBehavior: false,
                baseRecipeRuntimeIndex: 7,
                crimsonRecipeRuntimeIndex: 8,
                corruptionRecipeRuntimeIndex: 9,
                recipeCatalog: recipes);

            Assert.Multiple(() =>
            {
                Assert.That(entry.IsDecraft, Is.True);
                Assert.That(entry.BaseRecipeRuntimeIndex, Is.EqualTo(7));
                Assert.That(entry.CrimsonRecipeRuntimeIndex, Is.EqualTo(8));
                Assert.That(entry.CorruptionRecipeRuntimeIndex, Is.EqualTo(9));
            });
        }

        [Test]
        public void CreateEntry_NoSupportedBranchReturnsNull()
        {
            ShimmerTransformationEntry entry = VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                10,
                isCommonCoin: false,
                hasDirectTransformation: false,
                directResultItemId: 0,
                hasDynamicDirectResult: false,
                hasSpecialItemBehavior: false,
                hasNpcBehavior: false,
                baseRecipeRuntimeIndex: -1,
                crimsonRecipeRuntimeIndex: -1,
                corruptionRecipeRuntimeIndex: -1,
                recipeCatalog: CreateRecipeCatalog());

            Assert.That(entry, Is.Null);
        }

        [Test]
        public void CreateEntry_DanglingDecraftRecipeIndexThrows()
        {
            Assert.Throws<InvalidOperationException>(() => VanillaShimmerTransformationCatalogBuilder.CreateEntry(
                10,
                isCommonCoin: false,
                hasDirectTransformation: false,
                directResultItemId: 0,
                hasDynamicDirectResult: false,
                hasSpecialItemBehavior: false,
                hasNpcBehavior: false,
                baseRecipeRuntimeIndex: 7,
                crimsonRecipeRuntimeIndex: -1,
                corruptionRecipeRuntimeIndex: -1,
                recipeCatalog: CreateRecipeCatalog()));
        }

        private static RecipeCatalog CreateRecipeCatalog(params int[] runtimeIndices)
        {
            var entries = new RecipeCatalogEntry[runtimeIndices.Length];

            for (var index = 0; index < runtimeIndices.Length; index++)
            {
                int runtimeIndex = runtimeIndices[index];
                entries[index] = new RecipeCatalogEntry(
                    runtimeIndex,
                    1000 + runtimeIndex,
                    1,
                    Array.Empty<RecipeIngredient>(),
                    new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                    isAlchemy: false);
            }

            return RecipeCatalog.Create(entries);
        }
    }
}