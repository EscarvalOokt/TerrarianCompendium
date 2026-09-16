using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Tests.Localization
{
    [TestFixture]
    public sealed class LocalizationResourceCoverageTests
    {
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
        public void ExpectedKeys_AreUnique()
        {
            string[] keys = CompendiumTextKeys.EnumerateExpectedKeys().ToArray();

            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
        }
    }
}