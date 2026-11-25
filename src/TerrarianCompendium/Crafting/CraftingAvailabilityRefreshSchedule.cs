using System;
using TerrarianCompendium.Navigation;

namespace TerrarianCompendium.Crafting
{
    internal sealed class CraftingAvailabilityRefreshSchedule(Action refresh)
    {
        private const uint RefreshIntervalTicks = 10;
        private readonly Action _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
        private BrowserDestination? _lastDestination;
        private ulong _lastScanTick;
        private bool _lastSectionFiltered;

        public void Reset() => _lastDestination = null;

        public void Update(ulong tick, BrowserDestination destination, bool itemsFiltered, bool recipesFiltered)
        {
            if (!destination.NeedsCraftingAvailability(itemsFiltered, recipesFiltered))
            {
                Reset();
                return;
            }

            bool sectionFiltered = destination.Section == BrowserSection.Items ? itemsFiltered : recipesFiltered;
            if (_lastDestination == destination &&
                _lastSectionFiltered == sectionFiltered &&
                unchecked(tick - _lastScanTick) < RefreshIntervalTicks)
            {
                return;
            }

            _refresh();
            _lastDestination = destination;
            _lastSectionFiltered = sectionFiltered;
            _lastScanTick = tick;
        }
    }
}