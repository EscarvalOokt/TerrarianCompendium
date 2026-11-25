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

        private static readonly WorldLootSource _livingWoodChests = new(
            "living-wood-chests",
            CompendiumTextKeys.Acquisition.LivingWoodChests,
            WorldLootSourceKind.Chest,
            11,
            ItemID.LivingWoodChest);

        private static readonly WorldLootSource _iceChests = new(
            "ice-chests",
            CompendiumTextKeys.Acquisition.IceChests,
            WorldLootSourceKind.Chest,
            12,
            ItemID.IceChest);

        private static readonly WorldLootSource _sandstoneChests = new(
            "sandstone-chests",
            CompendiumTextKeys.Acquisition.SandstoneChests,
            WorldLootSourceKind.Chest,
            13,
            ItemID.DesertChest);

        private static readonly WorldLootSource _shadowChests = new(
            "shadow-chests",
            CompendiumTextKeys.Acquisition.ShadowChests,
            WorldLootSourceKind.Chest,
            14,
            ItemID.ShadowChest);

        private static readonly WorldLootSource _errorWorldChests = new(
            "error-world-chests",
            CompendiumTextKeys.Acquisition.ErrorWorldChests,
            WorldLootSourceKind.Chest,
            15,
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
            Add(
                relations,
                _livingWoodChests,
                ItemID.LivingWoodWand,
                ItemID.LeafWand,
                ItemID.TheRunicPixie,
                ItemID.AcornSlingshot,
                ItemID.SunflowerMinecart,
                ItemID.LadybugMinecart);
            Add(
                relations,
                _iceChests,
                ItemID.IceBoomerang,
                ItemID.IceBlade,
                ItemID.IceSkates,
                ItemID.SnowballCannon,
                ItemID.BlizzardinaBottle,
                ItemID.FlurryBoots,
                ItemID.GlacierFang,
                ItemID.IceBow,
                ItemID.Extractinator,
                ItemID.IceTorch,
                ItemID.IceMirror,
                ItemID.DeerThing,
                ItemID.IceMachine,
                ItemID.Fish);
            Add(
                relations,
                _sandstoneChests,
                ItemID.AncientChisel,
                ItemID.SandBoots,
                ItemID.MysticCoilSnake,
                ItemID.MagicConch,
                ItemID.ThunderSpear,
                ItemID.ThunderStaff,
                ItemID.CatBast,
                ItemID.EncumberingStone,
                ItemID.DesertMinecart,
                ItemID.ScarabBomb);
            Add(
                relations,
                _shadowChests,
                ItemID.DarkLance,
                ItemID.Sunfury,
                ItemID.FlowerofFire,
                ItemID.Flamelash,
                ItemID.HellwingBow,
                ItemID.UnholyTrident,
                ItemID.TreasureMagnet,
                ItemID.HellMinecart,
                ItemID.OrnateShadowKey,
                ItemID.HellCake);
            Add(
                relations,
                _errorWorldChests,
                ItemID.UltrabrightHelmet,
                ItemID.WizardHat,
                ItemID.MagicHat,
                ItemID.StylistKilLaKillScissorsIWish,
                ItemID.Code1,
                ItemID.YoYoGlove,
                ItemID.CombatWrench,
                ItemID.ChainKnife,
                ItemID.Terragrim,
                ItemID.PainterPaintballGun,
                ItemID.ZapinatorGray,
                ItemID.SlimeStaff,
                ItemID.FartInABalloon,
                ItemID.MoonCharm,
                ItemID.Jetpack,
                ItemID.BloodyMachete,
                ItemID.MagicQuiver,
                ItemID.JimsDrone,
                ItemID.StrangePlant1,
                ItemID.StrangePlant2,
                ItemID.StrangePlant3,
                ItemID.StrangePlant4,
                ItemID.TeleportationPylonVictory,
                ItemID.LawnFlamingo,
                ItemID.MagicLantern,
                ItemID.ReaverShark,
                ItemID.SawtoothShark,
                ItemID.AntiGravityHook,
                ItemID.StaticHook,
                ItemID.QueenSlimeHook,
                ItemID.VampireFrogStaff,
                ItemID.GravediggerShovel);
            Add(
                relations,
                _jungleShrineChests,
                ItemID.FeralClaws,
                ItemID.AnkletoftheWind,
                ItemID.StaffofRegrowth,
                ItemID.Boomstick,
                ItemID.FiberglassFishingPole,
                ItemID.FlowerBoots,
                ItemID.LivingMahoganyWand,
                ItemID.LivingMahoganyLeafWand,
                ItemID.BeeMinecart,
                ItemID.RatMountItem,
                ItemID.Seaweed,
                ItemID.HoneyDispenser);
            Add(
                relations,
                _waterChests,
                ItemID.WaterWalkingBoots,
                ItemID.BreathingReed,
                ItemID.Trident,
                ItemID.Flipper,
                ItemID.FloatingTube,
                ItemID.DivingHelmet,
                ItemID.SharkBait,
                ItemID.SandcastleBucket);
            Add(
                relations,
                _skywareChests,
                ItemID.ShinyRedBalloon,
                ItemID.Starfury,
                ItemID.LuckyHorseshoe,
                ItemID.CelestialMagnet,
                ItemID.CreativeWings,
                ItemID.SkyMill,
                ItemID.SeeTheWorldForWhatItIs,
                ItemID.HighPitch,
                ItemID.BlessingfromTheHeavens,
                ItemID.Constellation,
                ItemID.LoveisintheTrashSlot,
                ItemID.SunOrnament,
                ItemID.Cloud);
            Add(
                relations,
                _pyramidChests,
                ItemID.PharaohsMask,
                ItemID.SandstorminaBottle,
                ItemID.FlyingCarpet,
                ItemID.PharaohsRobe);
            Add(relations, _deadMansChests, ItemID.DeadMansSweater);
            Add(
                relations,
                _dungeonChests,
                ItemID.Muramasa,
                ItemID.CobaltShield,
                ItemID.AquaScepter,
                ItemID.BubbleGun,
                ItemID.BlueMoon,
                ItemID.MagicMissile,
                ItemID.Valor,
                ItemID.GoldenKey,
                ItemID.Handgun,
                ItemID.LihzahrdPowerCell,
                ItemID.LivingWoodWand,
                ItemID.LeafWand,
                ItemID.BabyBirdStaff,
                ItemID.ShadowKey,
                ItemID.DeadCellsRamRune,
                ItemID.SilverBracer,
                ItemID.BoneWelder,
                ItemID.MoonLordBody);
            Add(
                relations,
                _dungeonBiomeChests,
                ItemID.PiranhaGun,
                ItemID.ScourgeoftheCorruptor,
                ItemID.VampireKnives,
                ItemID.RainbowGun,
                ItemID.StaffoftheFrostHydra,
                ItemID.StormTigerStaff,
                ItemID.RemnantsofDevotion);
            Add(
                relations,
                _templeChests,
                ItemID.LihzahrdPowerCell,
                ItemID.LihzahrdFurnace,
                ItemID.SolarTablet,
                ItemID.LunarTabletFragment);
            AddPotCandidates(relations);
            AddTreeShakingFruitCandidates(relations);

            return new WorldLootSourceIndex(relations);
        }

        private static void AddGeneratedChestCandidates(List<WorldLootRelation> relations)
        {
            // Primary rolls in surface chests, including the alternate wand roll in Remix.
            Add(
                relations,
                _generatedChests,
                ItemID.Spear,
                ItemID.Blowpipe,
                ItemID.WoodenBoomerang,
                ItemID.Aglet,
                ItemID.ClimbingClaws,
                ItemID.Umbrella,
                ItemID.CordageGuide,
                ItemID.MagicDagger,
                ItemID.WandofSparking,
                ItemID.Radar,
                ItemID.PortableStool,
                ItemID.PoisonBarb);

            // Additional primary rolls, companion items and supplies across chest depths.
            Add(
                relations,
                _generatedChests,
                ItemID.LavaCharm,
                ItemID.FlareGun,
                ItemID.Flare,
                ItemID.RainbowFlare,
                ItemID.ShoeSpikes,
                ItemID.Extractinator,
                ItemID.Mace,
                ItemID.BatHook,
                ItemID.MoonStone,
                ItemID.WebSlinger,
                ItemID.ShroomMinecart,
                ItemID.MushroomHat,
                ItemID.MushroomVest,
                ItemID.MushroomPants,
                ItemID.FlamingArrow,
                ItemID.SuspiciousLookingEye,
                ItemID.JestersArrow,
                ItemID.AngelStatue,
                ItemID.Bomb,
                ItemID.HealingPotion,
                ItemID.ThrowingKnife,
                ItemID.RegenerationPotion,
                ItemID.GillsPotion,
                ItemID.ArcheryPotion,
                ItemID.TitanPotion,
                ItemID.TrapsightPotion,
                ItemID.HerbBag);

            // Bar stacks follow the ore tiers selected during world generation.
            Add(
                relations,
                _generatedChests,
                ItemID.CopperBar,
                ItemID.TinBar,
                ItemID.IronBar,
                ItemID.LeadBar,
                ItemID.SilverBar,
                ItemID.TungstenBar,
                ItemID.GoldBar,
                ItemID.PlatinumBar);

            // Voice items can be appended after the primary and secondary chest rolls.
            Add(
                relations,
                _generatedChests,
                ItemID.PlayerVoiceFrogItem,
                ItemID.PlayerVoiceGoatItem,
                ItemID.PlayerVoiceRetroItem,
                ItemID.PlayerVoiceCatItem,
                ItemID.PlayerVoiceDogItem,
                ItemID.PlayerVoiceTurkeyItem,
                ItemID.PlayerVoiceGoblinItem,
                ItemID.PlayerVoiceCrowItem,
                ItemID.PlayerVoiceBalloonItem,
                ItemID.PlayerVoiceUndeadItem,
                ItemID.PlayerVoiceVampireItem,
                ItemID.PlayerVoiceCowbellItem,
                ItemID.PlayerVoiceChickenFeetItem,
                ItemID.PlayerVoiceFairyItem);

            Add(
                relations,
                _generatedChests,
                ItemID.BandofRegeneration,
                ItemID.MagicMirror,
                ItemID.CloudinaBottle,
                ItemID.HermesBoots,
                ItemID.Dynamite,
                ItemID.MeteoriteBar,
                ItemID.HellfireArrow,
                ItemID.SilverBullet,
                ItemID.TungstenBullet,
                ItemID.RestorationPotion,
                ItemID.Torch,
                ItemID.Bottle,
                ItemID.Glowstick,
                ItemID.RainbowGlowstick,
                ItemID.SilverCoin,
                ItemID.GoldCoin,
                ItemID.Wood,
                ItemID.CanOfWorms,
                ItemID.Grenade,
                ItemID.Rope,
                ItemID.WoodenArrow,
                ItemID.Shuriken,
                ItemID.LesserHealingPotion);
            Add(
                relations,
                _generatedChests,
                ItemID.IronskinPotion,
                ItemID.ShinePotion,
                ItemID.NightOwlPotion,
                ItemID.SwiftnessPotion,
                ItemID.MiningPotion,
                ItemID.BuilderPotion,
                ItemID.SpelunkerPotion,
                ItemID.FeatherfallPotion,
                ItemID.ManaRegenerationPotion,
                ItemID.ObsidianSkinPotion,
                ItemID.MagicPowerPotion,
                ItemID.InvisibilityPotion,
                ItemID.HunterPotion,
                ItemID.HeartreachPotion,
                ItemID.GravitationPotion,
                ItemID.ThornsPotion,
                ItemID.WaterWalkingPotion,
                ItemID.BattlePotion,
                ItemID.TeleportationPotion,
                ItemID.InfernoPotion,
                ItemID.LifeforcePotion,
                ItemID.RecallPotion,
                ItemID.PotionOfReturn);
            Add(
                relations,
                _generatedChests,
                ItemID.BatMountItem,
                ItemID.SpectreGoggles,
                ItemID.MoonLordLegs,
                ItemID.RedPotion,
                ItemID.MidnightRainbowDye,
                ItemID.LivingRainbowDye,
                ItemID.IntenseRainbowDye,
                ItemID.RainbowDye,
                ItemID.RainbowCursor,
                ItemID.MeowmereMinecart);
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