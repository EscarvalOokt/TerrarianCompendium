using System;
using Terraria;
using Terraria.GameContent.Creative;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Angler;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Discovery;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Persistence;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Session
{
    internal sealed class BrowserSession : IDisposable
    {
        private const byte JourneyCharacterDifficulty = 3;

        private readonly VanillaAnglerQuestStateScanner _anglerQuestStateScanner;
        private readonly ChecklistProgressPersistence _checklistPersistence;
        private readonly VanillaCraftingAvailabilityScanner _craftingAvailabilityScanner;
        private readonly EquippedItemDiscoveryScanner _equippedItemDiscoveryScanner;
        private readonly InventoryDiscoveryScanner _inventoryDiscoveryScanner;
        private readonly JourneyResearchDiscoveryScanner _journeyResearchDiscoveryScanner;
        private readonly VanillaStorageDiscoveryScanner _vanillaStorageDiscoveryScanner;
        private bool _disposed;

        public BrowserSession(
            ItemCatalog catalog,
            AnglerRewardCatalog anglerRewardCatalog,
            RecipeCatalog recipeCatalog,
            ChecklistProgressPersistence checklistPersistence,
            CharacterProgressPathResolver characterProgressPathResolver,
            ILogger logger,
            Player initialPlayer,
            bool worldIsJourney,
            ItemsSacrificedUnlocksTracker journeyResearchTracker)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            _checklistPersistence =
                checklistPersistence ?? throw new ArgumentNullException(nameof(checklistPersistence));
            if (characterProgressPathResolver == null)
                throw new ArgumentNullException(nameof(characterProgressPathResolver));

            ChecklistState = new ChecklistState(catalog);

            if (anglerRewardCatalog != null)
            {
                var anglerQuestState = new AnglerQuestState();
                _anglerQuestStateScanner = new VanillaAnglerQuestStateScanner(anglerRewardCatalog, anglerQuestState);
                AnglerQuestModel = new AnglerQuestModel(catalog, anglerRewardCatalog, anglerQuestState);

                if (initialPlayer != null)
                    _anglerQuestStateScanner.Update(initialPlayer);
            }

            if (characterProgressPathResolver.TryResolveCurrentProgressPaths(out string path, out string legacyPath))
                _checklistPersistence.Open(path, legacyPath, ChecklistState, DateTime.UtcNow);
            else
                logger?.Warn(
                    $"[{TerrarianCompendiumMod.ModName}] The active character save path could not be resolved.");

            _inventoryDiscoveryScanner = new InventoryDiscoveryScanner(ChecklistState);
            _equippedItemDiscoveryScanner = new EquippedItemDiscoveryScanner(ChecklistState);
            _vanillaStorageDiscoveryScanner = new VanillaStorageDiscoveryScanner(ChecklistState);

            if (recipeCatalog != null)
            {
                CraftingAvailabilityState = new CraftingAvailabilityState(recipeCatalog);
                _craftingAvailabilityScanner = new VanillaCraftingAvailabilityScanner(
                    recipeCatalog,
                    CraftingAvailabilityState,
                    logger);
            }

            if (initialPlayer != null &&
                JourneyResearchAvailability.IsAvailable(
                    worldIsJourney,
                    initialPlayer.difficulty == JourneyCharacterDifficulty))
            {
                _journeyResearchDiscoveryScanner = new JourneyResearchDiscoveryScanner(
                    catalog,
                    ChecklistState,
                    journeyResearchTracker);

                JourneyResearchState = _journeyResearchDiscoveryScanner.ResearchState;
            }
        }

        public AnglerQuestModel AnglerQuestModel { get; }

        public ChecklistState ChecklistState { get; }

        public CraftingAvailabilityState CraftingAvailabilityState { get; }

        public JourneyResearchState JourneyResearchState { get; }

        public void Dispose()
        {
            if (_disposed)
                return;

            _checklistPersistence.Close();

            _disposed = true;
        }

        public void Update(Player player, bool storageDiscoveryEnabled)
        {
            if (_disposed)
                return;

            if (player == null)
                throw new ArgumentNullException(nameof(player));

            _anglerQuestStateScanner?.Update(player);
            _journeyResearchDiscoveryScanner?.Update();

            Item[] inventory = player.inventory;

            if (inventory == null)
                return;

            _inventoryDiscoveryScanner.Scan(inventory);
            _equippedItemDiscoveryScanner.Scan(player);

            if (storageDiscoveryEnabled)
                _vanillaStorageDiscoveryScanner.Scan(player);
        }

        public void RefreshCraftingAvailability(Player player)
        {
            if (!_disposed && player?.inventory != null)
                _craftingAvailabilityScanner?.Update(player);
        }
    }
}