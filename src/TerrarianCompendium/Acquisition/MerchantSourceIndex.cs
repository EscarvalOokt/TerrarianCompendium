using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Acquisition
{
    internal enum MerchantSourceConditionKind
    {
        HardMode,
        DayTime,
        BloodMoon,
        Halloween,
        Xmas,
        BirthdayParty,
        HappyWindyDay,
        Eclipse,
        LanternNight,
        Storm,
        WorldCrimson,
        WorldRemix,
        WorldTenthAnniversary,
        WorldNotTheBees,
        WorldGetGood,
        WorldInfectedSeed,
        WorldVampireSeed,
        WorldShadowOrbSmashed,
        WorldSilverOreTier,
        ZoneSnow,
        ZoneJungle,
        ZoneGraveyard,
        ZoneBeach,
        ZoneDesert,
        ZoneHallow,
        ZoneCorrupt,
        ZoneCrimson,
        ZoneGlowshroom,
        ZoneSkyHeight,
        ZoneUnderworldHeight,
        ShoppingZoneForest,
        DownedBoss1,
        DownedBoss2,
        DownedBoss3,
        DownedSlimeKing,
        DownedQueenSlime,
        DownedQueenBee,
        DownedClown,
        DownedAncientCultist,
        DownedFrost,
        DownedPirates,
        DownedPlantBoss,
        DownedGolemBoss,
        DownedMartians,
        DownedMoonlord,
        DownedMechBossAny,
        DownedMechBoss1,
        DownedMechBoss2,
        DownedMechBoss3,
        DownedTowerSolar,
        DownedDeerclops,
        NpcPresent,
        PlayerHasItem,
        PlayerHasItemInAnyInventory,
        PlayerLifeMaxAtLeast,
        PlayerManaMaxAtLeast,
        PlayerCoinValueAtLeast,
        PlayerTeamNonZero,
        MultiplayerClient,
        GolferScoreAtLeast,
        GolferScoreGreaterThan,
        BestiaryCompletionAtLeastBasisPoints,
        BestiaryFairyTorchUnlocked,
        MoonPhase,
        SkeletonMerchantEarlyCycle,
        PlayerAteArtisanBread,
        PylonNearbyNpcRequirement,
        PylonForestLocation,
        PylonCavernLocation,
        PylonOceanLocation
    }

    internal readonly struct MerchantSourceCondition(
        MerchantSourceConditionKind kind,
        int argument = 0,
        bool isNegated = false) : IEquatable<MerchantSourceCondition>, IComparable<MerchantSourceCondition>
    {
        public MerchantSourceConditionKind Kind { get; } = kind;

        public int Argument { get; } = argument;

        public bool IsNegated { get; } = isNegated;

        public int CompareTo(MerchantSourceCondition other)
        {
            int kindComparison = Kind.CompareTo(other.Kind);

            if (kindComparison != 0)
                return kindComparison;

            int argumentComparison = Argument.CompareTo(other.Argument);

            if (argumentComparison != 0)
                return argumentComparison;

            return IsNegated.CompareTo(other.IsNegated);
        }

        public bool Equals(MerchantSourceCondition other)
        {
            return Kind == other.Kind && Argument == other.Argument && IsNegated == other.IsNegated;
        }

        public override bool Equals(object obj)
        {
            return obj is MerchantSourceCondition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = hash * 397 ^ Argument;
                hash = hash * 397 ^ IsNegated.GetHashCode();
                return hash;
            }
        }
    }

    [Flags]
    internal enum MerchantSourceAvailabilityFlags
    {
        None = 0,
        RandomStock = 1 << 0,
        ShopCapacityLimited = 1 << 1
    }

    internal readonly struct MerchantSourceSpecialPrice : IEquatable<MerchantSourceSpecialPrice>
    {
        public MerchantSourceSpecialPrice(int currencyItemId, int amount)
        {
            if (currencyItemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(currencyItemId));

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            CurrencyItemId = currencyItemId;
            Amount = amount;
        }

        public int CurrencyItemId { get; }

        public int Amount { get; }

        public bool Equals(MerchantSourceSpecialPrice other)
        {
            return CurrencyItemId == other.CurrencyItemId && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is MerchantSourceSpecialPrice other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CurrencyItemId * 397) ^ Amount;
            }
        }
    }

    internal sealed class MerchantSourceVariant : IEquatable<MerchantSourceVariant>, IComparable<MerchantSourceVariant>
    {
        public MerchantSourceVariant(
            int nativeShopId,
            IEnumerable<MerchantSourceCondition> conditions,
            MerchantSourceAvailabilityFlags availabilityFlags = MerchantSourceAvailabilityFlags.None,
            MerchantSourceSpecialPrice? specialPrice = null)
        {
            if (nativeShopId <= 0)
                throw new ArgumentOutOfRangeException(nameof(nativeShopId));

            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));

            NativeShopId = nativeShopId;
            AvailabilityFlags = availabilityFlags;
            SpecialPrice = specialPrice;

            var unique = new HashSet<MerchantSourceCondition>(conditions);
            var ordered = new List<MerchantSourceCondition>(unique);
            ordered.Sort();
            Conditions = new ReadOnlyCollection<MerchantSourceCondition>(ordered);
        }

        public int NativeShopId { get; }

        public IReadOnlyList<MerchantSourceCondition> Conditions { get; }

        public MerchantSourceAvailabilityFlags AvailabilityFlags { get; }

        public MerchantSourceSpecialPrice? SpecialPrice { get; }

        public int CompareTo(MerchantSourceVariant other)
        {
            if (ReferenceEquals(other, null))
                return 1;

            int shopComparison = NativeShopId.CompareTo(other.NativeShopId);

            if (shopComparison != 0)
                return shopComparison;

            int flagsComparison = AvailabilityFlags.CompareTo(other.AvailabilityFlags);

            if (flagsComparison != 0)
                return flagsComparison;

            int conditionCountComparison = Conditions.Count.CompareTo(other.Conditions.Count);

            if (conditionCountComparison != 0)
                return conditionCountComparison;

            for (var index = 0; index < Conditions.Count; index++)
            {
                int conditionComparison = Conditions[index].CompareTo(other.Conditions[index]);

                if (conditionComparison != 0)
                    return conditionComparison;
            }

            MerchantSourceSpecialPrice? specialPrice = SpecialPrice;
            MerchantSourceSpecialPrice? otherSpecialPrice = other.SpecialPrice;

            if (specialPrice.HasValue != otherSpecialPrice.HasValue)
                return specialPrice.HasValue ? 1 : -1;

            if (!specialPrice.HasValue)
                return 0;

            MerchantSourceSpecialPrice leftPrice = specialPrice.GetValueOrDefault();
            MerchantSourceSpecialPrice rightPrice = otherSpecialPrice.GetValueOrDefault();

            int currencyComparison = leftPrice.CurrencyItemId.CompareTo(rightPrice.CurrencyItemId);

            return currencyComparison != 0 ? currencyComparison : leftPrice.Amount.CompareTo(rightPrice.Amount);
        }

        public bool Equals(MerchantSourceVariant other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other == null ||
                NativeShopId != other.NativeShopId ||
                AvailabilityFlags != other.AvailabilityFlags ||
                !Nullable.Equals(SpecialPrice, other.SpecialPrice) ||
                Conditions.Count != other.Conditions.Count)
            {
                return false;
            }

            for (var index = 0; index < Conditions.Count; index++)
            {
                if (!Conditions[index].Equals(other.Conditions[index]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is MerchantSourceVariant other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NativeShopId;
                hash = hash * 397 ^ (int)AvailabilityFlags;
                hash = hash * 397 ^ (SpecialPrice?.GetHashCode() ?? 0);

                for (var index = 0; index < Conditions.Count; index++)
                    hash = hash * 397 ^ Conditions[index].GetHashCode();

                return hash;
            }
        }
    }

    internal readonly struct MerchantSourceRelation(int merchantNpcId, int targetItemId, MerchantSourceVariant variant)
    {
        public int MerchantNpcId { get; } = merchantNpcId;

        public int TargetItemId { get; } = targetItemId;

        public MerchantSourceVariant Variant { get; } = variant ?? throw new ArgumentNullException(nameof(variant));
    }

    internal sealed class MerchantSourceOffer
    {
        internal MerchantSourceOffer(int merchantNpcId, int targetItemId, IReadOnlyList<MerchantSourceVariant> variants)
        {
            MerchantNpcId = merchantNpcId;
            TargetItemId = targetItemId;
            Variants = variants ?? throw new ArgumentNullException(nameof(variants));
        }

        public int MerchantNpcId { get; }

        public int TargetItemId { get; }

        public IReadOnlyList<MerchantSourceVariant> Variants { get; }
    }

    internal sealed class MerchantSourceIndex
    {
        private static readonly IReadOnlyList<MerchantSourceOffer> _emptyOffers = Array.Empty<MerchantSourceOffer>();

        private readonly Dictionary<int, IReadOnlyList<MerchantSourceOffer>> _offersByItemId;
        private readonly Dictionary<int, IReadOnlyList<MerchantSourceOffer>> _stockByMerchantNpcId;

        public MerchantSourceIndex(IEnumerable<MerchantSourceRelation> relations)
        {
            if (relations == null)
                throw new ArgumentNullException(nameof(relations));

            var variantsByPair = new Dictionary<long, HashSet<MerchantSourceVariant>>();
            var identityByPair = new Dictionary<long, PairIdentity>();

            foreach (MerchantSourceRelation relation in relations)
            {
                if (relation.MerchantNpcId <= 0 || relation.TargetItemId <= 0)
                    continue;

                long pairKey = CreatePairKey(relation.MerchantNpcId, relation.TargetItemId);

                if (!variantsByPair.TryGetValue(pairKey, out HashSet<MerchantSourceVariant> variants))
                {
                    variants = new HashSet<MerchantSourceVariant>();
                    variantsByPair.Add(pairKey, variants);
                    identityByPair.Add(pairKey, new PairIdentity(relation.MerchantNpcId, relation.TargetItemId));
                }

                variants.Add(relation.Variant);
            }

            var mutableByItem = new Dictionary<int, List<MerchantSourceOffer>>();
            var mutableByMerchant = new Dictionary<int, List<MerchantSourceOffer>>();

            foreach (KeyValuePair<long, HashSet<MerchantSourceVariant>> pair in variantsByPair)
            {
                PairIdentity identity = identityByPair[pair.Key];
                var orderedVariants = new List<MerchantSourceVariant>(pair.Value);
                orderedVariants.Sort();

                var offer = new MerchantSourceOffer(
                    identity.MerchantNpcId,
                    identity.TargetItemId,
                    new ReadOnlyCollection<MerchantSourceVariant>(orderedVariants));

                AddOffer(mutableByItem, identity.TargetItemId, offer);
                AddOffer(mutableByMerchant, identity.MerchantNpcId, offer);
            }

            _offersByItemId = FreezeOffers(mutableByItem, CompareByMerchantThenItem);
            _stockByMerchantNpcId = FreezeOffers(mutableByMerchant, CompareByItemThenMerchant);

            var itemIds = new List<int>(_offersByItemId.Keys);
            itemIds.Sort();
            ItemIds = new ReadOnlyCollection<int>(itemIds);
        }

        public IReadOnlyList<int> ItemIds { get; }

        public bool ContainsItem(int itemId)
        {
            return itemId > 0 && _offersByItemId.ContainsKey(itemId);
        }

        public bool ContainsMerchant(int merchantNpcId)
        {
            return merchantNpcId > 0 && _stockByMerchantNpcId.ContainsKey(merchantNpcId);
        }

        public IReadOnlyList<MerchantSourceOffer> GetOffersForItem(int itemId)
        {
            return itemId > 0 && _offersByItemId.TryGetValue(itemId, out IReadOnlyList<MerchantSourceOffer> offers)
                ? offers
                : _emptyOffers;
        }

        public IReadOnlyList<MerchantSourceOffer> GetStockForMerchant(int merchantNpcId)
        {
            return merchantNpcId > 0 &&
                   _stockByMerchantNpcId.TryGetValue(merchantNpcId, out IReadOnlyList<MerchantSourceOffer> offers)
                ? offers
                : _emptyOffers;
        }

        private static void AddOffer(
            Dictionary<int, List<MerchantSourceOffer>> target,
            int key,
            MerchantSourceOffer offer)
        {
            if (!target.TryGetValue(key, out List<MerchantSourceOffer> offers))
            {
                offers = new List<MerchantSourceOffer>();
                target.Add(key, offers);
            }

            offers.Add(offer);
        }

        private static Dictionary<int, IReadOnlyList<MerchantSourceOffer>> FreezeOffers(
            Dictionary<int, List<MerchantSourceOffer>> source,
            Comparison<MerchantSourceOffer> comparison)
        {
            var result = new Dictionary<int, IReadOnlyList<MerchantSourceOffer>>(source.Count);

            foreach (KeyValuePair<int, List<MerchantSourceOffer>> pair in source)
            {
                pair.Value.Sort(comparison);
                result.Add(pair.Key, new ReadOnlyCollection<MerchantSourceOffer>(pair.Value));
            }

            return result;
        }

        private static int CompareByMerchantThenItem(MerchantSourceOffer left, MerchantSourceOffer right)
        {
            int merchantComparison = left.MerchantNpcId.CompareTo(right.MerchantNpcId);

            return merchantComparison != 0 ? merchantComparison : left.TargetItemId.CompareTo(right.TargetItemId);
        }

        private static int CompareByItemThenMerchant(MerchantSourceOffer left, MerchantSourceOffer right)
        {
            int itemComparison = left.TargetItemId.CompareTo(right.TargetItemId);

            return itemComparison != 0 ? itemComparison : left.MerchantNpcId.CompareTo(right.MerchantNpcId);
        }

        private static long CreatePairKey(int merchantNpcId, int itemId)
        {
            return ((long)merchantNpcId << 32) | (uint)itemId;
        }

        private readonly struct PairIdentity(int merchantNpcId, int targetItemId)
        {
            public int MerchantNpcId { get; } = merchantNpcId;

            public int TargetItemId { get; } = targetItemId;
        }
    }
}