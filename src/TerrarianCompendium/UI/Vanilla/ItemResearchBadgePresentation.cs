using System;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class ItemResearchBadgePresentation(JourneyResearchState researchState)
    {
        private bool _isVisible;
        private int _itemId;
        private long _observedRevision = long.MinValue;

        public bool IsVisible => _isVisible;

        public void Bind(int itemId)
        {
            int normalizedItemId = Math.Max(0, itemId);

            if (_itemId == normalizedItemId)
                return;

            _itemId = normalizedItemId;
            _observedRevision = long.MinValue;
        }

        public bool Synchronize()
        {
            long revision = researchState?.Revision ?? 0;

            if (_observedRevision == revision)
                return false;

            _observedRevision = revision;
            bool isVisible = _itemId > 0 && researchState?.IsFullyResearched(_itemId) == true;

            if (_isVisible == isVisible)
                return false;

            _isVisible = isVisible;
            return true;
        }
    }
}