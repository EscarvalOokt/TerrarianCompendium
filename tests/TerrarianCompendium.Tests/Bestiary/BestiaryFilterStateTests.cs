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

            Assert.That(state.EncounterFilter, Is.EqualTo(BestiaryEncounterFilter.All));
            Assert.That(state.HasStockOnly, Is.False);
            Assert.That(state.ActiveNativeFilterIds, Is.Empty);
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
        public void ToggleNativeFilter_AddsAndRemovesFilter()
        {
            var state = new BestiaryFilterState([10]);

            state.ToggleNativeFilter(10);
            Assert.That(state.IsNativeFilterActive(10), Is.True);
            Assert.That(state.ActiveNativeFilterCount, Is.EqualTo(1));

            state.ToggleNativeFilter(10);
            Assert.That(state.IsNativeFilterActive(10), Is.False);
            Assert.That(state.ActiveNativeFilterCount, Is.Zero);
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
        public void Clear_ResetsEncounterNativeAndStockFilters()
        {
            var state = new BestiaryFilterState([10, 20])
            {
                EncounterFilter = BestiaryEncounterFilter.Encountered,
                HasStockOnly = true
            };
            state.ToggleNativeFilter(10);
            state.ToggleNativeFilter(20);
            long before = state.Revision;

            state.Clear();

            Assert.That(state.EncounterFilter, Is.EqualTo(BestiaryEncounterFilter.All));
            Assert.That(state.HasStockOnly, Is.False);
            Assert.That(state.ActiveNativeFilterIds, Is.Empty);
            Assert.That(state.ActiveFilterCount, Is.Zero);
            Assert.That(state.Revision, Is.EqualTo(before + 1));
        }

        [Test]
        public void ActiveFilterCount_CombinesEncounterNativeAndStockFilters()
        {
            var state = new BestiaryFilterState([10])
            {
                EncounterFilter = BestiaryEncounterFilter.Unknown,
                HasStockOnly = true
            };
            state.ToggleNativeFilter(10);

            Assert.That(state.ActiveFilterCount, Is.EqualTo(3));
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

            Assert.Throws<ArgumentOutOfRangeException>(() => state.ToggleNativeFilter(20));
        }
    }
}