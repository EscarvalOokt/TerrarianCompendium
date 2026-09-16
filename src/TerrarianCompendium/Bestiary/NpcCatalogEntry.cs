using System;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcCatalogEntry
    {
        public NpcCatalogEntry(int netId, int bestiaryOrder)
        {
            if (bestiaryOrder < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bestiaryOrder),
                    bestiaryOrder,
                    "Bestiary order must not be negative.");
            }

            NetId = netId;
            BestiaryOrder = bestiaryOrder;
        }

        public int NetId { get; }

        public int BestiaryOrder { get; }
    }
}