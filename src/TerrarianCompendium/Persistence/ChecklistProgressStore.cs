using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using TerrariaModder.Core.Logging;

namespace TerrarianCompendium.Persistence
{
    internal enum ChecklistProgressLoadStatus
    {
        Missing,
        Loaded,
        RecoveredFromBackup,
        Corrupt,
        UnsupportedVersion,
        Failed
    }

    internal sealed class ChecklistProgressLoadResult(
        ChecklistProgressLoadStatus status,
        IReadOnlyList<int> foundItemIds,
        bool requiresRewrite)
    {
        public ChecklistProgressLoadStatus Status { get; } = status;

        public IReadOnlyList<int> FoundItemIds { get; } =
            foundItemIds ?? throw new ArgumentNullException(nameof(foundItemIds));

        public bool RequiresRewrite { get; } = requiresRewrite;
    }

    internal sealed class ChecklistProgressStore(ILogger logger)
    {
        public const int CurrentVersion = 1;

        private readonly ILogger _logger = logger;

        public ChecklistProgressLoadResult Load(string path)
        {
            ValidatePath(path);

            string backupPath = GetBackupPath(path);

            if (!File.Exists(path))
                return LoadWithoutPrimaryFile(backupPath);

            FileReadResult primaryResult = ReadFile(path);

            if (primaryResult.Status == FileReadStatus.Success)
            {
                return new ChecklistProgressLoadResult(
                    ChecklistProgressLoadStatus.Loaded,
                    primaryResult.FoundItemIds,
                    requiresRewrite: false);
            }

            if (primaryResult.Status == FileReadStatus.UnsupportedVersion)
            {
                LogUnsupportedVersion(path, primaryResult.Version);

                return CreateEmptyLoadResult(ChecklistProgressLoadStatus.UnsupportedVersion, requiresRewrite: false);
            }

            LogReadFailure(path, primaryResult);

            if (File.Exists(backupPath))
            {
                FileReadResult backupResult = ReadFile(backupPath);

                if (backupResult.Status == FileReadStatus.Success)
                {
                    _logger?.Info($"Checklist persistence recovered progress from backup '{backupPath}'.");

                    return new ChecklistProgressLoadResult(
                        ChecklistProgressLoadStatus.RecoveredFromBackup,
                        backupResult.FoundItemIds,
                        requiresRewrite: true);
                }

                if (backupResult.Status == FileReadStatus.UnsupportedVersion)
                {
                    LogUnsupportedVersion(backupPath, backupResult.Version);

                    return CreateEmptyLoadResult(
                        ChecklistProgressLoadStatus.UnsupportedVersion,
                        requiresRewrite: false);
                }

                LogReadFailure(backupPath, backupResult);

                if (backupResult.Status == FileReadStatus.Failed)
                    return CreateEmptyLoadResult(ChecklistProgressLoadStatus.Failed, requiresRewrite: false);
            }

            return primaryResult.Status == FileReadStatus.Corrupt
                ? CreateEmptyLoadResult(ChecklistProgressLoadStatus.Corrupt, requiresRewrite: true)
                : CreateEmptyLoadResult(ChecklistProgressLoadStatus.Failed, requiresRewrite: false);
        }

        public ChecklistProgressLoadResult LoadWithLegacyFallback(
            string currentPath,
            string legacyPath,
            out bool usedLegacyPath)
        {
            ValidatePath(currentPath);
            ValidatePath(legacyPath);

            ChecklistProgressLoadResult currentResult = Load(currentPath);

            if (currentResult.Status != ChecklistProgressLoadStatus.Missing)
            {
                usedLegacyPath = false;

                return currentResult;
            }

            ChecklistProgressLoadResult legacyResult = Load(legacyPath);
            usedLegacyPath = legacyResult.Status != ChecklistProgressLoadStatus.Missing;

            return legacyResult;
        }

        public bool Save(string path, IEnumerable<int> foundItemIds, bool preserveExistingBackup = false)
        {
            ValidatePath(path);

            if (foundItemIds == null)
                throw new ArgumentNullException(nameof(foundItemIds));

            string directoryPath = Path.GetDirectoryName(path);
            string temporaryPath = GetTemporaryPath(path);
            string backupPath = GetBackupPath(path);

            try
            {
                if (!string.IsNullOrEmpty(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                var data = new ChecklistProgressData
                {
                    Version = CurrentVersion,
                    FoundItemIds = CreateNormalizedItemIdArray(foundItemIds)
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
                _logger?.Error($"Failed to save checklist progress to '{path}'.", exception);

                return false;
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }

        private ChecklistProgressLoadResult LoadWithoutPrimaryFile(string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                return CreateEmptyLoadResult(ChecklistProgressLoadStatus.Missing, requiresRewrite: false);
            }

            FileReadResult backupResult = ReadFile(backupPath);

            if (backupResult.Status == FileReadStatus.Success)
            {
                _logger?.Info(
                    $"Checklist persistence recovered progress from backup '{backupPath}' because the primary file is missing.");

                return new ChecklistProgressLoadResult(
                    ChecklistProgressLoadStatus.RecoveredFromBackup,
                    backupResult.FoundItemIds,
                    requiresRewrite: true);
            }

            if (backupResult.Status == FileReadStatus.UnsupportedVersion)
            {
                LogUnsupportedVersion(backupPath, backupResult.Version);

                return CreateEmptyLoadResult(ChecklistProgressLoadStatus.UnsupportedVersion, requiresRewrite: false);
            }

            LogReadFailure(backupPath, backupResult);

            return backupResult.Status == FileReadStatus.Corrupt
                ? CreateEmptyLoadResult(ChecklistProgressLoadStatus.Corrupt, requiresRewrite: true)
                : CreateEmptyLoadResult(ChecklistProgressLoadStatus.Failed, requiresRewrite: false);
        }

        private FileReadResult ReadFile(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var serializer = new DataContractJsonSerializer(typeof(ChecklistProgressData));
                var data = serializer.ReadObject(stream) as ChecklistProgressData;

                if (data == null)
                {
                    return FileReadResult.Corrupt(
                        new SerializationException("Checklist progress document did not contain a valid data object."));
                }

                if (data.Version != CurrentVersion)
                    return FileReadResult.UnsupportedVersion(data.Version);

                int[] foundItemIds = data.FoundItemIds ?? Array.Empty<int>();

                return FileReadResult.Success(CreateReadOnlyCopy(foundItemIds));
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
                    $"Checklist persistence file '{path}' is corrupt: " +
                    $"{result.Exception.GetType().Name}: {result.Exception.Message}");

                return;
            }

            _logger?.Warn(
                $"Checklist persistence file '{path}' could not be read: " +
                $"{result.Exception.GetType().Name}: {result.Exception.Message}");
        }

        private void LogUnsupportedVersion(string path, int version)
        {
            _logger?.Warn(
                $"Checklist persistence file '{path}' uses unsupported schema version {version}. " +
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
                    $"Failed to remove checklist persistence temporary file '{temporaryPath}': " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private static void WriteFile(string path, ChecklistProgressData data)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var serializer = new DataContractJsonSerializer(typeof(ChecklistProgressData));
            serializer.WriteObject(stream, data);
        }

        private static int[] CreateNormalizedItemIdArray(IEnumerable<int> foundItemIds)
        {
            var uniqueItemIds = new HashSet<int>();

            foreach (int itemId in foundItemIds)
                uniqueItemIds.Add(itemId);

            var sortedItemIds = new List<int>(uniqueItemIds);
            sortedItemIds.Sort();

            return sortedItemIds.ToArray();
        }

        private static IReadOnlyList<int> CreateReadOnlyCopy(int[] foundItemIds)
        {
            var copy = new int[foundItemIds.Length];
            Array.Copy(foundItemIds, copy, foundItemIds.Length);

            return Array.AsReadOnly(copy);
        }

        private static ChecklistProgressLoadResult CreateEmptyLoadResult(
            ChecklistProgressLoadStatus status,
            bool requiresRewrite)
        {
            return new ChecklistProgressLoadResult(status, Array.Empty<int>(), requiresRewrite);
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
                IReadOnlyList<int> foundItemIds,
                int version,
                Exception exception)
            {
                Status = status;
                FoundItemIds = foundItemIds;
                Version = version;
                Exception = exception;
            }

            public FileReadStatus Status { get; }

            public IReadOnlyList<int> FoundItemIds { get; }

            public int Version { get; }

            public Exception Exception { get; }

            public static FileReadResult Success(IReadOnlyList<int> foundItemIds)
            {
                return new FileReadResult(FileReadStatus.Success, foundItemIds, CurrentVersion, null);
            }

            public static FileReadResult Corrupt(Exception exception)
            {
                return new FileReadResult(FileReadStatus.Corrupt, Array.Empty<int>(), 0, exception);
            }

            public static FileReadResult UnsupportedVersion(int version)
            {
                return new FileReadResult(FileReadStatus.UnsupportedVersion, Array.Empty<int>(), version, null);
            }

            public static FileReadResult Failed(Exception exception)
            {
                return new FileReadResult(FileReadStatus.Failed, Array.Empty<int>(), 0, exception);
            }
        }
    }
}