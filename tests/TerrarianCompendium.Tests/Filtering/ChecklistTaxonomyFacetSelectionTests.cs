using System;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Filtering;

namespace TerrarianCompendium.Tests.Filtering
{
    [TestFixture]
    public sealed class ChecklistTaxonomyFacetSelectionTests
    {
        [Test]
        public void None_HasAllFacetsOff()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None;

            Assert.That(selection.IncludedFacets, Is.EqualTo(ItemTaxonomyFacetId.None));
            Assert.That(selection.ExcludedFacets, Is.EqualTo(ItemTaxonomyFacetId.None));
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Off));
        }

        [Test]
        public void SetState_Include_AddsOnlyIncludedMask()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Ammo,
                ChecklistTaxonomyFacetState.Include);

            Assert.That(selection.IncludedFacets, Is.EqualTo(ItemTaxonomyFacetId.Ammo));
            Assert.That(selection.ExcludedFacets, Is.EqualTo(ItemTaxonomyFacetId.None));
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Include));
        }

        [Test]
        public void SetState_Exclude_AddsOnlyExcludedMask()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None.SetState(
                ItemTaxonomyFacetId.Ammo,
                ChecklistTaxonomyFacetState.Exclude);

            Assert.That(selection.IncludedFacets, Is.EqualTo(ItemTaxonomyFacetId.None));
            Assert.That(selection.ExcludedFacets, Is.EqualTo(ItemTaxonomyFacetId.Ammo));
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Exclude));
        }

        [Test]
        public void SetState_ReplacesPreviousStateForSameFacet()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude);

            Assert.That(selection.IncludedFacets, Is.EqualTo(ItemTaxonomyFacetId.None));
            Assert.That(selection.ExcludedFacets, Is.EqualTo(ItemTaxonomyFacetId.Ammo));
        }

        [Test]
        public void SetState_Off_ClearsFacetFromBothMasks()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Off);

            Assert.That(selection, Is.EqualTo(ChecklistTaxonomyFacetSelection.None));
        }

        [Test]
        public void MultipleFacets_HaveIndependentStates()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude);

            Assert.That(
                selection.GetState(ItemTaxonomyFacetId.Materials),
                Is.EqualTo(ChecklistTaxonomyFacetState.Include));
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Exclude));
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Dyes), Is.EqualTo(ChecklistTaxonomyFacetState.Off));
        }

        [Test]
        public void Cycle_UsesOffIncludeExcludeOrder()
        {
            ChecklistTaxonomyFacetSelection selection = ChecklistTaxonomyFacetSelection.None;

            selection = selection.Cycle(ItemTaxonomyFacetId.Ammo);
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Include));

            selection = selection.Cycle(ItemTaxonomyFacetId.Ammo);
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Exclude));

            selection = selection.Cycle(ItemTaxonomyFacetId.Ammo);
            Assert.That(selection.GetState(ItemTaxonomyFacetId.Ammo), Is.EqualTo(ChecklistTaxonomyFacetState.Off));
        }

        [Test]
        public void SetState_WithUnknownFacet_Throws()
        {
            var unknown = (ItemTaxonomyFacetId)(1 << 20);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() =>
                {
                    ChecklistTaxonomyFacetSelection.None.SetState(unknown, ChecklistTaxonomyFacetState.Include);
                }));
        }

        [Test]
        public void SetState_WithCombinedFacetMask_Throws()
        {
            const ItemTaxonomyFacetId combined = ItemTaxonomyFacetId.Materials | ItemTaxonomyFacetId.Ammo;

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() =>
                {
                    ChecklistTaxonomyFacetSelection.None.SetState(combined, ChecklistTaxonomyFacetState.Include);
                }));
        }

        [Test]
        public void SetState_WithUnsupportedState_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() =>
                {
                    ChecklistTaxonomyFacetSelection.None.SetState(
                        ItemTaxonomyFacetId.Ammo,
                        (ChecklistTaxonomyFacetState)999);
                }));
        }

        [Test]
        public void Equality_WithSameStates_ReturnsTrue()
        {
            ChecklistTaxonomyFacetSelection left = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include)
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude);
            ChecklistTaxonomyFacetSelection right = ChecklistTaxonomyFacetSelection.None
                .SetState(ItemTaxonomyFacetId.Ammo, ChecklistTaxonomyFacetState.Exclude)
                .SetState(ItemTaxonomyFacetId.Materials, ChecklistTaxonomyFacetState.Include);

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }
    }
}