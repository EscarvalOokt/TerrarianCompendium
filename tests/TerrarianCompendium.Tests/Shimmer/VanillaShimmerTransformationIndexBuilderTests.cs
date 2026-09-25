using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class VanillaShimmerTransformationIndexBuilderTests
    {
        [Test]
        public void LunarBrickResults_CoverEveryMoonPhaseWithConfirmedResult()
        {
            int[] expected = [5408, 5401, 5403, 5402, 5406, 5407, 5405, 5404];

            Assert.That(VanillaShimmerTransformationIndexBuilder.GetLunarBrickResults(), Has.Count.EqualTo(8));

            for (var phase = 0; phase < expected.Length; phase++)
            {
                Assert.That(
                    VanillaShimmerTransformationIndexBuilder.GetLunarBrickResultItemId(phase),
                    Is.EqualTo(expected[phase]));
            }
        }

        [Test]
        public void ProgressionLock_UsesConfirmedBossProgressionRequirements()
        {
            var locked = new ShimmerRuntimeContextKey(
                downedSkeletron: false,
                downedGolem: false,
                downedMoonLord: false,
                worldIsCrimson: false,
                moonPhase: 0);
            var unlocked = new ShimmerRuntimeContextKey(
                downedSkeletron: true,
                downedGolem: true,
                downedMoonLord: true,
                worldIsCrimson: true,
                moonPhase: 7);

            Assert.Multiple(() =>
            {
                Assert.That(
                    VanillaShimmerNativeBridge.IsProgressionLocked(ShimmerProgressionRequirement.None, locked),
                    Is.False);
                Assert.That(
                    VanillaShimmerNativeBridge.IsProgressionLocked(
                        ShimmerProgressionRequirement.PostSkeletron |
                        ShimmerProgressionRequirement.PostGolem |
                        ShimmerProgressionRequirement.PostMoonLord,
                        locked),
                    Is.True);
                Assert.That(
                    VanillaShimmerNativeBridge.IsProgressionLocked(
                        ShimmerProgressionRequirement.PostSkeletron |
                        ShimmerProgressionRequirement.PostGolem |
                        ShimmerProgressionRequirement.PostMoonLord,
                        unlocked),
                    Is.False);
            });
        }

        [Test]
        public void ResolveWorldRecipeVariants_NoWorldOverride_ReturnsSingleUnconditionalVariant()
        {
            IReadOnlyList<ShimmerWorldRecipeVariant> variants =
                VanillaShimmerTransformationIndexBuilder.ResolveWorldRecipeVariants(5, null, null);

            Assert.That(variants, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(variants[0].RuntimeIndex, Is.EqualTo(5));
                Assert.That(variants[0].WorldCondition, Is.EqualTo(ShimmerWorldCondition.Any));
            });
        }

        [Test]
        public void ResolveWorldRecipeVariants_DifferentEffectiveRecipes_ReturnsBothWorldVariants()
        {
            IReadOnlyList<ShimmerWorldRecipeVariant> variants =
                VanillaShimmerTransformationIndexBuilder.ResolveWorldRecipeVariants(5, 6, 7);

            Assert.That(variants.Select(variant => variant.RuntimeIndex), Is.EqualTo([6, 7]));
            Assert.That(
                variants.Select(variant => variant.WorldCondition),
                Is.EqualTo([ShimmerWorldCondition.Crimson, ShimmerWorldCondition.Corruption]));
        }

        [Test]
        public void ResolveWorldRecipeVariants_OneWorldOverride_PreservesBaseForOtherWorld()
        {
            IReadOnlyList<ShimmerWorldRecipeVariant> variants =
                VanillaShimmerTransformationIndexBuilder.ResolveWorldRecipeVariants(5, 6, null);

            Assert.That(variants.Select(variant => variant.RuntimeIndex), Is.EqualTo([6, 5]));
            Assert.That(
                variants.Select(variant => variant.WorldCondition),
                Is.EqualTo([ShimmerWorldCondition.Crimson, ShimmerWorldCondition.Corruption]));
        }

        [Test]
        public void ResolveWorldRecipeVariants_EqualEffectiveRecipes_DeduplicatesToUnconditionalVariant()
        {
            IReadOnlyList<ShimmerWorldRecipeVariant> variants =
                VanillaShimmerTransformationIndexBuilder.ResolveWorldRecipeVariants(5, 6, 6);

            Assert.That(variants, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(variants[0].RuntimeIndex, Is.EqualTo(6));
                Assert.That(variants[0].WorldCondition, Is.EqualTo(ShimmerWorldCondition.Any));
            });
        }
    }
}