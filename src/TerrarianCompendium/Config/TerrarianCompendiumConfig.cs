using TerrariaModder.Core.Config;

namespace TerrarianCompendium.Config
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class TerrarianCompendiumConfig : ModConfig
    {
        public override int Version => 3;

        [Client]
        [Label("Storage Discovery")]
        [Description("Mark items as found while supported vanilla storage is open.")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool StorageDiscoveryEnabled { get; set; } = true;

        [Client]
        [Label("Inventory Item Navigation")]
        [Description("Open inventory items in Terrarian Compendium with Alt + Right Click.")]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool InventoryItemNavigationEnabled { get; set; } = true;
    }
}