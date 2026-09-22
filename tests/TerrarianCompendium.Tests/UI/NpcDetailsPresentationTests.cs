using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class NpcDetailsPresentationTests
    {
        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(NpcDetailsPresentation).Assembly,
            CompendiumLocalization.SourceCultureName);

        [Test]
        public void BuildStats_FullProjection_UsesStableOrderFormattingAndTooltips()
        {
            NpcDetailsProjection projection = CreateProjection(
                new NpcBestiaryStatsSnapshot(
                    damage: 123,
                    lifeMax: 456789,
                    defense: 42,
                    knockbackResist: 0.375f,
                    monetaryValue: 1000f),
                bestiaryRarityStars: 4,
                rareSpawnRarityLevel: 2);

            NpcDetailsStatPresentation[] stats = NpcDetailsPresentation.BuildStats(projection, _localization).ToArray();

            Assert.That(
                stats.Select(stat => stat.Kind).ToArray(),
                Is.EqualTo(
                [
                    NpcDetailsStatKind.Damage,
                    NpcDetailsStatKind.MaxLife,
                    NpcDetailsStatKind.Defense,
                    NpcDetailsStatKind.KnockbackTaken,
                    NpcDetailsStatKind.BestiaryRarity,
                    NpcDetailsStatKind.RareCreatureLevel
                ]));
            Assert.That(
                stats.Select(stat => stat.ValueText).ToArray(),
                Is.EqualTo(["123", "456789", "42", "37.5%", "★★★★", "2"]));
            Assert.That(
                stats.Select(stat => stat.TooltipText).ToArray(),
                Is.EqualTo(
                [
                    "Damage: 123",
                    "Max life: 456789",
                    "Defense: 42",
                    "Knockback taken: 37.5%",
                    "Rarity: ★★★★",
                    "Rare creature: level 2"
                ]));
        }

        [Test]
        public void BuildStats_HiddenCombatStats_PreservesBestiaryMetadataOnly()
        {
            NpcDetailsProjection projection = CreateProjection(
                stats: null,
                bestiaryRarityStars: 0,
                rareSpawnRarityLevel: 3);

            NpcDetailsStatPresentation[] stats = NpcDetailsPresentation.BuildStats(projection, _localization).ToArray();

            Assert.That(
                stats.Select(stat => stat.Kind).ToArray(),
                Is.EqualTo([NpcDetailsStatKind.BestiaryRarity, NpcDetailsStatKind.RareCreatureLevel]));
            Assert.That(stats.Select(stat => stat.ValueText).ToArray(), Is.EqualTo(["None", "3"]));
        }

        [Test]
        public void BuildStats_WithoutRareCreatureLevel_OmitsRareCreatureEntryWithoutGap()
        {
            NpcDetailsProjection projection = CreateProjection(
                new NpcBestiaryStatsSnapshot(10, 100, 5, 0.5f, 250f),
                bestiaryRarityStars: 2,
                rareSpawnRarityLevel: null);

            NpcDetailsStatPresentation[] stats = NpcDetailsPresentation.BuildStats(projection, _localization).ToArray();

            Assert.That(stats, Has.Length.EqualTo(5));
            Assert.That(stats.Any(stat => stat.Kind == NpcDetailsStatKind.RareCreatureLevel), Is.False);
            Assert.That(stats[stats.Length - 1].Kind, Is.EqualTo(NpcDetailsStatKind.BestiaryRarity));
            Assert.That(stats[stats.Length - 1].ValueText, Is.EqualTo("★★"));
        }

        [TestCase(0, "None")]
        [TestCase(1, "★")]
        [TestCase(5, "★★★★★")]
        public void BuildStats_BestiaryRarity_UsesExistingStarPresentation(int rarityStars, string expected)
        {
            NpcDetailsProjection projection = CreateProjection(
                stats: null,
                bestiaryRarityStars: rarityStars,
                rareSpawnRarityLevel: null);

            NpcDetailsStatPresentation rarity = NpcDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == NpcDetailsStatKind.BestiaryRarity);

            Assert.That(rarity.ValueText, Is.EqualTo(expected));
        }

        [TestCase(0f, "0%")]
        [TestCase(0.25f, "25%")]
        [TestCase(0.375f, "37.5%")]
        [TestCase(1f, "100%")]
        public void BuildStats_KnockbackTaken_UsesPercentagePresentation(float resistance, string expected)
        {
            NpcDetailsProjection projection = CreateProjection(
                new NpcBestiaryStatsSnapshot(10, 100, 5, resistance, 250f));

            NpcDetailsStatPresentation knockback = NpcDetailsPresentation.BuildStats(projection, _localization)
                .Single(stat => stat.Kind == NpcDetailsStatKind.KnockbackTaken);

            Assert.That(knockback.ValueText, Is.EqualTo(expected));
        }

        [Test]
        public void BuildStats_RepresentativeLargeValues_AreNotAbbreviated()
        {
            NpcDetailsProjection projection = CreateProjection(
                new NpcBestiaryStatsSnapshot(
                    damage: 123456789,
                    lifeMax: 987654321,
                    defense: 7654321,
                    knockbackResist: 0.1f,
                    monetaryValue: 0f));

            NpcDetailsStatPresentation[] stats = NpcDetailsPresentation.BuildStats(projection, _localization).ToArray();

            Assert.That(
                stats.Single(stat => stat.Kind == NpcDetailsStatKind.Damage).ValueText,
                Is.EqualTo("123456789"));
            Assert.That(
                stats.Single(stat => stat.Kind == NpcDetailsStatKind.MaxLife).ValueText,
                Is.EqualTo("987654321"));
            Assert.That(stats.Single(stat => stat.Kind == NpcDetailsStatKind.Defense).ValueText, Is.EqualTo("7654321"));
        }

        [Test]
        public void BuildStats_AlternateLocale_ChangesTooltipsWithoutChangingValues()
        {
            var translated = new Dictionary<string, string>
            {
                [CompendiumTextKeys.NpcDetails.Damage] = "Translated damage: {0}",
                [CompendiumTextKeys.NpcDetails.Rarity] = "Translated rarity: {0}",
                [CompendiumTextKeys.NpcDetails.RareCreature] = "Translated rare: {0}"
            };
            var alternate = CompendiumLocalization.CreateForTesting(_localization.SourceEntries, translated, "uk-UA");
            alternate.SynchronizeCulture("uk-UA");
            NpcDetailsProjection projection = CreateProjection(
                new NpcBestiaryStatsSnapshot(20, 100, 5, 0.5f, 250f),
                bestiaryRarityStars: 2,
                rareSpawnRarityLevel: 4);

            NpcDetailsStatPresentation[] stats = NpcDetailsPresentation.BuildStats(projection, alternate).ToArray();

            NpcDetailsStatPresentation damage = stats.Single(stat => stat.Kind == NpcDetailsStatKind.Damage);
            NpcDetailsStatPresentation rarity = stats.Single(stat => stat.Kind == NpcDetailsStatKind.BestiaryRarity);
            NpcDetailsStatPresentation rare = stats.Single(stat => stat.Kind == NpcDetailsStatKind.RareCreatureLevel);

            Assert.Multiple(() =>
            {
                Assert.That(damage.ValueText, Is.EqualTo("20"));
                Assert.That(damage.TooltipText, Is.EqualTo("Translated damage: 20"));
                Assert.That(rarity.ValueText, Is.EqualTo("★★"));
                Assert.That(rarity.TooltipText, Is.EqualTo("Translated rarity: ★★"));
                Assert.That(rare.ValueText, Is.EqualTo("4"));
                Assert.That(rare.TooltipText, Is.EqualTo("Translated rare: 4"));
            });
        }

        private static NpcDetailsProjection CreateProjection(
            NpcBestiaryStatsSnapshot stats,
            int bestiaryRarityStars = 0,
            int? rareSpawnRarityLevel = null)
        {
            var metadata = new NpcBestiaryMetadataSnapshot(
                isEncountered: true,
                name: "NPC",
                stats: stats,
                bestiaryRarityStars: bestiaryRarityStars,
                rareSpawnRarityLevel: rareSpawnRarityLevel,
                spawnConditions: null,
                baseDebuffImmunities: null);

            return new NpcDetailsProjection(
                npcNetId: 10,
                difficulty: NpcDifficultyMode.Classic,
                metadata: metadata,
                lootRows: Array.Empty<NpcDetailsLootRow>());
        }
    }
}