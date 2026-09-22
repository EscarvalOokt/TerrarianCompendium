using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Tests.Bestiary
{
    [TestFixture]
    public sealed class BestiaryBrowserModelTests
    {
        [Test]
        public void Constructor_UsesExpectedDefaultState()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState([1, 2]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                new Dictionary<int, BestiaryEntryObservation>
                {
                    [10] = Unknown(),
                    [20] = Encountered()
                });

            Assert.That(model.SearchQuery, Is.Empty);
            Assert.That(model.EncounterFilter, Is.EqualTo(BestiaryEncounterFilter.All));
            Assert.That(model.SortMode, Is.EqualTo(BestiarySortMode.BestiaryOrder));
            Assert.That(model.SortDirection, Is.EqualTo(BestiarySortDirection.Ascending));
            Assert.That(model.MerchantStockFilterAvailable, Is.False);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));
            Assert.That(model.Revision, Is.Zero);
        }

        [Test]
        public void NativeFilterSelection_ReplacesPreviousSelection()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2));
            var filterState = new BestiaryFilterState([1, 2]);
            var matches = new Dictionary<(int NpcId, int FilterId), bool>
            {
                [(10, 1)] = true,
                [(20, 2)] = true
            };
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                metadataMatcher: (entry, filterId) =>
                    matches.TryGetValue((entry.NetId, filterId), out bool value) && value);

            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(1);
            Assert.That(model.SynchronizeState(), Is.True);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));

            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(2);
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([20]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(1));
        }

        [Test]
        public void NativeAndDropCriteria_CombineWithinScope()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2));
            var filterState = new BestiaryFilterState([1]);
            var lootIndex = NpcLootIndex.Create(
                catalog,
                [
                    new NpcLootRelation(10, 100, 1, 1, 0.5f),
                    new NpcLootRelation(30, 300, 1, 1, 0.5f)
                ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                metadataMatcher: (entry, filterId) => filterId == 1 && entry.NetId is 10 or 20,
                npcLootIndex: lootIndex);

            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(1);
            filterState.DropCriterion = BestiaryFilterCriterion.HasDrops;
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(filterState.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.ForNative(1)));
            Assert.That(filterState.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.HasDrops));
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(1));
        }

        [Test]
        public void HasDrops_ReturnsOnlyNpcsWithStaticLootRelations()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            var lootIndex = NpcLootIndex.Create(
                catalog,
                [
                    new NpcLootRelation(10, 100, 1, 1, 0.5f),
                    new NpcLootRelation(20, 200, 1, 1, 0.5f)
                ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                npcLootIndex: lootIndex);

            Assert.That(model.LootAwareFiltersAvailable, Is.True);

            filterState.DropCriterion = BestiaryFilterCriterion.HasDrops;
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
        }

        [Test]
        public void HasMissingDrops_UsesChecklistStateAndRefreshesWithoutChangingSelection()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            ChecklistState checklistState = CreateChecklistState(100, 200);
            var lootIndex = NpcLootIndex.Create(
                catalog,
                [
                    new NpcLootRelation(10, 100, 1, 1, 0.5f),
                    new NpcLootRelation(10, 200, 1, 1, 0.5f),
                    new NpcLootRelation(20, 100, 1, 1, 0.5f)
                ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                checklistState: checklistState,
                npcLootIndex: lootIndex);

            filterState.DropCriterion = BestiaryFilterCriterion.HasMissingDrops;
            Assert.That(model.SynchronizeState(), Is.True);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));

            Assert.That(checklistState.MarkFound(100), Is.True);
            Assert.That(model.SynchronizeState(), Is.True);
            Assert.That(filterState.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.HasMissingDrops));
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));

            Assert.That(checklistState.MarkFound(200), Is.True);
            Assert.That(model.SynchronizeState(), Is.True);
            long revision = model.Revision;

            Assert.That(GetNetIds(model.VisibleEntries), Is.Empty);
            Assert.That(model.SynchronizeState(), Is.False);
            Assert.That(model.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void HasUnresearchedDrops_UsesResearchableIncompleteDropsAndRefreshesOnResearchRevision()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2), (40, 3));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            var lootIndex = NpcLootIndex.Create(
                catalog,
                [
                    new NpcLootRelation(10, 100, 1, 1, 0.5f),
                    new NpcLootRelation(20, 200, 1, 1, 0.5f),
                    new NpcLootRelation(30, 300, 1, 1, 0.5f),
                    new NpcLootRelation(40, 400, 1, 1, 0.5f)
                ]);
            var researchState = new JourneyResearchState(
            [
                new JourneyResearchDefinition(100, 100, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(200, 200, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(400, 400, 5, hasSharedResearchIdentity: false)
            ]);
            Assert.That(
                researchState.ReplaceProgressSnapshot(
                [
                    new KeyValuePair<int, int>(100, 5),
                    new KeyValuePair<int, int>(200, 2)
                ]),
                Is.True);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                npcLootIndex: lootIndex,
                journeyResearchState: researchState);

            Assert.That(model.UnresearchedDropsFilterAvailable, Is.True);

            filterState.DropCriterion = BestiaryFilterCriterion.HasUnresearchedDrops;
            Assert.That(model.SynchronizeState(), Is.True);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([20, 40]));

            Assert.That(
                researchState.ReplaceProgressSnapshot(
                [
                    new KeyValuePair<int, int>(100, 5),
                    new KeyValuePair<int, int>(200, 5),
                    new KeyValuePair<int, int>(400, 5)
                ]),
                Is.True);
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(filterState.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.HasUnresearchedDrops));
            Assert.That(GetNetIds(model.VisibleEntries), Is.Empty);
        }

        [Test]
        public void HasUnresearchedDrops_WithoutJourneyResearchState_NormalizesToAll()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState([1])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(1),
                DropCriterion = BestiaryFilterCriterion.HasUnresearchedDrops
            };
            var lootIndex = NpcLootIndex.Create(catalog, [new NpcLootRelation(10, 100, 1, 1, 0.5f)]);

            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                npcLootIndex: lootIndex);

            Assert.That(model.LootAwareFiltersAvailable, Is.True);
            Assert.That(model.UnresearchedDropsFilterAvailable, Is.False);
            Assert.That(filterState.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.ForNative(1)));
            Assert.That(filterState.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));
        }

        [Test]
        public void DropCriterion_WithoutLootIndex_NormalizesToAll()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState([1])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(1),
                DropCriterion = BestiaryFilterCriterion.HasMissingDrops
            };

            BestiaryBrowserModel model = CreateModel(catalog, filterState, CreateEncounteredStateMap(catalog));

            Assert.That(model.LootAwareFiltersAvailable, Is.False);
            Assert.That(model.UnresearchedDropsFilterAvailable, Is.False);
            Assert.That(filterState.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.ForNative(1)));
            Assert.That(filterState.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));
        }

        [Test]
        public void DropCriterion_DefinesScopeBeforeEncounterAndMerchantFilters()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            var states = new Dictionary<int, BestiaryEntryObservation>
            {
                [10] = Encountered(),
                [20] = Unknown(),
                [30] = Encountered()
            };
            var lootIndex = NpcLootIndex.Create(
                catalog,
                [
                    new NpcLootRelation(10, 100, 1, 1, 0.5f),
                    new NpcLootRelation(20, 200, 1, 1, 0.5f)
                ]);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 100, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(20, 200, new MerchantSourceVariant(1, []))
            ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                states,
                merchantSourceIndex: merchantIndex,
                npcLootIndex: lootIndex);

            filterState.DropCriterion = BestiaryFilterCriterion.HasDrops;
            filterState.EncounterFilter = BestiaryEncounterFilter.Encountered;
            filterState.HasStockOnly = true;
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            Assert.That(model.ScopeEncounteredCount, Is.EqualTo(1));
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));
        }

        [Test]
        public void HasDrops_DoesNotObserveUnrelatedCollectionOrResearchRevisions()
        {
            NpcCatalog catalog = CreateCatalog((10, 0));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            ChecklistState checklistState = CreateChecklistState(100);
            var lootIndex = NpcLootIndex.Create(catalog, [new NpcLootRelation(10, 100, 1, 1, 0.5f)]);
            var researchState = new JourneyResearchState(
                [new JourneyResearchDefinition(100, 100, 1, hasSharedResearchIdentity: false)]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                checklistState: checklistState,
                npcLootIndex: lootIndex,
                journeyResearchState: researchState);

            filterState.DropCriterion = BestiaryFilterCriterion.HasDrops;
            Assert.That(model.SynchronizeState(), Is.True);
            long revision = model.Revision;

            Assert.That(checklistState.MarkFound(100), Is.True);
            Assert.That(researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(100, 1)]), Is.True);

            Assert.That(model.SynchronizeState(), Is.False);
            Assert.That(model.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void SearchMetadataAndEncounterFilters_UseAndSemantics()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2));
            var filterState = new BestiaryFilterState([1]);
            var states = new Dictionary<int, BestiaryEntryObservation>
            {
                [10] = Unknown(),
                [20] = Encountered(),
                [30] = Encountered()
            };
            var names = new Dictionary<int, string>
            {
                [10] = "Blue Slime",
                [20] = "Green Slime",
                [30] = "Zombie"
            };
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                states,
                names,
                (entry, filterId) => filterId == 1 && entry.NetId is 10 or 20);

            model.SearchQuery = "slime";
            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(1);
            filterState.EncounterFilter = BestiaryEncounterFilter.Encountered;
            model.SynchronizeState();

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([20]));
        }

        [Test]
        public void FilteredProgress_UsesSearchAndMetadataBeforeEncounterFilter()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2), (40, 3));
            var filterState = new BestiaryFilterState([1]);
            var states = new Dictionary<int, BestiaryEntryObservation>
            {
                [10] = Unknown(),
                [20] = Encountered(),
                [30] = Encountered(),
                [40] = Unknown()
            };
            var names = new Dictionary<int, string>
            {
                [10] = "Slime One",
                [20] = "Slime Two",
                [30] = "Zombie",
                [40] = "Slime Three"
            };
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                states,
                names,
                (entry, filterId) => filterId == 1 && entry.NetId is 10 or 20);

            model.SearchQuery = "Slime";
            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(1);
            filterState.EncounterFilter = BestiaryEncounterFilter.Encountered;
            model.SynchronizeState();

            Assert.That(model.OverallEncounteredCount, Is.EqualTo(2));
            Assert.That(model.OverallTotalCount, Is.EqualTo(4));
            Assert.That(model.ScopeEncounteredCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            Assert.That(model.ScopeEncounteredRatio, Is.EqualTo(0.5d));
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([20]));
        }

        [Test]
        public void SearchMatcher_CanExposeUnknownNpcRealName()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            var states = new Dictionary<int, BestiaryEntryObservation>
            {
                [10] = Unknown(),
                [20] = Encountered()
            };
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                states,
                new Dictionary<int, string>
                {
                    [10] = "Blue Slime",
                    [20] = "Zombie"
                });

            model.SearchQuery = "BLUE SLIME";

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BlankSearch_DoesNotRestrictUniverse(string value)
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog));

            model.SearchQuery = "npc";
            model.SearchQuery = value;

            Assert.That(model.SearchQuery, Is.Empty);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));
        }

        [Test]
        public void EncounterFilter_ChangesProjectionThroughFilterState()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                new Dictionary<int, BestiaryEntryObservation>
                {
                    [10] = Unknown(),
                    [20] = Encountered()
                });

            filterState.EncounterFilter = BestiaryEncounterFilter.Unknown;
            bool changed = model.SynchronizeState();

            Assert.That(changed, Is.True);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));
        }

        [Test]
        public void HasStockOnly_ReturnsNpcsWithAnyPotentialStockEntry()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2), (40, 3));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 100, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(10, 101, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(
                    20,
                    200,
                    new MerchantSourceVariant(
                        2,
                        [new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)])),
                new MerchantSourceRelation(
                    30,
                    300,
                    new MerchantSourceVariant(19, [], MerchantSourceAvailabilityFlags.RandomStock))
            ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                merchantSourceIndex: merchantIndex);

            Assert.That(model.MerchantStockFilterAvailable, Is.True);

            filterState.HasStockOnly = true;
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20, 30]));
        }

        [Test]
        public void HasStockOnly_PreservesSearchAndNativeScopeTotalsAndCombinesWithEncounterFilter()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1), (30, 2), (40, 3));
            var filterState = new BestiaryFilterState([1]);
            var states = new Dictionary<int, BestiaryEntryObservation>
            {
                [10] = Encountered(),
                [20] = Unknown(),
                [30] = Encountered(),
                [40] = Encountered()
            };
            var names = new Dictionary<int, string>
            {
                [10] = "Forest Merchant A",
                [20] = "Forest Merchant B",
                [30] = "Forest No Stock",
                [40] = "Desert Merchant"
            };
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 100, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(20, 200, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(40, 400, new MerchantSourceVariant(1, []))
            ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                states,
                names,
                metadataMatcher: (entry, filterId) => filterId == 1 && entry.NetId != 40,
                merchantSourceIndex: merchantIndex);

            model.SearchQuery = "Forest";
            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(1);
            filterState.EncounterFilter = BestiaryEncounterFilter.Encountered;
            filterState.HasStockOnly = true;
            Assert.That(model.SynchronizeState(), Is.True);

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
            Assert.That(model.ScopeEncounteredCount, Is.EqualTo(2));
        }

        [Test]
        public void HasStockOnly_FilterRevision_InvalidatesProjectionOnce()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            var filterState = new BestiaryFilterState(Array.Empty<int>());
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(20, 100, new MerchantSourceVariant(1, []))
            ]);
            BestiaryBrowserModel model = CreateModel(
                catalog,
                filterState,
                CreateEncounteredStateMap(catalog),
                merchantSourceIndex: merchantIndex);

            filterState.HasStockOnly = true;
            bool changed = model.SynchronizeState();
            long revision = model.Revision;
            bool changedAgain = model.SynchronizeState();

            Assert.That(changed, Is.True);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([20]));
            Assert.That(changedAgain, Is.False);
            Assert.That(model.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void MissingMerchantBackend_DoesNotChangeDefaultProjection()
        {
            NpcCatalog catalog = CreateCatalog((10, 0), (20, 1));
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog));

            Assert.That(model.MerchantStockFilterAvailable, Is.False);
            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([10, 20]));
        }

        [Test]
        public void NativeFilterRevision_InvalidatesProjectionOnce()
        {
            NpcCatalog catalog = CreateCatalog((10, 0));
            var filterState = new BestiaryFilterState([1]);
            BestiaryBrowserModel model = CreateModel(catalog, filterState, CreateEncounteredStateMap(catalog));

            filterState.BestiaryCriterion = BestiaryFilterCriterion.ForNative(1);
            bool changed = model.SynchronizeState();
            long revision = model.Revision;
            bool changedAgain = model.SynchronizeState();

            Assert.That(changed, Is.True);
            Assert.That(revision, Is.EqualTo(1));
            Assert.That(changedAgain, Is.False);
            Assert.That(model.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void EncounterObservationChange_InvalidatesProjection()
        {
            NpcCatalog catalog = CreateCatalog((10, 0));
            var states = new Dictionary<int, BestiaryEntryObservation> { [10] = Unknown() };
            BestiaryBrowserModel model = CreateModel(catalog, new BestiaryFilterState(Array.Empty<int>()), states);

            states[10] = Encountered(4);

            Assert.That(model.SynchronizeState(), Is.True);
            Assert.That(model.OverallEncounteredCount, Is.EqualTo(1));
        }

        [Test]
        public void SortMode_AllSupportedModes_AreDelegatedWithCurrentDirection()
        {
            NpcCatalog catalog = CreateCatalog((30, 0), (-65, 1), (10, 2));
            var invoked = new List<(BestiarySortMode Mode, BestiarySortDirection Direction)>();
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog),
                sortEntries: (_, mode, direction) => invoked.Add((mode, direction)));
            invoked.Clear();

            BestiarySortMode[] modes =
            [
                BestiarySortMode.Name,
                BestiarySortMode.Rarity,
                BestiarySortMode.Attack,
                BestiarySortMode.Defense,
                BestiarySortMode.Coins,
                BestiarySortMode.HitPoints,
                BestiarySortMode.NpcId,
                BestiarySortMode.BestiaryOrder
            ];

            foreach (BestiarySortMode mode in modes)
                model.SortMode = mode;

            Assert.That(invoked, Is.EqualTo(modes.Select(mode => (mode, BestiarySortDirection.Ascending)).ToArray()));
        }

        [Test]
        public void SortDirection_ChangeAdvancesRevisionAndIsDelegated()
        {
            NpcCatalog catalog = CreateCatalog((30, 0), (10, 1));
            var invoked = new List<(BestiarySortMode Mode, BestiarySortDirection Direction)>();
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog),
                sortEntries: (_, mode, direction) => invoked.Add((mode, direction)));
            invoked.Clear();
            long before = model.Revision;

            model.SortDirection = BestiarySortDirection.Descending;

            Assert.That(model.Revision, Is.EqualTo(before + 1));
            Assert.That(invoked, Is.EqualTo([(BestiarySortMode.BestiaryOrder, BestiarySortDirection.Descending)]));
        }

        [Test]
        public void SortDirection_ReassignSameValue_DoesNotAdvanceRevision()
        {
            NpcCatalog catalog = CreateCatalog((10, 0));
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog));

            model.SortDirection = BestiarySortDirection.Descending;
            long revision = model.Revision;
            model.SortDirection = BestiarySortDirection.Descending;

            Assert.That(model.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void SortDirection_InvalidValue_IsRejected()
        {
            NpcCatalog catalog = CreateCatalog((10, 0));
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog));

            Assert.Throws<ArgumentOutOfRangeException>(() => model.SortDirection = (BestiarySortDirection)99);
        }

        [Test]
        public void BestiaryOrder_DirectionCanReverseProjectionWithoutMutatingCatalogOrder()
        {
            NpcCatalog catalog = CreateCatalog((30, 2), (10, 0), (20, 1));
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog),
                sortEntries: (entries, mode, direction) =>
                {
                    if (mode != BestiarySortMode.BestiaryOrder)
                        return;

                    entries.Sort((left, right) => left.BestiaryOrder.CompareTo(right.BestiaryOrder));

                    if (direction == BestiarySortDirection.Descending)
                        entries.Reverse();
                });

            model.SortDirection = BestiarySortDirection.Descending;

            Assert.That(GetNetIds(model.VisibleEntries), Is.EqualTo([30, 20, 10]));
            Assert.That(GetNetIds(catalog.Entries), Is.EqualTo([10, 20, 30]));
        }

        [Test]
        public void SortMode_ReassignSameValue_DoesNotAdvanceRevision()
        {
            NpcCatalog catalog = CreateCatalog((10, 0));
            BestiaryBrowserModel model = CreateModel(
                catalog,
                new BestiaryFilterState(Array.Empty<int>()),
                CreateEncounteredStateMap(catalog));

            model.SortMode = BestiarySortMode.Name;
            long revision = model.Revision;
            model.SortMode = BestiarySortMode.Name;

            Assert.That(model.Revision, Is.EqualTo(revision));
        }

        private static BestiaryBrowserModel CreateModel(
            NpcCatalog catalog,
            BestiaryFilterState filterState,
            Dictionary<int, BestiaryEntryObservation> states,
            Dictionary<int, string> names = null,
            Func<NpcCatalogEntry, int, bool> metadataMatcher = null,
            Action<List<NpcCatalogEntry>, BestiarySortMode, BestiarySortDirection> sortEntries = null,
            MerchantSourceIndex merchantSourceIndex = null,
            ChecklistState checklistState = null,
            NpcLootIndex npcLootIndex = null,
            JourneyResearchState journeyResearchState = null)
        {
            names ??= catalog.Entries.ToDictionary(entry => entry.NetId, entry => $"NPC {entry.NetId}");
            metadataMatcher ??= (_, _) => true;
            sortEntries ??= (_, _, _) => { };
            checklistState ??= CreateChecklistState();

            return new BestiaryBrowserModel(
                catalog,
                filterState,
                entry => states[entry.NetId],
                (entry, query) => names[entry.NetId].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0,
                metadataMatcher,
                sortEntries,
                checklistState,
                npcLootIndex,
                journeyResearchState,
                merchantSourceIndex);
        }

        private static Dictionary<int, BestiaryEntryObservation> CreateEncounteredStateMap(NpcCatalog catalog)
        {
            return catalog.Entries.ToDictionary(entry => entry.NetId, _ => Encountered());
        }

        private static BestiaryEntryObservation Unknown()
        {
            return new BestiaryEntryObservation(false, 0);
        }

        private static BestiaryEntryObservation Encountered(int token = 1)
        {
            return new BestiaryEntryObservation(true, token);
        }

        private static ChecklistState CreateChecklistState(params int[] itemIds)
        {
            var itemCatalog = ItemCatalog.Create(
                itemIds.Select(itemId => new ItemCatalogEntry(itemId, $"Item {itemId}")));
            return new ChecklistState(itemCatalog);
        }

        private static NpcCatalog CreateCatalog(params (int NetId, int Order)[] entries)
        {
            return NpcCatalog.Create(entries.Select(entry => new NpcCatalogEntry(entry.NetId, entry.Order)));
        }

        private static int[] GetNetIds(IReadOnlyList<NpcCatalogEntry> entries)
        {
            return entries.Select(entry => entry.NetId).ToArray();
        }
    }
}