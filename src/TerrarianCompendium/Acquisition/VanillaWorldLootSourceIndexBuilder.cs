using System.Collections.Generic;
using Terraria.ID;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Acquisition
{
    internal sealed class VanillaWorldLootSourceIndexBuilder
    {
        private static readonly WorldLootSource _generatedChests = new(
            "generated-chests",
            CompendiumTextKeys.Acquisition.WorldGeneratedChests,
            WorldLootSourceKind.Chest,
            10,
            ItemID.Chest);

        private static readonly WorldLootSource _jungleShrineChests = new(
            "jungle-shrine-chests",
            CompendiumTextKeys.Acquisition.JungleShrineChests,
            WorldLootSourceKind.Chest,
            20,
            ItemID.RichMahoganyChest);

        private static readonly WorldLootSource _waterChests = new(
            "water-chests",
            CompendiumTextKeys.Acquisition.WaterChests,
            WorldLootSourceKind.Chest,
            30,
            ItemID.WaterChest);

        private static readonly WorldLootSource _skywareChests = new(
            "skyware-chests",
            CompendiumTextKeys.Acquisition.SkywareChests,
            WorldLootSourceKind.Chest,
            40,
            ItemID.SkywareChest);

        private static readonly WorldLootSource _pyramidChests = new(
            "pyramid-chests",
            CompendiumTextKeys.Acquisition.PyramidChests,
            WorldLootSourceKind.Chest,
            50,
            ItemID.GoldChest);

        private static readonly WorldLootSource _deadMansChests = new(
            "dead-mans-chests",
            CompendiumTextKeys.Acquisition.DeadMansChests,
            WorldLootSourceKind.Chest,
            55,
            ItemID.DeadMansChest);

        private static readonly WorldLootSource _dungeonChests = new(
            "dungeon-chests",
            CompendiumTextKeys.Acquisition.DungeonChests,
            WorldLootSourceKind.Chest,
            60,
            ItemID.GoldChest);

        private static readonly WorldLootSource _dungeonBiomeChests = new(
            "dungeon-biome-chests",
            CompendiumTextKeys.Acquisition.DungeonBiomeChests,
            WorldLootSourceKind.Chest,
            70,
            ItemID.JungleChest);

        private static readonly WorldLootSource _templeChests = new(
            "temple-chests",
            CompendiumTextKeys.Acquisition.TempleChests,
            WorldLootSourceKind.Chest,
            80,
            ItemID.LihzahrdChest);

        private static readonly WorldLootSource _pots = new(
            "pots",
            CompendiumTextKeys.Acquisition.Pots,
            WorldLootSourceKind.Pot,
            100,
            null);

        private static readonly WorldLootSource _forestTreeShaking = new(
            "tree-shaking-forest",
            CompendiumTextKeys.Acquisition.ForestTreeShaking,
            WorldLootSourceKind.TreeShaking,
            110,
            ItemID.Wood);

        private static readonly WorldLootSource _snowTreeShaking = new(
            "tree-shaking-snow",
            CompendiumTextKeys.Acquisition.SnowTreeShaking,
            WorldLootSourceKind.TreeShaking,
            111,
            ItemID.BorealWood);

        private static readonly WorldLootSource _jungleTreeShaking = new(
            "tree-shaking-jungle",
            CompendiumTextKeys.Acquisition.JungleTreeShaking,
            WorldLootSourceKind.TreeShaking,
            112,
            ItemID.RichMahogany);

        private static readonly WorldLootSource _palmTreeShaking = new(
            "tree-shaking-palm",
            CompendiumTextKeys.Acquisition.PalmTreeShaking,
            WorldLootSourceKind.TreeShaking,
            113,
            ItemID.PalmWood);

        private static readonly WorldLootSource _corruptionTreeShaking = new(
            "tree-shaking-corruption",
            CompendiumTextKeys.Acquisition.CorruptionTreeShaking,
            WorldLootSourceKind.TreeShaking,
            114,
            ItemID.Ebonwood);

        private static readonly WorldLootSource _hallowTreeShaking = new(
            "tree-shaking-hallow",
            CompendiumTextKeys.Acquisition.HallowTreeShaking,
            WorldLootSourceKind.TreeShaking,
            115,
            ItemID.Pearlwood);

        private static readonly WorldLootSource _crimsonTreeShaking = new(
            "tree-shaking-crimson",
            CompendiumTextKeys.Acquisition.CrimsonTreeShaking,
            WorldLootSourceKind.TreeShaking,
            116,
            ItemID.Shadewood);

        private static readonly WorldLootSource _ashTreeShaking = new(
            "tree-shaking-ash",
            CompendiumTextKeys.Acquisition.AshTreeShaking,
            WorldLootSourceKind.TreeShaking,
            117,
            ItemID.AshWood);

        public WorldLootSourceIndex Build()
        {
            var relations = new List<WorldLootRelation>();

            AddGeneratedChestCandidates(relations);
            Add(relations, _jungleShrineChests, 211, 212, 213, 964, 2292, 3017);
            Add(relations, _waterChests, 863, 186, 277, 187, 4404, 268);
            Add(relations, _skywareChests, 159, 65, 158, 2219);
            Add(relations, _pyramidChests, 848, 857, 934);
            Add(relations, _deadMansChests, 5007);
            Add(relations, _dungeonChests, 155, 156, 157, 2623, 163, 113, 3317, 327, 164, 1293, 832, 4281);
            Add(relations, _dungeonBiomeChests, 1156, 1571, 1569, 1260, 1572, 4607);
            Add(relations, _templeChests, 1293);
            AddPotCandidates(relations);
            AddTreeShakingFruitCandidates(relations);

            return new WorldLootSourceIndex(relations);
        }

        private static void AddGeneratedChestCandidates(List<WorldLootRelation> relations)
        {
            Add(
                relations,
                _generatedChests,
                49,
                50,
                53,
                54,
                167,
                117,
                265,
                278,
                4915,
                227,
                292,
                298,
                299,
                290,
                2322,
                2325,
                296,
                295,
                293,
                288,
                294,
                297,
                304,
                2323,
                305,
                301,
                302,
                300,
                2351,
                2348,
                2345,
                2350,
                4870,
                8,
                31,
                282,
                5643,
                72,
                73,
                9,
                4345,
                168,
                965,
                40,
                42,
                28,
                2204,
                753,
                2198,
                669,
                2197,
                2195,
                2192,
                5515,
                5258,
                5226,
                5254,
                5238,
                5255,
                5388,
                751,
                5234,
                2767,
                2766,
                5010,
                4443,
                4737,
                4551,
                4978,
                5629,
                4429,
                4427,
                5597,
                4409,
                5001,
                678,
                3556,
                2870,
                1067,
                1066,
                5075,
                4469);
        }

        private static void AddPotCandidates(List<WorldLootRelation> relations)
        {
            Add(
                relations,
                _pots,
                133,
                664,
                4564,
                154,
                173,
                61,
                150,
                836,
                3272,
                1101,
                3081,
                3271,
                4286,
                4295,
                4294,
                4292,
                4283,
                4287,
                4284,
                4289,
                4296,
                4285,
                5277,
                5278,
                4009,
                4293,
                4282,
                4290,
                4291,
                1130,
                327,
                75,
                965,
                2997,
                58,
                292,
                298,
                299,
                290,
                2322,
                2324,
                2325,
                2350,
                289,
                303,
                291,
                2329,
                296,
                295,
                302,
                305,
                301,
                297,
                304,
                2323,
                2327,
                293,
                288,
                294,
                300,
                2326,
                4870,
                9,
                2503,
                620,
                8,
                282,
                4387,
                4386,
                4385,
                4388,
                974,
                286,
                4383,
                5293,
                40,
                42,
                168,
                265,
                47,
                278,
                4915,
                28,
                188,
                166,
                4423,
                71,
                72,
                73,
                74);
        }

        private static void AddTreeShakingFruitCandidates(List<WorldLootRelation> relations)
        {
            Add(relations, _forestTreeShaking, 4009, 4293, 4282, 4290, 4291);
            Add(relations, _snowTreeShaking, 4295, 4286);
            Add(relations, _jungleTreeShaking, 4292, 4294);
            Add(relations, _palmTreeShaking, 4287, 4283);
            Add(relations, _corruptionTreeShaking, 4289, 4284);
            Add(relations, _hallowTreeShaking, 4288, 4297);
            Add(relations, _crimsonTreeShaking, 4285, 4296);
            Add(relations, _ashTreeShaking, 5278, 5277);
        }

        private static void Add(List<WorldLootRelation> relations, WorldLootSource source, params int[] itemIds)
        {
            foreach (int itemId in itemIds)
            {
                if (itemId > 0)
                    relations.Add(new WorldLootRelation(source, itemId));
            }
        }
    }
}