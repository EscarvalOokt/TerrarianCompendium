using System;
using System.IO;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Persistence;

namespace TerrarianCompendium.Tests.Persistence
{
    [TestFixture]
    public sealed class ChecklistProgressStoreTests
    {
        [SetUp]
        public void SetUp()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                "TerrarianCompendium.Tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }

        private string _testDirectory;

        [Test]
        public void SaveAndLoad_WithValidProgress_RoundTripsData()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            bool saved = store.Save(progressPath, [1, 7, 20]);
            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(saved, Is.True);
            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([1, 7, 20]));
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void Save_NormalizesIdsToUniqueAscendingOrder()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            bool saved = store.Save(progressPath, [20, 1, 7, 1, 20]);
            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(saved, Is.True);
            Assert.That(loaded.FoundItemIds, Is.EqualTo([1, 7, 20]));
        }

        [Test]
        public void Load_WithMissingFile_ReturnsEmptyMissingResult()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Missing));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenCurrentExists_UsesCurrentProgress()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(currentPath, [1, 2]), Is.True);
            Assert.That(store.Save(legacyPath, [3, 4]), Is.True);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([1, 2]));
            Assert.That(usedLegacyPath, Is.False);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenCurrentIsMissing_UsesLegacyProgress()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(legacyPath, [3, 4]), Is.True);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([3, 4]));
            Assert.That(usedLegacyPath, Is.True);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenCurrentPrimaryIsMissingAndBackupIsValid_UsesCurrentBackup()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(currentPath, [1]), Is.True);
            Assert.That(store.Save(currentPath, [1, 2]), Is.True);
            Assert.That(store.Save(legacyPath, [3, 4]), Is.True);

            File.Delete(currentPath);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([1]));
            Assert.That(loaded.RequiresRewrite, Is.True);
            Assert.That(usedLegacyPath, Is.False);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenCurrentHasUnsupportedVersion_DoesNotUseLegacyProgress()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");

            File.WriteAllText(currentPath, "{\"version\":2,\"foundItemIds\":[1,2]}");

            var store = new ChecklistProgressStore(null);
            Assert.That(store.Save(legacyPath, [3, 4]), Is.True);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.UnsupportedVersion));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(usedLegacyPath, Is.False);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenCurrentIsCorrupt_DoesNotUseLegacyProgress()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");

            File.WriteAllText(currentPath, "{corrupt");

            var store = new ChecklistProgressStore(null);
            Assert.That(store.Save(legacyPath, [3, 4]), Is.True);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Corrupt));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(usedLegacyPath, Is.False);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenCurrentIsMissingAndLegacyBackupIsValid_UsesLegacyBackup()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(legacyPath, [3]), Is.True);
            Assert.That(store.Save(legacyPath, [3, 4]), Is.True);

            File.Delete(legacyPath);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([3]));
            Assert.That(loaded.RequiresRewrite, Is.True);
            Assert.That(usedLegacyPath, Is.True);
        }

        [Test]
        public void LoadWithLegacyFallback_WhenBothPathsAreMissing_ReturnsMissingWithoutLegacyUse()
        {
            string currentPath = Path.Combine(_testDirectory, "current.json");
            string legacyPath = Path.Combine(_testDirectory, "legacy.json");
            var store = new ChecklistProgressStore(null);

            ChecklistProgressLoadResult loaded = store.LoadWithLegacyFallback(
                currentPath,
                legacyPath,
                out bool usedLegacyPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Missing));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
            Assert.That(usedLegacyPath, Is.False);
        }

        [Test]
        public void Load_WithMalformedJson_ReturnsCorruptResult()
        {
            string progressPath = CreateProgressPath();
            File.WriteAllText(progressPath, "{not valid json");

            var store = new ChecklistProgressStore(null);

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Corrupt));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Load_WithUnsupportedOlderVersion_DoesNotInterpretDataAsCurrentSchema()
        {
            string progressPath = CreateProgressPath();
            File.WriteAllText(progressPath, "{\"version\":0,\"foundItemIds\":[1,2]}");

            var store = new ChecklistProgressStore(null);

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.UnsupportedVersion));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void Load_WithUnsupportedFutureVersion_DoesNotModifyFile()
        {
            string progressPath = CreateProgressPath();
            const string originalContent = "{\"version\":2,\"foundItemIds\":[1,2]}";

            File.WriteAllText(progressPath, originalContent);

            var store = new ChecklistProgressStore(null);

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.UnsupportedVersion));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
            Assert.That(File.ReadAllText(progressPath), Is.EqualTo(originalContent));
        }

        [Test]
        public void Load_WithMissingFoundItemIds_ReturnsEmptyLoadedProgress()
        {
            string progressPath = CreateProgressPath();
            File.WriteAllText(progressPath, "{\"version\":1}");

            var store = new ChecklistProgressStore(null);

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(loaded.FoundItemIds, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void Load_WithDuplicateIds_CanRestoreThroughChecklistStateIdempotently()
        {
            string progressPath = CreateProgressPath();
            File.WriteAllText(progressPath, "{\"version\":1,\"foundItemIds\":[1,1,2,2]}");

            var store = new ChecklistProgressStore(null);
            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            foreach (int itemId in loaded.FoundItemIds)
                state.MarkFound(itemId);

            Assert.That(state.FoundCount, Is.EqualTo(2));
            Assert.That(state.IsFound(1), Is.True);
            Assert.That(state.IsFound(2), Is.True);
        }

        [Test]
        public void Load_WithUnknownIds_RestoreIgnoresItemsOutsideCurrentCatalog()
        {
            string progressPath = CreateProgressPath();
            File.WriteAllText(progressPath, "{\"version\":1,\"foundItemIds\":[1,999]}");

            var store = new ChecklistProgressStore(null);
            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            foreach (int itemId in loaded.FoundItemIds)
                state.MarkFound(itemId);

            Assert.That(state.FoundCount, Is.EqualTo(1));
            Assert.That(state.IsFound(1), Is.True);
            Assert.That(state.IsFound(999), Is.False);
        }

        [Test]
        public void Save_WhenReplacingExistingProgress_CreatesBackupAndKeepsNewPrimary()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(progressPath, [1]), Is.True);
            Assert.That(store.Save(progressPath, [2]), Is.True);

            ChecklistProgressLoadResult primary = store.Load(progressPath);
            ChecklistProgressLoadResult backup = store.Load(progressPath + ".bak");

            Assert.That(primary.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(primary.FoundItemIds, Is.EqualTo([2]));
            Assert.That(backup.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(backup.FoundItemIds, Is.EqualTo([1]));
            Assert.That(File.Exists(progressPath + ".bak"), Is.True);
            Assert.That(File.Exists(progressPath + ".tmp"), Is.False);
        }

        [Test]
        public void Load_WithCorruptPrimaryAndValidBackup_RecoversBackup()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(progressPath, [1]), Is.True);
            Assert.That(store.Save(progressPath, [1, 2]), Is.True);

            File.WriteAllText(progressPath, "{corrupt");

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([1]));
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Load_WithCorruptPrimaryAndUnreadableBackup_ReturnsFailedWithoutRewrite()
        {
            string progressPath = CreateProgressPath();
            string backupPath = progressPath + ".bak";
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(progressPath, [1]), Is.True);
            Assert.That(store.Save(progressPath, [1, 2]), Is.True);

            ChecklistProgressLoadResult validatedBackup = store.Load(backupPath);

            Assert.That(validatedBackup.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(validatedBackup.FoundItemIds, Is.EqualTo([1]));

            File.WriteAllText(progressPath, "{corrupt");

            using (new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                ChecklistProgressLoadResult blocked = store.Load(progressPath);

                Assert.That(blocked.Status, Is.EqualTo(ChecklistProgressLoadStatus.Failed));
                Assert.That(blocked.FoundItemIds, Is.Empty);
                Assert.That(blocked.RequiresRewrite, Is.False);
            }

            ChecklistProgressLoadResult recovered = store.Load(progressPath);

            Assert.That(recovered.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.FoundItemIds, Is.EqualTo([1]));
            Assert.That(recovered.RequiresRewrite, Is.True);
        }

        [Test]
        public void Save_AfterCorruptPrimaryRecovery_PreservesValidatedBackup()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(progressPath, [1]), Is.True);
            Assert.That(store.Save(progressPath, [1, 2]), Is.True);

            File.WriteAllText(progressPath, "{corrupt");

            ChecklistProgressLoadResult recovered = store.Load(progressPath);

            Assert.That(recovered.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.FoundItemIds, Is.EqualTo([1]));
            Assert.That(store.Save(progressPath, recovered.FoundItemIds, preserveExistingBackup: true), Is.True);

            ChecklistProgressLoadResult primary = store.Load(progressPath);
            ChecklistProgressLoadResult backup = store.Load(progressPath + ".bak");

            Assert.That(primary.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(primary.FoundItemIds, Is.EqualTo([1]));
            Assert.That(backup.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(backup.FoundItemIds, Is.EqualTo([1]));
            Assert.That(File.Exists(progressPath + ".tmp"), Is.False);

            File.WriteAllText(progressPath, "{corrupt-again");

            ChecklistProgressLoadResult recoveredAgain = store.Load(progressPath);

            Assert.That(recoveredAgain.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(recoveredAgain.FoundItemIds, Is.EqualTo([1]));
        }

        [Test]
        public void Load_WithMissingPrimaryAndValidBackup_RecoversBackup()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(progressPath, [1]), Is.True);
            Assert.That(store.Save(progressPath, [1, 2]), Is.True);

            File.Delete(progressPath);

            ChecklistProgressLoadResult loaded = store.Load(progressPath);

            Assert.That(loaded.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(loaded.FoundItemIds, Is.EqualTo([1]));
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Save_AfterMissingPrimaryRecovery_PreservesExistingBackup()
        {
            string progressPath = CreateProgressPath();
            var store = new ChecklistProgressStore(null);

            Assert.That(store.Save(progressPath, [1]), Is.True);
            Assert.That(store.Save(progressPath, [1, 2]), Is.True);

            File.Delete(progressPath);

            ChecklistProgressLoadResult recovered = store.Load(progressPath);

            Assert.That(recovered.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.FoundItemIds, Is.EqualTo([1]));
            Assert.That(store.Save(progressPath, recovered.FoundItemIds, preserveExistingBackup: true), Is.True);

            ChecklistProgressLoadResult backup = store.Load(progressPath + ".bak");

            Assert.That(backup.Status, Is.EqualTo(ChecklistProgressLoadStatus.Loaded));
            Assert.That(backup.FoundItemIds, Is.EqualTo([1]));
            Assert.That(File.Exists(progressPath + ".tmp"), Is.False);

            File.WriteAllText(progressPath, "{corrupt");

            ChecklistProgressLoadResult recoveredAgain = store.Load(progressPath);

            Assert.That(recoveredAgain.Status, Is.EqualTo(ChecklistProgressLoadStatus.RecoveredFromBackup));
            Assert.That(recoveredAgain.FoundItemIds, Is.EqualTo([1]));
        }

        [Test]
        public void Save_WhenDestinationDirectoryCannotBeCreated_ReturnsFalse()
        {
            string blockingFilePath = Path.Combine(_testDirectory, "blocking-file");
            File.WriteAllText(blockingFilePath, "not a directory");

            string progressPath = Path.Combine(blockingFilePath, "progress.json");
            var store = new ChecklistProgressStore(null);

            bool saved = store.Save(progressPath, [1]);

            Assert.That(saved, Is.False);
        }

        private string CreateProgressPath()
        {
            return Path.Combine(_testDirectory, "progress.json");
        }

        private static ItemCatalog CreateCatalog(params int[] itemIds)
        {
            var entries = new ItemCatalogEntry[itemIds.Length];

            for (var index = 0; index < itemIds.Length; index++)
            {
                int itemId = itemIds[index];
                entries[index] = new ItemCatalogEntry(itemId, $"Item {itemId}");
            }

            return ItemCatalog.Create(entries);
        }
    }
}