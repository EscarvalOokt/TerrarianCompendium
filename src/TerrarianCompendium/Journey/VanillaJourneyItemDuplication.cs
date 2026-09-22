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
            ItemSlot.Handle([sourceItem], JourneyDuplicationContext);
            return true;
        }
    }
}