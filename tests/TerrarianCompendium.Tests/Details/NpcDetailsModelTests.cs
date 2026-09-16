using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Tests.Details
{
    [TestFixture]
    public sealed class NpcDetailsModelTests
    {
        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(NpcDetailsModel).Assembly,
            CompendiumLocalization.SourceCultureName);

        [Test]
        public void TryGetProjection_UnknownNpc_ReturnsFalse()
        {
            TestContextData context = CreateContext();

            bool result = context.Model.TryGetProjection(
                999,
                NpcDifficultyMode.Classic,
                out NpcDetailsProjection projection);

            Assert.That(result, Is.False);
            Assert.That(projection, Is.Null);
        }

        [Test]
        public void TryGetProjection_EncounterState_DoesNotGateKnowledge()
        {
            TestContextData context = CreateContext(
                metadataFactory: (_, _) => CreateMetadata(isEncountered: false, name: "Blue Slime"),
                relations: [new NpcLootRelation(10, 1, 1, 2, 0.25f)]);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.IsEncountered, Is.False);
            Assert.That(projection.Name, Is.EqualTo("Blue Slime"));
            Assert.That(projection.Stats, Is.Not.Null);
            Assert.That(projection.LootRows, Has.Count.EqualTo(1));
            Assert.That(projection.LootRows[0].Item.ItemId, Is.EqualTo(1));
            Assert.That(projection.LootRows[0].DropRate, Is.EqualTo(0.25f));
        }

        [Test]
        public void TryGetProjection_Difficulty_IsPassedToMetadataProvider()
        {
            var requested = new List<NpcDifficultyMode>();
            TestContextData context = CreateContext(
                metadataFactory: (_, difficulty) =>
                {
                    requested.Add(difficulty);
                    return CreateMetadata(true, difficulty.ToString());
                });

            context.Model.TryGetProjection(10, NpcDifficultyMode.Expert, out NpcDetailsProjection expert);
            context.Model.TryGetProjection(10, NpcDifficultyMode.Master, out NpcDetailsProjection master);

            Assert.That(requested, Is.EqualTo([NpcDifficultyMode.Expert, NpcDifficultyMode.Master]));
            Assert.That(expert.Difficulty, Is.EqualTo(NpcDifficultyMode.Expert));
            Assert.That(master.Difficulty, Is.EqualTo(NpcDifficultyMode.Master));
        }

        [Test]
        public void TryGetProjection_HiddenStats_ArePreservedAsUnavailable()
        {
            TestContextData context = CreateContext(
                metadataFactory: (_, _) => new NpcBestiaryMetadataSnapshot(
                    true,
                    "Hidden",
                    stats: null,
                    bestiaryRarityStars: 0,
                    rareSpawnRarityLevel: null,
                    spawnConditions: null,
                    baseDebuffImmunities: null));

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.Stats, Is.Null);
        }

        [Test]
        public void TryGetProjection_ProjectsRaritySpawnConditionsAndImmunities()
        {
            TestContextData context = CreateContext(
                metadataFactory: (_, _) => new NpcBestiaryMetadataSnapshot(
                    true,
                    "Rare NPC",
                    new NpcBestiaryStatsSnapshot(10, 20, 3, 0.5f, 100f),
                    bestiaryRarityStars: 4,
                    rareSpawnRarityLevel: 2,
                    spawnConditions:
                    [
                        new NpcBestiarySpawnCondition("Bestiary_Biomes.Jungle", "Jungle"),
                        new NpcBestiarySpawnCondition("Bestiary_Times.NightTime", "Night")
                    ],
                    baseDebuffImmunities:
                    [
                        new NpcBestiaryDebuffImmunity(20, "Poisoned"),
                        new NpcBestiaryDebuffImmunity(24, "On Fire!")
                    ]));

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.BestiaryRarityStars, Is.EqualTo(4));
            Assert.That(projection.RareSpawnRarityLevel, Is.EqualTo(2));
            Assert.That(projection.SpawnConditions, Has.Count.EqualTo(2));
            Assert.That(projection.SpawnConditions[0].DisplayNameKey, Is.EqualTo("Bestiary_Biomes.Jungle"));
            Assert.That(projection.SpawnConditions[0].DisplayName, Is.EqualTo("Jungle"));
            Assert.That(projection.SpawnConditions[1].DisplayNameKey, Is.EqualTo("Bestiary_Times.NightTime"));
            Assert.That(projection.SpawnConditions[1].DisplayName, Is.EqualTo("Night"));
            Assert.That(projection.BaseDebuffImmunities, Has.Count.EqualTo(2));
        }

        [Test]
        public void TryGetProjection_LootAlwaysExposesItemStackAndRate()
        {
            TestContextData context = CreateContext(relations: [new NpcLootRelation(10, 1, 2, 5, 0.125f)]);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.LootRows, Has.Count.EqualTo(1));
            NpcDetailsLootRow row = projection.LootRows[0];
            Assert.That(row.Item.ItemId, Is.EqualTo(1));
            Assert.That(row.StackMin, Is.EqualTo(2));
            Assert.That(row.StackMax, Is.EqualTo(5));
            Assert.That(row.DropRate, Is.EqualTo(0.125f));
        }

        [Test]
        public void TryGetProjection_PureDifficultyCondition_SelectsApplicableModeAndIsNotQualifier()
        {
            var expertOnly = new NpcLootCondition(
                mode => mode is NpcDifficultyMode.Expert or NpcDifficultyMode.Master,
                "Expert Mode",
                showAsQualifier: false);
            TestContextData context = CreateContext(relations: [new NpcLootRelation(10, 1, 1, 1, 0.1f, [expertOnly])]);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection classic);
            context.Model.TryGetProjection(10, NpcDifficultyMode.Expert, out NpcDetailsProjection expert);

            Assert.That(classic.LootRows, Is.Empty);
            Assert.That(expert.LootRows, Has.Count.EqualTo(1));
            Assert.That(expert.LootRows[0].ConditionDescriptions, Is.Empty);
        }

        [Test]
        public void TryGetProjection_NonDifficultyCondition_RemainsQualifierWithoutCurrentWorldGating()
        {
            var worldCondition = new NpcLootCondition(_ => true, "In a Crimson world");
            TestContextData context = CreateContext(
                relations: [new NpcLootRelation(10, 1, 1, 1, 0.2f, [worldCondition])]);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.LootRows, Has.Count.EqualTo(1));
            Assert.That(projection.LootRows[0].ConditionDescriptions, Is.EqualTo(["In a Crimson world"]));
        }

        [Test]
        public void TryGetProjection_DuplicateStaticRelations_ArePreserved()
        {
            TestContextData context = CreateContext(
                relations:
                [
                    new NpcLootRelation(10, 1, 1, 1, 0.1f),
                    new NpcLootRelation(10, 1, 2, 2, 0.2f)
                ]);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.LootRows, Has.Count.EqualTo(2));
        }

        [Test]
        public void TryGetProjection_CatalogItem_UsesTextIndexAndCollectionState()
        {
            TestContextData context = CreateContext(relations: [new NpcLootRelation(10, 1, 1, 1, 0.1f)]);
            context.ItemTextIndex.ReplaceSnapshot(
                "test",
                new Dictionary<int, string> { [1] = "Localized Drop", [2] = "Other" },
                new Dictionary<int, string> { [1] = string.Empty, [2] = string.Empty });
            context.Checklist.MarkFound(1);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            NpcDetailsItemReference item = projection.LootRows[0].Item;
            Assert.That(item.Name, Is.EqualTo("Localized Drop"));
            Assert.That(item.IsCollectionTracked, Is.True);
            Assert.That(item.IsFound, Is.True);
            Assert.That(item.IsNavigable, Is.True);
        }

        [Test]
        public void TryGetProjection_ItemOutsideCatalog_UsesFallbackAndIsNotNavigable()
        {
            TestContextData context = CreateContext(relations: [new NpcLootRelation(10, 999, 1, 1, 0.1f)]);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            NpcDetailsItemReference item = projection.LootRows[0].Item;
            Assert.That(item.Name, Is.EqualTo("Item ID 999"));
            Assert.That(item.IsCollectionTracked, Is.False);
            Assert.That(item.IsNavigable, Is.False);
        }

        [Test]
        public void TryGetProjection_WithoutMerchantBackend_ReportsMerchantSourcesUnavailable()
        {
            TestContextData context = CreateContext();

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.MerchantSourceDataAvailable, Is.False);
            Assert.That(projection.MerchantStock, Is.Empty);
        }

        [Test]
        public void TryGetProjection_WithMerchantBackendButNoStock_ReportsAvailableEmptyStock()
        {
            TestContextData context = CreateContext(merchantSourceIndex: new MerchantSourceIndex([]));

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.MerchantSourceDataAvailable, Is.True);
            Assert.That(projection.MerchantStock, Is.Empty);
        }

        [Test]
        public void TryGetProjection_MerchantStock_ProjectsUnionAndAlwaysAvailableState()
        {
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 2, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(
                        1,
                        [new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)]))
            ]);
            TestContextData context = CreateContext(merchantSourceIndex: merchantIndex);
            context.ItemTextIndex.ReplaceSnapshot(
                "test",
                new Dictionary<int, string> { [1] = "Localized Drop", [2] = "Localized Other" },
                new Dictionary<int, string> { [1] = string.Empty, [2] = string.Empty });
            context.Checklist.MarkFound(2);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.MerchantSourceDataAvailable, Is.True);
            Assert.That(projection.MerchantStock, Has.Count.EqualTo(2));
            Assert.That(projection.MerchantStock[0].Item.ItemId, Is.EqualTo(1));
            Assert.That(projection.MerchantStock[0].Item.Name, Is.EqualTo("Localized Drop"));
            Assert.That(projection.MerchantStock[0].IsAlwaysAvailable, Is.False);
            Assert.That(projection.MerchantStock[0].Variants, Has.Count.EqualTo(1));
            Assert.That(projection.MerchantStock[0].Variants[0].ConditionDescriptions, Is.EqualTo(["Night"]));
            Assert.That(projection.MerchantStock[1].Item.ItemId, Is.EqualTo(2));
            Assert.That(projection.MerchantStock[1].Item.Name, Is.EqualTo("Localized Other"));
            Assert.That(projection.MerchantStock[1].Item.IsFound, Is.True);
            Assert.That(projection.MerchantStock[1].IsAlwaysAvailable, Is.True);
            Assert.That(projection.MerchantStock[1].Variants, Is.Empty);
        }

        [Test]
        public void TryGetProjection_MerchantStock_RandomAndCapacityVariantsAreNotAlwaysAvailable()
        {
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(1, [], MerchantSourceAvailabilityFlags.RandomStock)),
                new MerchantSourceRelation(
                    10,
                    2,
                    new MerchantSourceVariant(1, [], MerchantSourceAvailabilityFlags.ShopCapacityLimited))
            ]);
            TestContextData context = CreateContext(merchantSourceIndex: merchantIndex);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(projection.MerchantStock[0].IsAlwaysAvailable, Is.False);
            Assert.That(projection.MerchantStock[0].Variants[0].RandomStock, Is.True);
            Assert.That(projection.MerchantStock[0].Variants[0].ShopCapacityLimited, Is.False);
            Assert.That(projection.MerchantStock[1].IsAlwaysAvailable, Is.False);
            Assert.That(projection.MerchantStock[1].Variants[0].RandomStock, Is.False);
            Assert.That(projection.MerchantStock[1].Variants[0].ShopCapacityLimited, Is.True);
        }

        [Test]
        public void TryGetProjection_MerchantStock_PreservesAlternativeVariantsAndConditionGrouping()
        {
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(
                        1,
                        [
                            new MerchantSourceCondition(MerchantSourceConditionKind.HardMode),
                            new MerchantSourceCondition(MerchantSourceConditionKind.ZoneJungle)
                        ])),
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(1, [new MerchantSourceCondition(MerchantSourceConditionKind.BloodMoon)]))
            ]);
            TestContextData context = CreateContext(merchantSourceIndex: merchantIndex);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            NpcDetailsMerchantStockEntry stock = projection.MerchantStock[0];
            Assert.That(stock.IsAlwaysAvailable, Is.False);
            Assert.That(stock.Variants, Has.Count.EqualTo(2));
            Assert.That(stock.Variants[0].ConditionDescriptions, Is.EqualTo(["Blood Moon"]));
            Assert.That(stock.Variants[1].ConditionDescriptions, Is.EqualTo(["Hardmode", "Jungle biome"]));
        }

        [Test]
        public void TryGetProjection_MerchantStock_ResolvesParameterizedConditionNames()
        {
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(
                        1,
                        [
                            new MerchantSourceCondition(MerchantSourceConditionKind.NpcPresent, 20),
                            new MerchantSourceCondition(MerchantSourceConditionKind.PlayerHasItem, 2)
                        ]))
            ]);
            TestContextData context = CreateContext(
                merchantSourceIndex: merchantIndex,
                npcNameProvider: entry => entry.NetId == 20 ? "Guide" : "Merchant");
            context.ItemTextIndex.ReplaceSnapshot(
                "test",
                new Dictionary<int, string> { [1] = "Drop", [2] = "Magic Mirror" },
                new Dictionary<int, string> { [1] = string.Empty, [2] = string.Empty });

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection projection);

            Assert.That(
                projection.MerchantStock[0].Variants[0].ConditionDescriptions,
                Is.EqualTo(["Guide is present", "Player has Magic Mirror"]));
        }

        [Test]
        public void TryGetProjection_MerchantStock_ItemTextAndChecklistChangesInvalidateCachedProjection()
        {
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, []))
            ]);
            TestContextData context = CreateContext(merchantSourceIndex: merchantIndex);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection before);

            context.ItemTextIndex.ReplaceSnapshot(
                "test",
                new Dictionary<int, string> { [1] = "Localized Drop", [2] = "Other" },
                new Dictionary<int, string> { [1] = string.Empty, [2] = string.Empty });
            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection afterText);

            context.Checklist.MarkFound(1);
            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection afterChecklist);

            Assert.That(afterText, Is.Not.SameAs(before));
            Assert.That(afterText.MerchantStock[0].Item.Name, Is.EqualTo("Localized Drop"));
            Assert.That(afterChecklist, Is.Not.SameAs(afterText));
            Assert.That(afterChecklist.MerchantStock[0].Item.IsFound, Is.True);
        }

        [Test]
        public void TryGetProjection_DifficultyChange_InvalidatesCachedProjection()
        {
            TestContextData context = CreateContext();

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection before);
            context.Model.TryGetProjection(10, NpcDifficultyMode.Expert, out NpcDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
        }

        [Test]
        public void TryGetProjection_MetadataChange_InvalidatesCachedProjection()
        {
            var encounterState = new Dictionary<int, bool>
            {
                [10] = false
            };
            TestContextData context = CreateContext(
                metadataFactory: (entry, _) => CreateMetadata(encounterState[entry.NetId], "NPC"));

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection before);
            encounterState[10] = true;
            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(before.IsEncountered, Is.False);
            Assert.That(after.IsEncountered, Is.True);
        }

        [Test]
        public void TryGetProjection_SpawnConditionIdentityChange_InvalidatesCachedProjection()
        {
            var conditions = new Dictionary<int, NpcBestiarySpawnCondition>
            {
                [10] = new NpcBestiarySpawnCondition("Bestiary_Biomes.Forest", "Forest")
            };
            TestContextData context = CreateContext(
                metadataFactory: (entry, _) => new NpcBestiaryMetadataSnapshot(
                    true,
                    "NPC",
                    new NpcBestiaryStatsSnapshot(10, 100, 5, 0.5f, 250f),
                    2,
                    null,
                    [conditions[entry.NetId]],
                    Array.Empty<NpcBestiaryDebuffImmunity>()));

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection before);
            conditions[10] = new NpcBestiarySpawnCondition("Bestiary_Biomes.Jungle", "Jungle");
            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(before.SpawnConditions[0].DisplayNameKey, Is.EqualTo("Bestiary_Biomes.Forest"));
            Assert.That(after.SpawnConditions[0].DisplayNameKey, Is.EqualTo("Bestiary_Biomes.Jungle"));
        }

        [Test]
        public void TryGetProjection_LocalizationRevisionInvalidatesMerchantConditionPresentation()
        {
            var translated = new Dictionary<string, string>
            {
                [CompendiumTextKeys.Merchant.Condition(MerchantSourceConditionKind.DayTime, false)] =
                    "Localized daytime"
            };
            var localization = CompendiumLocalization.CreateForTesting(_localization.SourceEntries, translated);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(1, [new MerchantSourceCondition(MerchantSourceConditionKind.DayTime)]))
            ]);
            TestContextData context = CreateContext(merchantSourceIndex: merchantIndex, localization: localization);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection before);
            localization.SynchronizeCulture("test");
            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(before.MerchantStock[0].Variants[0].ConditionDescriptions, Is.EqualTo(["Daytime"]));
            Assert.That(after.MerchantStock[0].Variants[0].ConditionDescriptions, Is.EqualTo(["Localized daytime"]));
        }

        [Test]
        public void TryGetProjection_UnchangedInputs_ReusesProjection()
        {
            NpcBestiaryMetadataSnapshot metadata = CreateMetadata(true, "NPC");
            TestContextData context = CreateContext(metadataFactory: (_, _) => metadata);

            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection first);
            context.Model.TryGetProjection(10, NpcDifficultyMode.Classic, out NpcDetailsProjection second);

            Assert.That(second, Is.SameAs(first));
        }

        private static TestContextData CreateContext(
            Func<NpcCatalogEntry, NpcDifficultyMode, NpcBestiaryMetadataSnapshot> metadataFactory = null,
            IEnumerable<NpcLootRelation> relations = null,
            MerchantSourceIndex merchantSourceIndex = null,
            Func<NpcCatalogEntry, string> npcNameProvider = null,
            CompendiumLocalization localization = null)
        {
            var npcCatalog = NpcCatalog.Create(
            [
                new NpcCatalogEntry(10, 0),
                new NpcCatalogEntry(20, 1)
            ]);
            var itemCatalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(1, "Drop"),
                new ItemCatalogEntry(2, "Other")
            ]);
            var checklist = new ChecklistState(itemCatalog);
            var itemTextIndex = new ItemTextIndex(itemCatalog);
            var lootIndex = NpcLootIndex.Create(npcCatalog, relations ?? Array.Empty<NpcLootRelation>());
            metadataFactory ??= (_, _) => CreateMetadata(true, "NPC");
            localization ??= _localization;

            var model = new NpcDetailsModel(
                npcCatalog,
                lootIndex,
                itemCatalog,
                itemTextIndex,
                checklist,
                localization,
                metadataFactory,
                merchantSourceIndex,
                npcNameProvider);

            return new TestContextData(model, checklist, itemTextIndex);
        }

        private static NpcBestiaryMetadataSnapshot CreateMetadata(bool isEncountered, string name)
        {
            return new NpcBestiaryMetadataSnapshot(
                isEncountered,
                name,
                new NpcBestiaryStatsSnapshot(10, 100, 5, 0.5f, 250f),
                bestiaryRarityStars: 2,
                rareSpawnRarityLevel: null,
                spawnConditions: [new NpcBestiarySpawnCondition("Bestiary_Biomes.Surface", "Surface")],
                baseDebuffImmunities: [new NpcBestiaryDebuffImmunity(20, "Poisoned")]);
        }

        private sealed class TestContextData(
            NpcDetailsModel model,
            ChecklistState checklist,
            ItemTextIndex itemTextIndex)
        {
            public NpcDetailsModel Model { get; } = model;

            public ChecklistState Checklist { get; } = checklist;

            public ItemTextIndex ItemTextIndex { get; } = itemTextIndex;
        }
    }
}