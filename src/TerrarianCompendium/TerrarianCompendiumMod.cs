using System;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.Localization;
using TerrariaModder.Core;
using TerrariaModder.Core.Events;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.ArmorSets;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Config;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.Persistence;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.Session;
using TerrarianCompendium.UI;

namespace TerrarianCompendium
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class TerrarianCompendiumMod : IMod, IModLifecycle
    {
        public const string ModId = "terrarian-compendium";
        public const string ModName = "Terrarian Compendium";
        public const string ModVersion = "1.0.0";

        private ArmorSetCatalog _armorSetCatalog;
        private ArmorSetIndex _armorSetIndex;
        private BrowserSession _browserSession;
        private BrowserShell _browserShell;
        private CharacterProgressPathResolver _characterProgressPathResolver;
        private ChecklistProgressStore _checklistProgressStore;
        private TerrarianCompendiumConfig _config;
        private string _failedItemTextCultureName;
        private FishingSourceIndex _fishingSourceIndex;
        private bool _isDedicatedServer;
        private ItemCatalog _itemCatalog;
        private ItemTextIndex _itemTextIndex;
        private long _lastRecipeFavoritesSaveAttemptRevision = -1;
        private CompendiumLocalization _localization;
        private ILogger _logger;
        private MerchantSourceIndex _merchantSourceIndex;
        private NpcCatalog _npcCatalog;
        private NpcLootIndex _npcLootIndex;
        private OpenableItemLootIndex _openableItemLootIndex;
        private long _persistedRecipeFavoritesRevision;
        private RecipeCatalog _recipeCatalog;
        private string _recipeFavoritesPath;
        private RecipeFavoritesPathResolver _recipeFavoritesPathResolver;
        private bool _recipeFavoritesPersistenceRewriteRequired;
        private bool _recipeFavoritesPersistenceWriteEnabled;
        private bool _recipeFavoritesPreserveBackupOnNextSave;
        private RecipeFavoritesStore _recipeFavoritesStore;
        private RecipeFavoriteState _recipeFavoriteState;
        private RecipeIndex _recipeIndex;
        private RecipePersistentKeyIndex _recipePersistentKeyIndex;
        private WorldLootSourceIndex _worldLootSourceIndex;

        public string Id => ModId;
        public string Name => ModName;
        public string Version => ModVersion;

        public void Initialize(ModContext context)
        {
            _logger = context.Logger;
            _isDedicatedServer = Environment.GetEnvironmentVariable("TERRARIA_MODDER_DEDSERV") == "1";

            if (!_isDedicatedServer)
            {
                _config = context.GetConfig<TerrarianCompendiumConfig>();
                _characterProgressPathResolver = new CharacterProgressPathResolver();
                _checklistProgressStore = new ChecklistProgressStore(context.Logger);
                _recipeFavoritesPathResolver = new RecipeFavoritesPathResolver();
                _recipeFavoritesStore = new RecipeFavoritesStore(context.Logger);

                FrameEvents.OnPreUpdate += OnPreUpdate;
                FrameEvents.OnPostUpdate += OnPostUpdate;
                Main.OnTickForThirdPartySoftwareOnly += OnThirdPartySoftwareTick;

                context.RegisterKeybind(
                    "toggle",
                    "Toggle Terrarian Compendium",
                    "Open or close Terrarian Compendium",
                    "Alt+I",
                    OnToggleBrowser);
            }

            context.Logger?.Info($"[{ModName}] Initialized.");
        }

        public void Unload()
        {
            SaveRecipeFavoritesIfNeeded(forceRetry: true);
            DestroyBrowserShell();
            DestroyBrowserSession();

            Main.OnTickForThirdPartySoftwareOnly -= OnThirdPartySoftwareTick;
            FrameEvents.OnPreUpdate -= OnPreUpdate;
            FrameEvents.OnPostUpdate -= OnPostUpdate;

            _armorSetCatalog = null;
            _armorSetIndex = null;
            _fishingSourceIndex = null;
            _merchantSourceIndex = null;
            _itemCatalog = null;
            _itemTextIndex = null;
            _localization = null;
            _npcCatalog = null;
            _npcLootIndex = null;
            _openableItemLootIndex = null;
            _recipeCatalog = null;
            _recipeIndex = null;
            _recipePersistentKeyIndex = null;
            _worldLootSourceIndex = null;
            _recipeFavoriteState = null;
            _recipeFavoritesPath = null;
            _recipeFavoritesPreserveBackupOnNextSave = false;
            _recipeFavoritesPersistenceRewriteRequired = false;
            _recipeFavoritesPersistenceWriteEnabled = false;
            _lastRecipeFavoritesSaveAttemptRevision = -1;
            _persistedRecipeFavoritesRevision = 0;
            _recipeFavoritesStore = null;
            _recipeFavoritesPathResolver = null;
            _failedItemTextCultureName = null;
            _checklistProgressStore = null;
            _characterProgressPathResolver = null;
            _config = null;
            _logger = null;
        }

        public void OnContentReady(ModContext context)
        {
        }

        public void OnWorldLoad()
        {
            VanillaBestiaryNativeBridge.ClearMetadataCache();

            if (_fishingSourceIndex == null)
            {
                try
                {
                    _fishingSourceIndex = new VanillaFishingSourceIndexBuilder().Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla fishing source index.", exception);
                }
            }

            if (_merchantSourceIndex == null)
            {
                try
                {
                    _merchantSourceIndex = new VanillaMerchantSourceIndexBuilder().Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla merchant source index.", exception);
                }
            }

            if (_itemCatalog == null)
            {
                try
                {
                    Func<int, bool> fishingItemPredicate = _fishingSourceIndex == null
                        ? null
                        : _fishingSourceIndex.ContainsItem;
                    var catalogBuilder = new VanillaItemCatalogBuilder(_logger, fishingItemPredicate);
                    _itemCatalog = catalogBuilder.Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla item catalog.", exception);
                }
            }

            if (_worldLootSourceIndex == null)
            {
                try
                {
                    _worldLootSourceIndex = new VanillaWorldLootSourceIndexBuilder().Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla world loot source index.", exception);
                }
            }

            if (_openableItemLootIndex == null)
            {
                try
                {
                    _openableItemLootIndex = new VanillaOpenableItemLootIndexBuilder().Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla openable-item loot index.", exception);
                }
            }

            if (_armorSetCatalog == null && _itemCatalog != null)
            {
                try
                {
                    var catalogBuilder = new VanillaArmorSetCatalogBuilder();
                    _armorSetCatalog = catalogBuilder.Build(_itemCatalog);
                    _armorSetIndex = ArmorSetIndex.Create(_armorSetCatalog);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla armor-set catalog.", exception);
                    _armorSetCatalog = null;
                    _armorSetIndex = null;
                }
            }

            if (_npcCatalog == null)
            {
                try
                {
                    var catalogBuilder = new VanillaNpcCatalogBuilder(_logger);
                    _npcCatalog = catalogBuilder.Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla NPC catalog.", exception);
                }
            }

            if (_npcLootIndex == null && _npcCatalog != null)
            {
                try
                {
                    var indexBuilder = new VanillaNpcLootIndexBuilder(_logger);
                    _npcLootIndex = indexBuilder.Build(_npcCatalog);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla NPC loot index.", exception);
                }
            }

            if (_recipeCatalog == null)
            {
                try
                {
                    var catalogBuilder = new VanillaRecipeCatalogBuilder(_logger);
                    _recipeCatalog = catalogBuilder.Build();
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build vanilla recipe catalog.", exception);
                }
            }

            if (_recipePersistentKeyIndex == null && _recipeCatalog != null)
            {
                try
                {
                    _recipePersistentKeyIndex = RecipePersistentKeyIndex.Create(_recipeCatalog);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build persistent recipe key index.", exception);
                }
            }

            EnsureRecipeFavoritesState();

            if (_recipeIndex == null && _recipeCatalog != null)
            {
                try
                {
                    _recipeIndex = RecipeIndex.Create(_recipeCatalog);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to build recipe index.", exception);
                }
            }

            if (_isDedicatedServer || _itemCatalog == null)
                return;

            _failedItemTextCultureName = null;
            EnsureLocalizationCurrent();
            EnsureItemTextIndexCurrent();

            Player player = null;
            ItemsSacrificedUnlocksTracker journeyResearchTracker = null;

            if (TryGetLocalPlayer(out Player localPlayer))
            {
                player = localPlayer;
                journeyResearchTracker = Main.LocalPlayerCreativeTracker.ItemSacrifices;
            }

            _browserSession = new BrowserSession(
                _itemCatalog,
                _recipeCatalog,
                _checklistProgressStore,
                _characterProgressPathResolver,
                _logger,
                player,
                Main.IsJourneyMode,
                journeyResearchTracker);

            var navigationState = new BrowserNavigationState();

            var itemBrowserView = new ItemBrowserView(
                _itemCatalog,
                _browserSession.ChecklistState,
                _itemTextIndex,
                navigationState,
                _localization,
                _browserSession.JourneyResearchState,
                _recipeIndex,
                _browserSession.CraftingAvailabilityState,
                _merchantSourceIndex,
                _npcLootIndex);

            ArmorSetBrowserView armorSetBrowserView = null;
            ArmorSetDetailsView armorSetDetailsView = null;

            if (_armorSetCatalog != null && _armorSetIndex != null)
            {
                try
                {
                    armorSetBrowserView = new ArmorSetBrowserView(_armorSetCatalog, navigationState, _localization);
                    var armorSetDetailsModel = new ArmorSetDetailsModel(
                        _armorSetCatalog,
                        _itemCatalog,
                        _itemTextIndex,
                        _browserSession.ChecklistState,
                        Language.GetTextValue);
                    armorSetDetailsView = new ArmorSetDetailsView(armorSetDetailsModel, navigationState, _localization);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to create Armor Sets browser.", exception);
                    armorSetBrowserView = null;
                    armorSetDetailsView = null;
                }
            }

            BestiaryBrowserView bestiaryBrowserView = null;
            VanillaBestiaryFilterCatalog bestiaryFilterCatalog = null;

            if (_npcCatalog != null)
            {
                try
                {
                    bestiaryFilterCatalog = new VanillaBestiaryFilterCatalog(_npcCatalog);
                    var bestiaryFilterState = new BestiaryFilterState(bestiaryFilterCatalog.OptionIds);
                    var bestiaryBrowserModel = new BestiaryBrowserModel(
                        _npcCatalog,
                        bestiaryFilterState,
                        bestiaryFilterCatalog,
                        _merchantSourceIndex);
                    bestiaryBrowserView = new BestiaryBrowserView(
                        bestiaryBrowserModel,
                        bestiaryFilterState,
                        bestiaryFilterCatalog,
                        navigationState,
                        _localization);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to create Bestiary browser.", exception);
                }
            }

            RecipeBrowserView recipeBrowserView = null;
            RecipeDetailsView recipeDetailsView = null;
            RecipeStationDisplayIndex stationDisplayIndex = null;
            RecipeFilterState recipeFilterState = null;
            VanillaDirectCraftingService directCraftingService = null;

            if (_recipeCatalog != null && _recipeIndex != null && _browserSession.CraftingAvailabilityState != null)
            {
                directCraftingService = new VanillaDirectCraftingService(
                    _recipeCatalog,
                    _recipeIndex,
                    _browserSession.CraftingAvailabilityState,
                    _logger);
            }

            if (_recipeCatalog != null &&
                _recipeIndex != null &&
                _recipeFavoriteState != null &&
                _browserSession.CraftingAvailabilityState != null)
            {
                stationDisplayIndex = RecipeStationDisplayIndex.Create(_itemCatalog, _recipeCatalog);
                recipeFilterState = new RecipeFilterState();

                recipeBrowserView = new RecipeBrowserView(
                    _itemCatalog,
                    _itemTextIndex,
                    _recipeCatalog,
                    _recipeIndex,
                    stationDisplayIndex,
                    _browserSession.ChecklistState,
                    _browserSession.JourneyResearchState,
                    _recipeFavoriteState,
                    _browserSession.CraftingAvailabilityState,
                    recipeFilterState,
                    navigationState,
                    _localization);

                var recipeDetailsModel = new RecipeDetailsModel(
                    _recipeCatalog,
                    _itemCatalog,
                    _itemTextIndex,
                    _recipeIndex,
                    stationDisplayIndex,
                    _browserSession.ChecklistState,
                    _browserSession.JourneyResearchState,
                    _recipeFavoriteState,
                    _browserSession.CraftingAvailabilityState,
                    recipeFilterState,
                    _localization);

                recipeDetailsView = new RecipeDetailsView(
                    recipeDetailsModel,
                    _recipeFavoriteState,
                    navigationState,
                    _itemTextIndex,
                    stationDisplayIndex,
                    directCraftingService,
                    _localization);
            }

            var itemDetailsModel = new ItemDetailsModel(
                _itemCatalog,
                _browserSession.ChecklistState,
                _itemTextIndex,
                _browserSession.JourneyResearchState,
                _recipeIndex,
                _browserSession.CraftingAvailabilityState,
                stationDisplayIndex,
                _npcCatalog,
                _npcLootIndex,
                _npcCatalog != null && (_npcLootIndex != null || _merchantSourceIndex != null)
                    ? VanillaBestiaryNativeBridge.GetDisplayName
                    : null,
                _armorSetCatalog,
                _armorSetIndex,
                _worldLootSourceIndex,
                _openableItemLootIndex,
                _fishingSourceIndex,
                _merchantSourceIndex);

            var itemDetailsView = new ItemDetailsView(
                itemDetailsModel,
                navigationState,
                _itemTextIndex,
                stationDisplayIndex,
                directCraftingService,
                recipeFilterState,
                bestiaryFilterCatalog,
                _localization);
            NpcDetailsView npcDetailsView = null;

            if (_npcCatalog != null && _npcLootIndex != null && bestiaryFilterCatalog != null)
            {
                try
                {
                    var npcDetailsModel = new NpcDetailsModel(
                        _npcCatalog,
                        _npcLootIndex,
                        _itemCatalog,
                        _itemTextIndex,
                        _browserSession.ChecklistState,
                        _localization,
                        _merchantSourceIndex,
                        VanillaBestiaryNativeBridge.GetDisplayName);
                    npcDetailsView = new NpcDetailsView(
                        npcDetailsModel,
                        navigationState,
                        bestiaryFilterCatalog,
                        _localization);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to create NPC details.", exception);
                }
            }

            _browserShell = new BrowserShell(
                itemBrowserView,
                armorSetBrowserView,
                recipeBrowserView,
                bestiaryBrowserView,
                itemDetailsView,
                armorSetDetailsView,
                recipeDetailsView,
                npcDetailsView,
                navigationState,
                _localization);
            _browserShell.Register();
        }

        public void OnWorldUnload()
        {
            SaveRecipeFavoritesIfNeeded(forceRetry: true);
            DestroyBrowserShell();
            DestroyBrowserSession();
        }

        public void OnConfigChanged()
        {
            if (_isDedicatedServer)
                return;

            _logger?.Info($"[{ModName}] Config reloaded. " + $"StorageDiscoveryEnabled={IsStorageDiscoveryEnabled()}.");
        }

        private void OnPreUpdate()
        {
            _browserShell?.BeginUpdateFrame();
        }

        private void OnThirdPartySoftwareTick()
        {
            _browserShell?.HandleThirdPartySoftwareTick();
        }

        private void OnPostUpdate()
        {
            if (_browserSession != null)
            {
                EnsureLocalizationCurrent();
                EnsureItemTextIndexCurrent();
            }

            if (Main.ingameOptionsWindow)
                _browserShell?.Close();
            else
                _browserShell?.Update();

            SaveRecipeFavoritesIfNeeded();

            if (_browserSession == null)
                return;

            if (!TryGetLocalPlayer(out Player player))
                return;

            _browserSession.Update(player, IsStorageDiscoveryEnabled());
        }

        private void OnToggleBrowser()
        {
            _browserShell?.Toggle();
        }

        private void EnsureRecipeFavoritesState()
        {
            if (_isDedicatedServer || _recipePersistentKeyIndex == null)
                return;

            _recipeFavoriteState ??= new RecipeFavoriteState(_recipePersistentKeyIndex);

            if (!string.IsNullOrWhiteSpace(_recipeFavoritesPath) ||
                _recipeFavoritesPathResolver == null ||
                _recipeFavoritesStore == null)
            {
                return;
            }

            if (!_recipeFavoritesPathResolver.TryResolveCurrentPath(out string favoritesPath))
            {
                _logger?.Warn(
                    $"[{ModName}] Recipe favorites persistence is unavailable because the Terraria save path " +
                    "could not be resolved.");

                return;
            }

            _recipeFavoritesPath = favoritesPath;
            RecipeFavoritesLoadResult loadResult = _recipeFavoritesStore.Load(favoritesPath);
            _recipeFavoritesPersistenceWriteEnabled =
                loadResult.Status != RecipeFavoritesLoadStatus.UnsupportedVersion &&
                loadResult.Status != RecipeFavoritesLoadStatus.Failed;

            _recipeFavoriteState.ReplacePersistentKeySnapshot(loadResult.FavoriteRecipeKeys);
            _persistedRecipeFavoritesRevision = _recipeFavoriteState.Revision;
            _lastRecipeFavoritesSaveAttemptRevision = -1;
            _recipeFavoritesPreserveBackupOnNextSave =
                loadResult.Status == RecipeFavoritesLoadStatus.RecoveredFromBackup;
            _recipeFavoritesPersistenceRewriteRequired = loadResult.RequiresRewrite;

            if (_recipeFavoritesPersistenceRewriteRequired)
                SaveRecipeFavoritesIfNeeded();
        }

        private void SaveRecipeFavoritesIfNeeded(bool forceRetry = false)
        {
            if (_recipeFavoriteState == null ||
                _recipeFavoritesStore == null ||
                !_recipeFavoritesPersistenceWriteEnabled ||
                string.IsNullOrWhiteSpace(_recipeFavoritesPath))
            {
                return;
            }

            long currentRevision = _recipeFavoriteState.Revision;

            if (!_recipeFavoritesPersistenceRewriteRequired && currentRevision == _persistedRecipeFavoritesRevision)
            {
                return;
            }

            if (!forceRetry && currentRevision == _lastRecipeFavoritesSaveAttemptRevision)
                return;

            _lastRecipeFavoritesSaveAttemptRevision = currentRevision;

            if (!_recipeFavoritesStore.Save(
                    _recipeFavoritesPath,
                    _recipeFavoriteState.CreatePersistentKeySnapshot(),
                    _recipeFavoritesPreserveBackupOnNextSave))
            {
                return;
            }

            _persistedRecipeFavoritesRevision = currentRevision;
            _recipeFavoritesPreserveBackupOnNextSave = false;
            _recipeFavoritesPersistenceRewriteRequired = false;
        }

        private void EnsureLocalizationCurrent()
        {
            string cultureName = Language.ActiveCulture?.Name;

            if (_localization == null)
            {
                try
                {
                    _localization = CompendiumLocalization.LoadFromAssembly(
                        typeof(TerrarianCompendiumMod).Assembly,
                        cultureName);
                }
                catch (Exception exception)
                {
                    _logger?.Error($"[{ModName}] Failed to load project localization resources.", exception);
                    throw;
                }

                return;
            }

            _localization.SynchronizeCulture(cultureName);
        }

        private void EnsureItemTextIndexCurrent()
        {
            if (_itemCatalog == null)
                return;

            _itemTextIndex ??= new ItemTextIndex(_itemCatalog);

            string cultureName = Language.ActiveCulture?.Name;

            if (string.IsNullOrWhiteSpace(cultureName))
                return;

            if (_itemTextIndex.Revision > 0 &&
                string.Equals(_itemTextIndex.CultureName, cultureName, StringComparison.Ordinal))
            {
                return;
            }

            if (string.Equals(_failedItemTextCultureName, cultureName, StringComparison.Ordinal))
                return;

            try
            {
                var builder = new VanillaItemTextIndexBuilder(_logger);
                builder.Rebuild(_itemCatalog, _itemTextIndex, cultureName);
                _failedItemTextCultureName = null;
            }
            catch (Exception exception)
            {
                _failedItemTextCultureName = cultureName;
                _logger?.Error(
                    $"[{ModName}] Failed to build vanilla item text for culture '{cultureName}'.",
                    exception);
            }
        }

        private static bool TryGetLocalPlayer(out Player player)
        {
            player = null;

            Player[] players = Main.player;

            if (players == null)
                return false;

            int playerIndex = Main.myPlayer;

            if (playerIndex < 0 || playerIndex >= players.Length)
                return false;

            player = players[playerIndex];

            return player != null;
        }

        private bool IsStorageDiscoveryEnabled()
        {
            return _config?.StorageDiscoveryEnabled != false;
        }

        private void DestroyBrowserSession()
        {
            if (_browserSession == null)
                return;

            _browserSession.Dispose();
            _browserSession = null;
        }

        private void DestroyBrowserShell()
        {
            if (_browserShell == null)
                return;

            _browserShell.Close();
            _browserShell.Unregister();
            _browserShell = null;
        }
    }
}