using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class ItemDetailsPresentationTests
    {
        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(ItemDetailsPresentation).Assembly,
            CompendiumLocalization.SourceCultureName);

        [Test]
        public void BuildStats_FullProjection_UsesStableOrderAndFormatting()
        {
            ItemDetailsProjection projection = CreateProjection(
                rarity: 5,
                damage: 42,
                defense: 17,
                pickPower: 55,
                axePower: 80,
                hammerPower: 60,
                fishingPower: 25,
                detailsStats: new ItemDetailsStats(
                    ItemDetailsDamageType.Melee,
                    knockback: 4.5f,
                    baseCriticalHitChance: 9,
                    useTimeTicks: 18,
                    tagDamage: 7));

            ItemDetailsStatPresentation[] stats = ItemDetailsPresentation.BuildStats(projection, _localization)
                .ToArray();

            Assert.That(
                stats.Select(stat => stat.Kind).ToArray(),
                Is.EqualTo(
                [
                    ItemDetailsStatKind.Rarity,
                    ItemDetailsStatKind.Damage,
                    ItemDetailsStatKind.Defense,
                    ItemDetailsStatKind.Knockback,
                    ItemDetailsStatKind.CriticalHitChance,
                    ItemDetailsStatKind.UseTime,
                    ItemDetailsStatKind.TagDamage,
                    ItemDetailsStatKind.PickPower,
                    ItemDetailsStatKind.AxePower,
                    ItemDetailsStatKind.HammerPower,
                    ItemDetailsStatKind.FishingPower
                ]));

            Assert.That(
                stats.Select(stat => stat.ValueText).ToArray(),
                Is.EqualTo(
                [
                    "5",
                    "42 Melee",
                    "17",
                    "4.5 Average",
                    "9%",
                    "18 ticks",
                    "7",
                    "55%",
                    "80%",
                    "60%",
                    "25%"
                ]));
            Assert.Multiple(() =>
            {
                Assert.That(
                    stats.Single(stat => stat.Kind == ItemDetailsStatKind.Damage).TooltipText,
                    Is.EqualTo("Melee damage"));
                Assert.That(
                    stats.Single(stat => stat.Kind == ItemDetailsStatKind.UseTime).TooltipText,
                    Is.EqualTo("Attack speed — base use time in ticks; lower is faster."));
            });
        }

        [Test]
        public void BuildStats_NonApplicableStats_AreOmittedWithoutGaps()
        {
            ItemDetailsProjection projection = CreateProjection(
                rarity: 0,
                damage: 0,
                defense: 0,
                pickPower: 0,
                axePower: 0,
                hammerPower: 0,
                fishingPower: 0,
                detailsStats: default);

            ItemDetailsStatPresentation[] stats = ItemDetailsPresentation.BuildStats(projection, _localization)
                .ToArray();

            Assert.That(stats, Has.Length.EqualTo(1));
            Assert.That(stats[0].Kind, Is.EqualTo(ItemDetailsStatKind.Rarity));
            Assert.That(stats[0].ValueText, Is.EqualTo("0"));
        }

        [TestCase(0f, "0 No")]
        [TestCase(0.01f, "0.01 Extremely weak")]
        [TestCase(1.5f, "1.5 Extremely weak")]
        [TestCase(1.51f, "1.51 Very weak")]
        [TestCase(3f, "3 Very weak")]
        [TestCase(3.01f, "3.01 Weak")]
        [TestCase(4f, "4 Weak")]
        [TestCase(4.01f, "4.01 Average")]
        [TestCase(6f, "6 Average")]
        [TestCase(6.01f, "6.01 Strong")]
        [TestCase(7f, "7 Strong")]
        [TestCase(7.01f, "7.01 Very strong")]
        [TestCase(9f, "9 Very strong")]
        [TestCase(9.01f, "9.01 Extremely strong")]
        [TestCase(11f, "11 Extremely strong")]
        [TestCase(11.01f, "11.01 Insane")]
        public void BuildStats_Knockback_UsesVanillaCompatibleTierBoundaries(float knockback, string expectedValueText)
        {
            ItemDetailsProjection projection = CreateProjection(
                rarity: 1,
                damage: 10,
                detailsStats: new ItemDetailsStats(
                    ItemDetailsDamageType.Melee,
                    knockback: knockback,
                    baseCriticalHitChance: null,
                    useTimeTicks: null,
                    tagDamage: null));

            ItemDetailsStatPresentation presentation = ItemDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == ItemDetailsStatKind.Knockback);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.ValueText, Is.EqualTo(expectedValueText));
                Assert.That(presentation.TooltipText, Is.EqualTo("Knockback"));
            });
        }

        [TestCase((int)ItemDetailsDamageType.Melee, "20 Melee", "Melee damage")]
        [TestCase((int)ItemDetailsDamageType.Ranged, "20 Ranged", "Ranged damage")]
        [TestCase((int)ItemDetailsDamageType.Magic, "20 Magic", "Magic damage")]
        [TestCase((int)ItemDetailsDamageType.Summon, "20 Summon", "Summon damage")]
        [TestCase((int)ItemDetailsDamageType.Generic, "20 Generic", "Generic damage")]
        public void BuildStats_DamageType_IsIncludedInDamagePresentation(
            int damageType,
            string expectedValueText,
            string expectedTooltipText)
        {
            ItemDetailsProjection projection = CreateProjection(
                rarity: 1,
                damage: 20,
                detailsStats: new ItemDetailsStats(
                    (ItemDetailsDamageType)damageType,
                    knockback: null,
                    baseCriticalHitChance: null,
                    useTimeTicks: null,
                    tagDamage: null));

            ItemDetailsStatPresentation damage = ItemDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == ItemDetailsStatKind.Damage);

            Assert.Multiple(() =>
            {
                Assert.That(damage.ValueText, Is.EqualTo(expectedValueText));
                Assert.That(damage.TooltipText, Is.EqualTo(expectedTooltipText));
            });
        }

        [Test]
        public void BuildStats_DamageTypeNone_FallsBackToUntypedDamagePresentation()
        {
            ItemDetailsProjection projection = CreateProjection(
                rarity: 1,
                damage: 20,
                detailsStats: new ItemDetailsStats(
                    ItemDetailsDamageType.None,
                    knockback: null,
                    baseCriticalHitChance: null,
                    useTimeTicks: null,
                    tagDamage: null));

            ItemDetailsStatPresentation damage = ItemDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == ItemDetailsStatKind.Damage);

            Assert.Multiple(() =>
            {
                Assert.That(damage.ValueText, Is.EqualTo("20"));
                Assert.That(damage.TooltipText, Is.EqualTo("Damage"));
            });
        }

        [Test]
        public void BuildStats_UseTime_UsesTickSuffixAndAttackSpeedTooltip()
        {
            ItemDetailsProjection projection = CreateProjection(
                rarity: 1,
                damage: 20,
                detailsStats: new ItemDetailsStats(
                    ItemDetailsDamageType.Melee,
                    knockback: null,
                    baseCriticalHitChance: null,
                    useTimeTicks: 18,
                    tagDamage: null));

            ItemDetailsStatPresentation useTime = ItemDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == ItemDetailsStatKind.UseTime);

            Assert.Multiple(() =>
            {
                Assert.That(useTime.ValueText, Is.EqualTo("18 ticks"));
                Assert.That(useTime.TooltipText, Is.EqualTo("Attack speed — base use time in ticks; lower is faster."));
            });
        }

        [Test]
        public void BuildStats_FishingPower_UsesPercentageFormatting()
        {
            ItemDetailsProjection projection = CreateProjection(rarity: 1, fishingPower: 25);

            ItemDetailsStatPresentation fishingPower = ItemDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == ItemDetailsStatKind.FishingPower);

            Assert.Multiple(() =>
            {
                Assert.That(fishingPower.ValueText, Is.EqualTo("25%"));
                Assert.That(fishingPower.TooltipText, Is.EqualTo("Fishing power"));
            });
        }

        [Test]
        public void BuildStats_AlternateLocale_ChangesLabelsWithoutChangingNumericValues()
        {
            var translated = new Dictionary<string, string>
            {
                [CompendiumTextKeys.ItemStats.DamageMelee] = "Translated melee",
                [CompendiumTextKeys.ItemStats.DamageTyped] = "{0} translated damage",
                [CompendiumTextKeys.ItemStats.Damage] = "Translated damage"
            };
            var alternate = CompendiumLocalization.CreateForTesting(_localization.SourceEntries, translated, "uk-UA");
            alternate.SynchronizeCulture("uk-UA");
            ItemDetailsProjection projection = CreateProjection(
                rarity: 1,
                damage: 20,
                detailsStats: new ItemDetailsStats(
                    ItemDetailsDamageType.Melee,
                    knockback: null,
                    baseCriticalHitChance: null,
                    useTimeTicks: null,
                    tagDamage: null));

            ItemDetailsStatPresentation damage = ItemDetailsPresentation.BuildStats(projection, alternate)
                .Single(stat => stat.Kind == ItemDetailsStatKind.Damage);

            Assert.Multiple(() =>
            {
                Assert.That(damage.ValueText, Is.EqualTo("20 Translated melee"));
                Assert.That(damage.TooltipText, Is.EqualTo("Translated melee translated damage"));
            });
        }

        [TestCase(-11, 255, 175, 0)]
        [TestCase(-1, 130, 130, 130)]
        [TestCase(0, 255, 255, 255)]
        [TestCase(1, 150, 150, 255)]
        [TestCase(5, 255, 150, 255)]
        [TestCase(10, 255, 40, 100)]
        [TestCase(11, 180, 40, 255)]
        [TestCase(99, 180, 40, 255)]
        [TestCase(-10, 255, 255, 255)]
        public void GetRarityColor_FixedRaritiesMatchConfirmedVanillaMapping(
            int rarity,
            int expectedR,
            int expectedG,
            int expectedB)
        {
            Color4 color = ItemDetailsPresentation.GetRarityColor(
                rarity,
                discoR: 1,
                discoG: 2,
                discoB: 3,
                masterGreen: 4);

            AssertColor(color, expectedR, expectedG, expectedB);
        }

        [Test]
        public void GetRarityColor_DiscoRarityUsesProvidedDynamicColor()
        {
            Color4 color = ItemDetailsPresentation.GetRarityColor(
                -12,
                discoR: 12,
                discoG: 34,
                discoB: 56,
                masterGreen: 78);

            AssertColor(color, 12, 34, 56);
        }

        [Test]
        public void GetRarityColor_MasterRarityUsesProvidedDynamicGreenComponent()
        {
            Color4 color = ItemDetailsPresentation.GetRarityColor(
                -13,
                discoR: 12,
                discoG: 34,
                discoB: 56,
                masterGreen: 123);

            AssertColor(color, 255, 123, 0);
        }

        private static ItemDetailsProjection CreateProjection(
            int rarity,
            int damage = 0,
            int defense = 0,
            int pickPower = 0,
            int axePower = 0,
            int hammerPower = 0,
            int fishingPower = 0,
            ItemDetailsStats detailsStats = default)
        {
            return new ItemDetailsProjection(
                itemId: 1,
                name: "Item",
                description: string.Empty,
                value: 0,
                rarity: rarity,
                damage: damage,
                defense: defense,
                pickPower: pickPower,
                axePower: axePower,
                hammerPower: hammerPower,
                fishingPower: fishingPower,
                detailsStats: detailsStats,
                isFound: true,
                researchStatus: ItemResearchStatus.Unavailable,
                armorSets: [],
                recipeDataAvailable: false,
                producingRecipeCount: 0,
                usedInResults: [],
                hasRecipe: false,
                isCraftableNow: false,
                craftingStationRequiredTileId: null,
                npcLootDataAvailable: false,
                droppedByNpcSources: [],
                worldLootDataAvailable: false,
                worldSources: [],
                openableItemLootDataAvailable: false,
                openableSources: [],
                openableContents: [],
                fishingSourceDataAvailable: false,
                fishingVariants: []);
        }

        private static void AssertColor(Color4 color, int r, int g, int b)
        {
            Assert.Multiple(() =>
            {
                Assert.That((int)color.R, Is.EqualTo(r));
                Assert.That((int)color.G, Is.EqualTo(g));
                Assert.That((int)color.B, Is.EqualTo(b));
                Assert.That((int)color.A, Is.EqualTo(255));
            });
        }
    }
}