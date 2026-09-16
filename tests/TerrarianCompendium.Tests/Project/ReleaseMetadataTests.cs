using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TerrarianCompendium.Tests.Project
{
    [TestFixture]
    public sealed class ReleaseMetadataTests
    {
        private const string ExpectedReleaseVersion = "1.0.0";

        [Test]
        public void ManifestIdentity_MatchesRuntimeIdentity()
        {
            string manifest = ReadManifest();

            Assert.That(ReadManifestString(manifest, "id"), Is.EqualTo(TerrarianCompendiumMod.ModId));
            Assert.That(ReadManifestString(manifest, "name"), Is.EqualTo(TerrarianCompendiumMod.ModName));
        }

        [Test]
        public void ManifestVersion_MatchesRuntimeVersion()
        {
            string manifest = ReadManifest();

            Assert.That(ReadManifestString(manifest, "version"), Is.EqualTo(TerrarianCompendiumMod.ModVersion));
        }

        [Test]
        public void AssemblyVersion_MatchesRuntimeVersion()
        {
            Assembly assembly = typeof(TerrarianCompendiumMod).Assembly;
            var assemblyVersion = assembly.GetName().Version?.ToString(3);

            Assert.That(assemblyVersion, Is.EqualTo(TerrarianCompendiumMod.ModVersion));
        }

        [Test]
        public void ManifestEntryDll_MatchesProductionAssembly()
        {
            string manifest = ReadManifest();
            string entryDll = typeof(TerrarianCompendiumMod).Assembly.ManifestModule.Name;

            Assert.That(ReadManifestString(manifest, "entry_dll"), Is.EqualTo(entryDll));
        }

        [Test]
        public void RuntimeVersion_MatchesV1Release()
        {
            Assert.That(TerrarianCompendiumMod.ModVersion, Is.EqualTo(ExpectedReleaseVersion));
        }

        private static string ReadManifest()
        {
            string manifestPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Project", "manifest.json");

            Assert.That(File.Exists(manifestPath), Is.True, $"Manifest fixture was not found at '{manifestPath}'.");

            return File.ReadAllText(manifestPath);
        }

        private static string ReadManifestString(string manifest, string propertyName)
        {
            Match match = Regex.Match(
                manifest,
                $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"(?<value>[^\"]*)\"",
                RegexOptions.CultureInvariant);

            Assert.That(match.Success, Is.True, $"Manifest property '{propertyName}' was not found.");

            return match.Groups["value"].Value;
        }
    }
}