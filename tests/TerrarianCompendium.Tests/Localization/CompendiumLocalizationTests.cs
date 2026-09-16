using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Tests.Localization
{
    [TestFixture]
    public sealed class CompendiumLocalizationTests
    {
        private static readonly IReadOnlyDictionary<string, string> _source = new Dictionary<string, string>
        {
            ["Plain"] = "Source text",
            ["Formatted"] = "Value {0}: {1}",
            ["Number"] = "Number {0:0.0}"
        };

        [Test]
        public void SourceLocale_UsesSourceValuesWithoutIncrementingRevision()
        {
            var localization = CompendiumLocalization.CreateForTesting(_source);

            Assert.Multiple(() =>
            {
                Assert.That(localization.CultureName, Is.EqualTo(CompendiumLocalization.SourceCultureName));
                Assert.That(localization.Revision, Is.Zero);
                Assert.That(localization.Get("Plain"), Is.EqualTo("Source text"));
                Assert.That(localization.SynchronizeCulture(CompendiumLocalization.SourceCultureName), Is.False);
                Assert.That(localization.Revision, Is.Zero);
            });
        }

        [Test]
        public void SynchronizeCulture_UsesTranslationAndIncrementsRevisionOnlyWhenCultureChanges()
        {
            var translated = new Dictionary<string, string>
            {
                ["Plain"] = "Translated text"
            };
            var localization = CompendiumLocalization.CreateForTesting(_source, translated);

            Assert.That(localization.SynchronizeCulture("test"), Is.True);
            Assert.That(localization.Get("Plain"), Is.EqualTo("Translated text"));
            Assert.That(localization.Revision, Is.EqualTo(1));

            Assert.That(localization.SynchronizeCulture("test"), Is.False);
            Assert.That(localization.Revision, Is.EqualTo(1));

            Assert.That(localization.SynchronizeCulture(CompendiumLocalization.SourceCultureName), Is.True);
            Assert.That(localization.Get("Plain"), Is.EqualTo("Source text"));
            Assert.That(localization.Revision, Is.EqualTo(2));
        }

        [Test]
        public void MissingLocaleAndMissingTranslatedKey_FallBackToSource()
        {
            IReadOnlyDictionary<string, string> translated = new Dictionary<string, string>();
            var localization = CompendiumLocalization.CreateForTesting(_source, translated);

            localization.SynchronizeCulture("test");
            Assert.That(localization.Get("Plain"), Is.EqualTo("Source text"));

            localization.SynchronizeCulture("missing-locale");
            Assert.That(localization.Get("Plain"), Is.EqualTo("Source text"));
        }

        [Test]
        public void MissingSourceKey_FallsBackToKey()
        {
            var localization = CompendiumLocalization.CreateForTesting(_source);

            Assert.That(localization.Get("Missing.Key"), Is.EqualTo("Missing.Key"));
            Assert.That(localization.Format("Missing.Formatted", 1), Is.EqualTo("Missing.Formatted"));
        }

        [Test]
        public void Format_UsesTranslatedTemplateWhenPlaceholderContractMatches()
        {
            var translated = new Dictionary<string, string>
            {
                ["Formatted"] = "Translated {1} / {0}"
            };
            var localization = CompendiumLocalization.CreateForTesting(_source, translated);
            localization.SynchronizeCulture("test");

            Assert.That(localization.Format("Formatted", "A", "B"), Is.EqualTo("Translated B / A"));
        }

        [Test]
        public void Format_IncompatibleTranslatedPlaceholders_FallBackToSourceTemplate()
        {
            var translated = new Dictionary<string, string>
            {
                ["Formatted"] = "Broken {0}"
            };
            var localization = CompendiumLocalization.CreateForTesting(_source, translated);
            localization.SynchronizeCulture("test");

            Assert.That(localization.Format("Formatted", "A", "B"), Is.EqualTo("Value A: B"));
        }

        [Test]
        public void Get_DoesNotTruncateLongTranslatedText()
        {
            var longText = new string('x', 500);
            var translated = new Dictionary<string, string>
            {
                ["Plain"] = longText
            };
            var localization = CompendiumLocalization.CreateForTesting(_source, translated);
            localization.SynchronizeCulture("test");

            Assert.That(localization.Get("Plain"), Is.EqualTo(longText));
        }

        [Test]
        public void HaveCompatibleFormatItems_ComparesPlaceholderIndicesInsteadOfOrderOrFormatSpecifier()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CompendiumLocalization.HaveCompatibleFormatItems("{0} {1}", "{1:0.0} {0}"), Is.True);
                Assert.That(CompendiumLocalization.HaveCompatibleFormatItems("{0} {1}", "{0}"), Is.False);
            });
        }
    }
}