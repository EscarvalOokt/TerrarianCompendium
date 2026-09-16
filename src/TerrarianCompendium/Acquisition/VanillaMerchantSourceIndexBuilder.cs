using System;
using System.Collections.Generic;

namespace TerrarianCompendium.Acquisition
{
    internal sealed class VanillaMerchantSourceIndexBuilder
    {
        private const int Merchant = 17;
        private const int ArmsDealer = 19;
        private const int Dryad = 20;
        private const int Demolitionist = 38;
        private const int Clothier = 54;
        private const int GoblinTinkerer = 107;
        private const int Wizard = 108;
        private const int Mechanic = 124;
        private const int SantaClaus = 142;
        private const int Truffle = 160;
        private const int Steampunker = 178;
        private const int DyeTrader = 207;
        private const int PartyGirl = 208;
        private const int Cyborg = 209;
        private const int Painter = 227;
        private const int WitchDoctor = 228;
        private const int Pirate = 229;
        private const int Stylist = 353;
        private const int TravelingMerchant = 368;
        private const int SkeletonMerchant = 453;
        private const int Tavernkeep = 550;
        private const int Golfer = 588;
        private const int Zoologist = 633;
        private const int Princess = 663;
        private const int DefenderMedal = 3817;

        private static readonly ShopIdentity[] _pylonShops =
        [
            new ShopIdentity(Merchant, 1),
            new ShopIdentity(ArmsDealer, 2),
            new ShopIdentity(Dryad, 3),
            new ShopIdentity(Demolitionist, 4),
            new ShopIdentity(Clothier, 5),
            new ShopIdentity(GoblinTinkerer, 6),
            new ShopIdentity(Wizard, 7),
            new ShopIdentity(Mechanic, 8),
            new ShopIdentity(SantaClaus, 9),
            new ShopIdentity(Truffle, 10),
            new ShopIdentity(Steampunker, 11),
            new ShopIdentity(DyeTrader, 12),
            new ShopIdentity(PartyGirl, 13),
            new ShopIdentity(Cyborg, 14),
            new ShopIdentity(Painter, 15),
            new ShopIdentity(WitchDoctor, 16),
            new ShopIdentity(Pirate, 17),
            new ShopIdentity(Stylist, 18),
            new ShopIdentity(Golfer, 22),
            new ShopIdentity(Zoologist, 23),
            new ShopIdentity(Princess, 24),
            new ShopIdentity(Painter, 25)
        ];

        public MerchantSourceIndex Build()
        {
            var relations = new List<MerchantSourceRelation>();

            AddMerchant(relations);
            AddArmsDealer(relations);
            AddDryad(relations);
            AddDemolitionist(relations);
            AddClothier(relations);
            AddGoblinTinkerer(relations);
            AddWizard(relations);
            AddMechanic(relations);
            AddSantaClaus(relations);
            AddTruffle(relations);
            AddSteampunker(relations);
            AddDyeTrader(relations);
            AddPartyGirl(relations);
            AddCyborg(relations);
            AddPainter(relations);
            AddWitchDoctor(relations);
            AddPirate(relations);
            AddStylist(relations);
            AddTravelingMerchant(relations);
            AddSkeletonMerchant(relations);
            AddTavernkeep(relations);
            AddGolfer(relations);
            AddZoologist(relations);
            AddPrincess(relations);
            AddSharedPylons(relations);

            return new MerchantSourceIndex(relations);
        }

        private static void AddMerchant(List<MerchantSourceRelation> relations)
        {
            const int shop = 1;
            AddMany(relations, Merchant, shop, [88, 87, 35, 1991, 3509, 3506, 8, 28, 110, 40, 42, 965, 1786]);
            Add(
                relations,
                Merchant,
                shop,
                4388,
                C(MerchantSourceConditionKind.WorldNotTheBees),
                Not(MerchantSourceConditionKind.WorldRemix));
            AddMany(relations, Merchant, shop, [188, 189, 488, 1348, 3198], C(MerchantSourceConditionKind.HardMode));
            Add(relations, Merchant, shop, 967, C(MerchantSourceConditionKind.ZoneSnow));
            Add(relations, Merchant, shop, 33, C(MerchantSourceConditionKind.ZoneJungle));
            Add(
                relations,
                Merchant,
                shop,
                33,
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.WorldNotTheBees),
                Not(MerchantSourceConditionKind.WorldRemix));
            Add(
                relations,
                Merchant,
                shop,
                4074,
                C(MerchantSourceConditionKind.DayTime),
                C(MerchantSourceConditionKind.HappyWindyDay));
            Add(relations, Merchant, shop, 279, C(MerchantSourceConditionKind.BloodMoon));
            Add(relations, Merchant, shop, 282, Not(MerchantSourceConditionKind.DayTime));
            Add(relations, Merchant, shop, 5643, C(MerchantSourceConditionKind.BirthdayParty));
            Add(relations, Merchant, shop, 346, C(MerchantSourceConditionKind.DownedBoss3));
            AddMany(relations, Merchant, shop, [931, 1614], C(MerchantSourceConditionKind.PlayerHasItem, 930));
            AddMany(relations, Merchant, shop, [4063, 4673], C(MerchantSourceConditionKind.DownedBoss2));
            AddMany(relations, Merchant, shop, [4063, 4673], C(MerchantSourceConditionKind.DownedBoss3));
            AddMany(relations, Merchant, shop, [4063, 4673], C(MerchantSourceConditionKind.HardMode));
            Add(relations, Merchant, shop, 3108, C(MerchantSourceConditionKind.PlayerHasItem, 3107));
        }

        private static void AddArmsDealer(List<MerchantSourceRelation> relations)
        {
            const int shop = 2;
            AddMany(relations, ArmsDealer, shop, [97, 95, 98]);
            Add(
                relations,
                ArmsDealer,
                shop,
                4915,
                C(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.WorldSilverOreTier, 168));
            Add(
                relations,
                ArmsDealer,
                shop,
                4915,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.WorldSilverOreTier, 168));
            Add(
                relations,
                ArmsDealer,
                shop,
                278,
                C(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.WorldSilverOreTier, 168));
            Add(
                relations,
                ArmsDealer,
                shop,
                278,
                C(MerchantSourceConditionKind.HardMode),
                Not(MerchantSourceConditionKind.WorldSilverOreTier, 168));
            Add(
                relations,
                ArmsDealer,
                shop,
                47,
                C(MerchantSourceConditionKind.DownedBoss2),
                Not(MerchantSourceConditionKind.DayTime));
            Add(relations, ArmsDealer, shop, 47, C(MerchantSourceConditionKind.HardMode));
            Add(
                relations,
                ArmsDealer,
                shop,
                4703,
                C(MerchantSourceConditionKind.ZoneGraveyard),
                C(MerchantSourceConditionKind.DownedBoss3));
            Add(relations, ArmsDealer, shop, 324, Not(MerchantSourceConditionKind.DayTime));
            AddMany(relations, ArmsDealer, shop, [534, 1432, 2177], C(MerchantSourceConditionKind.HardMode));
            Add(relations, ArmsDealer, shop, 1261, C(MerchantSourceConditionKind.PlayerHasItem, 1258));
            Add(relations, ArmsDealer, shop, 1836, C(MerchantSourceConditionKind.PlayerHasItem, 1835));
            Add(relations, ArmsDealer, shop, 3108, C(MerchantSourceConditionKind.PlayerHasItem, 3107));
            Add(relations, ArmsDealer, shop, 1783, C(MerchantSourceConditionKind.PlayerHasItem, 1782));
            Add(relations, ArmsDealer, shop, 1785, C(MerchantSourceConditionKind.PlayerHasItem, 1784));
            AddMany(relations, ArmsDealer, shop, [1736, 1737, 1738], C(MerchantSourceConditionKind.Halloween));
        }

        private static void AddDryad(List<MerchantSourceRelation> relations)
        {
            const int shop = 3;
            Add(
                relations,
                Dryad,
                shop,
                2886,
                C(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.WorldCrimson),
                Not(MerchantSourceConditionKind.WorldRemix));
            Add(
                relations,
                Dryad,
                shop,
                2886,
                C(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.WorldCrimson),
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                Not(MerchantSourceConditionKind.WorldGetGood));
            AddMany(
                relations,
                Dryad,
                shop,
                [2171, 4508],
                C(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.WorldCrimson));
            Add(
                relations,
                Dryad,
                shop,
                67,
                C(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.WorldCrimson),
                Not(MerchantSourceConditionKind.WorldRemix));
            Add(
                relations,
                Dryad,
                shop,
                67,
                C(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.WorldCrimson),
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                Not(MerchantSourceConditionKind.WorldGetGood));
            AddMany(
                relations,
                Dryad,
                shop,
                [59, 4504],
                C(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.WorldCrimson));

            Add(
                relations,
                Dryad,
                shop,
                66,
                Not(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.WorldRemix));
            Add(
                relations,
                Dryad,
                shop,
                66,
                Not(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.WorldInfectedSeed));
            Add(
                relations,
                Dryad,
                shop,
                66,
                Not(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                Not(MerchantSourceConditionKind.WorldGetGood));
            AddMany(relations, Dryad, shop, [62, 63, 745], Not(MerchantSourceConditionKind.BloodMoon));
            Add(
                relations,
                Dryad,
                shop,
                59,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.ZoneGraveyard),
                C(MerchantSourceConditionKind.WorldCrimson));
            Add(
                relations,
                Dryad,
                shop,
                2171,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.ZoneGraveyard),
                Not(MerchantSourceConditionKind.WorldCrimson));

            AddMany(
                relations,
                Dryad,
                shop,
                [
                    27, 5309, 114, 1828, 747, 3215, 3216, 3219, 3220, 3221, 3222, 4047, 4045, 4044, 4043, 4042, 4046,
                    4041, 4241, 4048
                ]);
            AddMany(relations, Dryad, shop, [746, 369, 4505], C(MerchantSourceConditionKind.HardMode));
            Add(relations, Dryad, shop, 5214, C(MerchantSourceConditionKind.ZoneUnderworldHeight));
            Add(
                relations,
                Dryad,
                shop,
                194,
                C(MerchantSourceConditionKind.ZoneGlowshroom),
                Not(MerchantSourceConditionKind.ZoneUnderworldHeight));
            AddMany(relations, Dryad, shop, [1853, 1854], C(MerchantSourceConditionKind.Halloween));
            Add(relations, Dryad, shop, 3218, C(MerchantSourceConditionKind.WorldCrimson));
            Add(relations, Dryad, shop, 3217, Not(MerchantSourceConditionKind.WorldCrimson));

            AddMoonGroup(relations, Dryad, shop, [4430, 4431], 0, 1);
            AddMoonGroup(relations, Dryad, shop, [4433, 4434], 2, 3);
            AddMoonGroup(relations, Dryad, shop, [4436, 4437], 4, 5);
            AddMoonGroup(relations, Dryad, shop, [4439, 4440], 6, 7);
            AddMoonGroup(relations, Dryad, shop, [4432], C(MerchantSourceConditionKind.HardMode), 0, 1);
            AddMoonGroup(relations, Dryad, shop, [4435], C(MerchantSourceConditionKind.HardMode), 2, 3);
            AddMoonGroup(relations, Dryad, shop, [4438], C(MerchantSourceConditionKind.HardMode), 4, 5);
            AddMoonGroup(relations, Dryad, shop, [4441], C(MerchantSourceConditionKind.HardMode), 6, 7);

            Add(
                relations,
                Dryad,
                shop,
                8,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.WorldVampireSeed),
                C(MerchantSourceConditionKind.WorldInfectedSeed));
            Add(
                relations,
                Dryad,
                shop,
                4386,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.WorldVampireSeed),
                C(MerchantSourceConditionKind.WorldInfectedSeed),
                C(MerchantSourceConditionKind.WorldCrimson));
            Add(
                relations,
                Dryad,
                shop,
                4385,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.WorldVampireSeed),
                C(MerchantSourceConditionKind.WorldInfectedSeed),
                Not(MerchantSourceConditionKind.WorldCrimson));
        }

        private static void AddDemolitionist(List<MerchantSourceRelation> relations)
        {
            const int shop = 4;
            AddMany(relations, Demolitionist, shop, [168, 166, 167, 5481, 5464]);
            Add(
                relations,
                Demolitionist,
                shop,
                5542,
                C(MerchantSourceConditionKind.DownedBoss1),
                Not(MerchantSourceConditionKind.DayTime));
            Add(
                relations,
                Demolitionist,
                shop,
                5542,
                C(MerchantSourceConditionKind.DownedSlimeKing),
                Not(MerchantSourceConditionKind.DayTime));
            AddMany(relations, Demolitionist, shop, [265, 1347], C(MerchantSourceConditionKind.HardMode));
            Add(
                relations,
                Demolitionist,
                shop,
                937,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPlantBoss),
                C(MerchantSourceConditionKind.DownedPirates));
            Add(relations, Demolitionist, shop, 4827, C(MerchantSourceConditionKind.PlayerHasItem, 4827));
            Add(relations, Demolitionist, shop, 4824, C(MerchantSourceConditionKind.PlayerHasItem, 4824));
            Add(relations, Demolitionist, shop, 4825, C(MerchantSourceConditionKind.PlayerHasItem, 4825));
            Add(relations, Demolitionist, shop, 4826, C(MerchantSourceConditionKind.PlayerHasItem, 4826));
        }

        private static void AddClothier(List<MerchantSourceRelation> relations)
        {
            const int shop = 5;
            AddMany(relations, Clothier, shop, [254, 981, 269, 270, 271, 5308]);
            Add(relations, Clothier, shop, 242, C(MerchantSourceConditionKind.DayTime));
            AddMoonGroup(relations, Clothier, shop, [245, 246], 0);
            AddMany(
                relations,
                Clothier,
                shop,
                [1288, 1289],
                C(MerchantSourceConditionKind.MoonPhase),
                Not(MerchantSourceConditionKind.DayTime));
            AddMoonGroup(relations, Clothier, shop, [325, 326], 1);
            AddMany(relations, Clothier, shop, [503, 504, 505], C(MerchantSourceConditionKind.DownedClown));
            Add(relations, Clothier, shop, 322, C(MerchantSourceConditionKind.BloodMoon));
            AddMany(
                relations,
                Clothier,
                shop,
                [3362, 3363],
                C(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.DayTime));
            AddMany(
                relations,
                Clothier,
                shop,
                [2856, 2858],
                C(MerchantSourceConditionKind.DownedAncientCultist),
                C(MerchantSourceConditionKind.DayTime));
            AddMany(
                relations,
                Clothier,
                shop,
                [2857, 2859],
                C(MerchantSourceConditionKind.DownedAncientCultist),
                Not(MerchantSourceConditionKind.DayTime));
            AddMany(relations, Clothier, shop, [3242, 3243, 3244], C(MerchantSourceConditionKind.NpcPresent, 441));
            AddMany(
                relations,
                Clothier,
                shop,
                [4685, 4686, 4704, 4705, 4706, 4707, 4708, 4709],
                C(MerchantSourceConditionKind.ZoneGraveyard));
            Add(relations, Clothier, shop, 1429, C(MerchantSourceConditionKind.ZoneSnow));
            Add(relations, Clothier, shop, 1740, C(MerchantSourceConditionKind.Halloween));
            AddMoonGroup(relations, Clothier, shop, [869], C(MerchantSourceConditionKind.HardMode), 2);
            AddMoonGroup(relations, Clothier, shop, [4994, 4997], C(MerchantSourceConditionKind.HardMode), 3);
            AddMoonGroup(relations, Clothier, shop, [864, 865], C(MerchantSourceConditionKind.HardMode), 4);
            AddMoonGroup(relations, Clothier, shop, [4995, 4998], C(MerchantSourceConditionKind.HardMode), 5);
            AddMoonGroup(relations, Clothier, shop, [873, 874, 875], C(MerchantSourceConditionKind.HardMode), 6);
            AddMoonGroup(relations, Clothier, shop, [4996, 4999], C(MerchantSourceConditionKind.HardMode), 7);
            Add(
                relations,
                Clothier,
                shop,
                1275,
                C(MerchantSourceConditionKind.DownedFrost),
                C(MerchantSourceConditionKind.DayTime));
            Add(
                relations,
                Clothier,
                shop,
                1276,
                C(MerchantSourceConditionKind.DownedFrost),
                Not(MerchantSourceConditionKind.DayTime));
            AddMany(relations, Clothier, shop, [3246, 3247], C(MerchantSourceConditionKind.Halloween));
            AddMany(
                relations,
                Clothier,
                shop,
                [3730, 3731, 3733, 3734, 3735],
                C(MerchantSourceConditionKind.BirthdayParty));
            AddFlagged(
                relations,
                Clothier,
                shop,
                4744,
                MerchantSourceAvailabilityFlags.ShopCapacityLimited,
                C(MerchantSourceConditionKind.GolferScoreAtLeast, 2000));
            AddFlagged(relations, Clothier, shop, 5630, MerchantSourceAvailabilityFlags.ShopCapacityLimited);
        }

        private static void AddGoblinTinkerer(List<MerchantSourceRelation> relations)
        {
            AddMany(relations, GoblinTinkerer, 6, [128, 486, 398, 84, 407, 161, 5324]);
        }

        private static void AddWizard(List<MerchantSourceRelation> relations)
        {
            const int shop = 7;
            AddMany(relations, Wizard, shop, [487, 496, 500, 507, 508, 531, 149, 576, 3186]);
            Add(
                relations,
                Wizard,
                shop,
                5461,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.BloodMoon));
            Add(relations, Wizard, shop, 1739, C(MerchantSourceConditionKind.Halloween));
        }

        private static void AddMechanic(List<MerchantSourceRelation> relations)
        {
            const int shop = 8;
            AddMany(
                relations,
                Mechanic,
                shop,
                [
                    509, 850, 851, 3612, 510, 530, 513, 538, 529, 541, 542, 543, 852, 853, 4261, 3707, 2739, 849, 1263,
                    3616, 3725, 2799, 3619, 3627, 3629, 585, 584, 583, 4484, 4485
                ]);
            Add(relations, Mechanic, shop, 4409, C(MerchantSourceConditionKind.ZoneGraveyard));
            AddMoonGroup(relations, Mechanic, shop, [2295], C(MerchantSourceConditionKind.NpcPresent, 369), 1, 3, 5, 7);
        }

        private static void AddSantaClaus(List<MerchantSourceRelation> relations)
        {
            const int shop = 9;
            AddMany(relations, SantaClaus, shop, [588, 589, 590, 597, 598, 596]);

            for (var itemId = 1873; itemId < 1906; itemId++)
                Add(relations, SantaClaus, shop, itemId);
        }

        private static void AddTruffle(List<MerchantSourceRelation> relations)
        {
            const int shop = 10;
            AddMany(relations, Truffle, shop, [756, 787], C(MerchantSourceConditionKind.DownedMechBossAny));
            AddMany(relations, Truffle, shop, [868, 1181, 5231]);
            Add(relations, Truffle, shop, 1551, C(MerchantSourceConditionKind.DownedPlantBoss));
            Add(relations, Truffle, shop, 783, Not(MerchantSourceConditionKind.WorldRemix));
            Add(
                relations,
                Truffle,
                shop,
                783,
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                Not(MerchantSourceConditionKind.WorldGetGood));
        }

        private static void AddSteampunker(List<MerchantSourceRelation> relations)
        {
            const int shop = 11;
            AddWorldGate(relations, Steampunker, shop, 779);
            AddMoonGroup(relations, Steampunker, shop, [748], C(MerchantSourceConditionKind.HardMode), 4, 5, 6, 7);
            AddMany(relations, Steampunker, shop, [839, 840, 841], Not(MerchantSourceConditionKind.HardMode));
            AddMoonGroup(relations, Steampunker, shop, [839, 840, 841], 0, 1, 2, 3);
            Add(relations, Steampunker, shop, 948, C(MerchantSourceConditionKind.DownedGolemBoss));
            Add(relations, Steampunker, shop, 3623, C(MerchantSourceConditionKind.HardMode));
            AddMany(relations, Steampunker, shop, [3603, 3604, 3607, 3605, 3606, 3608, 3618, 3602, 3663, 3609, 3610]);
            Add(relations, Steampunker, shop, 995, C(MerchantSourceConditionKind.HardMode));
            Add(relations, Steampunker, shop, 995, Not(MerchantSourceConditionKind.WorldGetGood));
            Add(
                relations,
                Steampunker,
                shop,
                2203,
                C(MerchantSourceConditionKind.DownedBoss1),
                C(MerchantSourceConditionKind.DownedBoss2),
                C(MerchantSourceConditionKind.DownedBoss3));
            Add(relations, Steampunker, shop, 2193, C(MerchantSourceConditionKind.WorldCrimson));
            Add(relations, Steampunker, shop, 4142, Not(MerchantSourceConditionKind.WorldCrimson));
            Add(relations, Steampunker, shop, 2192, C(MerchantSourceConditionKind.ZoneGraveyard));
            Add(relations, Steampunker, shop, 2204, C(MerchantSourceConditionKind.ZoneJungle));
            Add(
                relations,
                Steampunker,
                shop,
                2195,
                C(MerchantSourceConditionKind.ZoneJungle),
                C(MerchantSourceConditionKind.DownedGolemBoss));
            Add(relations, Steampunker, shop, 2198, C(MerchantSourceConditionKind.ZoneSnow));
            Add(relations, Steampunker, shop, 2197, C(MerchantSourceConditionKind.ZoneSkyHeight));

            AddSteampunkerEventItem(relations, 784, C(MerchantSourceConditionKind.WorldCrimson));
            AddSteampunkerEventItem(relations, 782, Not(MerchantSourceConditionKind.WorldCrimson));
            AddWorldGate(
                relations,
                Steampunker,
                shop,
                781,
                Not(MerchantSourceConditionKind.Eclipse),
                Not(MerchantSourceConditionKind.BloodMoon),
                C(MerchantSourceConditionKind.ZoneHallow));
            AddWorldGate(
                relations,
                Steampunker,
                shop,
                780,
                Not(MerchantSourceConditionKind.Eclipse),
                Not(MerchantSourceConditionKind.BloodMoon),
                Not(MerchantSourceConditionKind.ZoneHallow));
            AddWorldGate(
                relations,
                Steampunker,
                shop,
                [5392, 5393, 5394],
                C(MerchantSourceConditionKind.DownedMoonlord));
            AddMany(relations, Steampunker, shop, [1344, 4472], C(MerchantSourceConditionKind.HardMode));
            Add(relations, Steampunker, shop, 1742, C(MerchantSourceConditionKind.Halloween));
        }

        private static void AddDyeTrader(List<MerchantSourceRelation> relations)
        {
            const int shop = 12;
            AddMany(relations, DyeTrader, shop, [1120, 5920, 1037, 2874]);
            AddMany(relations, DyeTrader, shop, [3248, 1741], C(MerchantSourceConditionKind.Halloween));
            Add(relations, DyeTrader, shop, 1969, C(MerchantSourceConditionKind.MultiplayerClient));
            AddMoonGroup(relations, DyeTrader, shop, [2871, 2872], 0);
            Add(
                relations,
                DyeTrader,
                shop,
                4663,
                Not(MerchantSourceConditionKind.DayTime),
                C(MerchantSourceConditionKind.BloodMoon));
            Add(relations, DyeTrader, shop, 4662, C(MerchantSourceConditionKind.ZoneGraveyard));
        }

        private static void AddPartyGirl(List<MerchantSourceRelation> relations)
        {
            const int shop = 13;
            AddMany(
                relations,
                PartyGirl,
                shop,
                [859, 1000, 1168, 1345, 1450, 3253, 4553, 2700, 2738, 4470, 4681, 4791, 3747, 3732, 3742]);
            Add(relations, PartyGirl, shop, 4743, C(MerchantSourceConditionKind.GolferScoreAtLeast, 500));
            Add(relations, PartyGirl, shop, 1449, C(MerchantSourceConditionKind.DayTime));
            Add(relations, PartyGirl, shop, 4552, Not(MerchantSourceConditionKind.DayTime));
            Add(relations, PartyGirl, shop, 4682, C(MerchantSourceConditionKind.ZoneGraveyard));
            Add(relations, PartyGirl, shop, 4702, C(MerchantSourceConditionKind.LanternNight));
            Add(relations, PartyGirl, shop, 3548, C(MerchantSourceConditionKind.PlayerHasItem, 3548));
            Add(relations, PartyGirl, shop, 3369, C(MerchantSourceConditionKind.NpcPresent, 229));
            Add(relations, PartyGirl, shop, 3546, C(MerchantSourceConditionKind.DownedGolemBoss));
            AddMany(
                relations,
                PartyGirl,
                shop,
                [3214, 2868, 970, 971, 972, 973],
                C(MerchantSourceConditionKind.HardMode));
            AddMany(
                relations,
                PartyGirl,
                shop,
                [3749, 3746, 3739, 3740, 3741, 3737, 3738, 3736, 3745, 3744, 3743],
                C(MerchantSourceConditionKind.BirthdayParty));
        }

        private static void AddCyborg(List<MerchantSourceRelation> relations)
        {
            const int shop = 14;
            AddMany(relations, Cyborg, shop, [771, 5598, 5599, 5928]);
            Add(relations, Cyborg, shop, 772, C(MerchantSourceConditionKind.BloodMoon));
            Add(relations, Cyborg, shop, 773, Not(MerchantSourceConditionKind.DayTime));
            Add(relations, Cyborg, shop, 773, C(MerchantSourceConditionKind.Eclipse));
            Add(relations, Cyborg, shop, 774, C(MerchantSourceConditionKind.Eclipse));
            Add(relations, Cyborg, shop, 4445, C(MerchantSourceConditionKind.DownedMartians));
            Add(
                relations,
                Cyborg,
                shop,
                4446,
                C(MerchantSourceConditionKind.DownedMartians),
                C(MerchantSourceConditionKind.BloodMoon));
            Add(
                relations,
                Cyborg,
                shop,
                4446,
                C(MerchantSourceConditionKind.DownedMartians),
                C(MerchantSourceConditionKind.Eclipse));
            AddMany(
                relations,
                Cyborg,
                shop,
                [4459, 760, 1346, 5452, 5451, 5738],
                C(MerchantSourceConditionKind.HardMode));
            AddMany(relations, Cyborg, shop, [4409, 4392], C(MerchantSourceConditionKind.ZoneGraveyard));
            AddMany(relations, Cyborg, shop, [1743, 1744, 1745], C(MerchantSourceConditionKind.Halloween));
            AddMany(relations, Cyborg, shop, [2862, 3109], C(MerchantSourceConditionKind.DownedMartians));
            Add(relations, Cyborg, shop, 3664, C(MerchantSourceConditionKind.PlayerHasItem, 3384));
            Add(relations, Cyborg, shop, 3664, C(MerchantSourceConditionKind.PlayerHasItem, 3664));
        }

        private static void AddPainter(List<MerchantSourceRelation> relations)
        {
            const int primaryShop = 15;
            AddMany(relations, Painter, primaryShop, [1071, 1072, 1100, 1097, 1099, 1098, 1966]);

            for (var itemId = 1073; itemId <= 1084; itemId++)
                Add(relations, Painter, primaryShop, itemId);

            AddMany(relations, Painter, primaryShop, [1967, 1968], C(MerchantSourceConditionKind.HardMode));
            Add(relations, Painter, primaryShop, 4668, C(MerchantSourceConditionKind.ZoneGraveyard));
            Add(
                relations,
                Painter,
                primaryShop,
                5344,
                C(MerchantSourceConditionKind.ZoneGraveyard),
                C(MerchantSourceConditionKind.DownedPlantBoss));
            Add(
                relations,
                Painter,
                primaryShop,
                5344,
                C(MerchantSourceConditionKind.ZoneGraveyard),
                C(MerchantSourceConditionKind.NpcPresent, 124));

            const int decorationShop = 25;

            for (var itemId = 1948; itemId <= 1957; itemId++)
                AddFlagged(
                    relations,
                    Painter,
                    decorationShop,
                    itemId,
                    MerchantSourceAvailabilityFlags.ShopCapacityLimited,
                    C(MerchantSourceConditionKind.Xmas));

            for (var itemId = 2158; itemId <= 2160; itemId++)
                AddFlagged(
                    relations,
                    Painter,
                    decorationShop,
                    itemId,
                    MerchantSourceAvailabilityFlags.ShopCapacityLimited);

            for (var itemId = 2008; itemId <= 2014; itemId++)
                AddFlagged(
                    relations,
                    Painter,
                    decorationShop,
                    itemId,
                    MerchantSourceAvailabilityFlags.ShopCapacityLimited);

            Add(relations, Painter, decorationShop, 1490, Not(MerchantSourceConditionKind.ZoneGraveyard));
            AddMoonGroup(
                relations,
                Painter,
                decorationShop,
                [1481],
                Not(MerchantSourceConditionKind.ZoneGraveyard),
                0,
                1);
            AddMoonGroup(
                relations,
                Painter,
                decorationShop,
                [1482],
                Not(MerchantSourceConditionKind.ZoneGraveyard),
                2,
                3);
            AddMoonGroup(
                relations,
                Painter,
                decorationShop,
                [1483],
                Not(MerchantSourceConditionKind.ZoneGraveyard),
                4,
                5);
            AddMoonGroup(
                relations,
                Painter,
                decorationShop,
                [1484],
                Not(MerchantSourceConditionKind.ZoneGraveyard),
                6,
                7);
            Add(relations, Painter, decorationShop, 5245, C(MerchantSourceConditionKind.ShoppingZoneForest));
            Add(relations, Painter, decorationShop, 1492, C(MerchantSourceConditionKind.ZoneCrimson));
            Add(relations, Painter, decorationShop, 1488, C(MerchantSourceConditionKind.ZoneCorrupt));
            Add(relations, Painter, decorationShop, 1489, C(MerchantSourceConditionKind.ZoneHallow));
            Add(relations, Painter, decorationShop, 1486, C(MerchantSourceConditionKind.ZoneJungle));
            AddMany(relations, Painter, decorationShop, [5491, 1487], C(MerchantSourceConditionKind.ZoneSnow));
            Add(relations, Painter, decorationShop, 1491, C(MerchantSourceConditionKind.ZoneDesert));
            Add(relations, Painter, decorationShop, 1493, C(MerchantSourceConditionKind.BloodMoon));
            Add(
                relations,
                Painter,
                decorationShop,
                1485,
                Not(MerchantSourceConditionKind.ZoneGraveyard),
                C(MerchantSourceConditionKind.ZoneSkyHeight));
            Add(
                relations,
                Painter,
                decorationShop,
                1494,
                Not(MerchantSourceConditionKind.ZoneGraveyard),
                C(MerchantSourceConditionKind.ZoneSkyHeight),
                C(MerchantSourceConditionKind.HardMode));
            Add(relations, Painter, decorationShop, 5251, C(MerchantSourceConditionKind.Storm));
            AddMany(
                relations,
                Painter,
                decorationShop,
                [4723, 4724, 4725, 4726, 4727, 5257, 4728, 4729],
                C(MerchantSourceConditionKind.ZoneGraveyard));
        }

        private static void AddWitchDoctor(List<MerchantSourceRelation> relations)
        {
            const int shop = 16;
            AddMany(relations, WitchDoctor, shop, [1430, 986, 909, 910, 940, 941, 942, 943, 944, 945, 4922, 4417]);
            Add(relations, WitchDoctor, shop, 2999, C(MerchantSourceConditionKind.NpcPresent, 108));
            Add(relations, WitchDoctor, shop, 6147, C(MerchantSourceConditionKind.ZoneJungle));
            Add(relations, WitchDoctor, shop, 1158, Not(MerchantSourceConditionKind.DayTime));
            AddMany(
                relations,
                WitchDoctor,
                shop,
                [1159, 1160, 1161, 1339],
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPlantBoss));
            Add(
                relations,
                WitchDoctor,
                shop,
                1167,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPlantBoss),
                C(MerchantSourceConditionKind.ZoneJungle));
            Add(
                relations,
                WitchDoctor,
                shop,
                1171,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.ZoneJungle));
            Add(
                relations,
                WitchDoctor,
                shop,
                1162,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.ZoneJungle),
                Not(MerchantSourceConditionKind.DayTime),
                C(MerchantSourceConditionKind.DownedPlantBoss));
            Add(relations, WitchDoctor, shop, 1836, C(MerchantSourceConditionKind.PlayerHasItem, 1835));
            Add(relations, WitchDoctor, shop, 1261, C(MerchantSourceConditionKind.PlayerHasItem, 1258));
            Add(relations, WitchDoctor, shop, 1791, C(MerchantSourceConditionKind.Halloween));
        }

        private static void AddPirate(List<MerchantSourceRelation> relations)
        {
            const int shop = 17;
            AddMany(relations, Pirate, shop, [928, 929, 876, 877, 878, 2434]);
            Add(relations, Pirate, shop, 5926, C(MerchantSourceConditionKind.ZoneGraveyard));
            Add(relations, Pirate, shop, 1180, C(MerchantSourceConditionKind.ZoneBeach));
            Add(
                relations,
                Pirate,
                shop,
                1337,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMechBossAny),
                C(MerchantSourceConditionKind.NpcPresent, 208));
        }

        private static void AddStylist(List<MerchantSourceRelation> relations)
        {
            const int shop = 18;
            AddMany(relations, Stylist, shop, [1990, 1979, 5104]);
            Add(relations, Stylist, shop, 1977, C(MerchantSourceConditionKind.PlayerLifeMaxAtLeast, 400));
            Add(relations, Stylist, shop, 1978, C(MerchantSourceConditionKind.PlayerManaMaxAtLeast, 200));
            Add(relations, Stylist, shop, 1980, C(MerchantSourceConditionKind.PlayerCoinValueAtLeast, 1000000));
            AddMoonDayParity(relations, Stylist, shop, 1981);
            Add(
                relations,
                Stylist,
                shop,
                1982,
                C(MerchantSourceConditionKind.PlayerTeamNonZero),
                C(MerchantSourceConditionKind.MultiplayerClient));
            Add(relations, Stylist, shop, 1983, C(MerchantSourceConditionKind.HardMode));
            Add(relations, Stylist, shop, 1984, C(MerchantSourceConditionKind.NpcPresent, 208));
            Add(
                relations,
                Stylist,
                shop,
                1985,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMechBoss1),
                C(MerchantSourceConditionKind.DownedMechBoss2),
                C(MerchantSourceConditionKind.DownedMechBoss3));
            Add(
                relations,
                Stylist,
                shop,
                1986,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMechBossAny));
            AddMany(
                relations,
                Stylist,
                shop,
                [2863, 3259],
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMartians));
            Add(relations, Stylist, shop, 5577, C(MerchantSourceConditionKind.ZoneGraveyard));
        }

        private static void AddTravelingMerchant(List<MerchantSourceRelation> relations)
        {
            const int shop = 19;
            int[] inventoryUnlockItems = [5667, 5663, 5664, 5665, 5666, 6174, 6148, 6149, 6150, 6151];

            for (var index = 0; index < inventoryUnlockItems.Length; index++)
            {
                MerchantSourceCondition condition = C(
                    MerchantSourceConditionKind.PlayerHasItemInAnyInventory,
                    inventoryUnlockItems[index]);
                AddMany(relations, TravelingMerchant, shop, [5735, 5736], condition);
            }

            MerchantSourceAvailabilityFlags random = MerchantSourceAvailabilityFlags.RandomStock;
            AddFlaggedMany(
                relations,
                TravelingMerchant,
                shop,
                [
                    3309, 3314, 1987, 2278, 2271, 2272, 2276, 2284, 2285, 2286, 2287, 4744, 3628, 4603, 4604, 5297,
                    4605, 4550,
                    5680, 5681, 5682, 2268, 1988, 2275, 2279, 2277, 4555, 4556, 4557, 4321, 4322, 4323, 4324, 4365,
                    5390, 5386, 5387,
                    4549, 4561, 4774, 5136, 5305, 4562, 4558, 4559, 4563, 4666, 4664, 4665, 5600, 2267, 2214, 2215,
                    2216, 2217, 3624,
                    2273, 2274, 2266, 2281, 2282, 2283, 2258, 2242, 2260, 2261, 2262, 3637, 3642, 3621, 3622, 3634,
                    3639, 3633, 3638,
                    3635, 3640, 3636, 3641, 4420, 3119, 3118, 3099, 5121, 5122, 5124, 5123, 5530, 5633, 5636, 5225,
                    5229, 5232,
                    5389, 5233, 5241, 5244, 5487, 5242, 5531
                ],
                random);

            AddFlaggedMany(
                relations,
                TravelingMerchant,
                shop,
                [2270, 4760, 4091],
                random,
                C(MerchantSourceConditionKind.HardMode));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                2223,
                random,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMechBoss1),
                C(MerchantSourceConditionKind.DownedMechBoss2),
                C(MerchantSourceConditionKind.DownedMechBoss3));
            AddFlagged(relations, TravelingMerchant, shop, 2296, random, C(MerchantSourceConditionKind.DownedBoss3));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                2269,
                random,
                C(MerchantSourceConditionKind.WorldShadowOrbSmashed));
            AddFlagged(relations, TravelingMerchant, shop, 3262, random, C(MerchantSourceConditionKind.DownedBoss1));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                3284,
                random,
                C(MerchantSourceConditionKind.DownedMechBossAny));
            AddFlagged(relations, TravelingMerchant, shop, 4348, random, C(MerchantSourceConditionKind.HardMode));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                4347,
                random,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedDeerclops));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                4347,
                random,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedSlimeKing));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                4347,
                random,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedBoss1));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                4347,
                random,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedBoss2));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                4347,
                random,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedBoss3));
            AddFlagged(
                relations,
                TravelingMerchant,
                shop,
                4347,
                random,
                Not(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedQueenBee));

            AddFlaggedMany(
                relations,
                TravelingMerchant,
                shop,
                [3596, 5243],
                random,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMoonlord));
            AddFlaggedMany(
                relations,
                TravelingMerchant,
                shop,
                [2865, 2866, 2867],
                random,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMartians));
            AddFlaggedMany(
                relations,
                TravelingMerchant,
                shop,
                [3055, 3056, 3057, 3058, 3059],
                random,
                C(MerchantSourceConditionKind.DownedFrost));
        }

        private static void AddSkeletonMerchant(List<MerchantSourceRelation> relations)
        {
            const int shop = 20;
            AddMoonGroup(relations, SkeletonMerchant, shop, [284], 0);
            AddMoonGroup(relations, SkeletonMerchant, shop, [946], 1);
            Add(
                relations,
                SkeletonMerchant,
                shop,
                3069,
                C(MerchantSourceConditionKind.MoonPhase, 2),
                Not(MerchantSourceConditionKind.WorldRemix));
            Add(
                relations,
                SkeletonMerchant,
                shop,
                517,
                C(MerchantSourceConditionKind.MoonPhase, 2),
                C(MerchantSourceConditionKind.WorldRemix));
            AddMoonGroup(relations, SkeletonMerchant, shop, [4341], 3);
            AddMoonGroup(relations, SkeletonMerchant, shop, [285], 4);
            AddMoonGroup(relations, SkeletonMerchant, shop, [953], 5);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3068], 6);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3084], 7);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3001], 0, 2, 4, 6);
            AddMoonGroup(relations, SkeletonMerchant, shop, [28], 1, 3, 5, 7);
            AddMoonGroup(relations, SkeletonMerchant, shop, [188], C(MerchantSourceConditionKind.HardMode), 1, 3, 5, 7);
            Add(relations, SkeletonMerchant, shop, 3002, Not(MerchantSourceConditionKind.DayTime));
            Add(relations, SkeletonMerchant, shop, 3002, C(MerchantSourceConditionKind.MoonPhase));
            Add(
                relations,
                SkeletonMerchant,
                shop,
                5377,
                Not(MerchantSourceConditionKind.DayTime),
                C(MerchantSourceConditionKind.PlayerHasItem, 930));
            Add(
                relations,
                SkeletonMerchant,
                shop,
                5377,
                C(MerchantSourceConditionKind.MoonPhase),
                C(MerchantSourceConditionKind.PlayerHasItem, 930));
            AddMoonGroup(
                relations,
                SkeletonMerchant,
                shop,
                [282],
                C(MerchantSourceConditionKind.DayTime),
                1,
                2,
                3,
                4,
                5,
                6,
                7);
            Add(relations, SkeletonMerchant, shop, 3004, C(MerchantSourceConditionKind.SkeletonMerchantEarlyCycle));
            Add(relations, SkeletonMerchant, shop, 8, Not(MerchantSourceConditionKind.SkeletonMerchantEarlyCycle));
            AddMoonGroup(relations, SkeletonMerchant, shop, [3003], 0, 1, 4, 5);
            AddMoonGroup(relations, SkeletonMerchant, shop, [40], 2, 3, 6, 7);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3310], 0, 4);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3313], 1, 5);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3312], 2, 6);
            AddMoonGroup(relations, SkeletonMerchant, shop, [3311], 3, 7);
            AddMoonGroup(relations, SkeletonMerchant, shop, [5640], 1, 2);
            AddMoonGroup(relations, SkeletonMerchant, shop, [5641], 3, 5);
            AddMoonGroup(relations, SkeletonMerchant, shop, [5642], 6, 7);
            AddMany(relations, SkeletonMerchant, shop, [166, 965]);
            AddMany(relations, SkeletonMerchant, shop, [3316, 3334], C(MerchantSourceConditionKind.HardMode));
            Add(
                relations,
                SkeletonMerchant,
                shop,
                5540,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMechBossAny));
            Add(
                relations,
                SkeletonMerchant,
                shop,
                3258,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.BloodMoon));
            Add(
                relations,
                SkeletonMerchant,
                shop,
                3043,
                C(MerchantSourceConditionKind.MoonPhase),
                Not(MerchantSourceConditionKind.DayTime));
            AddMoonGroup(
                relations,
                SkeletonMerchant,
                shop,
                [5326],
                Not(MerchantSourceConditionKind.PlayerAteArtisanBread),
                3,
                4,
                5);
        }

        private static void AddTavernkeep(List<MerchantSourceRelation> relations)
        {
            const int shop = 21;
            AddMany(relations, Tavernkeep, shop, [353, 3828, 3816]);
            AddSpecial(relations, Tavernkeep, shop, 3813, DefenderMedal, 50);
            AddSpecialMany(relations, Tavernkeep, shop, [3818, 3824, 3832, 3829], DefenderMedal, 5);

            MerchantSourceCondition hardMode = C(MerchantSourceConditionKind.HardMode);
            MerchantSourceCondition mech = C(MerchantSourceConditionKind.DownedMechBossAny);
            AddSpecialMany(
                relations,
                Tavernkeep,
                shop,
                [3819, 3825, 3833, 3830, 3800, 3801, 3802, 3797, 3798, 3799, 3803, 3804, 3805, 3806, 3807, 3808],
                DefenderMedal,
                15,
                hardMode,
                mech);

            MerchantSourceCondition golem = C(MerchantSourceConditionKind.DownedGolemBoss);
            AddSpecialMany(relations, Tavernkeep, shop, [3820, 3826, 3834, 3831], DefenderMedal, 60, hardMode, golem);
            AddSpecialMany(
                relations,
                Tavernkeep,
                shop,
                [3871, 3872, 3873, 3874, 3875, 3876, 3877, 3878, 3879, 3880, 3881, 3882],
                DefenderMedal,
                50,
                hardMode,
                golem);
        }

        private static void AddGolfer(List<MerchantSourceRelation> relations)
        {
            const int shop = 22;
            AddMany(
                relations,
                Golfer,
                shop,
                [
                    4587, 4590, 4589, 4588, 4083, 4084, 4085, 4086, 4087, 4088, 4089, 3989, 4095, 4040, 4319, 4320,
                    4135, 4138, 4136, 4137, 4049
                ]);
            AddMany(
                relations,
                Golfer,
                shop,
                [4039, 4094, 4093, 4092],
                C(MerchantSourceConditionKind.GolferScoreAtLeast, 500));
            AddMany(
                relations,
                Golfer,
                shop,
                [4591, 4594, 4593, 4592],
                C(MerchantSourceConditionKind.GolferScoreGreaterThan, 1000));
            Add(relations, Golfer, shop, 4265, C(MerchantSourceConditionKind.GolferScoreGreaterThan, 500));
            AddMany(
                relations,
                Golfer,
                shop,
                [4595, 4598, 4597, 4596],
                C(MerchantSourceConditionKind.GolferScoreGreaterThan, 2000));
            Add(
                relations,
                Golfer,
                shop,
                4264,
                C(MerchantSourceConditionKind.GolferScoreGreaterThan, 2000),
                C(MerchantSourceConditionKind.DownedBoss3));
            Add(relations, Golfer, shop, 4599, C(MerchantSourceConditionKind.GolferScoreGreaterThan, 500));
            Add(relations, Golfer, shop, 4600, C(MerchantSourceConditionKind.GolferScoreAtLeast, 1000));
            Add(relations, Golfer, shop, 4601, C(MerchantSourceConditionKind.GolferScoreAtLeast, 2000));
            AddMoonGroup(
                relations,
                Golfer,
                shop,
                [4658],
                C(MerchantSourceConditionKind.GolferScoreAtLeast, 2000),
                0,
                1);
            AddMoonGroup(
                relations,
                Golfer,
                shop,
                [4659],
                C(MerchantSourceConditionKind.GolferScoreAtLeast, 2000),
                2,
                3);
            AddMoonGroup(
                relations,
                Golfer,
                shop,
                [4660],
                C(MerchantSourceConditionKind.GolferScoreAtLeast, 2000),
                4,
                5);
            AddMoonGroup(
                relations,
                Golfer,
                shop,
                [4661],
                C(MerchantSourceConditionKind.GolferScoreAtLeast, 2000),
                6,
                7);
        }

        private static void AddZoologist(List<MerchantSourceRelation> relations)
        {
            const int shop = 23;
            AddMany(relations, Zoologist, shop, [4767, 4829]);
            Add(relations, Zoologist, shop, 4776, C(MerchantSourceConditionKind.BestiaryFairyTorchUnlocked));
            Add(
                relations,
                Zoologist,
                shop,
                5253,
                C(MerchantSourceConditionKind.MoonPhase),
                Not(MerchantSourceConditionKind.DayTime));
            AddBestiaryThreshold(relations, Zoologist, shop, 5635, 4500);
            AddBestiaryThreshold(relations, Zoologist, shop, 4759, 1000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4672, 300);
            AddBestiaryThreshold(relations, Zoologist, shop, 4830, 2500);
            AddBestiaryThreshold(relations, Zoologist, shop, 4910, 4500);
            AddBestiaryThreshold(relations, Zoologist, shop, 4871, 3000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4907, 3000);
            Add(relations, Zoologist, shop, 4677, C(MerchantSourceConditionKind.DownedTowerSolar));
            AddBestiaryThreshold(relations, Zoologist, shop, 4676, 1000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4762, 3000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4716, 2500);
            AddBestiaryThreshold(relations, Zoologist, shop, 4785, 3000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4786, 3000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4787, 3000);
            Add(
                relations,
                Zoologist,
                shop,
                4788,
                C(MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints, 3000),
                C(MerchantSourceConditionKind.HardMode));
            AddBestiaryThreshold(relations, Zoologist, shop, 4763, 2500);
            AddBestiaryThreshold(relations, Zoologist, shop, 4955, 4000);
            Add(
                relations,
                Zoologist,
                shop,
                4736,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.BloodMoon));
            Add(relations, Zoologist, shop, 4701, C(MerchantSourceConditionKind.DownedPlantBoss));
            AddBestiaryThreshold(relations, Zoologist, shop, 4765, 5000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4766, 5000);
            AddBestiaryThreshold(relations, Zoologist, shop, 5285, 5000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4777, 5000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4735, 7000);
            AddBestiaryThreshold(relations, Zoologist, shop, 4951, 10000);
            Add(relations, Zoologist, shop, 5466, C(MerchantSourceConditionKind.BirthdayParty));
            AddMoonGroup(relations, Zoologist, shop, [4768, 4769], 0, 1);
            AddMoonGroup(relations, Zoologist, shop, [4770, 4771], 2, 3);
            AddMoonGroup(relations, Zoologist, shop, [4772, 4773], 4, 5);
            AddMoonGroup(relations, Zoologist, shop, [4560, 4775], 6, 7);
            Add(
                relations,
                Zoologist,
                shop,
                8,
                C(MerchantSourceConditionKind.WorldVampireSeed),
                Not(MerchantSourceConditionKind.WorldInfectedSeed));
        }

        private static void AddPrincess(List<MerchantSourceRelation> relations)
        {
            const int shop = 24;
            AddMany(
                relations,
                Princess,
                shop,
                [
                    5071, 5072, 5073, 5076, 5077, 5078, 5079, 5080, 5081, 5082, 5083, 5084, 5085, 5086, 5087, 5310,
                    5222, 5228, 5088
                ]);
            Add(
                relations,
                Princess,
                shop,
                5266,
                C(MerchantSourceConditionKind.DownedSlimeKing),
                C(MerchantSourceConditionKind.DownedQueenSlime));
            Add(
                relations,
                Princess,
                shop,
                5044,
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedMoonlord));
            AddMany(
                relations,
                Princess,
                shop,
                [1309, 1859, 1358],
                C(MerchantSourceConditionKind.WorldTenthAnniversary));
            Add(
                relations,
                Princess,
                shop,
                857,
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.ZoneDesert));
            Add(
                relations,
                Princess,
                shop,
                4144,
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.BloodMoon));
            AddMoonGroup(
                relations,
                Princess,
                shop,
                [2584],
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPirates),
                0,
                1);
            AddMoonGroup(
                relations,
                Princess,
                shop,
                [854],
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPirates),
                2,
                3);
            AddMoonGroup(
                relations,
                Princess,
                shop,
                [855],
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPirates),
                4,
                5);
            AddMoonGroup(
                relations,
                Princess,
                shop,
                [905],
                C(MerchantSourceConditionKind.WorldTenthAnniversary),
                C(MerchantSourceConditionKind.HardMode),
                C(MerchantSourceConditionKind.DownedPirates),
                6,
                7);
        }

        private static void AddSharedPylons(List<MerchantSourceRelation> relations)
        {
            for (var index = 0; index < _pylonShops.Length; index++)
            {
                ShopIdentity shop = _pylonShops[index];
                MerchantSourceCondition nearbyNpcs = C(MerchantSourceConditionKind.PylonNearbyNpcRequirement);
                MerchantSourceCondition noCorruption = Not(MerchantSourceConditionKind.ZoneCorrupt);
                MerchantSourceCondition noCrimson = Not(MerchantSourceConditionKind.ZoneCrimson);
                MerchantSourceAvailabilityFlags limited = MerchantSourceAvailabilityFlags.ShopCapacityLimited;

                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4876,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.PylonForestLocation));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4920,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneSnow));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4919,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneDesert));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    5652,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneUnderworldHeight));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4917,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.PylonCavernLocation));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4918,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.PylonOceanLocation));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4875,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneJungle));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4916,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneHallow));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4921,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneGlowshroom),
                    Not(MerchantSourceConditionKind.WorldRemix));
                AddFlagged(
                    relations,
                    shop.MerchantNpcId,
                    shop.NativeShopId,
                    4921,
                    limited,
                    nearbyNpcs,
                    noCorruption,
                    noCrimson,
                    C(MerchantSourceConditionKind.ZoneGlowshroom),
                    Not(MerchantSourceConditionKind.ZoneUnderworldHeight));
            }
        }

        private static void AddSteampunkerEventItem(
            List<MerchantSourceRelation> relations,
            int itemId,
            MerchantSourceCondition corruptionCondition)
        {
            AddWorldGate(
                relations,
                Steampunker,
                11,
                itemId,
                corruptionCondition,
                C(MerchantSourceConditionKind.Eclipse));
            AddWorldGate(
                relations,
                Steampunker,
                11,
                itemId,
                corruptionCondition,
                C(MerchantSourceConditionKind.BloodMoon));
        }

        private static void AddWorldGate(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int itemId,
            params MerchantSourceCondition[] conditions)
        {
            Add(
                relations,
                merchantNpcId,
                nativeShopId,
                itemId,
                Append(conditions, Not(MerchantSourceConditionKind.WorldRemix)));
            Add(
                relations,
                merchantNpcId,
                nativeShopId,
                itemId,
                Append(
                    conditions,
                    C(MerchantSourceConditionKind.WorldTenthAnniversary),
                    Not(MerchantSourceConditionKind.WorldGetGood)));
        }

        private static void AddWorldGate(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            params MerchantSourceCondition[] conditions)
        {
            for (var index = 0; index < itemIds.Length; index++)
                AddWorldGate(relations, merchantNpcId, nativeShopId, itemIds[index], conditions);
        }

        private static void AddMoonDayParity(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int itemId)
        {
            AddMoonGroup(
                relations,
                merchantNpcId,
                nativeShopId,
                [itemId],
                C(MerchantSourceConditionKind.DayTime),
                0,
                2,
                4,
                6);
            AddMoonGroup(
                relations,
                merchantNpcId,
                nativeShopId,
                [itemId],
                Not(MerchantSourceConditionKind.DayTime),
                1,
                3,
                5,
                7);
        }

        private static void AddBestiaryThreshold(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int itemId,
            int basisPoints)
        {
            Add(
                relations,
                merchantNpcId,
                nativeShopId,
                itemId,
                C(MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints, basisPoints));
        }

        private static void AddMoonGroup(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            params int[] moonPhases)
        {
            AddMoonGroup(
                relations,
                merchantNpcId,
                nativeShopId,
                itemIds,
                Array.Empty<MerchantSourceCondition>(),
                moonPhases);
        }

        private static void AddMoonGroup(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            MerchantSourceCondition condition,
            params int[] moonPhases)
        {
            AddMoonGroup(relations, merchantNpcId, nativeShopId, itemIds, [condition], moonPhases);
        }

        private static void AddMoonGroup(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            MerchantSourceCondition firstCondition,
            MerchantSourceCondition secondCondition,
            MerchantSourceCondition thirdCondition,
            params int[] moonPhases)
        {
            AddMoonGroup(
                relations,
                merchantNpcId,
                nativeShopId,
                itemIds,
                [firstCondition, secondCondition, thirdCondition],
                moonPhases);
        }

        private static void AddMoonGroup(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            MerchantSourceCondition[] commonConditions,
            params int[] moonPhases)
        {
            for (var phaseIndex = 0; phaseIndex < moonPhases.Length; phaseIndex++)
            {
                MerchantSourceCondition[] conditions = Append(
                    commonConditions,
                    C(MerchantSourceConditionKind.MoonPhase, moonPhases[phaseIndex]));
                AddMany(relations, merchantNpcId, nativeShopId, itemIds, conditions);
            }
        }

        private static void AddMany(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            params MerchantSourceCondition[] conditions)
        {
            for (var index = 0; index < itemIds.Length; index++)
                Add(relations, merchantNpcId, nativeShopId, itemIds[index], conditions);
        }

        private static void Add(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int itemId,
            params MerchantSourceCondition[] conditions)
        {
            relations.Add(
                new MerchantSourceRelation(merchantNpcId, itemId, new MerchantSourceVariant(nativeShopId, conditions)));
        }

        private static void AddFlaggedMany(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            MerchantSourceAvailabilityFlags flags,
            params MerchantSourceCondition[] conditions)
        {
            for (var index = 0; index < itemIds.Length; index++)
                AddFlagged(relations, merchantNpcId, nativeShopId, itemIds[index], flags, conditions);
        }

        private static void AddFlagged(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int itemId,
            MerchantSourceAvailabilityFlags flags,
            params MerchantSourceCondition[] conditions)
        {
            relations.Add(
                new MerchantSourceRelation(
                    merchantNpcId,
                    itemId,
                    new MerchantSourceVariant(nativeShopId, conditions, flags)));
        }

        private static void AddSpecialMany(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int[] itemIds,
            int currencyItemId,
            int amount,
            params MerchantSourceCondition[] conditions)
        {
            for (var index = 0; index < itemIds.Length; index++)
                AddSpecial(relations, merchantNpcId, nativeShopId, itemIds[index], currencyItemId, amount, conditions);
        }

        private static void AddSpecial(
            List<MerchantSourceRelation> relations,
            int merchantNpcId,
            int nativeShopId,
            int itemId,
            int currencyItemId,
            int amount,
            params MerchantSourceCondition[] conditions)
        {
            relations.Add(
                new MerchantSourceRelation(
                    merchantNpcId,
                    itemId,
                    new MerchantSourceVariant(
                        nativeShopId,
                        conditions,
                        MerchantSourceAvailabilityFlags.None,
                        new MerchantSourceSpecialPrice(currencyItemId, amount))));
        }

        private static MerchantSourceCondition C(MerchantSourceConditionKind kind, int argument = 0)
        {
            return new MerchantSourceCondition(kind, argument);
        }

        private static MerchantSourceCondition Not(MerchantSourceConditionKind kind, int argument = 0)
        {
            return new MerchantSourceCondition(kind, argument, isNegated: true);
        }

        private static MerchantSourceCondition[] Append(
            MerchantSourceCondition[] source,
            params MerchantSourceCondition[] additions)
        {
            var result = new MerchantSourceCondition[source.Length + additions.Length];
            Array.Copy(source, result, source.Length);
            Array.Copy(additions, 0, result, source.Length, additions.Length);
            return result;
        }

        private readonly struct ShopIdentity(int merchantNpcId, int nativeShopId)
        {
            public int MerchantNpcId { get; } = merchantNpcId;

            public int NativeShopId { get; } = nativeShopId;
        }
    }
}