using System;
using System.IO;
using NUnit.Framework;
using TerrarianCompendium.Persistence;

namespace TerrarianCompendium.Tests.Persistence
{
    [TestFixture]
    public sealed class RecipeFavoritesPathResolverTests
    {
        [Test]
        public void BuildPath_WithSameSaveRoot_ReturnsSamePath()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string first = RecipeFavoritesPathResolver.BuildPath(saveRoot);
            string second = RecipeFavoritesPathResolver.BuildPath(saveRoot);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void BuildPath_PlacesFileInsideTerrarianCompendiumRoot()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string favoritesPath = RecipeFavoritesPathResolver.BuildPath(saveRoot);
            string expectedDirectory = Path.Combine(saveRoot, "TerrariaModder", "terrarian-compendium");

            Assert.That(Path.GetDirectoryName(favoritesPath), Is.EqualTo(expectedDirectory));
            Assert.That(Path.GetFileName(favoritesPath), Is.EqualTo("recipe-favorites.json"));
        }

        [Test]
        public void BuildPath_DoesNotUseCharacterSpecificDirectory()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string favoritesPath = RecipeFavoritesPathResolver.BuildPath(saveRoot);

            Assert.That(
                favoritesPath,
                Does.Not.Contain(Path.DirectorySeparatorChar + "players" + Path.DirectorySeparatorChar));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BuildPath_WithBlankSaveRoot_Throws(string saveRoot)
        {
            Assert.Throws<ArgumentException>((Action)(() => { RecipeFavoritesPathResolver.BuildPath(saveRoot); }));
        }
    }
}