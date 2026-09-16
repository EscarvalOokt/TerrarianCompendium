using System;
using System.IO;
using Terraria;

namespace TerrarianCompendium.Persistence
{
    internal sealed class RecipeFavoritesPathResolver
    {
        private const string FavoritesFileName = "recipe-favorites.json";
        private const string TerrariaModderDirectoryName = "TerrariaModder";

        public bool TryResolveCurrentPath(out string favoritesPath)
        {
            favoritesPath = null;

            if (string.IsNullOrWhiteSpace(Main.SavePath))
                return false;

            favoritesPath = BuildPath(Main.SavePath);
            return true;
        }

        internal static string BuildPath(string saveRoot)
        {
            if (string.IsNullOrWhiteSpace(saveRoot))
                throw new ArgumentException("Save root must not be empty or whitespace.", nameof(saveRoot));

            return Path.Combine(saveRoot, TerrariaModderDirectoryName, TerrarianCompendiumMod.ModId, FavoritesFileName);
        }
    }
}