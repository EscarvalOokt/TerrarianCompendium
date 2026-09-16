using System;
using Terraria;
using TerrarianCompendium.Checklist;

namespace TerrarianCompendium.Discovery
{
    internal sealed class EquippedItemDiscoveryScanner(ChecklistState checklistState)
    {
        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        public void Scan(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            ScanItems(player.armor);
            ScanItems(player.dye);
            ScanItems(player.miscEquips);
            ScanItems(player.miscDyes);

            EquipmentLoadout[] loadouts = player.Loadouts;

            if (loadouts == null)
                return;

            foreach (EquipmentLoadout loadout in loadouts)
            {
                if (loadout == null)
                    continue;

                ScanItems(loadout.Armor);
                ScanItems(loadout.Dye);
            }
        }

        private void ScanItems(Item[] items)
        {
            if (items == null)
                return;

            foreach (Item item in items)
            {
                if (item == null)
                    continue;

                _checklistState.MarkFound(item.type);
            }
        }
    }
}