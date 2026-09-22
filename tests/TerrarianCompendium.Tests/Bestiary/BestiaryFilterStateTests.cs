using System;
using NUnit.Framework;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.Tests.Bestiary
{
    [TestFixture]
    public sealed class BestiaryFilterStateTests
    {
        [Test]
        public void Constructor_UsesDefaultState()
        {
            var state = new BestiaryFilterState([10, 20]);

            Assert.That(state.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(state.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(state.EncounterFilter, Is.EqualTo(BestiaryEncounterFilter.All));
            Assert.That(state.HasStockOnly, Is.False);
            Assert.That(state.ActiveFilterCount, Is.Zero);
            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void EncounterFilter_IsOneOfManyAndCountsAsSingleFilter()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                EncounterFilter = BestiaryEncounterFilter.Encountered
            };

            state.EncounterFilter = BestiaryEncounterFilter.Unknown;

            Assert.That(state.EncounterFilter, Is.EqualTo(BestiaryEncounterFilter.Unknown));
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void HasStockOnly_ChangesRevisionAndCountsAsSingleFilter()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                HasStockOnly = true
            };

            Assert.That(state.HasStockOnly, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(1));

            state.HasStockOnly = false;

            Assert.That(state.HasStockOnly, Is.False);
            Assert.That(state.ActiveFilterCount, Is.Zero);
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void SettingSameHasStockOnly_DoesNotAdvanceRevision()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                HasStockOnly = false
            };

            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void BestiaryCriterion_NativeSelectionIsMutuallyExclusive()
        {
            var state = new BestiaryFilterState([10, 20])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10)
            };

            state.BestiaryCriterion = BestiaryFilterCriterion.ForNative(20);

            Assert.That(state.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.ForNative(20)));
            Assert.That(state.IsNativeFilterActive(10), Is.False);
            Assert.That(state.IsNativeFilterActive(20), Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void DropCriterion_LootAwareSelectionIsMutuallyExclusive()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                DropCriterion = BestiaryFilterCriterion.HasDrops
            };

            state.DropCriterion = BestiaryFilterCriterion.HasMissingDrops;
            state.DropCriterion = BestiaryFilterCriterion.HasUnresearchedDrops;

            Assert.That(state.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.HasUnresearchedDrops));
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(3));
        }

        [Test]
        public void NativeAndDropSelections_AreIndependent()
        {
            var state = new BestiaryFilterState([10])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10),
                DropCriterion = BestiaryFilterCriterion.HasMissingDrops
            };

            Assert.That(state.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.ForNative(10)));
            Assert.That(state.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.HasMissingDrops));
            Assert.That(state.IsNativeFilterActive(10), Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(2));
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void DropCriterion_HasMissingDrops_UsesOnlyChecklistState()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                DropCriterion = BestiaryFilterCriterion.HasMissingDrops
            };

            Assert.That(state.UsesChecklistState, Is.True);
            Assert.That(state.UsesJourneyResearchState, Is.False);
        }

        [Test]
        public void DropCriterion_HasUnresearchedDrops_UsesOnlyJourneyResearchState()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                DropCriterion = BestiaryFilterCriterion.HasUnresearchedDrops
            };

            Assert.That(state.UsesChecklistState, Is.False);
            Assert.That(state.UsesJourneyResearchState, Is.True);
        }

        [Test]
        public void DropCriterion_OtherModes_DoNotUseCollectionOrResearchState()
        {
            var state = new BestiaryFilterState(Array.Empty<int>());
            BestiaryFilterCriterion[] criteria =
            [
                BestiaryFilterCriterion.All,
                BestiaryFilterCriterion.HasDrops
            ];

            foreach (BestiaryFilterCriterion criterion in criteria)
            {
                state.DropCriterion = criterion;
                Assert.That(state.UsesChecklistState, Is.False);
                Assert.That(state.UsesJourneyResearchState, Is.False);
            }
        }

        [Test]
        public void SettingSameBestiaryCriterion_DoesNotAdvanceRevision()
        {
            var state = new BestiaryFilterState([10])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10)
            };
            long revision = state.Revision;

            state.BestiaryCriterion = BestiaryFilterCriterion.ForNative(10);

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void SettingSameDropCriterion_DoesNotAdvanceRevision()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                DropCriterion = BestiaryFilterCriterion.HasDrops
            };
            long revision = state.Revision;

            state.DropCriterion = BestiaryFilterCriterion.HasDrops;

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void BestiaryCriterion_AllClearsOnlyNativeSelection()
        {
            var state = new BestiaryFilterState([10])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10),
                DropCriterion = BestiaryFilterCriterion.HasDrops
            };

            state.BestiaryCriterion = BestiaryFilterCriterion.All;

            Assert.That(state.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(state.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.HasDrops));
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(3));
        }

        [Test]
        public void DropCriterion_AllClearsOnlyDropSelection()
        {
            var state = new BestiaryFilterState([10])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10),
                DropCriterion = BestiaryFilterCriterion.HasDrops
            };

            state.DropCriterion = BestiaryFilterCriterion.All;

            Assert.That(state.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.ForNative(10)));
            Assert.That(state.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(3));
        }

        [Test]
        public void SettingSameEncounterFilter_DoesNotAdvanceRevision()
        {
            var state = new BestiaryFilterState(Array.Empty<int>())
            {
                EncounterFilter = BestiaryEncounterFilter.All
            };

            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void Clear_ResetsBestiaryDropsEncounterAndStockFilters()
        {
            var state = new BestiaryFilterState([10])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10),
                DropCriterion = BestiaryFilterCriterion.HasMissingDrops,
                EncounterFilter = BestiaryEncounterFilter.Encountered,
                HasStockOnly = true
            };
            long before = state.Revision;

            state.Clear();

            Assert.That(state.BestiaryCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(state.DropCriterion, Is.EqualTo(BestiaryFilterCriterion.All));
            Assert.That(state.EncounterFilter, Is.EqualTo(BestiaryEncounterFilter.All));
            Assert.That(state.HasStockOnly, Is.False);
            Assert.That(state.ActiveFilterCount, Is.Zero);
            Assert.That(state.Revision, Is.EqualTo(before + 1));
        }

        [Test]
        public void ActiveFilterCount_CombinesBestiaryDropsEncounterAndStockGroups()
        {
            var state = new BestiaryFilterState([10])
            {
                BestiaryCriterion = BestiaryFilterCriterion.ForNative(10),
                DropCriterion = BestiaryFilterCriterion.HasDrops,
                EncounterFilter = BestiaryEncounterFilter.Unknown,
                HasStockOnly = true
            };

            Assert.That(state.ActiveFilterCount, Is.EqualTo(4));
        }

        [Test]
        public void Clear_DefaultState_DoesNotAdvanceRevision()
        {
            var state = new BestiaryFilterState([10]);

            state.Clear();

            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void UnknownNativeFilterId_IsRejected()
        {
            var state = new BestiaryFilterState([10]);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.BestiaryCriterion = BestiaryFilterCriterion.ForNative(20));
        }

        [Test]
        public void BestiaryCriterion_LootAwareCriterionIsRejected()
        {
            var state = new BestiaryFilterState([10]);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.BestiaryCriterion = BestiaryFilterCriterion.HasDrops);
        }

        [Test]
        public void DropCriterion_NativeCriterionIsRejected()
        {
            var state = new BestiaryFilterState([10]);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.DropCriterion = BestiaryFilterCriterion.ForNative(10));
        }
    }
}