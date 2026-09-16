using System;
using Terraria;
using TerrarianCompendium.Checklist;

namespace TerrarianCompendium.Discovery
{
    internal sealed class VanillaStorageDiscoveryScanner(ChecklistState checklistState)
    {
        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        public void Scan(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            Chest container = player.GetCurrentContainer();

            if (container?.item == null)
                return;

            foreach (Item item in container.item)
            {
                if (item == null)
                    continue;

                _checklistState.MarkFound(item.type);
            }
        }
    }
}