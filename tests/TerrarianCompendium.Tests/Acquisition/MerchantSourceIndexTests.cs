using System;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class MerchantSourceIndexTests
    {
        [Test]
        public void Constructor_GroupsBidirectionallyDeduplicatesAndSorts()
        {
            var alwaysShop2 = new MerchantSourceVariant(2, Array.Empty<MerchantSourceCondition>());
            var nightShop1 = new MerchantSourceVariant(
                1,
                [new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)]);

            var index = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(20, 300, alwaysShop2),
                new MerchantSourceRelation(10, 300, nightShop1),
                new MerchantSourceRelation(10, 200, alwaysShop2),
                new MerchantSourceRelation(10, 300, nightShop1),
                new MerchantSourceRelation(0, 400, alwaysShop2),
                new MerchantSourceRelation(10, 0, alwaysShop2)
            ]);

            Assert.That(index.ItemIds, Is.EqualTo([200, 300]));
            Assert.That(index.GetOffersForItem(300).Select(offer => offer.MerchantNpcId), Is.EqualTo([10, 20]));
            Assert.That(index.GetStockForMerchant(10).Select(offer => offer.TargetItemId), Is.EqualTo([200, 300]));
            Assert.That(index.GetStockForMerchant(10)[1].Variants, Has.Count.EqualTo(1));
            Assert.That(index.ContainsItem(300), Is.True);
            Assert.That(index.ContainsItem(0), Is.False);
            Assert.That(index.ContainsMerchant(20), Is.True);
            Assert.That(index.ContainsMerchant(-1), Is.False);
        }

        [Test]
        public void Variant_NormalizesConditionsAndKeepsNegationAndArgumentsDistinct()
        {
            var variant = new MerchantSourceVariant(
                4,
                [
                    new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, 3),
                    new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true),
                    new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, 3),
                    new MerchantSourceCondition(MerchantSourceConditionKind.PlayerHasItem, 930)
                ]);

            Assert.That(variant.Conditions, Has.Count.EqualTo(3));
            Assert.That(
                variant.Conditions,
                Does.Contain(new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)));
            Assert.That(
                variant.Conditions,
                Does.Contain(new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, 3)));
            Assert.That(
                variant.Conditions,
                Does.Contain(new MerchantSourceCondition(MerchantSourceConditionKind.PlayerHasItem, 930)));
            Assert.That(
                variant.Conditions,
                Does.Not.Contain(new MerchantSourceCondition(MerchantSourceConditionKind.DayTime)));
        }

        [Test]
        public void Constructor_PreservesAlternativeVariantsForSameMerchantAndItem()
        {
            var index = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    17,
                    4063,
                    new MerchantSourceVariant(
                        1,
                        [new MerchantSourceCondition(MerchantSourceConditionKind.DownedBoss2)])),
                new MerchantSourceRelation(
                    17,
                    4063,
                    new MerchantSourceVariant(
                        1,
                        [new MerchantSourceCondition(MerchantSourceConditionKind.DownedBoss3)])),
                new MerchantSourceRelation(
                    17,
                    4063,
                    new MerchantSourceVariant(1, [new MerchantSourceCondition(MerchantSourceConditionKind.HardMode)]))
            ]);

            MerchantSourceOffer offer = index.GetStockForMerchant(17).Single(offer => offer.TargetItemId == 4063);

            Assert.That(offer.Variants, Has.Count.EqualTo(3));
            Assert.That(
                offer.Variants.Select(variant => variant.Conditions.Single().Kind),
                Is.EquivalentTo(
                [
                    MerchantSourceConditionKind.DownedBoss2,
                    MerchantSourceConditionKind.DownedBoss3,
                    MerchantSourceConditionKind.HardMode
                ]));
        }

        [Test]
        public void Constructor_PreservesFlagsNativeShopAndSpecialPrice()
        {
            var expectedPrice = new MerchantSourceSpecialPrice(3817, 5);
            var variant = new MerchantSourceVariant(
                21,
                [new MerchantSourceCondition(MerchantSourceConditionKind.HardMode)],
                MerchantSourceAvailabilityFlags.RandomStock | MerchantSourceAvailabilityFlags.ShopCapacityLimited,
                expectedPrice);
            var index = new MerchantSourceIndex([new MerchantSourceRelation(550, 3818, variant)]);

            MerchantSourceVariant actual = index.GetStockForMerchant(550).Single().Variants.Single();

            Assert.That(actual.NativeShopId, Is.EqualTo(21));
            Assert.That(
                actual.AvailabilityFlags,
                Is.EqualTo(
                    MerchantSourceAvailabilityFlags.RandomStock | MerchantSourceAvailabilityFlags.ShopCapacityLimited));
            Assert.That(actual.SpecialPrice, Is.EqualTo(expectedPrice));
        }

        [Test]
        public void Queries_ReturnEmptyCollectionsAndFalseForMissingRelations()
        {
            var index = new MerchantSourceIndex(Array.Empty<MerchantSourceRelation>());

            Assert.That(index.GetOffersForItem(1), Is.Empty);
            Assert.That(index.GetStockForMerchant(1), Is.Empty);
        }

        [Test]
        public void SameMerchantAndItem_CanPreserveVariantsFromDifferentNativeShops()
        {
            var index = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    227,
                    4920,
                    new MerchantSourceVariant(15, [new MerchantSourceCondition(MerchantSourceConditionKind.ZoneSnow)])),
                new MerchantSourceRelation(
                    227,
                    4920,
                    new MerchantSourceVariant(25, [new MerchantSourceCondition(MerchantSourceConditionKind.ZoneSnow)]))
            ]);

            MerchantSourceOffer offer = index.GetStockForMerchant(227).Single();

            Assert.That(offer.Variants.Select(variant => variant.NativeShopId), Is.EqualTo([15, 25]));
        }
    }
}