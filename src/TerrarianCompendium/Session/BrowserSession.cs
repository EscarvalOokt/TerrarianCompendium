using System;
using Terraria;
using Terraria.GameContent.Creative;
using TerrariaModder.Core.Logging;
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

        private readonly CharacterProgressPathResolver _characterProgressPathResolver;
        private readonly ChecklistProgressStore _checklistProgressStore;
        private readonly VanillaCraftingAvailabilityScanner _craftingAvailabilityScanner;
        private readonly EquippedItemDiscoveryScanner _equippedItemDiscoveryScanner;
        private readonly InventoryDiscoveryScanner _inventoryDiscoveryScanner;
        private readonly JourneyResearchDiscoveryScanner _journeyResearchDiscoveryScanner;
        private readonly ILogger _logger;
        private readonly VanillaStorageDiscoveryScanner _vanillaStorageDiscoveryScanner;
        private string _checklistProgressPath;
        private bool _disposed;
        private string _legacyChecklistProgressPath;
        private int _persistedFoundCount;
        private bool _persistencePreserveBackupOnNextSave;
        private bool _persistenceRewriteRequired;
        private bool _persistenceWriteEnabled;

        public BrowserSession(
            ItemCatalog catalog,
            RecipeCatalog recipeCatalog,
            ChecklistProgressStore checklistProgressStore,
            CharacterProgressPathResolver characterProgressPathResolver,
            ILogger logger,
            Player initialPlayer,
            bool worldIsJourney,
            ItemsSacrificedUnlocksTracker journeyResearchTracker)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            _checklistProgressStore =
                checklistProgressStore ?? throw new ArgumentNullException(nameof(checklistProgressStore));
            _characterProgressPathResolver = characterProgressPathResolver ??
                                             throw new ArgumentNullException(nameof(characterProgressPathResolver));
            _logger = logger;

            ChecklistState = new ChecklistState(catalog);

            RestoreChecklistProgress();

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

        public ChecklistState ChecklistState { get; }

        public CraftingAvailabilityState CraftingAvailabilityState { get; }

        public JourneyResearchState JourneyResearchState { get; }

        public void Dispose()
        {
            if (_disposed)
                return;

            SaveChecklistProgressIfNeeded();
            ResetPersistenceSession();

            _disposed = true;
        }

        public void Update(Player player, bool storageDiscoveryEnabled)
        {
            if (_disposed)
                return;

            if (player == null)
                throw new ArgumentNullException(nameof(player));

            _journeyResearchDiscoveryScanner?.Update();

            Item[] inventory = player.inventory;

            if (inventory == null)
                return;

            _craftingAvailabilityScanner?.Update(player);
            _inventoryDiscoveryScanner.Scan(inventory);
            _equippedItemDiscoveryScanner.Scan(player);

            if (storageDiscoveryEnabled)
                _vanillaStorageDiscoveryScanner.Scan(player);
        }

        private void RestoreChecklistProgress()
        {
            ResetPersistenceSession();

            if (!_characterProgressPathResolver.TryResolveCurrentProgressPaths(
                    out string progressPath,
                    out string legacyProgressPath))
            {
                _logger?.Warn(
                    $"[{TerrarianCompendiumMod.ModName}] " +
                    "Checklist persistence is unavailable because the active character save path " +
                    "could not be resolved.");

                return;
            }

            _checklistProgressPath = progressPath;
            _legacyChecklistProgressPath = legacyProgressPath;

            ChecklistProgressLoadResult loadResult = _checklistProgressStore.LoadWithLegacyFallback(
                progressPath,
                legacyProgressPath,
                out bool usedLegacyPath);

            _persistenceWriteEnabled = loadResult.Status != ChecklistProgressLoadStatus.UnsupportedVersion &&
                                       loadResult.Status != ChecklistProgressLoadStatus.Failed;

            foreach (int itemId in loadResult.FoundItemIds)
                ChecklistState.MarkFound(itemId);

            bool loadedFromLegacy = usedLegacyPath &&
                                    (loadResult.Status == ChecklistProgressLoadStatus.Loaded ||
                                     loadResult.Status == ChecklistProgressLoadStatus.RecoveredFromBackup);

            _persistedFoundCount = ChecklistState.FoundCount;
            _persistencePreserveBackupOnNextSave = loadResult.Status == ChecklistProgressLoadStatus.RecoveredFromBackup;
            _persistenceRewriteRequired = loadResult.RequiresRewrite || loadedFromLegacy;

            if (loadedFromLegacy)
                SaveChecklistProgressIfNeeded();
        }

        private void SaveChecklistProgressIfNeeded()
        {
            if (!_persistenceWriteEnabled || string.IsNullOrWhiteSpace(_checklistProgressPath))
                return;

            if (!_persistenceRewriteRequired && ChecklistState.FoundCount == _persistedFoundCount)
                return;

            if (!_checklistProgressStore.Save(
                    _checklistProgressPath,
                    ChecklistState.CreateFoundItemIdSnapshot(),
                    _persistencePreserveBackupOnNextSave))
            {
                return;
            }

            _persistedFoundCount = ChecklistState.FoundCount;
            _persistencePreserveBackupOnNextSave = false;
            _persistenceRewriteRequired = false;
        }

        private void ResetPersistenceSession()
        {
            _checklistProgressPath = null;
            _legacyChecklistProgressPath = null;
            _persistedFoundCount = 0;
            _persistencePreserveBackupOnNextSave = false;
            _persistenceRewriteRequired = false;
            _persistenceWriteEnabled = false;
        }
    }
}