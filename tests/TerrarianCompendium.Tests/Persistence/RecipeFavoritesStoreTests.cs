using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TerrarianCompendium.Persistence;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Persistence
{
    [TestFixture]
    public sealed class RecipeFavoritesStoreTests
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
        public void SaveAndLoad_WithValidFavorites_RoundTripsPersistentKeys()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            bool saved = store.Save(path, [first, second]);
            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(saved, Is.True);
            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(loaded.FavoriteRecipeKeys), Is.EqualTo(SortValues(first, second)));
            Assert.That(loaded.RequiresRewrite, Is.False);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void Save_NormalizesKeysToUniqueOrdinalOrder()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(300);
            RecipePersistentKey second = CreateKey(100);
            RecipePersistentKey third = CreateKey(200);

            Assert.That(store.Save(path, [first, second, third, second, first]), Is.True);
            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(GetValues(loaded.FavoriteRecipeKeys), Is.EqualTo(SortValues(first, second, third)));
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void Load_WithMissingFile_ReturnsEmptyMissingResult()
        {
            var store = new RecipeFavoritesStore(null);

            RecipeFavoritesLoadResult loaded = store.Load(CreateFavoritesPath());

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Missing));
            Assert.That(loaded.FavoriteRecipeKeys, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void Load_WithMalformedJson_ReturnsCorruptResultRequiringRewrite()
        {
            string path = CreateFavoritesPath();
            File.WriteAllText(path, "{not valid json");
            var store = new RecipeFavoritesStore(null);

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Corrupt));
            Assert.That(loaded.FavoriteRecipeKeys, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Load_WithMalformedPersistentKey_ReturnsCorruptResultRequiringRewrite()
        {
            string path = CreateFavoritesPath();
            File.WriteAllText(path, "{\"version\":1,\"favoriteRecipeKeys\":[\"v2|not-supported\"]}");
            var store = new RecipeFavoritesStore(null);

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Corrupt));
            Assert.That(loaded.FavoriteRecipeKeys, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Load_WithUnsupportedVersion_DoesNotInterpretOrRequestRewrite()
        {
            string path = CreateFavoritesPath();
            RecipePersistentKey key = CreateKey(100);
            File.WriteAllText(path, $"{{\"version\":2,\"favoriteRecipeKeys\":[\"{key.Value}\"]}}");
            var store = new RecipeFavoritesStore(null);

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.UnsupportedVersion));
            Assert.That(loaded.FavoriteRecipeKeys, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.False);
        }

        [Test]
        public void Load_WithDuplicateOrNonCanonicalOrder_NormalizesAndRequestsRewrite()
        {
            string path = CreateFavoritesPath();
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);
            string[] sorted = SortValues(first, second);
            string high = sorted[1];
            string low = sorted[0];
            File.WriteAllText(path, $"{{\"version\":1,\"favoriteRecipeKeys\":[\"{high}\",\"{low}\",\"{high}\"]}}");
            var store = new RecipeFavoritesStore(null);

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(loaded.FavoriteRecipeKeys), Is.EqualTo(sorted));
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Save_WhenReplacingExistingFile_CreatesBackupAndKeepsNewPrimary()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            Assert.That(store.Save(path, [first]), Is.True);
            Assert.That(store.Save(path, [second]), Is.True);

            RecipeFavoritesLoadResult primary = store.Load(path);
            RecipeFavoritesLoadResult backup = store.Load(path + ".bak");

            Assert.That(primary.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(primary.FavoriteRecipeKeys), Is.EqualTo([second.Value]));
            Assert.That(backup.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(backup.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(File.Exists(path + ".bak"), Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void Load_WithCorruptPrimaryAndValidBackup_RecoversBackup()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            Assert.That(store.Save(path, [first]), Is.True);
            Assert.That(store.Save(path, [second]), Is.True);
            File.WriteAllText(path, "{corrupt");

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(loaded.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Load_WithCorruptPrimaryAndUnreadableBackup_ReturnsFailedWithoutRewrite()
        {
            string path = CreateFavoritesPath();
            string backupPath = path + ".bak";
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            Assert.That(store.Save(path, [first]), Is.True);
            Assert.That(store.Save(path, [second]), Is.True);

            RecipeFavoritesLoadResult validatedBackup = store.Load(backupPath);

            Assert.That(validatedBackup.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(validatedBackup.FavoriteRecipeKeys), Is.EqualTo([first.Value]));

            File.WriteAllText(path, "{corrupt");

            using (new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                RecipeFavoritesLoadResult blocked = store.Load(path);

                Assert.That(blocked.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Failed));
                Assert.That(blocked.FavoriteRecipeKeys, Is.Empty);
                Assert.That(blocked.RequiresRewrite, Is.False);
            }

            RecipeFavoritesLoadResult recovered = store.Load(path);

            Assert.That(recovered.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(recovered.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(recovered.RequiresRewrite, Is.True);
        }

        [Test]
        public void Save_AfterCorruptPrimaryRecovery_PreservesValidatedBackup()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            Assert.That(store.Save(path, [first]), Is.True);
            Assert.That(store.Save(path, [second]), Is.True);
            File.WriteAllText(path, "{corrupt");

            RecipeFavoritesLoadResult recovered = store.Load(path);

            Assert.That(recovered.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(recovered.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(store.Save(path, recovered.FavoriteRecipeKeys, preserveExistingBackup: true), Is.True);

            RecipeFavoritesLoadResult primary = store.Load(path);
            RecipeFavoritesLoadResult backup = store.Load(path + ".bak");

            Assert.That(primary.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(primary.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(backup.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(backup.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(File.Exists(path + ".tmp"), Is.False);

            File.WriteAllText(path, "{corrupt-again");

            RecipeFavoritesLoadResult recoveredAgain = store.Load(path);

            Assert.That(recoveredAgain.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(recoveredAgain.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
        }

        [Test]
        public void Load_WithMissingPrimaryAndValidBackup_RecoversBackup()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            Assert.That(store.Save(path, [first]), Is.True);
            Assert.That(store.Save(path, [second]), Is.True);
            File.Delete(path);

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(loaded.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Save_AfterMissingPrimaryRecovery_PreservesExistingBackup()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);
            RecipePersistentKey first = CreateKey(100);
            RecipePersistentKey second = CreateKey(200);

            Assert.That(store.Save(path, [first]), Is.True);
            Assert.That(store.Save(path, [second]), Is.True);
            File.Delete(path);

            RecipeFavoritesLoadResult recovered = store.Load(path);

            Assert.That(recovered.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(recovered.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(store.Save(path, recovered.FavoriteRecipeKeys, preserveExistingBackup: true), Is.True);

            RecipeFavoritesLoadResult backup = store.Load(path + ".bak");

            Assert.That(backup.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(GetValues(backup.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
            Assert.That(File.Exists(path + ".tmp"), Is.False);

            File.WriteAllText(path, "{corrupt");

            RecipeFavoritesLoadResult recoveredAgain = store.Load(path);

            Assert.That(recoveredAgain.Status, Is.EqualTo(RecipeFavoritesLoadStatus.RecoveredFromBackup));
            Assert.That(GetValues(recoveredAgain.FavoriteRecipeKeys), Is.EqualTo([first.Value]));
        }

        [Test]
        public void Load_WithNullKeyArray_TreatsDocumentAsEmptyAndRequestsCanonicalRewrite()
        {
            string path = CreateFavoritesPath();
            File.WriteAllText(path, "{\"version\":1,\"favoriteRecipeKeys\":null}");
            var store = new RecipeFavoritesStore(null);

            RecipeFavoritesLoadResult loaded = store.Load(path);

            Assert.That(loaded.Status, Is.EqualTo(RecipeFavoritesLoadStatus.Loaded));
            Assert.That(loaded.FavoriteRecipeKeys, Is.Empty);
            Assert.That(loaded.RequiresRewrite, Is.True);
        }

        [Test]
        public void Save_WithNullCollection_Throws()
        {
            var store = new RecipeFavoritesStore(null);

            Assert.Throws<ArgumentNullException>((Action)(() => { store.Save(CreateFavoritesPath(), null); }));
        }

        [Test]
        public void Save_WithNullKey_ThrowsAndRemovesTemporaryFile()
        {
            string path = CreateFavoritesPath();
            var store = new RecipeFavoritesStore(null);

            Assert.Throws<ArgumentException>((Action)(() => { store.Save(path, [null]); }));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        private string CreateFavoritesPath()
        {
            return Path.Combine(_testDirectory, "recipe-favorites.json");
        }

        private static RecipePersistentKey CreateKey(int resultItemId)
        {
            return RecipePersistentKey.Create(
                new RecipeCatalogEntry(
                    runtimeIndex: resultItemId,
                    resultItemId: resultItemId,
                    resultStack: 1,
                    ingredients: [],
                    environmentRequirements: new RecipeEnvironmentRequirements(
                        null,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false),
                    isAlchemy: false));
        }

        private static string[] GetValues(IReadOnlyList<RecipePersistentKey> keys)
        {
            var values = new string[keys.Count];

            for (var index = 0; index < keys.Count; index++)
                values[index] = keys[index].Value;

            return values;
        }

        private static string[] SortValues(params RecipePersistentKey[] keys)
        {
            var values = new string[keys.Length];

            for (var index = 0; index < keys.Length; index++)
                values[index] = keys[index].Value;

            Array.Sort(values, StringComparer.Ordinal);
            return values;
        }
    }
}