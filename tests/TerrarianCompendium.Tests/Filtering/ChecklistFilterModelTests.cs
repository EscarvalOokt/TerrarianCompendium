using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Filtering
{
    [TestFixture]
    public sealed class ChecklistFilterModelTests
    {
        [Test]
        public void Constructor_WithNullCatalog_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { _ = new ChecklistFilterModel(null, state, CreateItemTextIndex(catalog)); }));
        }

        [Test]
        public void Constructor_WithNullChecklistState_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { _ = new ChecklistFilterModel(catalog, null, CreateItemTextIndex(catalog)); }));
        }

        [Test]
        public void Constructor_WithNullItemTextIndex_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { _ = new ChecklistFilterModel(catalog, state, null); }));
        }

        [Test]
        public void DefaultSortMode_UsesNativeOrder()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Zulu", nativeSortGroup: 20, nativeSortOrder: 0),
                new ItemCatalogEntry(2, "Alpha", nativeSortGroup: 10, nativeSortOrder: 20),
                new ItemCatalogEntry(3, "Beta", nativeSortGroup: 10, nativeSortOrder: 10));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.SortMode, Is.EqualTo(ChecklistSortMode.Native));
            Assert.That(GetItemIds(model.Items), Is.EqualTo([3, 2, 1]));
        }

        [Test]
        public void DefaultScopeProgress_CoversEntireCatalog()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.ScopeFoundCount, Is.EqualTo(2));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
        }

        [Test]
        public void ScopeCompletionRatio_WithPartialProgress_ReturnsExpectedRatio()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"),
                new ItemCatalogEntry(4, "Fourth"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(0.5));
        }

        [Test]
        public void ScopeCompletionRatio_WithEmptyScope_ReturnsZero()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.SearchQuery = "Pickaxe";

            Assert.That(model.ScopeFoundCount, Is.EqualTo(0));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(0));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(0.0));
        }

        [Test]
        public void ScopeCompletionRatio_AfterStateChange_RefreshesResult()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(0.0));
            Assert.That(state.MarkFound(2), Is.True);
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(0.5));
        }

        [Test]
        public void SearchQuery_WithSubstring_MatchesItemNames()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"),
                new ItemCatalogEntry(3, "Copper Pickaxe"));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.SearchQuery = "Sword";

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void SearchQuery_IsCaseInsensitive()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"),
                new ItemCatalogEntry(3, "Copper Pickaxe"));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.SearchQuery = "cOpPeR";

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
        }

        [Test]
        public void SearchQuery_UsesItemTextIndexNameInsteadOfCatalogName()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            ReplaceItemTextNames(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "Медный меч",
                    [2] = "Деревянный меч"
                },
                "ru-RU");
            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SortMode = ChecklistSortMode.ItemId,
                SearchQuery = "Copper"
            };

            Assert.That(model.Items, Is.Empty);

            model.SearchQuery = "медный";
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void SearchQuery_AfterItemTextRevisionChange_RefreshesWithoutChangingQuery()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SearchQuery = "target",
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(model.Items, Is.Empty);

            ReplaceItemTextNames(itemTextIndex, catalog, new Dictionary<int, string> { [2] = "Target Item" }, "fr-FR");

            Assert.That(model.SearchQuery, Is.EqualTo("target"));
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void SearchQuery_TrimsSurroundingWhitespace()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"),
                new ItemCatalogEntry(3, "Copper Pickaxe"));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.SearchQuery = "  Sword  ";

            Assert.That(model.SearchQuery, Is.EqualTo("Sword"));
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void SearchQuery_WithWhitespaceOnly_ReturnsAllItems()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.SearchQuery = "   ";

            Assert.That(model.SearchQuery, Is.Empty);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void SearchQuery_WithNoMatches_ReturnsEmptyResult()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.SearchQuery = "Pickaxe";

            Assert.That(model.Items, Is.Empty);
        }

        [Test]
        public void SearchDescriptions_DefaultsToDisabled()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Hermes Boots"));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            ReplaceItemTextDescriptions(
                itemTextIndex,
                catalog,
                new Dictionary<int, string> { [1] = "The wearer can run super fast" });

            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SearchQuery = "super fast",
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(model.SearchDescriptions, Is.False);
            Assert.That(model.Items, Is.Empty);
        }

        [Test]
        public void SearchDescriptions_WhenEnabled_MatchesDescriptionTextCaseInsensitively()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Hermes Boots"),
                new ItemCatalogEntry(2, "Cloud in a Bottle"));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            ReplaceItemTextDescriptions(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "The wearer can run super fast",
                    [2] = "Allows the holder to double jump"
                });

            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SearchDescriptions = true,
                SearchQuery = "SuPeR FaSt",
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void SearchDescriptions_StillMatchesItemNames()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SearchDescriptions = true,
                SearchQuery = "Copper",
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void SearchDescriptions_CombinesWithNavigationFacetsCompletionAndScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "First",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    2,
                    "Second",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    3,
                    "Third",
                    ItemCategoryMembership.Tools,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(4, "Fourth", ItemCategoryMembership.Weapons));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            ReplaceItemTextDescriptions(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "shared description",
                    [2] = "shared description",
                    [3] = "shared description",
                    [4] = "shared description"
                });

            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SearchDescriptions = true,
                SearchQuery = "shared description",
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons),
                TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                    ItemTaxonomyFacetId.Ammo,
                    ChecklistTaxonomyFacetState.Include),
                CompletionFilter = ChecklistCompletionFilter.Missing,
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
        }

        [Test]
        public void SearchDescriptions_CombinesWithResearchFilter()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 1, hasSharedResearchIdentity: false));
            Assert.That(researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 1)]), Is.True);

            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            ReplaceItemTextDescriptions(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "shared description",
                    [2] = "shared description"
                });

            var model = new ChecklistFilterModel(catalog, state, itemTextIndex, researchState)
            {
                SearchDescriptions = true,
                SearchQuery = "shared description",
                ResearchFilter = ChecklistResearchFilter.Researched,
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
        }

        [Test]
        public void SearchDescriptions_AfterIndexRevisionChange_RefreshesWithoutChangingQuery()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SearchDescriptions = true,
                SearchQuery = "speed",
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(model.Items, Is.Empty);

            ReplaceItemTextDescriptions(
                itemTextIndex,
                catalog,
                new Dictionary<int, string> { [2] = "Increases movement speed" },
                "fr-FR");

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void CompletionFilter_All_ReturnsFoundAndMissingItems()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.CompletionFilter = ChecklistCompletionFilter.All;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void CompletionFilter_Missing_ReturnsOnlyMissingItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.CompletionFilter = ChecklistCompletionFilter.Missing;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3]));
        }

        [Test]
        public void CompletionFilter_Found_ReturnsOnlyFoundItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.CompletionFilter = ChecklistCompletionFilter.Found;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
        }

        [Test]
        public void CompletionFilter_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
            double completionRatio = model.ScopeCompletionRatio;

            model.CompletionFilter = ChecklistCompletionFilter.Missing;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(completionRatio));

            model.CompletionFilter = ChecklistCompletionFilter.Found;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(completionRatio));
        }

        [Test]
        public void NativeSort_OrdersByGroupThenOrder()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Fourth", nativeSortGroup: 20, nativeSortOrder: 1),
                new ItemCatalogEntry(2, "Second", nativeSortGroup: 10, nativeSortOrder: 20),
                new ItemCatalogEntry(3, "First", nativeSortGroup: 10, nativeSortOrder: 10),
                new ItemCatalogEntry(4, "Third", nativeSortGroup: 20, nativeSortOrder: 0));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(GetItemIds(model.Items), Is.EqualTo([3, 2, 4, 1]));
        }

        [Test]
        public void NativeSort_WithEqualGroupAndOrder_UsesNameThenItemId()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(4, "Beta", nativeSortGroup: 10, nativeSortOrder: 5),
                new ItemCatalogEntry(3, "Alpha", nativeSortGroup: 10, nativeSortOrder: 5),
                new ItemCatalogEntry(2, "Alpha", nativeSortGroup: 10, nativeSortOrder: 5));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3, 4]));
        }

        [Test]
        public void NativeSort_WithEqualGroupAndOrder_UsesCurrentItemTextNames()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Catalog Alpha", nativeSortGroup: 10, nativeSortOrder: 5),
                new ItemCatalogEntry(2, "Catalog Beta", nativeSortGroup: 10, nativeSortOrder: 5));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            var model = new ChecklistFilterModel(catalog, state, itemTextIndex);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));

            ReplaceItemTextNames(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "Zulu",
                    [2] = "Alpha"
                },
                "fr-FR");

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1]));
        }

        [Test]
        public void ItemIdSort_UsesCatalogIdOrder()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(3, "Alpha", nativeSortGroup: 1),
                new ItemCatalogEntry(1, "Zulu", nativeSortGroup: 3),
                new ItemCatalogEntry(2, "Beta", nativeSortGroup: 2));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3]));
        }

        [Test]
        public void NameSort_OrdersAlphabetically()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Zulu"),
                new ItemCatalogEntry(2, "Alpha"),
                new ItemCatalogEntry(3, "Beta"));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Name
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3, 1]));
        }

        [Test]
        public void NameSort_AfterItemTextRevisionChange_UsesLocalizedNames()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Alpha"), new ItemCatalogEntry(2, "Beta"));
            var state = new ChecklistState(catalog);
            ItemTextIndex itemTextIndex = CreateItemTextIndex(catalog);
            var model = new ChecklistFilterModel(catalog, state, itemTextIndex)
            {
                SortMode = ChecklistSortMode.Name
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));

            ReplaceItemTextNames(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "Zulu",
                    [2] = "Alpha"
                },
                "fr-FR");

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1]));
        }

        [Test]
        public void NameSort_IsCaseInsensitive()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Banana"),
                new ItemCatalogEntry(2, "apple"),
                new ItemCatalogEntry(3, "cherry"));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Name
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1, 3]));
        }

        [Test]
        public void NameSort_WithCaseEquivalentNames_UsesItemIdAsTieBreaker()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(3, "Beta"),
                new ItemCatalogEntry(2, "alpha"),
                new ItemCatalogEntry(1, "ALPHA"));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Name
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3]));
        }

        [Test]
        public void ChangingSortMode_RefreshesResult()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Zulu", nativeSortGroup: 30),
                new ItemCatalogEntry(2, "Alpha", nativeSortGroup: 20),
                new ItemCatalogEntry(3, "Beta", nativeSortGroup: 10));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(GetItemIds(model.Items), Is.EqualTo([3, 2, 1]));

            model.SortMode = ChecklistSortMode.Name;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3, 1]));

            model.SortMode = ChecklistSortMode.ItemId;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3]));

            model.SortMode = ChecklistSortMode.Native;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([3, 2, 1]));
        }

        [Test]
        public void SortMode_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Zulu", nativeSortGroup: 20),
                new ItemCatalogEntry(2, "Alpha", nativeSortGroup: 10));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            double ratio = model.ScopeCompletionRatio;

            model.SortMode = ChecklistSortMode.Name;
            _ = model.Items;

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(ratio));
        }

        [Test]
        public void DefaultSortDirection_IsAscending()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.SortDirection, Is.EqualTo(ChecklistSortDirection.Ascending));
        }

        [Test]
        public void NativeSort_Descending_ReversesCompleteNativeOrder()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Fourth", nativeSortGroup: 20, nativeSortOrder: 1),
                new ItemCatalogEntry(2, "Second", nativeSortGroup: 10, nativeSortOrder: 20),
                new ItemCatalogEntry(3, "First", nativeSortGroup: 10, nativeSortOrder: 10),
                new ItemCatalogEntry(4, "Third", nativeSortGroup: 20, nativeSortOrder: 0));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortDirection = ChecklistSortDirection.Descending
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 4, 2, 3]));
        }

        [Test]
        public void ItemIdSort_SupportsBothDirections()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(3, "Alpha"),
                new ItemCatalogEntry(1, "Zulu"),
                new ItemCatalogEntry(2, "Beta"));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3]));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([3, 2, 1]));
        }

        [Test]
        public void NameSort_Descending_ReversesNameOrderButKeepsItemIdTieBreakerForward()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(3, "Beta"),
                new ItemCatalogEntry(2, "alpha"),
                new ItemCatalogEntry(1, "ALPHA"));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Name,
                SortDirection = ChecklistSortDirection.Descending
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([3, 1, 2]));
        }

        [Test]
        public void ValueSort_SupportsBothDirections()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "Low", value: 100),
                CreateSortEntry(2, "High", value: 300),
                CreateSortEntry(3, "Middle", value: 200));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Value
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3, 2]));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3, 1]));
        }

        [Test]
        public void RaritySort_OrdersSpecialRaritiesAboveRegularRarities()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "Regular Low", rarity: 0),
                CreateSortEntry(2, "Regular High", rarity: 11),
                CreateSortEntry(3, "Quest", rarity: -11),
                CreateSortEntry(4, "Expert", rarity: -12),
                CreateSortEntry(5, "Master", rarity: -13));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Rarity
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3, 4, 5]));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([5, 4, 3, 2, 1]));
        }

        [Test]
        public void DamageSort_SupportsBothDirectionsInWeaponsScope()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "Low", ItemCategoryMembership.Weapons, damage: 10),
                CreateSortEntry(2, "High", ItemCategoryMembership.Weapons, damage: 30),
                CreateSortEntry(3, "Middle", ItemCategoryMembership.Weapons, damage: 20));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons),
                SortMode = ChecklistSortMode.Damage
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3, 2]));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3, 1]));
        }

        [Test]
        public void DefenseSort_SupportsBothDirectionsInArmorScope()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "Low", ItemCategoryMembership.Armor, defense: 2),
                CreateSortEntry(2, "High", ItemCategoryMembership.Armor, defense: 8),
                CreateSortEntry(3, "Middle", ItemCategoryMembership.Armor, defense: 5));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Armor),
                SortMode = ChecklistSortMode.Defense
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3, 2]));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3, 1]));
        }

        [Test]
        public void ToolPowerSorts_UseCorrespondingMetricsAndDirections()
        {
            const ItemSemanticMembership toolMemberships = ItemSemanticMembership.Pickaxes |
                                                           ItemSemanticMembership.Axes |
                                                           ItemSemanticMembership.Hammers |
                                                           ItemSemanticMembership.FishingRods;

            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(
                    1,
                    "First",
                    ItemCategoryMembership.Tools,
                    toolMemberships,
                    pickPower: 10,
                    axePower: 30,
                    hammerPower: 20,
                    fishingPower: 40),
                CreateSortEntry(
                    2,
                    "Second",
                    ItemCategoryMembership.Tools,
                    toolMemberships,
                    pickPower: 30,
                    axePower: 10,
                    hammerPower: 40,
                    fishingPower: 20),
                CreateSortEntry(
                    3,
                    "Third",
                    ItemCategoryMembership.Tools,
                    toolMemberships,
                    pickPower: 20,
                    axePower: 40,
                    hammerPower: 10,
                    fishingPower: 30));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            AssertContextualSort(
                model,
                ItemNavigationNodeId.ToolsPickaxes,
                ChecklistSortMode.PickPower,
                [1, 3, 2],
                [2, 3, 1]);
            AssertContextualSort(
                model,
                ItemNavigationNodeId.ToolsAxes,
                ChecklistSortMode.AxePower,
                [2, 1, 3],
                [3, 1, 2]);
            AssertContextualSort(
                model,
                ItemNavigationNodeId.ToolsHammers,
                ChecklistSortMode.HammerPower,
                [3, 1, 2],
                [2, 1, 3]);
            AssertContextualSort(
                model,
                ItemNavigationNodeId.ToolsFishingRods,
                ChecklistSortMode.FishingPower,
                [2, 3, 1],
                [1, 3, 2]);
        }

        [Test]
        public void MetricSort_WithEqualPrimaryValues_UsesForwardNativeOrderInBothDirections()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "Second", nativeSortGroup: 20, value: 100),
                CreateSortEntry(2, "First", nativeSortGroup: 10, value: 100),
                CreateSortEntry(3, "Third", nativeSortGroup: 30, value: 100));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Value
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1, 3]));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1, 3]));
        }

        [Test]
        public void AvailableSortModes_ReflectCurrentNavigationScope()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.AvailableSortModes, Is.EqualTo(CommonSortModes()));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.Damage)));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMelee);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.Damage)));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ArmorHead);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.Defense)));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Tools);
            Assert.That(
                model.AvailableSortModes,
                Is.EqualTo(
                [
                    ChecklistSortMode.Native,
                    ChecklistSortMode.ItemId,
                    ChecklistSortMode.Name,
                    ChecklistSortMode.Value,
                    ChecklistSortMode.Rarity,
                    ChecklistSortMode.PickPower,
                    ChecklistSortMode.AxePower,
                    ChecklistSortMode.HammerPower,
                    ChecklistSortMode.FishingPower
                ]));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ToolsPickaxes);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.PickPower)));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ToolsAxes);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.AxePower)));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ToolsHammers);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.HammerPower)));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ToolsFishingRods);
            Assert.That(model.AvailableSortModes, Is.EqualTo(WithContextualSort(ChecklistSortMode.FishingPower)));
        }

        [Test]
        public void AvailableSortModes_ForToolsOther_ContainOnlyCommonModes()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Residual Tool", ItemCategoryMembership.Tools));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Tools)
            };

            Assert.That(model.AvailableSortModes, Is.EqualTo(CommonSortModes()));
        }

        [Test]
        public void SortMode_WhenContextualModeIsNotAvailable_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<InvalidOperationException>((Action)(() => { model.SortMode = ChecklistSortMode.Damage; }));
        }

        [Test]
        public void ChangingNavigationFilter_PreservesContextualSortInsideApplicableSubtree()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons),
                SortMode = ChecklistSortMode.Damage,
                SortDirection = ChecklistSortDirection.Descending
            };

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMelee);

            Assert.That(model.SortMode, Is.EqualTo(ChecklistSortMode.Damage));
            Assert.That(model.SortDirection, Is.EqualTo(ChecklistSortDirection.Descending));
        }

        [Test]
        public void ChangingNavigationFilter_ResetsInapplicableSortToNativeAndPreservesDirection()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons),
                SortMode = ChecklistSortMode.Damage,
                SortDirection = ChecklistSortDirection.Descending
            };

            model.NavigationFilter = ChecklistNavigationFilter.AllItems;

            Assert.That(model.SortMode, Is.EqualTo(ChecklistSortMode.Native));
            Assert.That(model.SortDirection, Is.EqualTo(ChecklistSortDirection.Descending));
        }

        [Test]
        public void ChangingToolNavigation_ResetsInapplicableToolSortToNative()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ToolsPickaxes),
                SortMode = ChecklistSortMode.PickPower
            };

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.ToolsAxes);

            Assert.That(model.SortMode, Is.EqualTo(ChecklistSortMode.Native));
        }

        [Test]
        public void ChangingSortMode_PreservesSortDirection()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "First", value: 100),
                CreateSortEntry(2, "Second", value: 200));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortDirection = ChecklistSortDirection.Descending,
                SortMode = ChecklistSortMode.Value
            };

            Assert.That(model.SortDirection, Is.EqualTo(ChecklistSortDirection.Descending));
        }

        [Test]
        public void NonNavigationFilters_PreserveSortModeAndDirection()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First", ItemCategoryMembership.Materials),
                new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 1, hasSharedResearchIdentity: false));
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog), researchState)
            {
                SortMode = ChecklistSortMode.Value,
                SortDirection = ChecklistSortDirection.Descending,
                SearchQuery = "First",
                SearchDescriptions = true,
                TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                    ItemTaxonomyFacetId.Materials,
                    ChecklistTaxonomyFacetState.Include),
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched
            };

            Assert.That(model.SortMode, Is.EqualTo(ChecklistSortMode.Value));
            Assert.That(model.SortDirection, Is.EqualTo(ChecklistSortDirection.Descending));
        }

        [Test]
        public void SortDirection_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                CreateSortEntry(1, "First", value: 100),
                CreateSortEntry(2, "Second", value: 200));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.Value
            };

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            double ratio = model.ScopeCompletionRatio;

            model.SortDirection = ChecklistSortDirection.Descending;
            _ = model.Items;

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(ratio));
        }

        [Test]
        public void MissingFilter_AfterItemBecomesFound_RefreshesResult()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.CompletionFilter = ChecklistCompletionFilter.Missing;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void FoundFilter_AfterItemBecomesFound_RefreshesResult()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.CompletionFilter = ChecklistCompletionFilter.Found;

            Assert.That(model.Items, Is.Empty);
            Assert.That(state.MarkFound(2), Is.True);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void Filters_AfterChecklistClear_RefreshResults()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(2), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.CompletionFilter = ChecklistCompletionFilter.Found;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));

            Assert.That(state.Clear(), Is.True);
            Assert.That(model.Items, Is.Empty);

            model.CompletionFilter = ChecklistCompletionFilter.Missing;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }


        [Test]
        public void DefaultCraftingFilter_IsAll()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.CraftingFilter, Is.EqualTo(ChecklistCraftingFilter.All));
        }

        [Test]
        public void CraftingFilter_WithUnsupportedValue_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { model.CraftingFilter = (ChecklistCraftingFilter)999; }));
        }

        [Test]
        public void CraftingFilter_WithoutCompleteBackend_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            var withoutAvailability = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                recipeIndex: recipeIndex);

            var withoutIndex = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                craftingAvailabilityState: craftingState);

            Assert.Throws<InvalidOperationException>(
                (Action)(() => { withoutAvailability.CraftingFilter = ChecklistCraftingFilter.HasRecipe; }));
            Assert.Throws<InvalidOperationException>(
                (Action)(() => { withoutIndex.CraftingFilter = ChecklistCraftingFilter.CraftableNow; }));
        }

        [Test]
        public void CraftingFilter_HasRecipe_ReturnsOnlyItemsWithProducingRecipe()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Produced"),
                new ItemCatalogEntry(2, "Ingredient Only"),
                new ItemCatalogEntry(3, "Unrelated"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, 2), CreateRecipeEntry(1, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            ChecklistFilterModel model = CreateCraftingItemIdModel(catalog, state, recipeIndex, craftingState);

            model.CraftingFilter = ChecklistCraftingFilter.HasRecipe;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(recipeIndex.GetRecipesUsing(2), Has.Count.EqualTo(1));
        }

        [Test]
        public void CraftingFilter_CraftableNow_ReturnsOnlyItemsWithAvailableProducingRecipe()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Available"),
                new ItemCatalogEntry(2, "Unavailable"),
                new ItemCatalogEntry(3, "No Recipe"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 99));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([0, 2]);
            ChecklistFilterModel model = CreateCraftingItemIdModel(catalog, state, recipeIndex, craftingState);

            model.CraftingFilter = ChecklistCraftingFilter.CraftableNow;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void CraftingFilter_CraftableNow_WithMultipleProducingRecipes_UsesAnyAvailableRecipe()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([1]);
            ChecklistFilterModel model = CreateCraftingItemIdModel(catalog, state, recipeIndex, craftingState);

            model.CraftingFilter = ChecklistCraftingFilter.CraftableNow;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void CraftingFilter_CraftableNow_RefreshesWhenAvailabilityRevisionChanges()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            ChecklistFilterModel model = CreateCraftingItemIdModel(catalog, state, recipeIndex, craftingState);
            model.CraftingFilter = ChecklistCraftingFilter.CraftableNow;

            Assert.That(model.Items, Is.Empty);

            Assert.That(craftingState.ReplaceSnapshot([0]), Is.True);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));

            Assert.That(craftingState.ReplaceSnapshot([]), Is.True);
            Assert.That(model.Items, Is.Empty);
        }

        [Test]
        public void CraftingFilter_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([1]);
            ChecklistFilterModel model = CreateCraftingItemIdModel(catalog, state, recipeIndex, craftingState);

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));

            model.CraftingFilter = ChecklistCraftingFilter.HasRecipe;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));

            model.CraftingFilter = ChecklistCraftingFilter.CraftableNow;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
        }

        [Test]
        public void CraftingFilter_CombinesWithExistingFiltersAfterScopeCalculation()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Alpha Blade",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials),
                new ItemCatalogEntry(
                    2,
                    "Alpha Found",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials),
                new ItemCatalogEntry(
                    3,
                    "Beta Blade",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials),
                new ItemCatalogEntry(4, "Alpha Tool", ItemCategoryMembership.Tools | ItemCategoryMembership.Materials),
                new ItemCatalogEntry(5, "Alpha No Material", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(
                    6,
                    "Alpha Researched",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials),
                new ItemCatalogEntry(
                    7,
                    "Alpha Not Craftable",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(2), Is.True);

            JourneyResearchState researchState = CreateResearchState(
                CreateResearchDefinition(1),
                CreateResearchDefinition(2),
                CreateResearchDefinition(3),
                CreateResearchDefinition(4),
                CreateResearchDefinition(5),
                CreateResearchDefinition(6),
                CreateResearchDefinition(7));
            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(6, 1)]);

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3),
                CreateRecipeEntry(3, 4),
                CreateRecipeEntry(4, 5),
                CreateRecipeEntry(5, 6),
                CreateRecipeEntry(6, 7));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([0, 1, 2, 3, 4, 5]);

            ChecklistFilterModel model = CreateCraftingItemIdModel(
                catalog,
                state,
                recipeIndex,
                craftingState,
                researchState);
            model.SearchQuery = "Alpha";
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Materials,
                ChecklistTaxonomyFacetState.Include);
            model.CompletionFilter = ChecklistCompletionFilter.Missing;
            model.ResearchFilter = ChecklistResearchFilter.Unresearched;
            model.CraftingFilter = ChecklistCraftingFilter.CraftableNow;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(4));
        }

        [Test]
        public void CraftingFilter_DoesNotMutateChecklistRecipesOrAvailabilityState()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Ingredient"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, 2));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([0]);
            long checklistRevision = state.Revision;
            long craftingRevision = craftingState.Revision;
            IReadOnlyList<RecipeCatalogEntry> producingBefore = recipeIndex.GetRecipesProducing(1);
            IReadOnlyList<RecipeCatalogEntry> usingBefore = recipeIndex.GetRecipesUsing(2);
            IReadOnlyList<int> availabilityBefore = craftingState.CreateAvailableRecipeIndexSnapshot();
            ChecklistFilterModel model = CreateCraftingItemIdModel(catalog, state, recipeIndex, craftingState);

            model.CraftingFilter = ChecklistCraftingFilter.HasRecipe;
            _ = model.Items;
            model.CraftingFilter = ChecklistCraftingFilter.CraftableNow;
            _ = model.Items;

            Assert.That(state.Revision, Is.EqualTo(checklistRevision));
            Assert.That(craftingState.Revision, Is.EqualTo(craftingRevision));
            Assert.That(craftingState.CreateAvailableRecipeIndexSnapshot(), Is.EqualTo(availabilityBefore));
            Assert.That(recipeIndex.GetRecipesProducing(1), Is.EqualTo(producingBefore));
            Assert.That(recipeIndex.GetRecipesUsing(2), Is.EqualTo(usingBefore));
        }


        [Test]
        public void NpcDropsOnly_DefaultsToFalse()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.NpcDropsOnly, Is.False);
        }

        [Test]
        public void NpcDropsOnly_WithoutNpcLootBackend_ThrowsWhenEnabled()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<InvalidOperationException>(() => model.NpcDropsOnly = true);
            Assert.That(model.NpcDropsOnly, Is.False);
        }

        [Test]
        public void NpcDropsOnly_ReturnsItemsWithAnyNpcDropSource()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Unconditional"),
                new ItemCatalogEntry(2, "Conditional"),
                new ItemCatalogEntry(3, "No NPC drop"));
            var state = new ChecklistState(catalog);
            NpcLootIndex npcLootIndex = CreateNpcLootIndex(
                new NpcLootRelation(10, 1, 1, 1, 0.25f),
                new NpcLootRelation(
                    20,
                    2,
                    1,
                    1,
                    0.5f,
                    [new NpcLootCondition(_ => true, "Only in another world state")]));
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                npcLootIndex: npcLootIndex)
            {
                SortMode = ChecklistSortMode.ItemId,
                NpcDropsOnly = true
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void NpcDropsOnly_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            NpcLootIndex npcLootIndex = CreateNpcLootIndex(new NpcLootRelation(10, 2, 1, 1, 0.25f));
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                npcLootIndex: npcLootIndex)
            {
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));

            model.NpcDropsOnly = true;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
        }

        [Test]
        public void NpcDropsOnly_CombinesWithPurchasableUsingAndSemanticsAndRefreshesWhenToggled()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Both"),
                new ItemCatalogEntry(2, "NPC only"),
                new ItemCatalogEntry(3, "Merchant only"),
                new ItemCatalogEntry(4, "Neither"));
            var state = new ChecklistState(catalog);
            NpcLootIndex npcLootIndex = CreateNpcLootIndex(
                new NpcLootRelation(10, 1, 1, 1, 0.25f),
                new NpcLootRelation(10, 2, 1, 1, 0.25f));
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(20, 1, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(20, 3, new MerchantSourceVariant(1, []))
            ]);
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                merchantSourceIndex: merchantIndex,
                npcLootIndex: npcLootIndex)
            {
                SortMode = ChecklistSortMode.ItemId,
                NpcDropsOnly = true
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));

            model.PurchasableOnly = true;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));

            model.NpcDropsOnly = false;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
        }

        [Test]
        public void NpcDropsOnly_CombinesWithExistingPostScopeFiltersAndRefreshesWhenToggled()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "NPC drop missing", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "NPC drop found", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "No NPC drop", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(4, "Wrong category", ItemCategoryMembership.Tools),
                new ItemCatalogEntry(5, "Already researched", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(6, "No recipe", ItemCategoryMembership.Weapons));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(2), Is.True);

            JourneyResearchState researchState = CreateResearchState(
                CreateResearchDefinition(1),
                CreateResearchDefinition(2),
                CreateResearchDefinition(3),
                CreateResearchDefinition(4),
                CreateResearchDefinition(5),
                CreateResearchDefinition(6));
            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(5, 1)]);

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3),
                CreateRecipeEntry(3, 4),
                CreateRecipeEntry(4, 5));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            NpcLootIndex npcLootIndex = CreateNpcLootIndex(
                new NpcLootRelation(10, 1, 1, 1, 0.25f),
                new NpcLootRelation(10, 2, 1, 1, 0.25f),
                new NpcLootRelation(10, 4, 1, 1, 0.25f),
                new NpcLootRelation(10, 5, 1, 1, 0.25f),
                new NpcLootRelation(10, 6, 1, 1, 0.25f));
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                researchState,
                recipeIndex,
                craftingState,
                npcLootIndex: npcLootIndex)
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons),
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched,
                CraftingFilter = ChecklistCraftingFilter.HasRecipe,
                SortMode = ChecklistSortMode.ItemId,
                NpcDropsOnly = true
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(5));

            model.NpcDropsOnly = false;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(5));
        }


        [Test]
        public void PurchasableOnly_DefaultsToFalse()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.PurchasableOnly, Is.False);
        }

        [Test]
        public void PurchasableOnly_WithoutMerchantBackend_ThrowsWhenEnabled()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<InvalidOperationException>(() => model.PurchasableOnly = true);
            Assert.That(model.PurchasableOnly, Is.False);
        }

        [Test]
        public void PurchasableOnly_ReturnsItemsWithAnyPotentialMerchantOffer()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Always"),
                new ItemCatalogEntry(2, "Conditional"),
                new ItemCatalogEntry(3, "Random"),
                new ItemCatalogEntry(4, "No merchant"));
            var state = new ChecklistState(catalog);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(11, 1, new MerchantSourceVariant(2, [])),
                new MerchantSourceRelation(
                    20,
                    2,
                    new MerchantSourceVariant(
                        2,
                        [new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)])),
                new MerchantSourceRelation(
                    30,
                    3,
                    new MerchantSourceVariant(19, [], MerchantSourceAvailabilityFlags.RandomStock))
            ]);
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                merchantSourceIndex: merchantIndex)
            {
                SortMode = ChecklistSortMode.ItemId,
                PurchasableOnly = true
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3]));
        }

        [Test]
        public void PurchasableOnly_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 2, new MerchantSourceVariant(1, []))
            ]);
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                merchantSourceIndex: merchantIndex)
            {
                SortMode = ChecklistSortMode.ItemId
            };

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));

            model.PurchasableOnly = true;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
        }

        [Test]
        public void PurchasableOnly_CombinesWithExistingPostScopeFiltersAndRefreshesWhenToggled()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Purchasable missing", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Purchasable found", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "Not purchasable", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(4, "Wrong category", ItemCategoryMembership.Tools),
                new ItemCatalogEntry(5, "Already researched", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(6, "No recipe", ItemCategoryMembership.Weapons));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(2), Is.True);

            JourneyResearchState researchState = CreateResearchState(
                CreateResearchDefinition(1),
                CreateResearchDefinition(2),
                CreateResearchDefinition(3),
                CreateResearchDefinition(4),
                CreateResearchDefinition(5),
                CreateResearchDefinition(6));
            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(5, 1)]);

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3),
                CreateRecipeEntry(3, 4),
                CreateRecipeEntry(4, 5));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(10, 2, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(10, 4, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(10, 5, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(10, 6, new MerchantSourceVariant(1, []))
            ]);
            var model = new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                researchState,
                recipeIndex,
                craftingState,
                merchantIndex)
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons),
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched,
                CraftingFilter = ChecklistCraftingFilter.HasRecipe,
                SortMode = ChecklistSortMode.ItemId,
                PurchasableOnly = true
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(5));

            model.PurchasableOnly = false;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(5));
        }


        [Test]
        public void DefaultFilters_ReturnAllCatalogItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(3, "Third", nativeSortGroup: 30),
                new ItemCatalogEntry(1, "First", nativeSortGroup: 10),
                new ItemCatalogEntry(2, "Second", nativeSortGroup: 20));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.NavigationFilter, Is.EqualTo(ChecklistNavigationFilter.AllItems));
            Assert.That(model.TaxonomyFacetSelection, Is.EqualTo(ChecklistTaxonomyFacetSelection.None));
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2, 3]));
        }

        [TestCase((int)ItemNavigationNodeId.Weapons, (int)ItemCategoryMembership.Weapons)]
        [TestCase((int)ItemNavigationNodeId.Armor, (int)ItemCategoryMembership.Armor)]
        [TestCase((int)ItemNavigationNodeId.Vanity, (int)ItemCategoryMembership.Vanity)]
        [TestCase((int)ItemNavigationNodeId.Blocks, (int)ItemCategoryMembership.Blocks)]
        [TestCase((int)ItemNavigationNodeId.Furniture, (int)ItemCategoryMembership.Furniture)]
        [TestCase((int)ItemNavigationNodeId.Accessories, (int)ItemCategoryMembership.Accessories)]
        [TestCase((int)ItemNavigationNodeId.MiscAccessories, (int)ItemCategoryMembership.MiscAccessories)]
        [TestCase((int)ItemNavigationNodeId.Consumables, (int)ItemCategoryMembership.Consumables)]
        [TestCase((int)ItemNavigationNodeId.Tools, (int)ItemCategoryMembership.Tools)]
        [TestCase((int)ItemNavigationNodeId.Materials, (int)ItemCategoryMembership.Materials)]
        [TestCase((int)ItemNavigationNodeId.Misc, (int)ItemCategoryMembership.Misc)]
        public void NavigationFilter_NativeRoot_ReturnsItemsWithSelectedMembership(int nodeValue, int categoryValue)
        {
            var nodeId = (ItemNavigationNodeId)nodeValue;
            var category = (ItemCategoryMembership)categoryValue;
            ItemCategoryMembership otherCategory = category == ItemCategoryMembership.Weapons
                ? ItemCategoryMembership.Tools
                : ItemCategoryMembership.Weapons;

            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First", category),
                new ItemCatalogEntry(2, "Second", otherCategory),
                new ItemCatalogEntry(3, "Third", category));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(nodeId);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
        }

        [Test]
        public void NavigationFilter_WithOverlappingNativeMemberships_MatchesEachRootWithoutDuplicates()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Damaging Tool", ItemCategoryMembership.Weapons | ItemCategoryMembership.Tools),
                new ItemCatalogEntry(2, "Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "Tool", ItemCategoryMembership.Tools));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Tools);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));
        }

        [Test]
        public void NavigationFilter_DeepNode_AppliesParentAndSemanticScope()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Weapon Melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee | ItemSemanticMembership.Yoyos),
                new ItemCatalogEntry(
                    2,
                    "Tool Yoyo Signal",
                    ItemCategoryMembership.Tools,
                    semanticMemberships: ItemSemanticMembership.Melee | ItemSemanticMembership.Yoyos),
                new ItemCatalogEntry(
                    3,
                    "Other Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Magic));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMeleeYoyos);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void NavigationFilter_GroupingOnlyRanged_UsesAcceptedUnionWithinWeapons()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Ranged Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.RangedWeapon),
                new ItemCatalogEntry(
                    2,
                    "Ammo",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    3,
                    "Ammo Outside Weapons",
                    ItemCategoryMembership.Consumables,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    4,
                    "Melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsRanged);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void NavigationFilter_Other_ReturnsOnlyContextualResidual()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(2, "Residual Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(
                    3,
                    "Residual With Tool Semantic",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Pickaxes),
                new ItemCatalogEntry(4, "Outside", ItemCategoryMembership.Tools));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Weapons);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 3]));
        }

        [Test]
        public void HasOtherItems_UsesCanonicalNavigationChildren()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(2, "Residual", ItemCategoryMembership.Weapons));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Weapons), Is.True);
            Assert.That(model.HasOtherItems(ItemNavigationNodeId.WeaponsMeleeYoyos), Is.False);
        }

        [Test]
        public void NavigationFilter_MiscQuestFishAndOther_UseCanonicalMiscProjection()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Quest Fish",
                    ItemCategoryMembership.Misc,
                    semanticMemberships: ItemSemanticMembership.QuestFish),
                new ItemCatalogEntry(2, "Residual Misc", ItemCategoryMembership.Misc),
                new ItemCatalogEntry(
                    3,
                    "Quest Fish outside Misc",
                    ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.QuestFish));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.MiscQuestFish);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Misc), Is.True);
            model.NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Misc);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void NavigationFilter_CombinesWithSearchFacetsAndCompletionWithoutChangingScopeSemantics()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Copper Ammo",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    2,
                    "Copper Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.RangedWeapon),
                new ItemCatalogEntry(
                    3,
                    "Copper Ammo Outside Weapons",
                    ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    4,
                    "Wood Ammo",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.Ammo));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.SearchQuery = "Copper";
            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Include);

            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(1));

            model.CompletionFilter = ChecklistCompletionFilter.Missing;

            Assert.That(model.Items, Is.Empty);
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(1));
        }

        [Test]
        public void NavigationFilter_CombinesWithJourneyResearchFilter()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(
                    2,
                    "Magic",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Magic),
                new ItemCatalogEntry(
                    3,
                    "Tool",
                    ItemCategoryMembership.Tools,
                    semanticMemberships: ItemSemanticMembership.Pickaxes));

            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(2, 5)]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMelee);
            model.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void ChangingNavigationFilter_PreservesFacetsAndRefreshesResult()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Weapon Ammo",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    2,
                    "Consumable Ammo",
                    ItemCategoryMembership.Consumables,
                    semanticMemberships: ItemSemanticMembership.Ammo));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Dyes, ChecklistTaxonomyFacetState.Exclude);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Consumables);
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
            Assert.That(
                model.TaxonomyFacetSelection.GetState(ItemTaxonomyFacetId.Ammo),
                Is.EqualTo(ChecklistTaxonomyFacetState.Include));
            Assert.That(
                model.TaxonomyFacetSelection.GetState(ItemTaxonomyFacetId.Dyes),
                Is.EqualTo(ChecklistTaxonomyFacetState.Exclude));
        }

        [Test]
        public void ChangingNavigationFilter_PreservesIndependentFilterDimensions()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Weapon Ammo",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    2,
                    "Consumable Ammo",
                    ItemCategoryMembership.Consumables,
                    semanticMemberships: ItemSemanticMembership.Ammo));

            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 1, hasSharedResearchIdentity: false));

            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog), researchState)
            {
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched,
                TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                    ItemTaxonomyFacetId.Ammo,
                    ChecklistTaxonomyFacetState.Include),
                SortMode = ChecklistSortMode.Name,
                SortDirection = ChecklistSortDirection.Descending,
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons)
            };

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Consumables);

            Assert.That(model.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.Missing));
            Assert.That(model.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.Unresearched));
            Assert.That(
                model.TaxonomyFacetSelection.GetState(ItemTaxonomyFacetId.Ammo),
                Is.EqualTo(ChecklistTaxonomyFacetState.Include));
            Assert.That(model.SortMode, Is.EqualTo(ChecklistSortMode.Name));
            Assert.That(model.SortDirection, Is.EqualTo(ChecklistSortDirection.Descending));
        }

        [Test]
        public void ScopeProgress_AfterChecklistStateChanges_RefreshesInsideNavigationScope()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Weapon One", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Weapon Two", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "Tool", ItemCategoryMembership.Tools));

            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);

            Assert.That(model.ScopeFoundCount, Is.Zero);
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));

            Assert.That(state.MarkFound(2), Is.True);
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));

            Assert.That(state.Clear(), Is.True);
            Assert.That(model.ScopeFoundCount, Is.Zero);
        }

        [Test]
        public void Sorting_IsAppliedAfterNavigationFiltering()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Zulu",
                    ItemCategoryMembership.Weapons,
                    nativeSortGroup: 20,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(
                    2,
                    "Alpha",
                    ItemCategoryMembership.Weapons,
                    nativeSortGroup: 10,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(
                    3,
                    "Outside",
                    ItemCategoryMembership.Tools,
                    nativeSortGroup: 0,
                    semanticMemberships: ItemSemanticMembership.Melee));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMelee)
            };

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1]));

            model.SortMode = ChecklistSortMode.Name;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([2, 1]));

            model.SortMode = ChecklistSortMode.ItemId;
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void ChangingFiltersAndSorting_DoesNotModifyChecklistState()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Copper Sword",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials,
                    nativeSortGroup: 20,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(2, "Wooden Sword", ItemCategoryMembership.Weapons, nativeSortGroup: 30),
                new ItemCatalogEntry(3, "Copper Pickaxe", ItemCategoryMembership.Tools, nativeSortGroup: 10));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(2), Is.True);

            int foundCount = state.FoundCount;
            double completionRatio = state.CompletionRatio;
            IReadOnlyList<int> foundItemIds = state.CreateFoundItemIdSnapshot();

            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SearchQuery = "Copper",
                CompletionFilter = ChecklistCompletionFilter.Missing,
                NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMelee),
                TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                    ItemTaxonomyFacetId.Materials,
                    ChecklistTaxonomyFacetState.Include),
                SortMode = ChecklistSortMode.Name,
                SortDirection = ChecklistSortDirection.Descending
            };

            _ = model.Items;
            _ = model.ScopeFoundCount;
            _ = model.ScopeTotalCount;
            _ = model.ScopeCompletionRatio;

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Tools);
            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None;
            model.SortMode = ChecklistSortMode.ItemId;
            _ = model.Items;

            model.NavigationFilter = ChecklistNavigationFilter.AllItems;
            model.SortMode = ChecklistSortMode.Native;
            _ = model.Items;

            Assert.That(state.FoundCount, Is.EqualTo(foundCount));
            Assert.That(state.CompletionRatio, Is.EqualTo(completionRatio));
            Assert.That(state.CreateFoundItemIdSnapshot(), Is.EqualTo(foundItemIds));
        }

        [Test]
        public void NavigationFilter_WithUnknownNode_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));
            var unsupported = ChecklistNavigationFilter.ForNode((ItemNavigationNodeId)999999);

            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => { model.NavigationFilter = unsupported; }));
        }

        [Test]
        public void NavigationFilter_OtherOnLeafNode_Throws()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Yoyo",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee | ItemSemanticMembership.Yoyos));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<InvalidOperationException>(
                (Action)(() =>
                {
                    model.NavigationFilter =
                        ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.WeaponsMeleeYoyos);
                }));
        }

        [Test]
        public void NavigationFilter_OtherWithoutResidual_Throws()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(
                    2,
                    "Magic",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Magic));

            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Weapons), Is.False);
            Assert.Throws<InvalidOperationException>(
                (Action)(() =>
                {
                    model.NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Weapons);
                }));
        }

        [Test]
        public void DefaultResearchFilter_IsAll()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState =
                CreateResearchState(new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog), researchState);

            Assert.That(model.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.All));
            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void ResearchFilter_All_IncludesResearchableAndNonResearchableItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Researchable"),
                new ItemCatalogEntry(2, "Non Researchable"));

            var state = new ChecklistState(catalog);
            JourneyResearchState researchState =
                CreateResearchState(new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.ResearchFilter = ChecklistResearchFilter.All;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void ResearchFilter_Researched_ReturnsOnlyFullyResearchedItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Zero"),
                new ItemCatalogEntry(2, "Partial"),
                new ItemCatalogEntry(3, "Complete"),
                new ItemCatalogEntry(4, "Non Researchable"));

            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot(
            [
                new KeyValuePair<int, int>(2, 3),
                new KeyValuePair<int, int>(3, 5)
            ]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.ResearchFilter = ChecklistResearchFilter.Researched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([3]));
        }

        [Test]
        public void ResearchFilter_Unresearched_ReturnsZeroAndPartialResearchItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Zero"),
                new ItemCatalogEntry(2, "Partial"),
                new ItemCatalogEntry(3, "Complete"),
                new ItemCatalogEntry(4, "Non Researchable"));

            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot(
            [
                new KeyValuePair<int, int>(2, 3),
                new KeyValuePair<int, int>(3, 5)
            ]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void ResearchFilter_CombinesWithFoundCompletionFilter()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(2), Is.True);

            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot(
            [
                new KeyValuePair<int, int>(1, 2),
                new KeyValuePair<int, int>(2, 5)
            ]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.CompletionFilter = ChecklistCompletionFilter.Found;
            model.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void ResearchFilter_CombinesWithMissingCompletionFilter()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);

            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(3, 5)]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.CompletionFilter = ChecklistCompletionFilter.Missing;
            model.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void ResearchFilter_CombinesWithSearch()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"),
                new ItemCatalogEntry(3, "Copper Pickaxe"));

            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 5)]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.SearchQuery = "Copper";
            model.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([3]));
        }

        [Test]
        public void ResearchFilter_DoesNotChangeScopeProgress()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"),
                new ItemCatalogEntry(3, "Third"));

            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(2), Is.True);

            JourneyResearchState researchState = CreateResearchState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 2, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 3, 5, hasSharedResearchIdentity: false));

            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 5)]);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.ResearchFilter = ChecklistResearchFilter.Researched;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(2));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(3));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(2.0 / 3.0));
        }

        [Test]
        public void ResearchStateRevision_AfterProgressChange_RefreshesResult()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState =
                CreateResearchState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            ChecklistFilterModel model = CreateItemIdModel(catalog, state, researchState);
            model.ResearchFilter = ChecklistResearchFilter.Researched;

            Assert.That(model.Items, Is.Empty);

            Assert.That(researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 5)]), Is.True);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void ResearchFilter_WithoutJourneyResearchState_RejectsJourneyFilters()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<InvalidOperationException>(
                (Action)(() => { model.ResearchFilter = ChecklistResearchFilter.Researched; }));
            Assert.Throws<InvalidOperationException>(
                (Action)(() => { model.ResearchFilter = ChecklistResearchFilter.Unresearched; }));
        }


        [Test]
        public void TaxonomyFacetSelection_WithNone_DoesNotChangeResult()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second", semanticMemberships: ItemSemanticMembership.Ammo));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));
        }

        [Test]
        public void TaxonomyFacetSelection_IncludeNativeMaterials_MatchesNativeMembership()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Material", ItemCategoryMembership.Materials),
                new ItemCatalogEntry(2, "Other", ItemCategoryMembership.Misc));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Materials,
                ChecklistTaxonomyFacetState.Include);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void TaxonomyFacetSelection_IncludeSemanticAmmo_MatchesSemanticMembership()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(2, "Other"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Ammo,
                ChecklistTaxonomyFacetState.Include);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void TaxonomyFacetSelection_Fishing_UsesDirectFishingMembership()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Direct Fish", semanticMemberships: ItemSemanticMembership.Fishing),
                new ItemCatalogEntry(2, "Crate Content"),
                new ItemCatalogEntry(3, "Other Fish", semanticMemberships: ItemSemanticMembership.Fishing));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Fishing,
                ChecklistTaxonomyFacetState.Include);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 3]));

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Fishing,
                ChecklistTaxonomyFacetState.Exclude);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void TaxonomyFacetSelection_WhenChanged_RefreshesCachedResult()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(2, "Other"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1, 2]));

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Ammo,
                ChecklistTaxonomyFacetState.Exclude);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void TaxonomyFacetSelection_MultipleIncludedFacets_CombineWithAnd()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Material Ammo",
                    ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    2,
                    "Ammo Only",
                    ItemCategoryMembership.Misc,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(3, "Material Only", ItemCategoryMembership.Materials));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Include);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void TaxonomyFacetSelection_ExcludedFacet_RemovesMatchingItems()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(2, "Other"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Ammo,
                ChecklistTaxonomyFacetState.Exclude);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([2]));
        }

        [Test]
        public void TaxonomyFacetSelection_MultipleExcludedFacets_RejectAnyMatchingExcludedFacet()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Material", ItemCategoryMembership.Materials),
                new ItemCatalogEntry(2, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(
                    3,
                    "Material Ammo",
                    ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(4, "Other"));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);

            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Exclude)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([4]));
        }

        [Test]
        public void TaxonomyFacetSelection_IncludeAndExclude_CombineWithinNavigationScope()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Weapon Material",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials),
                new ItemCatalogEntry(
                    2,
                    "Weapon Material Ammo",
                    ItemCategoryMembership.Weapons | ItemCategoryMembership.Materials,
                    semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(3, "Material Outside Weapons", ItemCategoryMembership.Materials),
                new ItemCatalogEntry(4, "Weapon", ItemCategoryMembership.Weapons));
            var state = new ChecklistState(catalog);
            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude);

            Assert.That(GetItemIds(model.Items), Is.EqualTo([1]));
        }

        [Test]
        public void TaxonomyFacetSelection_AffectsScopeProgressBeforeCompletionFilter()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Found Ammo", semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(2, "Missing Ammo", semanticMemberships: ItemSemanticMembership.Ammo),
                new ItemCatalogEntry(3, "Found Other"),
                new ItemCatalogEntry(4, "Missing Other"));
            var state = new ChecklistState(catalog);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            ChecklistFilterModel model = CreateItemIdModel(catalog, state);
            model.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Ammo,
                ChecklistTaxonomyFacetState.Exclude);
            model.CompletionFilter = ChecklistCompletionFilter.Missing;

            Assert.That(GetItemIds(model.Items), Is.EqualTo([4]));
            Assert.That(model.ScopeFoundCount, Is.EqualTo(1));
            Assert.That(model.ScopeTotalCount, Is.EqualTo(2));
            Assert.That(model.ScopeCompletionRatio, Is.EqualTo(0.5));
        }

        [Test]
        public void ResearchFilter_WithUnsupportedValue_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState =
                CreateResearchState(new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog), researchState);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { model.ResearchFilter = (ChecklistResearchFilter)999; }));
        }

        [Test]
        public void CompletionFilter_WithUnsupportedValue_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { model.CompletionFilter = (ChecklistCompletionFilter)999; }));
        }

        [Test]
        public void SortMode_WithUnsupportedValue_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => { model.SortMode = (ChecklistSortMode)999; }));
        }

        [Test]
        public void SortDirection_WithUnsupportedValue_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog));

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { model.SortDirection = (ChecklistSortDirection)999; }));
        }

        private static void AssertContextualSort(
            ChecklistFilterModel model,
            ItemNavigationNodeId nodeId,
            ChecklistSortMode sortMode,
            int[] ascendingIds,
            int[] descendingIds)
        {
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(nodeId);
            model.SortMode = sortMode;
            model.SortDirection = ChecklistSortDirection.Ascending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo(ascendingIds));

            model.SortDirection = ChecklistSortDirection.Descending;

            Assert.That(GetItemIds(model.Items), Is.EqualTo(descendingIds));
        }

        private static ChecklistSortMode[] CommonSortModes()
        {
            return
            [
                ChecklistSortMode.Native,
                ChecklistSortMode.ItemId,
                ChecklistSortMode.Name,
                ChecklistSortMode.Value,
                ChecklistSortMode.Rarity
            ];
        }

        private static ChecklistSortMode[] WithContextualSort(ChecklistSortMode contextualSortMode)
        {
            return
            [
                ChecklistSortMode.Native,
                ChecklistSortMode.ItemId,
                ChecklistSortMode.Name,
                ChecklistSortMode.Value,
                ChecklistSortMode.Rarity,
                contextualSortMode
            ];
        }

        private static ItemCatalogEntry CreateSortEntry(
            int id,
            string name,
            ItemCategoryMembership categoryMemberships = ItemCategoryMembership.None,
            ItemSemanticMembership semanticMemberships = ItemSemanticMembership.None,
            int nativeSortGroup = 0,
            int nativeSortOrder = 0,
            int value = 0,
            int rarity = 0,
            int damage = 0,
            int defense = 0,
            int pickPower = 0,
            int axePower = 0,
            int hammerPower = 0,
            int fishingPower = 0)
        {
            return new ItemCatalogEntry(
                id,
                name,
                categoryMemberships,
                nativeSortGroup,
                nativeSortOrder,
                semanticMemberships,
                new ItemSortMetrics(value, rarity, damage, defense, pickPower, axePower, hammerPower, fishingPower));
        }

        private static ItemTextIndex CreateItemTextIndex(ItemCatalog catalog)
        {
            return new ItemTextIndex(catalog);
        }

        private static void ReplaceItemTextDescriptions(
            ItemTextIndex itemTextIndex,
            ItemCatalog catalog,
            IReadOnlyDictionary<int, string> overrides,
            string cultureName = "en-US")
        {
            var names = new Dictionary<int, string>(catalog.Count);
            var descriptions = new Dictionary<int, string>(catalog.Count);

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                names.Add(entry.Id, entry.Name);
                descriptions.Add(
                    entry.Id,
                    overrides != null && overrides.TryGetValue(entry.Id, out string description)
                        ? description
                        : string.Empty);
            }

            itemTextIndex.ReplaceSnapshot(cultureName, names, descriptions);
        }

        private static void ReplaceItemTextNames(
            ItemTextIndex itemTextIndex,
            ItemCatalog catalog,
            IReadOnlyDictionary<int, string> overrides,
            string cultureName)
        {
            var names = new Dictionary<int, string>(catalog.Count);
            var descriptions = new Dictionary<int, string>(catalog.Count);

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                names.Add(
                    entry.Id,
                    overrides != null && overrides.TryGetValue(entry.Id, out string name) ? name : entry.Name);
                descriptions.Add(entry.Id, string.Empty);
            }

            itemTextIndex.ReplaceSnapshot(cultureName, names, descriptions);
        }

        private static JourneyResearchDefinition CreateResearchDefinition(int itemId)
        {
            return new JourneyResearchDefinition(itemId, itemId, 1, hasSharedResearchIdentity: false);
        }

        private static RecipeCatalog CreateRecipeCatalog(params RecipeCatalogEntry[] recipes)
        {
            return RecipeCatalog.Create(recipes);
        }

        private static RecipeCatalogEntry CreateRecipeEntry(
            int runtimeIndex,
            int resultItemId,
            params int[] ingredientItemIds)
        {
            var ingredients = new RecipeIngredient[ingredientItemIds.Length];

            for (var index = 0; index < ingredientItemIds.Length; index++)
            {
                int itemId = ingredientItemIds[index];
                ingredients[index] = new RecipeIngredient(itemId, 1, RecipeIngredientRequirement.ForItem(itemId));
            }

            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                ingredients,
                new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                isAlchemy: false);
        }

        private static ChecklistFilterModel CreateCraftingItemIdModel(
            ItemCatalog catalog,
            ChecklistState state,
            RecipeIndex recipeIndex,
            CraftingAvailabilityState craftingAvailabilityState,
            JourneyResearchState researchState = null)
        {
            return new ChecklistFilterModel(
                catalog,
                state,
                CreateItemTextIndex(catalog),
                researchState,
                recipeIndex,
                craftingAvailabilityState)
            {
                SortMode = ChecklistSortMode.ItemId
            };
        }

        private static NpcLootIndex CreateNpcLootIndex(params NpcLootRelation[] relations)
        {
            int[] npcNetIds = relations.Select(relation => relation.NpcNetId).Distinct().ToArray();
            var entries = new NpcCatalogEntry[npcNetIds.Length];

            for (var index = 0; index < npcNetIds.Length; index++)
            {
                int npcNetId = npcNetIds[index];
                entries[index] = new NpcCatalogEntry(npcNetId, index);
            }

            return NpcLootIndex.Create(NpcCatalog.Create(entries), relations);
        }

        private static JourneyResearchState CreateResearchState(params JourneyResearchDefinition[] definitions)
        {
            return new JourneyResearchState(definitions);
        }

        private static ChecklistFilterModel CreateItemIdModel(
            ItemCatalog catalog,
            ChecklistState state,
            JourneyResearchState researchState)
        {
            return new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog), researchState)
            {
                SortMode = ChecklistSortMode.ItemId
            };
        }

        private static ChecklistFilterModel CreateItemIdModel(ItemCatalog catalog, ChecklistState state)
        {
            return new ChecklistFilterModel(catalog, state, CreateItemTextIndex(catalog))
            {
                SortMode = ChecklistSortMode.ItemId
            };
        }

        private static ItemCatalog CreateCatalog(params ItemCatalogEntry[] entries)
        {
            return ItemCatalog.Create(entries);
        }

        private static int[] GetItemIds(IReadOnlyList<ItemCatalogEntry> items)
        {
            return items.Select(entry => entry.Id).ToArray();
        }
    }
}