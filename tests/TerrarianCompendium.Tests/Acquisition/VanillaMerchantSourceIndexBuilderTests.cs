using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class VanillaMerchantSourceIndexBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            _index = new VanillaMerchantSourceIndexBuilder().Build();
        }

        private MerchantSourceIndex _index;

        [Test]
        public void Merchant_ContainsAlwaysAndNightOnlyRepresentativeOffers()
        {
            MerchantSourceOffer miningHelmet = GetOffer(17, 88);
            MerchantSourceOffer glowstick = GetOffer(17, 282);

            Assert.That(miningHelmet.Variants, Has.Count.EqualTo(1));
            Assert.That(miningHelmet.Variants[0].NativeShopId, Is.EqualTo(1));
            Assert.That(miningHelmet.Variants[0].Conditions, Is.Empty);
            Assert.That(miningHelmet.Variants[0].SpecialPrice, Is.Null);

            Assert.That(
                glowstick.Variants.Any(variant => HasCondition(
                    variant,
                    MerchantSourceConditionKind.DayTime,
                    isNegated: true)),
                Is.True);
        }

        [Test]
        public void Merchant_JungleOffer_PreservesBothNativeAlternatives()
        {
            MerchantSourceOffer offer = GetOffer(17, 33);

            Assert.That(offer.Variants, Has.Count.EqualTo(2));
            Assert.That(
                offer.Variants.Any(variant => HasCondition(variant, MerchantSourceConditionKind.ZoneJungle)),
                Is.True);
            Assert.That(
                offer.Variants.Any(variant =>
                    HasCondition(variant, MerchantSourceConditionKind.WorldTenthAnniversary) &&
                    HasCondition(variant, MerchantSourceConditionKind.WorldNotTheBees) &&
                    HasCondition(variant, MerchantSourceConditionKind.WorldRemix, isNegated: true)),
                Is.True);
        }

        [Test]
        public void Merchant_ProgressionOrOffer_IsNormalizedIntoAlternativeVariants()
        {
            MerchantSourceOffer offer = GetOffer(17, 4063);

            Assert.That(offer.Variants, Has.Count.EqualTo(3));
            Assert.That(
                offer.Variants.Any(variant => HasOnlyCondition(variant, MerchantSourceConditionKind.DownedBoss2)),
                Is.True);
            Assert.That(
                offer.Variants.Any(variant => HasOnlyCondition(variant, MerchantSourceConditionKind.DownedBoss3)),
                Is.True);
            Assert.That(
                offer.Variants.Any(variant => HasOnlyCondition(variant, MerchantSourceConditionKind.HardMode)),
                Is.True);
        }

        [Test]
        public void TravelingMerchant_PreservesRandomAndProgressionGatedStock()
        {
            MerchantSourceOffer ordinaryRandom = GetOffer(368, 3099);
            MerchantSourceOffer hardmodeRandom = GetOffer(368, 2270);

            Assert.That(ordinaryRandom.Variants, Has.Count.EqualTo(1));
            Assert.That(ordinaryRandom.Variants[0].NativeShopId, Is.EqualTo(19));
            Assert.That(
                ordinaryRandom.Variants[0].AvailabilityFlags.HasFlag(MerchantSourceAvailabilityFlags.RandomStock),
                Is.True);
            Assert.That(ordinaryRandom.Variants[0].Conditions, Is.Empty);

            Assert.That(
                hardmodeRandom.Variants.Any(variant =>
                    variant.AvailabilityFlags.HasFlag(MerchantSourceAvailabilityFlags.RandomStock) &&
                    HasCondition(variant, MerchantSourceConditionKind.HardMode)),
                Is.True);
        }

        [Test]
        public void TravelingMerchant_CompanionItem_IsIndexedAsRandomStock()
        {
            MerchantSourceOffer offer = GetOffer(368, 2261);

            Assert.That(
                offer.Variants.Any(variant =>
                    variant.AvailabilityFlags.HasFlag(MerchantSourceAvailabilityFlags.RandomStock)),
                Is.True);
        }

        [Test]
        public void SkeletonMerchant_PreservesMoonAndNegativeWorldConditions()
        {
            MerchantSourceOffer phaseZero = GetOffer(453, 284);
            MerchantSourceOffer phaseOne = GetOffer(453, 946);
            MerchantSourceOffer nonRemixPhaseTwo = GetOffer(453, 3069);

            Assert.That(
                phaseZero.Variants.Any(variant => HasCondition(variant, MerchantSourceConditionKind.MoonPhase)),
                Is.True);
            Assert.That(
                phaseOne.Variants.Any(variant => HasCondition(variant, MerchantSourceConditionKind.MoonPhase, 1)),
                Is.True);
            Assert.That(
                nonRemixPhaseTwo.Variants.Any(variant =>
                    HasCondition(variant, MerchantSourceConditionKind.MoonPhase, 2) &&
                    HasCondition(variant, MerchantSourceConditionKind.WorldRemix, isNegated: true)),
                Is.True);
        }

        [Test]
        public void Tavernkeep_PreservesDefenderMedalPriceAndProgressionTier()
        {
            MerchantSourceOffer tierOne = GetOffer(550, 3818);
            MerchantSourceOffer tierTwo = GetOffer(550, 3819);

            Assert.That(tierOne.Variants, Has.Count.EqualTo(1));
            Assert.That(tierOne.Variants[0].SpecialPrice, Is.EqualTo(new MerchantSourceSpecialPrice(3817, 5)));
            Assert.That(tierOne.Variants[0].Conditions, Is.Empty);

            Assert.That(
                tierTwo.Variants.Any(variant => variant.SpecialPrice.HasValue &&
                                                variant.SpecialPrice.Value.Equals(
                                                    new MerchantSourceSpecialPrice(3817, 15)) &&
                                                HasCondition(variant, MerchantSourceConditionKind.HardMode) &&
                                                HasCondition(variant, MerchantSourceConditionKind.DownedMechBossAny)),
                Is.True);
        }

        [Test]
        public void WitchDoctor_PreservesCompoundAndConditions()
        {
            MerchantSourceOffer offer = GetOffer(228, 1162);

            Assert.That(
                offer.Variants.Any(variant => HasCondition(variant, MerchantSourceConditionKind.HardMode) &&
                                              HasCondition(variant, MerchantSourceConditionKind.ZoneJungle) &&
                                              HasCondition(
                                                  variant,
                                                  MerchantSourceConditionKind.DayTime,
                                                  isNegated: true) &&
                                              HasCondition(variant, MerchantSourceConditionKind.DownedPlantBoss)),
                Is.True);
        }

        [Test]
        public void Clothier_PreservesShopCapacityFlag()
        {
            MerchantSourceOffer offer = GetOffer(54, 5630);

            Assert.That(
                offer.Variants.Any(variant =>
                    variant.AvailabilityFlags.HasFlag(MerchantSourceAvailabilityFlags.ShopCapacityLimited)),
                Is.True);
        }

        [Test]
        public void SharedPylonStock_IsAddedToEligibleShopsAndPreservesPainterShopIdentity()
        {
            MerchantSourceOffer merchantSnowPylon = GetOffer(17, 4920);
            MerchantSourceOffer painterSnowPylon = GetOffer(227, 4920);

            Assert.That(
                merchantSnowPylon.Variants.Any(variant => variant.NativeShopId == 1 &&
                                                          variant.AvailabilityFlags.HasFlag(
                                                              MerchantSourceAvailabilityFlags.ShopCapacityLimited) &&
                                                          HasCondition(
                                                              variant,
                                                              MerchantSourceConditionKind.PylonNearbyNpcRequirement) &&
                                                          HasCondition(variant, MerchantSourceConditionKind.ZoneSnow)),
                Is.True);

            Assert.That(
                painterSnowPylon.Variants.Select(variant => variant.NativeShopId).Distinct(),
                Is.EqualTo([15, 25]));
        }

        [Test]
        public void OrdinaryMerchantOffer_DoesNotStoreRuntimeCoinPrice()
        {
            MerchantSourceOffer offer = GetOffer(17, 88);

            Assert.That(offer.Variants.All(variant => variant.SpecialPrice == null), Is.True);
        }

        private MerchantSourceOffer GetOffer(int merchantNpcId, int itemId)
        {
            MerchantSourceOffer[] offers = _index.GetStockForMerchant(merchantNpcId)
                .Where(offer => offer.TargetItemId == itemId)
                .ToArray();

            Assert.That(offers, Has.Length.EqualTo(1));
            return offers[0];
        }

        private static bool HasOnlyCondition(MerchantSourceVariant variant, MerchantSourceConditionKind kind)
        {
            return variant.Conditions.Count == 1 && HasCondition(variant, kind);
        }

        private static bool HasCondition(
            MerchantSourceVariant variant,
            MerchantSourceConditionKind kind,
            int argument = 0,
            bool isNegated = false)
        {
            return variant.Conditions.Contains(new MerchantSourceCondition(kind, argument, isNegated));
        }
    }
}