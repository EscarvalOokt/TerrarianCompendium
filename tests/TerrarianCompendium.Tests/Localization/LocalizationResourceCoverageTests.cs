using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using NUnit.Framework;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Tests.Localization
{
    [TestFixture]
    public sealed class LocalizationResourceCoverageTests
    {
        private const string ReleaseCultureName = "ru-RU";
        private const string ResourcePrefix = "TerrarianCompendium.Localization.Locales.";
        private const string ResourceSuffix = ".json";

        [Test]
        public void SourceLocale_ContainsExactlyAllDeclaredKeysWithNonEmptyValues()
        {
            var localization = CompendiumLocalization.LoadFromAssembly(
                typeof(CompendiumLocalization).Assembly,
                CompendiumLocalization.SourceCultureName);
            string[] expected = CompendiumTextKeys.EnumerateExpectedKeys().Distinct().OrderBy(key => key).ToArray();
            string[] actual = localization.SourceEntries.Keys.OrderBy(key => key).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(expected, Is.EqualTo(actual));

                foreach (string key in expected)
                {
                    Assert.That(
                        localization.SourceEntries[key],
                        Is.Not.Null.And.Not.Empty,
                        $"Source locale value is missing for '{key}'.");
                }
            });
        }

        [Test]
        public void ReleaseLocale_ContainsExactlyAllDeclaredKeysWithNonEmptyValuesAndCompatiblePlaceholders()
        {
            var localization = CompendiumLocalization.LoadFromAssembly(
                typeof(CompendiumLocalization).Assembly,
                CompendiumLocalization.SourceCultureName);
            IReadOnlyDictionary<string, string> releaseEntries = ReadEmbeddedLocale(ReleaseCultureName);
            string[] expected = CompendiumTextKeys.EnumerateExpectedKeys().Distinct().OrderBy(key => key).ToArray();
            string[] actual = releaseEntries.Keys.OrderBy(key => key).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(expected, Is.EqualTo(actual));

                foreach (string key in expected)
                {
                    bool hasReleaseValue = releaseEntries.TryGetValue(key, out string releaseValue);
                    Assert.That(hasReleaseValue, Is.True, $"Release locale key '{key}' is missing.");
                    if (!hasReleaseValue)
                        continue;

                    Assert.That(
                        releaseValue,
                        Is.Not.Null.And.Not.Empty,
                        $"Release locale value is missing for '{key}'.");

                    Assert.That(
                        CompendiumLocalization.HaveCompatibleFormatItems(localization.SourceEntries[key], releaseValue),
                        Is.True,
                        $"Release locale placeholders are incompatible for '{key}'.");
                }
            });
        }

        [Test]
        public void ExpectedKeys_AreUnique()
        {
            string[] keys = CompendiumTextKeys.EnumerateExpectedKeys().ToArray();

            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
        }

        private static IReadOnlyDictionary<string, string> ReadEmbeddedLocale(string cultureName)
        {
            Assembly assembly = typeof(CompendiumLocalization).Assembly;
            string resourceName = ResourcePrefix + cultureName + ResourceSuffix;

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            Assert.That(stream, Is.Not.Null, $"Embedded locale resource '{resourceName}' is missing.");

            var settings = new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            };
            var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), settings);
            var entries = serializer.ReadObject(stream) as Dictionary<string, string>;

            Assert.That(entries, Is.Not.Null, $"Embedded locale resource '{resourceName}' is invalid.");
            return entries;
        }
    }
}