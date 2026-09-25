using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class ShimmerBrowserModelTests
    {
        [Test]
        public void Defaults_ReturnUniqueResultItemsInsteadOfInputs()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10, 20, 30);
            ShimmerBrowserModel model = CreateModel(
                catalog,
                variants:
                [
                    Direct(10, 1),
                    Direct(20, 1),
                    Direct(30, 2)
                ]);

            Assert.That(GetIds(model), Is.EqualTo([1, 2]));
            Assert.That(model.IsItemAvailable(1), Is.True);
            Assert.That(model.IsItemAvailable(10), Is.False);
        }

        [Test]
        public void ContextInput_ReturnsResultsProducedFromThatInput()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10, 20);
            ShimmerBrowserModel model = CreateModel(catalog, variants: [Direct(10, 1), Direct(10, 2), Direct(20, 2)]);

            model.ContextItemId = 10;

            Assert.That(GetIds(model), Is.EqualTo([1, 2]));
        }

        [Test]
        public void ProducingOnlyAndUsingOnlyItems_AreBothValidContexts()
        {
            ItemCatalog catalog = CreateCatalog(1, 10, 20);
            ShimmerBrowserModel model = CreateModel(catalog, variants: [Direct(10, 1)]);

            Assert.Multiple(() =>
            {
                Assert.That(model.IsContextItemAvailable(1), Is.True);
                Assert.That(model.IsContextItemAvailable(10), Is.True);
                Assert.That(model.IsContextItemAvailable(20), Is.False);
            });
        }

        [Test]
        public void SearchAndTaxonomy_AreAppliedToResultUniverse()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(1, "Magic Result", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Other Result", ItemCategoryMembership.Furniture),
                new ItemCatalogEntry(10, "Magic Input", ItemCategoryMembership.Furniture),
                new ItemCatalogEntry(20, "Other Input", ItemCategoryMembership.Weapons)
            ]);
            ShimmerBrowserModel model = CreateModel(catalog, variants: [Direct(10, 1), Direct(20, 2)]);

            model.SearchQuery = "magic";
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);

            Assert.That(GetIds(model), Is.EqualTo([1]));
        }

        [Test]
        public void OtherResidual_IsCalculatedFromResultUniverse()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(
                    1,
                    "Unclassified Result",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.None),
                new ItemCatalogEntry(
                    2,
                    "Melee Result",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(10, "Input"),
                new ItemCatalogEntry(20, "Input 2")
            ]);
            ShimmerBrowserModel model = CreateModel(catalog, variants: [Direct(10, 1), Direct(20, 2)]);

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Weapons), Is.True);

            model.NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Weapons);
            Assert.That(GetIds(model), Is.EqualTo([1]));
        }

        [Test]
        public void CompletionAndResearchFilters_AreAppliedToResultItem()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10, 20);
            var checklist = new ChecklistState(catalog);
            checklist.MarkFound(1);
            JourneyResearchState research = CreateResearchState(2, amountNeeded: 5, progress: 2);
            var filters = new ShimmerFilterState
            {
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched
            };
            ShimmerBrowserModel model = CreateModel(
                catalog,
                filters,
                checklist,
                research,
                variants: [Direct(10, 1), Direct(20, 2)]);

            Assert.That(GetIds(model), Is.EqualTo([2]));
        }

        [Test]
        public void VariantFilters_KeepResultWhenAnySingleVariantMatchesAllCriteria()
        {
            ItemCatalog catalog = CreateCatalog(1, 10, 20, 30);
            ShimmerTransformationVariant unlockedDirect = Direct(10, 1);
            ShimmerTransformationVariant lockedDirect = Direct(20, 1, ShimmerProgressionRequirement.PostMoonLord);
            ShimmerTransformationVariant lockedDecraft = Decraft(30, 1, ShimmerProgressionRequirement.PostMoonLord);
            var filters = new ShimmerFilterState
            {
                Kind = ShimmerTransformationKind.Direct,
                Progression = ShimmerProgressionFilter.Locked
            };
            ShimmerBrowserModel model = CreateModel(
                catalog,
                filters,
                isLocked: variant => variant.ProgressionRequirement != ShimmerProgressionRequirement.None,
                variants: [unlockedDirect, lockedDirect, lockedDecraft]);

            Assert.That(GetIds(model), Is.EqualTo([1]));
        }

        [Test]
        public void RuntimeContextWithoutProgressionFilter_DoesNotInvalidateProjection()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            var runtime = new RuntimeState();
            ShimmerBrowserModel model = CreateModel(
                catalog,
                runtimeState: runtime,
                variants: [Direct(10, 1, ShimmerProgressionRequirement.PostMoonLord)]);
            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;

            runtime.Context = new ShimmerRuntimeContextKey(false, false, true, true, 7);

            Assert.That(model.Synchronize(), Is.False);
            Assert.That(model.MatchingItems, Is.SameAs(before));
        }

        [Test]
        public void ProgressionFilter_RuntimeProgressChangeInvalidatesProjection()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            var runtime = new RuntimeState();
            var filters = new ShimmerFilterState { Progression = ShimmerProgressionFilter.Locked };
            ShimmerBrowserModel model = CreateModel(
                catalog,
                filters,
                runtimeState: runtime,
                isLocked: _ => !runtime.Context.DownedMoonLord,
                variants: [Direct(10, 1, ShimmerProgressionRequirement.PostMoonLord)]);

            Assert.That(GetIds(model), Is.EqualTo([1]));

            runtime.Context = new ShimmerRuntimeContextKey(false, false, true, false, 0);

            Assert.That(model.Synchronize(), Is.True);
            Assert.That(GetIds(model), Is.Empty);
        }

        [Test]
        public void WorldAndMoonContext_DoNotRemoveStaticResultItems()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10, 20);
            var runtime = new RuntimeState();
            ShimmerBrowserModel model = CreateModel(
                catalog,
                runtimeState: runtime,
                variants:
                [
                    new ShimmerTransformationVariant(
                        10,
                        ShimmerTransformationKind.Direct,
                        1,
                        [new ShimmerTransformationOutput(1, 1)],
                        moonPhase: 0),
                    new ShimmerTransformationVariant(
                        20,
                        ShimmerTransformationKind.Decraft,
                        1,
                        [new ShimmerTransformationOutput(2, 1)],
                        worldCondition: ShimmerWorldCondition.Crimson,
                        decraftingRecipeRuntimeIndex: 7)
                ]);

            Assert.That(GetIds(model), Is.EqualTo([1, 2]));

            runtime.Context = new ShimmerRuntimeContextKey(false, false, false, false, 7);
            model.Synchronize();

            Assert.That(GetIds(model), Is.EqualTo([1, 2]));
        }

        [Test]
        public void Constructor_NullDependenciesThrow()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            var textIndex = new ItemTextIndex(catalog);
            var index = ShimmerTransformationIndex.Create([Direct(10, 1)]);
            var filters = new ShimmerFilterState();
            var checklist = new ChecklistState(catalog);

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => _ = new ShimmerBrowserModel(
                    null,
                    textIndex,
                    index,
                    filters,
                    checklist,
                    null,
                    _ => false,
                    DefaultContext));
                Assert.Throws<ArgumentNullException>(() => _ = new ShimmerBrowserModel(
                    catalog,
                    null,
                    index,
                    filters,
                    checklist,
                    null,
                    _ => false,
                    DefaultContext));
                Assert.Throws<ArgumentNullException>(() => _ = new ShimmerBrowserModel(
                    catalog,
                    textIndex,
                    null,
                    filters,
                    checklist,
                    null,
                    _ => false,
                    DefaultContext));
                Assert.Throws<ArgumentNullException>(() => _ = new ShimmerBrowserModel(
                    catalog,
                    textIndex,
                    index,
                    null,
                    checklist,
                    null,
                    _ => false,
                    DefaultContext));
                Assert.Throws<ArgumentNullException>(() => _ = new ShimmerBrowserModel(
                    catalog,
                    textIndex,
                    index,
                    filters,
                    null,
                    null,
                    _ => false,
                    DefaultContext));
            });
        }

        private static ShimmerBrowserModel CreateModel(
            ItemCatalog catalog,
            ShimmerFilterState filters = null,
            ChecklistState checklist = null,
            JourneyResearchState research = null,
            RuntimeState runtimeState = null,
            Func<ShimmerTransformationVariant, bool> isLocked = null,
            params ShimmerTransformationVariant[] variants)
        {
            runtimeState ??= new RuntimeState();
            return new ShimmerBrowserModel(
                catalog,
                new ItemTextIndex(catalog),
                ShimmerTransformationIndex.Create(variants),
                filters ?? new ShimmerFilterState(),
                checklist ?? new ChecklistState(catalog),
                research,
                isLocked ?? (_ => false),
                () => runtimeState.Context);
        }

        private static int[] GetIds(ShimmerBrowserModel model)
        {
            return model.MatchingItems.Select(item => item.Id).ToArray();
        }

        private static ShimmerTransformationVariant Direct(
            int inputItemId,
            int resultItemId,
            ShimmerProgressionRequirement progression = ShimmerProgressionRequirement.None)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Direct,
                1,
                [new ShimmerTransformationOutput(resultItemId, 1)],
                progression);
        }

        private static ShimmerTransformationVariant Decraft(
            int inputItemId,
            int resultItemId,
            ShimmerProgressionRequirement progression = ShimmerProgressionRequirement.None)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Decraft,
                1,
                [new ShimmerTransformationOutput(resultItemId, 1)],
                progression,
                decraftingRecipeRuntimeIndex: 7);
        }

        private static ItemCatalog CreateCatalog(params int[] ids)
        {
            return ItemCatalog.Create(ids.Select(id => new ItemCatalogEntry(id, "Item " + id)));
        }

        private static JourneyResearchState CreateResearchState(int itemId, int amountNeeded, int progress)
        {
            var state = new JourneyResearchState(
                [new JourneyResearchDefinition(itemId, itemId, amountNeeded, hasSharedResearchIdentity: false)]);
            if (progress > 0)
                state.ReplaceProgressSnapshot([new KeyValuePair<int, int>(itemId, progress)]);
            return state;
        }

        private static ShimmerRuntimeContextKey DefaultContext()
        {
            return default;
        }

        private sealed class RuntimeState
        {
            public ShimmerRuntimeContextKey Context { get; set; }
        }
    }
}