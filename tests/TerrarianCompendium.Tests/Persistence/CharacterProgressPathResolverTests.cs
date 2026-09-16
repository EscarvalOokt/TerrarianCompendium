using System;
using System.IO;
using NUnit.Framework;
using TerrarianCompendium.Persistence;

namespace TerrarianCompendium.Tests.Persistence
{
    [TestFixture]
    public sealed class CharacterProgressPathResolverTests
    {
        [Test]
        public void BuildProgressPath_WithSameCharacterIdentity_ReturnsSamePath()
        {
            string saveRoot = Path.Combine("C:", "Terraria");
            const string playerSavePath = @"C:\Terraria\Players\Character.plr";

            string first = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            string second = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void BuildProgressPath_WithDifferentPlayerPaths_ReturnsDifferentPaths()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string first = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                @"C:\Terraria\Players\First.plr",
                isCloudSave: false);

            string second = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                @"C:\Terraria\Players\Second.plr",
                isCloudSave: false);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void BuildProgressPath_LocalAndCloudIdentity_ReturnDifferentPaths()
        {
            string saveRoot = Path.Combine("C:", "Terraria");
            const string playerSavePath = "players/Character.plr";

            string local = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            string cloud = CharacterProgressPathResolver.BuildProgressPath(saveRoot, playerSavePath, isCloudSave: true);

            Assert.That(cloud, Is.Not.EqualTo(local));
        }

        [Test]
        public void BuildProgressPath_NormalizesPathCaseAndSeparators()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string first = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                @"C:\Terraria\Players\Character.plr",
                isCloudSave: false);

            string second = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                "c:/terraria/players/character.plr",
                isCloudSave: false);

            Assert.That(second, Is.EqualTo(first));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BuildProgressPath_WithBlankPlayerSavePath_Throws(string playerSavePath)
        {
            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    CharacterProgressPathResolver.BuildProgressPath("Terraria", playerSavePath, isCloudSave: false);
                }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BuildProgressPath_WithBlankSaveRoot_Throws(string saveRoot)
        {
            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    CharacterProgressPathResolver.BuildProgressPath(
                        saveRoot,
                        @"C:\Terraria\Players\Character.plr",
                        isCloudSave: false);
                }));
        }

        [Test]
        public void BuildProgressPath_PlacesFileInsideTerrarianCompendiumPlayersDirectory()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string progressPath = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                @"C:\Terraria\Players\Character.plr",
                isCloudSave: false);

            string expectedDirectory = Path.Combine(saveRoot, "TerrariaModder", "terrarian-compendium", "players");

            Assert.That(Path.GetDirectoryName(progressPath), Is.EqualTo(expectedDirectory));
        }

        [Test]
        public void BuildLegacyProgressPath_PlacesFileInsideItemChecklistPlayersDirectory()
        {
            string saveRoot = Path.Combine("C:", "Terraria");

            string progressPath = CharacterProgressPathResolver.BuildLegacyProgressPath(
                saveRoot,
                @"C:\Terraria\Players\Character.plr",
                isCloudSave: false);

            string expectedDirectory = Path.Combine(saveRoot, "TerrariaModder", "item-checklist", "players");

            Assert.That(Path.GetDirectoryName(progressPath), Is.EqualTo(expectedDirectory));
        }

        [Test]
        public void CurrentAndLegacyProgressPaths_UseSameCharacterFileName()
        {
            string saveRoot = Path.Combine("C:", "Terraria");
            const string playerSavePath = @"C:\Terraria\Players\Character.plr";

            string currentPath = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            string legacyPath = CharacterProgressPathResolver.BuildLegacyProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            Assert.That(Path.GetFileName(currentPath), Is.EqualTo(Path.GetFileName(legacyPath)));
        }

        [Test]
        public void CurrentAndLegacyProgressPaths_PreserveLocalCloudIdentitySemantics()
        {
            string saveRoot = Path.Combine("C:", "Terraria");
            const string playerSavePath = "players/Character.plr";

            string currentLocal = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            string currentCloud = CharacterProgressPathResolver.BuildProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: true);

            string legacyLocal = CharacterProgressPathResolver.BuildLegacyProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: false);

            string legacyCloud = CharacterProgressPathResolver.BuildLegacyProgressPath(
                saveRoot,
                playerSavePath,
                isCloudSave: true);

            Assert.That(Path.GetFileName(currentLocal), Is.EqualTo(Path.GetFileName(legacyLocal)));
            Assert.That(Path.GetFileName(currentCloud), Is.EqualTo(Path.GetFileName(legacyCloud)));
            Assert.That(Path.GetFileName(currentCloud), Is.Not.EqualTo(Path.GetFileName(currentLocal)));
        }

        [Test]
        public void BuildProgressPath_UsesSha256CharacterKeyAsFileName()
        {
            string progressPath = CharacterProgressPathResolver.BuildProgressPath(
                "Terraria",
                @"C:\Terraria\Players\Character.plr",
                isCloudSave: false);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(progressPath);

            Assert.That(fileNameWithoutExtension, Has.Length.EqualTo(64));

            foreach (char character in fileNameWithoutExtension)
            {
                bool isLowerHex = character >= '0' && character <= '9' || character >= 'a' && character <= 'f';

                Assert.That(isLowerHex, Is.True);
            }

            Assert.That(Path.GetExtension(progressPath), Is.EqualTo(".json"));
        }
    }
}