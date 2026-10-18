using Terraria;
using Terraria.UI;

namespace TerrarianCompendium.Journey
{
    internal static class VanillaJourneyItemDuplication
    {
        private const int JourneyDuplicationContext = 29;

        public static bool TryDuplicate(int itemId, JourneyResearchState researchState)
        {
            if (itemId <= 0 || researchState?.IsFullyResearched(itemId) != true)
                return false;

            var sourceItem = new Item();
            sourceItem.SetDefaults(itemId);
            Item[] items = [sourceItem];

            // Preserve vanilla handling without Core's ItemSlot.Handle panel click-through block.
            ItemSlot.OverrideHover(items, JourneyDuplicationContext);
            ItemSlot.LeftClick(items, JourneyDuplicationContext);
            ItemSlot.RightClick(items, JourneyDuplicationContext);
            ItemSlot.MouseHover(items, JourneyDuplicationContext);
            return true;
        }
    }
}