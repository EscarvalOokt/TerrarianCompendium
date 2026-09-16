using System;
using Terraria;
using TerrarianCompendium.Checklist;

namespace TerrarianCompendium.Discovery
{
    internal sealed class InventoryDiscoveryScanner(ChecklistState checklistState)
    {
        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        public void Scan(Item[] inventory)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            foreach (Item item in inventory)
            {
                if (item == null)
                    continue;

                _checklistState.MarkFound(item.type);
            }
        }
    }
}