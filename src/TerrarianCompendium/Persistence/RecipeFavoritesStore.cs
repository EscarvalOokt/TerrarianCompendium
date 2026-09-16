using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Persistence
{
    internal enum RecipeFavoritesLoadStatus
    {
        Missing,
        Loaded,
        RecoveredFromBackup,
        Corrupt,
        UnsupportedVersion,
        Failed
    }

    internal sealed class RecipeFavoritesLoadResult(
        RecipeFavoritesLoadStatus status,
        IReadOnlyList<RecipePersistentKey> favoriteRecipeKeys,
        bool requiresRewrite)
    {
        public RecipeFavoritesLoadStatus Status { get; } = status;

        public IReadOnlyList<RecipePersistentKey> FavoriteRecipeKeys { get; } =
            favoriteRecipeKeys ?? throw new ArgumentNullException(nameof(favoriteRecipeKeys));

        public bool RequiresRewrite { get; } = requiresRewrite;
    }

    internal sealed class RecipeFavoritesStore(ILogger logger)
    {
        public const int CurrentVersion = 1;

        private readonly ILogger _logger = logger;

        public RecipeFavoritesLoadResult Load(string path)
        {
            ValidatePath(path);

            string backupPath = GetBackupPath(path);

            if (!File.Exists(path))
                return LoadWithoutPrimaryFile(backupPath);

            FileReadResult primaryResult = ReadFile(path);

            if (primaryResult.Status == FileReadStatus.Success)
            {
                return new RecipeFavoritesLoadResult(
                    RecipeFavoritesLoadStatus.Loaded,
                    primaryResult.FavoriteRecipeKeys,
                    primaryResult.RequiresRewrite);
            }

            if (primaryResult.Status == FileReadStatus.UnsupportedVersion)
            {
                LogUnsupportedVersion(path, primaryResult.Version);

                return CreateEmptyLoadResult(RecipeFavoritesLoadStatus.UnsupportedVersion, requiresRewrite: false);
            }

            LogReadFailure(path, primaryResult);

            if (File.Exists(backupPath))
            {
                FileReadResult backupResult = ReadFile(backupPath);

                if (backupResult.Status == FileReadStatus.Success)
                {
                    _logger?.Info($"Recipe favorites recovered from backup '{backupPath}'.");

                    return new RecipeFavoritesLoadResult(
                        RecipeFavoritesLoadStatus.RecoveredFromBackup,
                        backupResult.FavoriteRecipeKeys,
                        requiresRewrite: true);
                }

                if (backupResult.Status == FileReadStatus.UnsupportedVersion)
                {
                    LogUnsupportedVersion(backupPath, backupResult.Version);

                    return CreateEmptyLoadResult(RecipeFavoritesLoadStatus.UnsupportedVersion, requiresRewrite: false);
                }

                LogReadFailure(backupPath, backupResult);

                if (backupResult.Status == FileReadStatus.Failed)
                    return CreateEmptyLoadResult(RecipeFavoritesLoadStatus.Failed, requiresRewrite: false);
            }

            return primaryResult.Status == FileReadStatus.Corrupt
                ? CreateEmptyLoadResult(RecipeFavoritesLoadStatus.Corrupt, requiresRewrite: true)
                : CreateEmptyLoadResult(RecipeFavoritesLoadStatus.Failed, requiresRewrite: false);
        }

        public bool Save(
            string path,
            IEnumerable<RecipePersistentKey> favoriteRecipeKeys,
            bool preserveExistingBackup = false)
        {
            ValidatePath(path);

            if (favoriteRecipeKeys == null)
                throw new ArgumentNullException(nameof(favoriteRecipeKeys));

            string directoryPath = Path.GetDirectoryName(path);
            string temporaryPath = GetTemporaryPath(path);
            string backupPath = GetBackupPath(path);

            try
            {
                if (!string.IsNullOrEmpty(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                var data = new RecipeFavoritesData
                {
                    Version = CurrentVersion,
                    FavoriteRecipeKeys = CreateNormalizedKeyValueArray(favoriteRecipeKeys)
                };

                WriteFile(temporaryPath, data);

                if (File.Exists(path))
                {
                    string replacementBackupPath = preserveExistingBackup ? null : backupPath;
                    File.Replace(temporaryPath, path, replacementBackupPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return true;
            }
            catch (Exception exception) when (IsPersistenceException(exception))
            {
                _logger?.Error($"Failed to save recipe favorites to '{path}'.", exception);

                return false;
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }

        private RecipeFavoritesLoadResult LoadWithoutPrimaryFile(string backupPath)
        {
            if (!File.Exists(backupPath))
                return CreateEmptyLoadResult(RecipeFavoritesLoadStatus.Missing, requiresRewrite: false);

            FileReadResult backupResult = ReadFile(backupPath);

            if (backupResult.Status == FileReadStatus.Success)
            {
                _logger?.Info(
                    $"Recipe favorites recovered from backup '{backupPath}' because the primary file is missing.");

                return new RecipeFavoritesLoadResult(
                    RecipeFavoritesLoadStatus.RecoveredFromBackup,
                    backupResult.FavoriteRecipeKeys,
                    requiresRewrite: true);
            }

            if (backupResult.Status == FileReadStatus.UnsupportedVersion)
            {
                LogUnsupportedVersion(backupPath, backupResult.Version);

                return CreateEmptyLoadResult(RecipeFavoritesLoadStatus.UnsupportedVersion, requiresRewrite: false);
            }

            LogReadFailure(backupPath, backupResult);

            return backupResult.Status == FileReadStatus.Corrupt
                ? CreateEmptyLoadResult(RecipeFavoritesLoadStatus.Corrupt, requiresRewrite: true)
                : CreateEmptyLoadResult(RecipeFavoritesLoadStatus.Failed, requiresRewrite: false);
        }

        private FileReadResult ReadFile(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var serializer = new DataContractJsonSerializer(typeof(RecipeFavoritesData));
                var data = serializer.ReadObject(stream) as RecipeFavoritesData;

                if (data == null)
                {
                    return FileReadResult.Corrupt(
                        new SerializationException("Recipe favorites document did not contain a valid data object."));
                }

                if (data.Version != CurrentVersion)
                    return FileReadResult.UnsupportedVersion(data.Version);

                string[] storedValues = data.FavoriteRecipeKeys ?? Array.Empty<string>();
                var keys = new List<RecipePersistentKey>(storedValues.Length);
                var uniqueKeys = new HashSet<RecipePersistentKey>();
                bool requiresRewrite = data.FavoriteRecipeKeys == null;

                for (var index = 0; index < storedValues.Length; index++)
                {
                    string storedValue = storedValues[index];

                    if (!RecipePersistentKey.TryParse(storedValue, out RecipePersistentKey key))
                    {
                        return FileReadResult.Corrupt(
                            new SerializationException(
                                $"Recipe favorites document contains an invalid persistent recipe key at index {index}."));
                    }

                    if (!uniqueKeys.Add(key))
                        requiresRewrite = true;
                }

                keys.AddRange(uniqueKeys);
                keys.Sort(CompareKeys);

                if (!requiresRewrite && !HasCanonicalStoredOrder(storedValues, keys))
                    requiresRewrite = true;

                return FileReadResult.Success(new ReadOnlyCollection<RecipePersistentKey>(keys), requiresRewrite);
            }
            catch (SerializationException exception)
            {
                return FileReadResult.Corrupt(exception);
            }
            catch (InvalidDataContractException exception)
            {
                return FileReadResult.Corrupt(exception);
            }
            catch (XmlException exception)
            {
                return FileReadResult.Corrupt(exception);
            }
            catch (Exception exception) when (IsFileAccessException(exception))
            {
                return FileReadResult.Failed(exception);
            }
        }

        private void LogReadFailure(string path, FileReadResult result)
        {
            if (result.Exception == null)
                return;

            if (result.Status == FileReadStatus.Corrupt)
            {
                _logger?.Warn(
                    $"Recipe favorites file '{path}' is corrupt: " +
                    $"{result.Exception.GetType().Name}: {result.Exception.Message}");

                return;
            }

            _logger?.Warn(
                $"Recipe favorites file '{path}' could not be read: " +
                $"{result.Exception.GetType().Name}: {result.Exception.Message}");
        }

        private void LogUnsupportedVersion(string path, int version)
        {
            _logger?.Warn(
                $"Recipe favorites file '{path}' uses unsupported schema version {version}. " +
                $"Current supported version is {CurrentVersion}; the file will not be overwritten.");
        }

        private void TryDeleteTemporaryFile(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch (Exception exception) when (IsFileAccessException(exception))
            {
                _logger?.Warn(
                    $"Failed to remove recipe favorites temporary file '{temporaryPath}': " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private static void WriteFile(string path, RecipeFavoritesData data)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var serializer = new DataContractJsonSerializer(typeof(RecipeFavoritesData));
            serializer.WriteObject(stream, data);
        }

        private static string[] CreateNormalizedKeyValueArray(IEnumerable<RecipePersistentKey> favoriteRecipeKeys)
        {
            var uniqueKeys = new HashSet<RecipePersistentKey>();

            foreach (RecipePersistentKey key in favoriteRecipeKeys)
            {
                if (key == null)
                {
                    throw new ArgumentException(
                        "Favorite recipe keys must not contain null values.",
                        nameof(favoriteRecipeKeys));
                }

                uniqueKeys.Add(key);
            }

            var keys = new List<RecipePersistentKey>(uniqueKeys);
            keys.Sort(CompareKeys);
            var values = new string[keys.Count];

            for (var index = 0; index < keys.Count; index++)
                values[index] = keys[index].Value;

            return values;
        }

        private static bool HasCanonicalStoredOrder(string[] storedValues, IReadOnlyList<RecipePersistentKey> keys)
        {
            if (storedValues.Length != keys.Count)
                return false;

            for (var index = 0; index < storedValues.Length; index++)
            {
                if (!string.Equals(storedValues[index], keys[index].Value, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static int CompareKeys(RecipePersistentKey left, RecipePersistentKey right)
        {
            return string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        }

        private static RecipeFavoritesLoadResult CreateEmptyLoadResult(
            RecipeFavoritesLoadStatus status,
            bool requiresRewrite)
        {
            return new RecipeFavoritesLoadResult(status, Array.Empty<RecipePersistentKey>(), requiresRewrite);
        }

        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Persistence path must not be empty or whitespace.", nameof(path));
        }

        private static string GetTemporaryPath(string path)
        {
            return path + ".tmp";
        }

        private static string GetBackupPath(string path)
        {
            return path + ".bak";
        }

        private static bool IsPersistenceException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is NotSupportedException ||
                   exception is SerializationException ||
                   exception is InvalidDataContractException ||
                   exception is XmlException;
        }

        private static bool IsFileAccessException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is NotSupportedException;
        }

        private enum FileReadStatus
        {
            Success,
            Corrupt,
            UnsupportedVersion,
            Failed
        }

        private sealed class FileReadResult
        {
            private FileReadResult(
                FileReadStatus status,
                IReadOnlyList<RecipePersistentKey> favoriteRecipeKeys,
                int version,
                Exception exception,
                bool requiresRewrite)
            {
                Status = status;
                FavoriteRecipeKeys = favoriteRecipeKeys;
                Version = version;
                Exception = exception;
                RequiresRewrite = requiresRewrite;
            }

            public FileReadStatus Status { get; }

            public IReadOnlyList<RecipePersistentKey> FavoriteRecipeKeys { get; }

            public int Version { get; }

            public Exception Exception { get; }

            public bool RequiresRewrite { get; }

            public static FileReadResult Success(
                IReadOnlyList<RecipePersistentKey> favoriteRecipeKeys,
                bool requiresRewrite)
            {
                return new FileReadResult(
                    FileReadStatus.Success,
                    favoriteRecipeKeys,
                    CurrentVersion,
                    null,
                    requiresRewrite);
            }

            public static FileReadResult Corrupt(Exception exception)
            {
                return new FileReadResult(
                    FileReadStatus.Corrupt,
                    Array.Empty<RecipePersistentKey>(),
                    0,
                    exception,
                    requiresRewrite: false);
            }

            public static FileReadResult UnsupportedVersion(int version)
            {
                return new FileReadResult(
                    FileReadStatus.UnsupportedVersion,
                    Array.Empty<RecipePersistentKey>(),
                    version,
                    null,
                    requiresRewrite: false);
            }

            public static FileReadResult Failed(Exception exception)
            {
                return new FileReadResult(
                    FileReadStatus.Failed,
                    Array.Empty<RecipePersistentKey>(),
                    0,
                    exception,
                    requiresRewrite: false);
            }
        }
    }
}