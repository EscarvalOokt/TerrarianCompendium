using System;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class VanillaRecipeCatalogBuilderTests
    {
        [Test]
        public void CreateItemRequirement_PreservesConcreteItemIdentity()
        {
            RecipeIngredientRequirement requirement = VanillaRecipeCatalogBuilder.CreateItemRequirement(23);

            Assert.That(requirement.Kind, Is.EqualTo(RecipeIngredientRequirementKind.Item));
            Assert.That(requirement.IdentityId, Is.EqualTo(23));
            Assert.That(requirement.ValidItemIds, Is.EqualTo(new[] { 23 }));
        }

        [Test]
        public void CreateRecipeGroupRequirement_RemovesFakeOffsetAndPreservesMembers()
        {
            const int fakeItemIdOffset = 1_000_000;
            int[] validItemIds = { 619, 9, 27 };

            RecipeIngredientRequirement requirement = VanillaRecipeCatalogBuilder.CreateRecipeGroupRequirement(
                fakeItemIdOffset + 25,
                fakeItemIdOffset,
                validItemIds);

            validItemIds[0] = 999;

            Assert.That(requirement.Kind, Is.EqualTo(RecipeIngredientRequirementKind.RecipeGroup));
            Assert.That(requirement.IdentityId, Is.EqualTo(25));
            Assert.That(requirement.ValidItemIds, Is.EqualTo(new[] { 9, 27, 619 }));
        }

        [Test]
        public void NormalizeRecipeGroupId_WithoutFakeOffset_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { VanillaRecipeCatalogBuilder.NormalizeRecipeGroupId(25, 1_000_000); }));
        }

        [Test]
        public void CreateEnvironmentRequirements_WithNegativeTile_UsesNoStation()
        {
            RecipeEnvironmentRequirements requirements =
                VanillaRecipeCatalogBuilder.CreateEnvironmentRequirements(
                    -1,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false);

            Assert.That(requirements.RequiredTileId, Is.Null);
        }

        [Test]
        public void CreateEnvironmentRequirements_WithStation_PreservesCanonicalTile()
        {
            RecipeEnvironmentRequirements requirements =
                VanillaRecipeCatalogBuilder.CreateEnvironmentRequirements(
                    18,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false);

            Assert.That(requirements.RequiredTileId, Is.EqualTo(18));
        }

        [Test]
        public void CreateEnvironmentRequirements_PreservesAllFlagsIndependently()
        {
            RecipeEnvironmentRequirements requirements = VanillaRecipeCatalogBuilder.CreateEnvironmentRequirements(
                -1,
                requiresWater: true,
                requiresHoney: false,
                requiresLava: true,
                requiresSnowBiome: false,
                requiresGraveyardBiome: true,
                requiresMechdusa: false,
                requiresTorchGodsFavor: true);

            Assert.That(requirements.RequiresWater, Is.True);
            Assert.That(requirements.RequiresHoney, Is.False);
            Assert.That(requirements.RequiresLava, Is.True);
            Assert.That(requirements.RequiresSnowBiome, Is.False);
            Assert.That(requirements.RequiresGraveyardBiome, Is.True);
            Assert.That(requirements.RequiresMechdusa, Is.False);
            Assert.That(requirements.RequiresTorchGodsFavor, Is.True);
        }

        [Test]
        public void CreateEntry_PreservesResultIngredientsEnvironmentAndAlchemy()
        {
            var ingredient = new RecipeIngredient(9, 1, RecipeIngredientRequirement.ForItem(9));

            RecipeCatalogEntry entry = VanillaRecipeCatalogBuilder.CreateEntry(
                12,
                8,
                3,
                new[] { ingredient },
                18,
                requiresWater: true,
                requiresHoney: false,
                requiresLava: false,
                requiresSnowBiome: true,
                requiresGraveyardBiome: false,
                requiresMechdusa: true,
                requiresTorchGodsFavor: false,
                isAlchemy: true);

            Assert.That(entry.RuntimeIndex, Is.EqualTo(12));
            Assert.That(entry.ResultItemId, Is.EqualTo(8));
            Assert.That(entry.ResultStack, Is.EqualTo(3));
            Assert.That(entry.Ingredients, Is.EqualTo(new[] { ingredient }));
            Assert.That(entry.EnvironmentRequirements.RequiredTileId, Is.EqualTo(18));
            Assert.That(entry.EnvironmentRequirements.RequiresWater, Is.True);
            Assert.That(entry.EnvironmentRequirements.RequiresSnowBiome, Is.True);
            Assert.That(entry.EnvironmentRequirements.RequiresMechdusa, Is.True);
            Assert.That(entry.IsAlchemy, Is.True);
        }
    }
}