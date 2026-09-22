using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Tests.Details
{
    [TestFixture]
    public sealed class MerchantSourceConditionPresentationTests
    {
        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(MerchantSourceConditionPresentation).Assembly,
            CompendiumLocalization.SourceCultureName);

        [Test]
        public void GetDescription_BooleanConditions_UsePositiveAndNegativePresentation()
        {
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.DayTime)),
                Is.EqualTo("Daytime"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)),
                Is.EqualTo("Night"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.HardMode)),
                Is.EqualTo("Hardmode"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.HardMode, isNegated: true)),
                Is.EqualTo("Pre-Hardmode"));
        }

        [Test]
        public void GetDescription_ParameterizedNames_UseResolversAndSafeFallbacks()
        {
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.PlayerHasItem, 42),
                    itemNameResolver: id => id == 42 ? "Magic Item" : null),
                Is.EqualTo("Player has Magic Item"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.NpcPresent, 17),
                    npcNameResolver: id => id == 17 ? "Merchant" : null),
                Is.EqualTo("Merchant is present"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.PlayerHasItem, 99)),
                Is.EqualTo("Player has Unknown item"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.NpcPresent, 88)),
                Is.EqualTo("Unknown NPC is present"));
        }

        [Test]
        public void GetDescription_WorldSilverOreTier_ResolvesActualOreAlternative()
        {
            string ResolveItemName(int itemId)
            {
                return itemId switch
                {
                    14 => "Silver Ore",
                    701 => "Tungsten Ore",
                    _ => null
                };
            }

            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.WorldSilverOreTier, 168),
                    itemNameResolver: ResolveItemName),
                Is.EqualTo("World silver-tier ore: Tungsten Ore"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.WorldSilverOreTier, 168, isNegated: true),
                    itemNameResolver: ResolveItemName),
                Is.EqualTo("World silver-tier ore: Silver Ore"));
        }

        [Test]
        public void GetDescription_CoinRequirement_UsesLocalizedDenominationName()
        {
            Func<int, string> itemNameResolver = id => id == 74 ? "Platinum Coin" : null;

            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.PlayerCoinValueAtLeast, 1000000),
                    itemNameResolver: itemNameResolver),
                Is.EqualTo("Player coin value at least 1 Platinum Coin"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(
                        MerchantSourceConditionKind.PlayerCoinValueAtLeast,
                        1000000,
                        isNegated: true),
                    itemNameResolver: itemNameResolver),
                Is.EqualTo("Player coin value below 1 Platinum Coin"));
        }

        [Test]
        public void GetDescription_MoonPhase_UsesNativeNamesForAllKnownPhases()
        {
            string[] nativeKeys =
            [
                "GameUI.FullMoon",
                "GameUI.WaningGibbous",
                "GameUI.ThirdQuarter",
                "GameUI.WaningCrescent",
                "GameUI.NewMoon",
                "GameUI.WaxingCrescent",
                "GameUI.FirstQuarter",
                "GameUI.WaxingGibbous"
            ];
            string[] names =
            [
                "Full Moon",
                "Waning Gibbous",
                "Third Quarter",
                "Waning Crescent",
                "New Moon",
                "Waxing Crescent",
                "First Quarter",
                "Waxing Gibbous"
            ];
            var nativeText = new Dictionary<string, string>();

            for (var index = 0; index < nativeKeys.Length; index++)
                nativeText.Add(nativeKeys[index], names[index]);

            for (var phase = 0; phase < names.Length; phase++)
            {
                string description = MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, phase),
                    nativeTextResolver: key => nativeText.TryGetValue(key, out string value) ? value : null);

                Assert.That(description, Is.EqualTo("Moon phase: " + names[phase]));
            }
        }

        [Test]
        public void GetDescription_MoonPhase_InvalidValue_UsesSafeFallback()
        {
            string description = MerchantSourceConditionPresentation.GetDescription(
                _localization,
                new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, 99),
                nativeTextResolver: _ => "Unexpected");

            Assert.That(description, Is.EqualTo("Moon phase: Unknown moon phase"));
        }

        [Test]
        public void GetDescription_AuditedProgressionAndPylonConditions_UsePlayerFacingText()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.DownedBoss1)),
                    Is.EqualTo("Eye of Cthulhu defeated"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.DownedBoss2)),
                    Is.EqualTo("Eater of Worlds or Brain of Cthulhu defeated"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.DownedMechBoss2)),
                    Is.EqualTo("The Twins defeated"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.DownedFrost)),
                    Is.EqualTo("Frost Legion completed"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.PylonNearbyNpcRequirement)),
                    Is.EqualTo("At least 2 housed town NPCs nearby"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.WorldGetGood)),
                    Is.EqualTo("For the Worthy world"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.WorldShadowOrbSmashed)),
                    Is.EqualTo("At least one Shadow Orb or Crimson Heart destroyed"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.ShoppingZoneForest)),
                    Is.EqualTo("Surface Forest area"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.ZoneSkyHeight)),
                    Is.EqualTo("Space / Sky height"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.ZoneUnderworldHeight)),
                    Is.EqualTo("Underworld"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.BestiaryFairyTorchUnlocked)),
                    Is.EqualTo("Pink, Green, and Blue Fairies discovered in the Bestiary"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.PlayerAteArtisanBread)),
                    Is.EqualTo("Artisan Loaf bonus consumed"));
                Assert.That(
                    MerchantSourceConditionPresentation.GetDescription(
                        _localization,
                        new MerchantSourceCondition(MerchantSourceConditionKind.MultiplayerClient)),
                    Is.EqualTo("Multiplayer"));
                Assert.That(
                    _localization.Get(CompendiumTextKeys.Merchant.RandomStock),
                    Is.EqualTo("Randomly selected for the Traveling Merchant's stock"));
                Assert.That(
                    _localization.Get(CompendiumTextKeys.Merchant.ShopCapacityLimited),
                    Is.EqualTo("May not appear if the shop has no free slot"));
            });
        }

        [Test]
        public void GetDescription_NumericConditions_IncludeNormalizedArguments()
        {
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(
                        MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints,
                        1250)),
                Is.EqualTo("Bestiary completion at least 12.5%"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(
                        MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints,
                        1250,
                        isNegated: true)),
                Is.EqualTo("Bestiary completion below 12.5%"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.PlayerLifeMaxAtLeast, 400)),
                Is.EqualTo("Maximum life at least 400"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(
                        MerchantSourceConditionKind.PlayerLifeMaxAtLeast,
                        400,
                        isNegated: true)),
                Is.EqualTo("Maximum life below 400"));
        }

        [Test]
        public void GetDescription_AllKnownKinds_HaveNonEmptyPresentation()
        {
            foreach (MerchantSourceConditionKind kind in Enum.GetValues(typeof(MerchantSourceConditionKind)))
            {
                string description = MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(kind, 25),
                    itemNameResolver: id => $"Item {id}",
                    npcNameResolver: id => $"NPC {id}",
                    nativeTextResolver: key => key);

                Assert.That(description, Is.Not.Null, $"Missing presentation for {kind}.");
                Assert.That(description, Is.Not.Empty, $"Missing presentation for {kind}.");
            }
        }

        [Test]
        public void GetDescription_AlternateLocale_UsesTranslatedConditionText()
        {
            var translated = new Dictionary<string, string>
            {
                [CompendiumTextKeys.Merchant.Condition(MerchantSourceConditionKind.DayTime, false)] =
                    "Translated daytime"
            };
            var alternate = CompendiumLocalization.CreateForTesting(_localization.SourceEntries, translated);
            alternate.SynchronizeCulture("test");

            string description = MerchantSourceConditionPresentation.GetDescription(
                alternate,
                new MerchantSourceCondition(MerchantSourceConditionKind.DayTime));

            Assert.That(description, Is.EqualTo("Translated daytime"));
        }

        [Test]
        public void GetDescription_UnknownKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition((MerchantSourceConditionKind)int.MaxValue));
            });
        }
    }
}