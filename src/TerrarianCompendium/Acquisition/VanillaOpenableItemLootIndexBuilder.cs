using System.Collections.Generic;

namespace TerrarianCompendium.Acquisition
{
    internal sealed class VanillaOpenableItemLootIndexBuilder
    {
        private static readonly int[] _devSetItems =
        [
            666, 667, 668, 665, 3287,
            1554, 1555, 1556, 1586, 1587, 1588,
            1557, 1558, 1559, 1585,
            1560, 1561, 1562, 1584,
            1563, 1564, 1565, 3582,
            1566, 1567, 1568,
            1580, 1581, 1582, 1583,
            3226, 3227, 3228, 3288,
            3583, 3581, 3578, 3579, 3580,
            3585, 3586, 3587, 3588, 3024,
            3589, 3590, 3591, 3592, 3599,
            3368, 3921, 3922, 3923, 3924,
            3925, 3926, 3927, 3928, 3929,
            4732, 4733, 4734, 4730,
            4747, 4748, 4749, 4746,
            4751, 4752, 4753, 4750,
            4755, 4756, 4757, 4754,
            5583, 5584, 5585, 5586, 5587,
            5683, 5684, 5685, 5686,
            6137, 6138, 6139, 6140, 6141
        ];

        private static readonly int[] _genericBiomeCratePreHardmodeItems =
        [
            73,
            12, 699, 11, 700, 14, 701, 13, 702,
            22, 21, 19, 704, 705, 706,
            288, 296, 304, 305, 2322, 2323,
            188, 189, 2675, 2676
        ];

        private static readonly int[] _genericBiomeCrateHardmodeItems =
        [
            73,
            12, 699, 11, 700, 14, 701, 13, 702,
            364, 1104, 365, 1105, 366, 1106,
            22, 21, 19, 704, 705, 706,
            381, 382, 391, 1184, 1191, 1198,
            288, 296, 304, 305, 2322, 2323,
            188, 189, 2675, 2676
        ];

        private static readonly int[] _lavaCrateCommonPreHardmodeItems =
        [
            73, 174, 175,
            288, 296, 304, 305, 2322, 2323,
            188, 189, 2675, 2676
        ];

        private static readonly int[] _lavaCrateCommonHardmodeItems =
        [
            73, 174, 175,
            364, 1104, 365, 1105, 366, 1106,
            381, 382, 391, 1184, 1191, 1198,
            288, 296, 304, 305, 2322, 2323,
            188, 189, 2675, 2676
        ];

        public OpenableItemLootIndex Build()
        {
            var relations = new List<OpenableItemLootRelation>();

            AddBossBags(relations);
            AddFishingCrates(relations);
            AddMiscellaneousOpenables(relations);

            return new OpenableItemLootIndex(relations);
        }

        private static void AddBossBags(List<OpenableItemLootRelation> relations)
        {
            // OpenBossBag also converts the represented boss NPC.value into coin items after
            // the bag-specific loot branch. That value-derived monetary payout is intentionally
            // not treated as curated bag contents here; this index records direct loot candidates
            // selected by the bag/openable-specific rules.
            Add(relations, 3318, 2430, 2493, 256, 257, 258, 2610, 2585, 998, 1309, 3090);
            Add(relations, 3319, 2112, 1299, 880, 56, 2171, 59, 47, 3097);
            Add(relations, 3320, 56, 86, 994, 2111, 3224);
            Add(relations, 3321, 880, 1329, 2104, 3060, 3223);
            Add(relations, 3322, 2108, 1121, 1123, 2888, 3333, 1132, 1170, 2502, 5483, 1129, 842, 843, 844, 1130, 2431);
            Add(relations, 3323, 3245, 1281, 1273, 1313);
            Add(relations, 3324, 2105, 367, 3335, 489, 490, 491, 2998, 514, 426, 434, 4912);
            AddWithDev(relations, 3325, 2113, 548, 1225, 3355);
            AddWithDev(relations, 3326, 2106, 549, 1225, 3354);
            AddWithDev(relations, 3327, 2107, 547, 1225, 3356);
            AddWithDev(
                relations,
                3328,
                2109,
                1141,
                3336,
                1182,
                1305,
                1157,
                3021,
                758,
                771,
                1255,
                788,
                1178,
                1259,
                1155,
                3018,
                5477);
            AddWithDev(relations, 3329, 3337, 2110, 6158, 1294, 1258, 1261, 1122, 899, 1248, 1295, 1296, 1297, 2218);
            AddWithDev(relations, 3330, 3367, 2588, 2609, 5526, 2624, 2622, 2621, 5478, 3291, 157, 2623);
            AddWithDev(relations, 3331, 3372);
            AddWithDev(
                relations,
                3332,
                3373,
                4469,
                3384,
                3460,
                1131,
                3577,
                4954,
                3063,
                3389,
                3065,
                1553,
                3930,
                3541,
                3570,
                3571,
                3569,
                5480);
            AddWithDev(relations, 3860, 3863, 3859, 3827, 3870, 3858, 3883, 3817);

            // BossBagOgre (3861) and BossBagDarkMage (3862) are valid BossBag/OpenableBag items,
            // but OpenBossBag has no bag-specific content branch for them. They only reach the shared
            // NPC.value-derived monetary payout below the switch, which this contents index excludes.
            AddWithDev(relations, 4782, 4989, 4784, 4823, 4715, 4778, 5075, 4923, 4952, 4953, 4914);
            Add(relations, 4957, 4987, 4986, 4959, 4981, 4758, 4980, 4982, 4983, 4984);
            Add(relations, 5111, 5100, 5109, 5385, 5098, 5101, 5113, 5117, 5118, 5119, 5095);
        }

        private static void AddFishingCrates(List<OpenableItemLootRelation> relations)
        {
            int[] woodenCratePreHardmodeItems =
            [
                3200, 3201, 285, 953, 4341, 3068, 3084, 6165, 997, 73, 72,
                12, 699, 11, 700,
                20, 703, 22, 704,
                288, 290, 292, 299, 298, 304, 291, 2322, 2323, 2329,
                28, 110, 2674, 2675
            ];
            int[] woodenCrateHardmodeItems =
            [
                3064, 3200, 3201, 2424, 285, 953, 4341, 3068, 3084, 6165, 73, 72,
                12, 699, 11, 700, 364, 1104,
                20, 703, 22, 704, 381, 1184,
                288, 290, 292, 299, 298, 304, 291, 2322, 2323, 2329,
                28, 110, 2674, 2675
            ];
            Add(relations, 2334, woodenCratePreHardmodeItems);
            Add(relations, 3979, woodenCrateHardmodeItems);

            int[] ironCratePreHardmodeItems =
            [
                2501, 2587, 2608, 3200, 3201, 73,
                12, 699, 11, 700, 14, 701,
                20, 703, 22, 704, 21, 705,
                288, 296, 304, 305, 2322, 2323, 2324, 2327,
                188, 189, 2675, 2676
            ];
            int[] ironCrateHardmodeItems =
            [
                3064, 2501, 2587, 2608, 3200, 3201, 73,
                12, 699, 11, 700, 14, 701, 364, 1104, 365, 1105,
                20, 703, 22, 704, 21, 705, 381, 1184, 382, 1191,
                288, 296, 304, 305, 2322, 2323, 2324, 2327,
                188, 189, 2675, 2676
            ];
            Add(relations, 2335, ironCratePreHardmodeItems);
            Add(relations, 3980, ironCrateHardmodeItems);

            int[] goldenCratePreHardmodeItems =
            [
                29, 2491, 73,
                14, 701, 13, 702,
                21, 19, 705, 706,
                288, 296, 305, 2322, 2323,
                188, 189, 2676, 989
            ];
            int[] goldenCrateHardmodeItems =
            [
                3064, 29, 2491, 73,
                14, 701, 13, 702, 365, 1105, 366, 1106,
                21, 19, 705, 706, 382, 391, 1191, 1198,
                288, 296, 305, 2322, 2323,
                188, 189, 2676, 989
            ];
            Add(relations, 2336, goldenCratePreHardmodeItems);
            Add(relations, 3981, goldenCrateHardmodeItems);

            AddBiomeCrate(relations, 5002, false, 863, 186, 4404, 277, 187, 5527, 4090, 4460, 4425);
            AddBiomeCrate(relations, 5003, true, 863, 186, 4404, 277, 187, 5527, 4090, 4460, 4425);
            AddBiomeCrate(relations, 3203, false, 162, 111, 96, 115, 64);
            AddBiomeCrate(relations, 3982, true, 162, 111, 96, 115, 64, 521, 522);
            AddBiomeCrate(relations, 3204, false, 800, 802, 1256, 1290, 3062);
            AddBiomeCrate(relations, 3983, true, 800, 802, 1256, 1290, 3062, 521, 1332);
            AddBiomeCrate(
                relations,
                3205,
                false,
                3085,
                149,
                134,
                137,
                139,
                1438,
                1375,
                1573,
                1420,
                1435,
                1434,
                1421,
                5234,
                1374,
                1425,
                1372,
                1441,
                1373,
                1433,
                1436,
                1426,
                1424,
                1419,
                2995,
                1422,
                1439,
                1502,
                1423,
                1437,
                1500);
            AddBiomeCrate(
                relations,
                3984,
                true,
                3085,
                149,
                134,
                137,
                139,
                1438,
                1375,
                1573,
                1420,
                1435,
                1434,
                1421,
                5234,
                1374,
                1425,
                1372,
                1441,
                1373,
                1433,
                1436,
                1426,
                1424,
                1419,
                2995,
                1422,
                1439,
                1502,
                1423,
                1437,
                1500);
            AddBiomeCrate(
                relations,
                3206,
                false,
                158,
                65,
                159,
                2219,
                5226,
                5254,
                5238,
                5258,
                5255,
                5388,
                751,
                4978,
                2197);
            AddBiomeCrate(
                relations,
                3985,
                true,
                158,
                65,
                159,
                2219,
                5226,
                5254,
                5238,
                5258,
                5255,
                5388,
                751,
                4978,
                2197);
            AddBiomeCrate(relations, 3207, false);
            AddBiomeCrate(relations, 3986, true, 520, 502);
            AddBiomeCrate(relations, 3208, false, 3017, 212, 964, 211, 213, 2292, 4564, 753);
            AddBiomeCrate(relations, 3987, true, 3017, 212, 964, 211, 213, 2292, 4564, 753);
            AddBiomeCrate(relations, 4405, false, 670, 724, 950, 1319, 725, 987, 1579, 6153, 669);
            AddBiomeCrate(relations, 4406, true, 670, 724, 950, 1319, 725, 987, 1579, 6153, 669);
            AddBiomeCrate(
                relations,
                4407,
                false,
                4056,
                4442,
                4055,
                4061,
                4062,
                4276,
                4262,
                4263,
                4423,
                3380,
                857,
                4639,
                4627,
                4628,
                4632,
                4630,
                4638,
                4629,
                4633,
                4634,
                4635,
                4636,
                4637,
                4631,
                4626);
            AddBiomeCrate(
                relations,
                4408,
                true,
                4056,
                4442,
                4055,
                4061,
                4062,
                4276,
                4262,
                4263,
                4423,
                3380,
                857,
                4639,
                4627,
                4628,
                4632,
                4630,
                4638,
                4629,
                4633,
                4634,
                4635,
                4636,
                4637,
                4631,
                4626);
            AddLavaCrate(
                relations,
                4877,
                false,
                906,
                4822,
                4828,
                4880,
                4881,
                4868,
                4858,
                4879,
                4824,
                4902,
                4903,
                4904,
                4905,
                4906,
                1497,
                1475,
                1479,
                1542,
                1476,
                1538,
                1501,
                1478,
                1539,
                1540,
                1499,
                1541,
                221,
                4443,
                4737,
                4551);
            AddLavaCrate(
                relations,
                4878,
                true,
                906,
                4822,
                4828,
                4880,
                4881,
                4868,
                4858,
                4879,
                4824,
                4902,
                4903,
                4904,
                4905,
                4906,
                1497,
                1475,
                1479,
                1542,
                1476,
                1538,
                1501,
                1478,
                1539,
                1540,
                1499,
                1541,
                221,
                4443,
                4737,
                4551);
        }

        private static void AddMiscellaneousOpenables(List<OpenableItemLootRelation> relations)
        {
            Add(relations, 3093, 313, 314, 315, 317, 316, 318, 2358, 307, 308, 309, 311, 310, 312, 2357);
            Add(relations, 4345, 2002, 3191, 2895);
            Add(relations, 4410, 4411, 4412, 4413, 4414);
            Add(relations, 6142, 5665, 5666);
            Add(relations, 3085, 3317, 155, 156, 157, 2623, 163, 113, 164, 329, 5465, 5515, 6156);
            Add(relations, 4879, 274, 220, 112, 683, 218, 3019, 5010);
            Add(relations, 599, 602, 586, 591);
            Add(relations, 600, 602, 586, 591);
            Add(relations, 601, 602, 586, 591);
            Add(
                relations,
                1869,
                602,
                1922,
                1927,
                1870,
                97,
                1909,
                1917,
                1915,
                1918,
                1921,
                1923,
                1907,
                1908,
                1932,
                1933,
                1934,
                1935,
                1936,
                1937,
                1940,
                1941,
                1942,
                1938,
                1939,
                1911,
                1919,
                1920,
                1912,
                1913,
                1872,
                586,
                591);
            Add(
                relations,
                1774,
                1810,
                1800,
                1809,
                1846,
                1847,
                1848,
                1849,
                1850,
                1749,
                1750,
                1751,
                1746,
                1747,
                1748,
                1752,
                1753,
                1767,
                1768,
                1769,
                1770,
                1771,
                1772,
                1773,
                1754,
                1755,
                1756,
                1757,
                1758,
                1759,
                1760,
                1761,
                1762,
                1763,
                1764,
                1765,
                1766,
                1775,
                1776,
                1777,
                1778,
                1779,
                1780,
                1781,
                1819,
                1820,
                1821,
                1822,
                1823,
                1824,
                1838,
                1839,
                1840,
                1841,
                1842,
                1843,
                1851,
                1852);
        }

        private static void AddWithDev(List<OpenableItemLootRelation> relations, int sourceItemId, params int[] itemIds)
        {
            Add(relations, sourceItemId, itemIds);
            Add(relations, sourceItemId, _devSetItems);
        }

        private static void AddBiomeCrate(
            List<OpenableItemLootRelation> relations,
            int sourceItemId,
            bool hardmode,
            params int[] itemIds)
        {
            Add(relations, sourceItemId, itemIds);
            Add(
                relations,
                sourceItemId,
                hardmode ? _genericBiomeCrateHardmodeItems : _genericBiomeCratePreHardmodeItems);
        }

        private static void AddLavaCrate(
            List<OpenableItemLootRelation> relations,
            int sourceItemId,
            bool hardmode,
            params int[] itemIds)
        {
            Add(relations, sourceItemId, itemIds);
            Add(relations, sourceItemId, hardmode ? _lavaCrateCommonHardmodeItems : _lavaCrateCommonPreHardmodeItems);
        }

        private static void Add(List<OpenableItemLootRelation> relations, int sourceItemId, params int[] itemIds)
        {
            foreach (int itemId in itemIds)
            {
                if (itemId > 0)
                    relations.Add(new OpenableItemLootRelation(sourceItemId, itemId));
            }
        }
    }
}