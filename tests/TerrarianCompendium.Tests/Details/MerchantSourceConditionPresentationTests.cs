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
        public void GetDescription_ParameterizedNames_UseResolversAndFallbacks()
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
                Is.EqualTo("Player has Item ID 99"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.NpcPresent, 88)),
                Is.EqualTo("NPC ID 88 is present"));
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
                    new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, 3)),
                Is.EqualTo("Moon phase 3"));
            Assert.That(
                MerchantSourceConditionPresentation.GetDescription(
                    _localization,
                    new MerchantSourceCondition(MerchantSourceConditionKind.PlayerLifeMaxAtLeast, 400)),
                Is.EqualTo("Maximum life at least 400"));
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
                    npcNameResolver: id => $"NPC {id}");

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