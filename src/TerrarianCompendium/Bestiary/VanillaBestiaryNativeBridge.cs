using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;

namespace TerrarianCompendium.Bestiary
{
    internal static class VanillaBestiaryNativeBridge
    {
        private static readonly Dictionary<MetadataCacheKey, NpcBestiaryMetadataSnapshot> _metadataCache = new();

        private static FieldInfo _dropRateInfoField;
        private static bool _dropRateInfoFieldResolutionAttempted;
        private static FieldInfo _namePlateKeyField;
        private static bool _namePlateKeyFieldResolutionAttempted;

        public static void ClearMetadataCache()
        {
            _metadataCache.Clear();
        }

        public static bool TryGetNpcNetId(BestiaryEntry entry, out int netId)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.Info == null)
                throw new InvalidOperationException("Terraria Bestiary entry has no info collection.");

            var found = false;
            netId = 0;

            for (var index = 0; index < entry.Info.Count; index++)
            {
                if (entry.Info[index] is not NPCNetIdBestiaryInfoElement npcNetIdElement)
                    continue;

                if (found)
                {
                    throw new InvalidOperationException(
                        "Terraria Bestiary entry contains more than one NPC net ID element.");
                }

                netId = npcNetIdElement.NetId;
                found = true;
            }

            return found;
        }

        public static BestiaryEntry GetBestiaryEntry(NpcCatalogEntry catalogEntry)
        {
            if (catalogEntry == null)
                throw new ArgumentNullException(nameof(catalogEntry));

            BestiaryDatabase database = Main.BestiaryDB;

            if (database == null)
                throw new InvalidOperationException("Terraria Bestiary database is not initialized.");

            List<BestiaryEntry> entries = database.Entries;

            if (entries == null)
                throw new InvalidOperationException("Terraria Bestiary database has no finalized entry collection.");

            if (catalogEntry.BestiaryOrder < 0 || catalogEntry.BestiaryOrder >= entries.Count)
            {
                throw new InvalidOperationException(
                    $"NPC catalog entry {catalogEntry.NetId} references Bestiary order " +
                    $"{catalogEntry.BestiaryOrder}, but the finalized Bestiary contains {entries.Count} entries.");
            }

            BestiaryEntry entry = entries[catalogEntry.BestiaryOrder];

            if (entry == null)
            {
                throw new InvalidOperationException(
                    $"Terraria Bestiary entry at order {catalogEntry.BestiaryOrder} is not initialized.");
            }

            if (!TryGetNpcNetId(entry, out int netId))
            {
                throw new InvalidOperationException(
                    $"Terraria Bestiary entry at order {catalogEntry.BestiaryOrder} is no longer NPC-backed.");
            }

            if (netId != catalogEntry.NetId)
            {
                throw new InvalidOperationException(
                    $"NPC catalog entry {catalogEntry.NetId} no longer matches finalized Bestiary order " +
                    $"{catalogEntry.BestiaryOrder}, which now references NPC net ID {netId}.");
            }

            return entry;
        }

        public static BestiaryEntryUnlockState GetUnlockState(NpcCatalogEntry catalogEntry)
        {
            BestiaryEntry entry = GetBestiaryEntry(catalogEntry);

            if (entry.UIInfoProvider == null)
            {
                throw new InvalidOperationException(
                    $"Terraria Bestiary entry for NPC net ID {catalogEntry.NetId} has no UI info provider.");
            }

            return entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState;
        }

        public static BestiaryEntryObservation GetEntryObservation(NpcCatalogEntry catalogEntry)
        {
            BestiaryEntryUnlockState unlockState = GetUnlockState(catalogEntry);

            return new BestiaryEntryObservation(
                unlockState > BestiaryEntryUnlockState.NotKnownAtAll_0,
                (int)unlockState);
        }

        public static string GetDisplayName(NpcCatalogEntry catalogEntry)
        {
            BestiaryEntry entry = GetBestiaryEntry(catalogEntry);
            NamePlateInfoElement namePlate = FindSingleInfoElement<NamePlateInfoElement>(entry);

            if (namePlate == null)
            {
                throw new InvalidOperationException(
                    $"Terraria Bestiary entry for NPC net ID {catalogEntry.NetId} has no name-plate metadata.");
            }

            FieldInfo field = ResolveNamePlateKeyField();

            if (field == null)
            {
                throw new InvalidOperationException(
                    "Terraria NamePlateInfoElement._key field is unavailable or has an unexpected type.");
            }

            if (field.GetValue(namePlate) is not string key || string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    $"Terraria Bestiary name metadata for NPC net ID {catalogEntry.NetId} has no localization key.");
            }

            return Language.GetTextValue(key);
        }

        public static Func<NpcCatalogEntry, string, bool> CreateSearchMatcher()
        {
            var searchFilter = new Filters.BySearch();
            string activeSearch = null;

            return (catalogEntry, searchText) =>
            {
                if (catalogEntry == null)
                    throw new ArgumentNullException(nameof(catalogEntry));

                string normalizedSearch = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();

                if (normalizedSearch == null)
                    return true;

                string displayName = GetDisplayName(catalogEntry);

                if (displayName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (!string.Equals(activeSearch, normalizedSearch, StringComparison.Ordinal))
                {
                    activeSearch = normalizedSearch;
                    searchFilter.SetSearch(normalizedSearch);
                }

                return searchFilter.FitsFilter(GetBestiaryEntry(catalogEntry));
            };
        }

        public static void SortEntries(
            List<NpcCatalogEntry> entries,
            BestiarySortMode sortMode,
            BestiarySortDirection sortDirection)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            ValidateSortMode(sortMode);
            ValidateSortDirection(sortDirection);

            if (entries.Count < 2)
                return;

            if (sortMode == BestiarySortMode.BestiaryOrder)
            {
                entries.Sort((left, right) => ApplyDirection(
                    left.BestiaryOrder.CompareTo(right.BestiaryOrder),
                    sortDirection));
                return;
            }

            if (sortMode == BestiarySortMode.NpcId)
            {
                entries.Sort((left, right) => CompareWithBestiaryOrderFallback(
                    left,
                    right,
                    ApplyDirection(left.NetId.CompareTo(right.NetId), sortDirection)));
                return;
            }

            if (sortMode == BestiarySortMode.Name)
            {
                entries.Sort((left, right) => CompareWithBestiaryOrderFallback(
                    left,
                    right,
                    ApplyDirection(
                        string.Compare(GetDisplayName(left), GetDisplayName(right), StringComparison.CurrentCulture),
                        sortDirection)));
                return;
            }

            IComparer<BestiaryEntry> nativeSortStep = CreateNativeSortStep(sortMode);

            if (sortMode is BestiarySortMode.Attack
                or BestiarySortMode.Defense
                or BestiarySortMode.Coins
                or BestiarySortMode.HitPoints)
            {
                RefreshEntriesBeforeStatSorting(entries);
            }

            entries.Sort((left, right) =>
            {
                int nativeComparison = nativeSortStep.Compare(GetBestiaryEntry(left), GetBestiaryEntry(right));
                int normalizedComparison = sortDirection == BestiarySortDirection.Ascending
                    ? -nativeComparison
                    : nativeComparison;
                return CompareWithBestiaryOrderFallback(left, right, normalizedComparison);
            });
        }

        public static NpcBestiaryMetadataSnapshot GetMetadataSnapshot(
            NpcCatalogEntry catalogEntry,
            NpcDifficultyMode difficulty)
        {
            if (catalogEntry == null)
                throw new ArgumentNullException(nameof(catalogEntry));

            ValidateDifficulty(difficulty);

            BestiaryEntryObservation observation = GetEntryObservation(catalogEntry);
            string cultureName = Language.ActiveCulture?.Name ?? string.Empty;
            var cacheKey = new MetadataCacheKey(
                catalogEntry.NetId,
                difficulty,
                cultureName,
                Main.hardMode,
                NPC.downedPlantBoss,
                Main.getGoodWorld,
                observation.StateToken);

            if (_metadataCache.TryGetValue(cacheKey, out NpcBestiaryMetadataSnapshot cached))
                return cached;

            BestiaryEntry entry = GetBestiaryEntry(catalogEntry);
            NPC npc = CreateNpcSnapshot(catalogEntry.NetId, difficulty);
            NPCStatsReportInfoElement statsElement = FindSingleInfoElement<NPCStatsReportInfoElement>(entry);
            NpcBestiaryStatsSnapshot stats = null;

            if (statsElement != null)
            {
                statsElement.UpdateBeforeSorting();

                if (!statsElement.HideStats)
                {
                    int lifeMax = npc.lifeMax;

                    if (catalogEntry.NetId == 13)
                        lifeMax *= GetEaterOfWorldsSegmentsCount(difficulty);
                    else if (catalogEntry.NetId == 491)
                        lifeMax = 4 * CreateNpcSnapshot(492, difficulty).lifeMax;

                    stats = new NpcBestiaryStatsSnapshot(
                        npc.damage,
                        lifeMax,
                        npc.defense,
                        npc.knockBackResist,
                        npc.value);
                }
            }

            int rarityStars = ContentSamples.NpcBestiaryRarityStars[catalogEntry.NetId];
            RareSpawnBestiaryInfoElement rareSpawn = FindSingleInfoElement<RareSpawnBestiaryInfoElement>(entry);
            IReadOnlyList<NpcBestiarySpawnCondition> spawnConditions = ExtractSpawnConditions(entry);
            IReadOnlyList<NpcBestiaryDebuffImmunity> immunities = ExtractBaseDebuffImmunities(npc);

            var result = new NpcBestiaryMetadataSnapshot(
                observation.IsEncountered,
                GetDisplayName(catalogEntry),
                stats,
                rarityStars,
                rareSpawn?.RarityLevel,
                spawnConditions,
                immunities);

            _metadataCache[cacheKey] = result;
            return result;
        }

        public static NpcBestiaryKillStatisticsSnapshot GetKillStatisticsSnapshot(NpcCatalogEntry catalogEntry)
        {
            if (catalogEntry == null)
                throw new ArgumentNullException(nameof(catalogEntry));

            BestiaryEntry entry = GetBestiaryEntry(catalogEntry);
            NPC npc = CreateNpcSnapshot(catalogEntry.NetId, NpcDifficultyMode.Classic);
            bool hasKillCounter = FindSingleInfoElement<NPCKillCounterInfoElement>(entry) != null;
            var slainCount = 0;

            if (hasKillCounter)
            {
                if (Main.BestiaryTracker?.Kills == null)
                    throw new InvalidOperationException("Terraria Bestiary kill tracker is not initialized.");

                slainCount = Main.BestiaryTracker.Kills.GetKillCount(npc);
            }

            int bannerId = BannerSystem.NPCtoBanner(npc.BannerID());

            if (bannerId <= 0)
            {
                return new NpcBestiaryKillStatisticsSnapshot(
                    hasKillCounter,
                    slainCount,
                    bannerItemId: null,
                    bannerKillCount: 0,
                    killsPerBanner: 0);
            }

            int bannerItemId = BannerSystem.BannerToItem(bannerId);
            int killsPerBanner = ItemID.Sets.KillsToBanner[bannerItemId];
            int bannerKillCount = BannerSystem.GetKillCount(bannerId);

            return new NpcBestiaryKillStatisticsSnapshot(
                hasKillCounter,
                slainCount,
                bannerItemId,
                bannerKillCount,
                killsPerBanner);
        }

        public static DropRateInfo GetDropRateInfo(ItemDropBestiaryInfoElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            FieldInfo field = ResolveDropRateInfoField();

            if (field == null)
            {
                throw new InvalidOperationException(
                    "Terraria ItemDropBestiaryInfoElement._droprateInfo field is unavailable or has an unexpected type.");
            }

            object value = field.GetValue(element);

            if (value is not DropRateInfo dropRateInfo)
            {
                throw new InvalidOperationException(
                    "Terraria ItemDropBestiaryInfoElement._droprateInfo did not contain DropRateInfo.");
            }

            return dropRateInfo;
        }

        private static NPC CreateNpcSnapshot(int npcNetId, NpcDifficultyMode difficulty)
        {
            var npc = new NPC();
            npc.SetDefaults(
                npcNetId,
                new NPCSpawnParams
                {
                    difficultyOverride = GetNativeDifficulty(difficulty),
                    playerCountForMultiplayerDifficultyOverride = 1
                });
            return npc;
        }

        private static float GetNativeDifficulty(NpcDifficultyMode difficulty)
        {
            switch (difficulty)
            {
                case NpcDifficultyMode.Classic:
                    return GameDifficultyLevel.Classic;
                case NpcDifficultyMode.Expert:
                    return GameDifficultyLevel.Expert;
                case NpcDifficultyMode.Master:
                    return GameDifficultyLevel.Master;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(difficulty),
                        difficulty,
                        "Unsupported NPC difficulty mode.");
            }
        }

        private static int GetEaterOfWorldsSegmentsCount(NpcDifficultyMode difficulty)
        {
            switch (difficulty)
            {
                case NpcDifficultyMode.Classic:
                    return 65;
                case NpcDifficultyMode.Expert:
                case NpcDifficultyMode.Master:
                    return 70;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(difficulty),
                        difficulty,
                        "Unsupported NPC difficulty mode.");
            }
        }

        private static IReadOnlyList<NpcBestiarySpawnCondition> ExtractSpawnConditions(BestiaryEntry entry)
        {
            var result = new List<NpcBestiarySpawnCondition>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < entry.Info.Count; index++)
            {
                IBestiaryInfoElement element = entry.Info[index];

                if (element is not IFilterInfoProvider provider ||
                    !element.GetType().Name.StartsWith("SpawnCondition", StringComparison.Ordinal))
                {
                    continue;
                }

                string key = provider.GetDisplayNameKey();

                if (string.IsNullOrWhiteSpace(key) || !seenKeys.Add(key))
                    continue;

                string value = Language.GetTextValue(key);

                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(new NpcBestiarySpawnCondition(key, value));
            }

            return result.Count == 0 ? Array.Empty<NpcBestiarySpawnCondition>() : result.AsReadOnly();
        }

        private static IReadOnlyList<NpcBestiaryDebuffImmunity> ExtractBaseDebuffImmunities(NPC npc)
        {
            if (npc.buffImmune == null)
                return Array.Empty<NpcBestiaryDebuffImmunity>();

            int count = Math.Min(BuffID.Count, npc.buffImmune.Length);
            var result = new List<NpcBestiaryDebuffImmunity>();

            for (var buffId = 1; buffId < count; buffId++)
            {
                if (!npc.buffImmune[buffId] ||
                    Main.debuff == null ||
                    buffId >= Main.debuff.Length ||
                    !Main.debuff[buffId])
                    continue;

                string name = Lang.GetBuffName(buffId);
                result.Add(new NpcBestiaryDebuffImmunity(buffId, name ?? $"Buff ID {buffId}"));
            }

            return result.Count == 0 ? Array.Empty<NpcBestiaryDebuffImmunity>() : result.AsReadOnly();
        }

        private static void RefreshEntriesBeforeStatSorting(IReadOnlyList<NpcCatalogEntry> entries)
        {
            for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                BestiaryEntry entry = GetBestiaryEntry(entries[entryIndex]);

                for (var infoIndex = 0; infoIndex < entry.Info.Count; infoIndex++)
                {
                    if (entry.Info[infoIndex] is IUpdateBeforeSorting updatable)
                        updatable.UpdateBeforeSorting();
                }
            }
        }

        private static IComparer<BestiaryEntry> CreateNativeSortStep(BestiarySortMode sortMode)
        {
            switch (sortMode)
            {
                case BestiarySortMode.Rarity:
                    return new SortingSteps.ByBestiaryRarity();
                case BestiarySortMode.Attack:
                    return new SortingSteps.ByAttack();
                case BestiarySortMode.Defense:
                    return new SortingSteps.ByDefense();
                case BestiarySortMode.Coins:
                    return new SortingSteps.ByCoins();
                case BestiarySortMode.HitPoints:
                    return new SortingSteps.ByHP();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sortMode),
                        sortMode,
                        "Sort mode has no native sort step.");
            }
        }

        private static int ApplyDirection(int comparison, BestiarySortDirection sortDirection)
        {
            return sortDirection == BestiarySortDirection.Ascending ? comparison : -comparison;
        }

        private static int CompareWithBestiaryOrderFallback(
            NpcCatalogEntry left,
            NpcCatalogEntry right,
            int primaryComparison)
        {
            return primaryComparison != 0 ? primaryComparison : left.BestiaryOrder.CompareTo(right.BestiaryOrder);
        }

        private static T FindSingleInfoElement<T>(BestiaryEntry entry) where T : class, IBestiaryInfoElement
        {
            T found = null;

            for (var index = 0; index < entry.Info.Count; index++)
            {
                if (entry.Info[index] is not T candidate)
                    continue;

                if (found != null)
                {
                    throw new InvalidOperationException(
                        $"Terraria Bestiary entry contains more than one {typeof(T).Name}.");
                }

                found = candidate;
            }

            return found;
        }

        private static FieldInfo ResolveNamePlateKeyField()
        {
            if (_namePlateKeyFieldResolutionAttempted)
                return _namePlateKeyField;

            _namePlateKeyFieldResolutionAttempted = true;
            FieldInfo field = typeof(NamePlateInfoElement).GetField(
                "_key",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (field == null || field.FieldType != typeof(string))
                return null;

            _namePlateKeyField = field;
            return _namePlateKeyField;
        }

        private static FieldInfo ResolveDropRateInfoField()
        {
            if (_dropRateInfoFieldResolutionAttempted)
                return _dropRateInfoField;

            _dropRateInfoFieldResolutionAttempted = true;

            FieldInfo field = typeof(ItemDropBestiaryInfoElement).GetField(
                "_droprateInfo",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (field == null || field.FieldType != typeof(DropRateInfo))
                return null;

            _dropRateInfoField = field;

            return _dropRateInfoField;
        }

        private static void ValidateDifficulty(NpcDifficultyMode difficulty)
        {
            if (difficulty is NpcDifficultyMode.Classic or NpcDifficultyMode.Expert or NpcDifficultyMode.Master)
                return;

            throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported NPC difficulty mode.");
        }

        private static void ValidateSortDirection(BestiarySortDirection sortDirection)
        {
            if (sortDirection is BestiarySortDirection.Ascending or BestiarySortDirection.Descending)
                return;

            throw new ArgumentOutOfRangeException(
                nameof(sortDirection),
                sortDirection,
                "Unsupported Bestiary sort direction.");
        }

        private static void ValidateSortMode(BestiarySortMode sortMode)
        {
            if (sortMode is BestiarySortMode.BestiaryOrder
                or BestiarySortMode.Name
                or BestiarySortMode.Rarity
                or BestiarySortMode.Attack
                or BestiarySortMode.Defense
                or BestiarySortMode.Coins
                or BestiarySortMode.HitPoints
                or BestiarySortMode.NpcId)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, "Unsupported Bestiary sort mode.");
        }

        private readonly struct MetadataCacheKey(
            int npcNetId,
            NpcDifficultyMode difficulty,
            string cultureName,
            bool hardMode,
            bool downedPlantBoss,
            bool getGoodWorld,
            int observationToken) : IEquatable<MetadataCacheKey>
        {
            private int NpcNetId { get; } = npcNetId;
            private NpcDifficultyMode Difficulty { get; } = difficulty;
            private string CultureName { get; } = cultureName ?? string.Empty;
            private bool HardMode { get; } = hardMode;
            private bool DownedPlantBoss { get; } = downedPlantBoss;
            private bool GetGoodWorld { get; } = getGoodWorld;
            private int ObservationToken { get; } = observationToken;

            public bool Equals(MetadataCacheKey other)
            {
                return NpcNetId == other.NpcNetId &&
                       Difficulty == other.Difficulty &&
                       HardMode == other.HardMode &&
                       DownedPlantBoss == other.DownedPlantBoss &&
                       GetGoodWorld == other.GetGoodWorld &&
                       ObservationToken == other.ObservationToken &&
                       string.Equals(CultureName, other.CultureName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MetadataCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = NpcNetId;
                    hashCode = (hashCode * 397) ^ (int)Difficulty;
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(CultureName);
                    hashCode = (hashCode * 397) ^ HardMode.GetHashCode();
                    hashCode = (hashCode * 397) ^ DownedPlantBoss.GetHashCode();
                    hashCode = (hashCode * 397) ^ GetGoodWorld.GetHashCode();
                    hashCode = (hashCode * 397) ^ ObservationToken;
                    return hashCode;
                }
            }
        }
    }
}