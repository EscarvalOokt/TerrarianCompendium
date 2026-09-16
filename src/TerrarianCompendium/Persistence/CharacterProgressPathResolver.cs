using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.IO;

namespace TerrarianCompendium.Persistence
{
    internal sealed class CharacterProgressPathResolver
    {
        private const string LegacyPersistenceRoot = "item-checklist";
        private const string PlayersDirectoryName = "players";
        private const string TerrariaModderDirectoryName = "TerrariaModder";

        public bool TryResolveCurrentProgressPaths(out string progressPath, out string legacyProgressPath)
        {
            progressPath = null;
            legacyProgressPath = null;

            PlayerFileData playerFileData = Main.ActivePlayerFileData;

            if (playerFileData == null || string.IsNullOrWhiteSpace(playerFileData.Path))
                return false;

            if (string.IsNullOrWhiteSpace(Main.SavePath))
                return false;

            progressPath = BuildProgressPath(Main.SavePath, playerFileData.Path, playerFileData.IsCloudSave);
            legacyProgressPath = BuildLegacyProgressPath(
                Main.SavePath,
                playerFileData.Path,
                playerFileData.IsCloudSave);

            return true;
        }

        internal static string BuildProgressPath(string saveRoot, string playerSavePath, bool isCloudSave)
        {
            return BuildProgressPath(saveRoot, playerSavePath, isCloudSave, TerrarianCompendiumMod.ModId);
        }

        internal static string BuildLegacyProgressPath(string saveRoot, string playerSavePath, bool isCloudSave)
        {
            return BuildProgressPath(saveRoot, playerSavePath, isCloudSave, LegacyPersistenceRoot);
        }

        private static string BuildProgressPath(
            string saveRoot,
            string playerSavePath,
            bool isCloudSave,
            string persistenceRoot)
        {
            if (string.IsNullOrWhiteSpace(saveRoot))
                throw new ArgumentException("Save root must not be empty or whitespace.", nameof(saveRoot));

            if (string.IsNullOrWhiteSpace(playerSavePath))
            {
                throw new ArgumentException(
                    "Player save path must not be empty or whitespace.",
                    nameof(playerSavePath));
            }

            string normalizedPlayerSavePath = NormalizePlayerSavePath(playerSavePath);
            string identity = (isCloudSave ? "cloud:" : "local:") + normalizedPlayerSavePath;
            string characterKey = ComputeSha256(identity);

            return Path.Combine(
                saveRoot,
                TerrariaModderDirectoryName,
                persistenceRoot,
                PlayersDirectoryName,
                characterKey + ".json");
        }

        private static string NormalizePlayerSavePath(string playerSavePath)
        {
            string normalizedPath = playerSavePath.Trim().Replace('\\', '/');

            while (normalizedPath.Contains("//"))
                normalizedPath = normalizedPath.Replace("//", "/");

            normalizedPath = normalizedPath.TrimEnd('/');

            if (normalizedPath.Length == 0)
            {
                throw new ArgumentException("Player save path must contain a file path.", nameof(playerSavePath));
            }

            return normalizedPath.ToUpperInvariant();
        }

        private static string ComputeSha256(string value)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(valueBytes);
            var result = new StringBuilder(hash.Length * 2);

            foreach (byte valueByte in hash)
                result.Append(valueByte.ToString("x2"));

            return result.ToString();
        }
    }
}