using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class ItemFilterPopupTests
    {
        [Test]
        public void CalculateActiveFilterCount_DefaultState_ReturnsZero()
        {
            int result = Calculate();

            Assert.That(result, Is.Zero);
        }

        [Test]
        public void CalculateActiveFilterCount_CompletionFilter_CountsAsOne()
        {
            int result = Calculate(completionFilter: ChecklistCompletionFilter.Missing);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void CalculateActiveFilterCount_ResearchFilter_CountsOnlyWhenAvailable()
        {
            int unavailableResult = Calculate(
                journeyResearchAvailable: false,
                researchFilter: ChecklistResearchFilter.Researched);
            int availableResult = Calculate(
                journeyResearchAvailable: true,
                researchFilter: ChecklistResearchFilter.Researched);

            Assert.Multiple(() =>
            {
                Assert.That(unavailableResult, Is.Zero);
                Assert.That(availableResult, Is.EqualTo(1));
            });
        }

        [Test]
        public void CalculateActiveFilterCount_CraftingFilter_CountsOnlyWhenAvailable()
        {
            int unavailableResult = Calculate(
                craftingFiltersAvailable: false,
                craftingFilter: ChecklistCraftingFilter.CraftableNow);
            int availableResult = Calculate(
                craftingFiltersAvailable: true,
                craftingFilter: ChecklistCraftingFilter.CraftableNow);

            Assert.Multiple(() =>
            {
                Assert.That(unavailableResult, Is.Zero);
                Assert.That(availableResult, Is.EqualTo(1));
            });
        }

        [Test]
        public void CalculateActiveFilterCount_NpcDrops_CountsOnlyWhenAvailable()
        {
            int unavailableResult = Calculate(npcDropFiltersAvailable: false, npcDropsOnly: true);
            int availableResult = Calculate(npcDropFiltersAvailable: true, npcDropsOnly: true);

            Assert.Multiple(() =>
            {
                Assert.That(unavailableResult, Is.Zero);
                Assert.That(availableResult, Is.EqualTo(1));
            });
        }

        [Test]
        public void CalculateActiveFilterCount_Purchasable_CountsOnlyWhenAvailable()
        {
            int unavailableResult = Calculate(merchantFiltersAvailable: false, purchasableOnly: true);
            int availableResult = Calculate(merchantFiltersAvailable: true, purchasableOnly: true);

            Assert.Multiple(() =>
            {
                Assert.That(unavailableResult, Is.Zero);
                Assert.That(availableResult, Is.EqualTo(1));
            });
        }

        [Test]
        public void CalculateActiveFilterCount_AcquisitionFilters_CountIndependently()
        {
            int npcOnly = Calculate(npcDropFiltersAvailable: true, npcDropsOnly: true);
            int merchantOnly = Calculate(merchantFiltersAvailable: true, purchasableOnly: true);
            int both = Calculate(
                npcDropFiltersAvailable: true,
                npcDropsOnly: true,
                merchantFiltersAvailable: true,
                purchasableOnly: true);

            Assert.Multiple(() =>
            {
                Assert.That(npcOnly, Is.EqualTo(1));
                Assert.That(merchantOnly, Is.EqualTo(1));
                Assert.That(both, Is.EqualTo(2));
            });
        }

        [Test]
        public void CalculateActiveFilterCount_ActiveFacet_CountsAsOne()
        {
            ChecklistTaxonomyFacetSelection includeSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Materials,
                ChecklistTaxonomyFacetState.Include);

            ChecklistTaxonomyFacetSelection excludeSelection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Materials,
                ChecklistTaxonomyFacetState.Exclude);

            int includeResult = Calculate(facetSelection: includeSelection);
            int excludeResult = Calculate(facetSelection: excludeSelection);

            Assert.Multiple(() =>
            {
                Assert.That(includeResult, Is.EqualTo(1));
                Assert.That(excludeResult, Is.EqualTo(1));
            });
        }

        [Test]
        public void CalculateActiveFilterCount_MultipleFacets_CountsEachFacet()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Dyes, ChecklistTaxonomyFacetState.Exclude);

            int result = Calculate(facetSelection: selection);

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void CalculateActiveFilterCount_MixedCriteria_ReturnsCombinedCount()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.MusicBoxes, ChecklistTaxonomyFacetState.Exclude);

            int result = Calculate(
                completionFilter: ChecklistCompletionFilter.Found,
                journeyResearchAvailable: true,
                researchFilter: ChecklistResearchFilter.Unresearched,
                craftingFiltersAvailable: true,
                craftingFilter: ChecklistCraftingFilter.HasRecipe,
                npcDropFiltersAvailable: true,
                npcDropsOnly: true,
                merchantFiltersAvailable: true,
                purchasableOnly: true,
                facetSelection: selection);

            Assert.That(result, Is.EqualTo(7));
        }

        private static int Calculate(
            ChecklistCompletionFilter completionFilter = ChecklistCompletionFilter.All,
            bool journeyResearchAvailable = false,
            ChecklistResearchFilter researchFilter = ChecklistResearchFilter.All,
            bool craftingFiltersAvailable = false,
            ChecklistCraftingFilter craftingFilter = ChecklistCraftingFilter.All,
            bool npcDropFiltersAvailable = false,
            bool npcDropsOnly = false,
            bool merchantFiltersAvailable = false,
            bool purchasableOnly = false,
            ChecklistTaxonomyFacetSelection facetSelection = default)
        {
            return ItemFilterPresentation.CalculateActiveFilterCount(
                completionFilter,
                journeyResearchAvailable,
                researchFilter,
                craftingFiltersAvailable,
                craftingFilter,
                npcDropFiltersAvailable,
                npcDropsOnly,
                merchantFiltersAvailable,
                purchasableOnly,
                facetSelection);
        }
    }
}