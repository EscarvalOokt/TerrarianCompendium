using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Catalog
{
    [TestFixture]
    public sealed class ItemTextIndexTests
    {
        [Test]
        public void Constructor_WithNullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { _ = new ItemTextIndex(null); }));
        }

        [Test]
        public void NewIndex_UsesCatalogNamesEmptyDescriptionsAndZeroRevision()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            var index = new ItemTextIndex(catalog);

            Assert.That(index.CultureName, Is.Empty);
            Assert.That(index.Revision, Is.Zero);
            Assert.That(index.GetName(1), Is.EqualTo("First"));
            Assert.That(index.GetName(2), Is.EqualTo("Second"));
            Assert.That(index.GetDescription(1), Is.Empty);
            Assert.That(index.GetDescription(2), Is.Empty);
            Assert.That(index.MatchesDescription(1, "anything"), Is.False);
        }

        [Test]
        public void GetName_WithUnknownItemId_ReturnsEmptyString()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);

            Assert.That(index.GetName(999), Is.Empty);
        }

        [Test]
        public void GetDescription_WithUnknownItemId_ReturnsEmptyString()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);

            Assert.That(index.GetDescription(999), Is.Empty);
        }

        [Test]
        public void ReplaceSnapshot_UpdatesDescriptionAccessorAndRevision()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);

            index.ReplaceSnapshot(
                "en-US",
                new Dictionary<int, string> { [1] = "Localized" },
                new Dictionary<int, string> { [1] = "Line one\nLine two" });

            Assert.That(index.Revision, Is.EqualTo(1));
            Assert.That(index.GetDescription(1), Is.EqualTo("Line one\nLine two"));
        }

        [Test]
        public void MatchesDescription_IsCaseInsensitive()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Hermes Boots"));
            var index = new ItemTextIndex(catalog);
            index.ReplaceSnapshot(
                "en-US",
                new Dictionary<int, string> { [1] = "Hermes Boots" },
                new Dictionary<int, string> { [1] = "The wearer can run super fast" });

            Assert.That(index.MatchesDescription(1, "SuPeR FaSt"), Is.True);
        }

        [Test]
        public void MatchesDescription_WithUnknownItemId_ReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);

            Assert.That(index.MatchesDescription(999, "First"), Is.False);
        }

        [Test]
        public void MatchesDescription_WithEmptyQuery_ReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);
            index.ReplaceSnapshot(
                "en-US",
                new Dictionary<int, string> { [1] = "First" },
                new Dictionary<int, string> { [1] = "Description" });

            Assert.That(index.MatchesDescription(1, string.Empty), Is.False);
        }

        [Test]
        public void ReplaceSnapshot_UpdatesNamesDescriptionsCultureAndRevisionTogether()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Fallback"));
            var index = new ItemTextIndex(catalog);

            index.ReplaceSnapshot(
                "en-US",
                new Dictionary<int, string> { [1] = "First name" },
                new Dictionary<int, string> { [1] = "First description" });
            long firstRevision = index.Revision;

            index.ReplaceSnapshot(
                "fr-FR",
                new Dictionary<int, string> { [1] = "Second name" },
                new Dictionary<int, string> { [1] = "Second description" });

            Assert.That(firstRevision, Is.EqualTo(1));
            Assert.That(index.Revision, Is.EqualTo(2));
            Assert.That(index.CultureName, Is.EqualTo("fr-FR"));
            Assert.That(index.GetName(1), Is.EqualTo("Second name"));
            Assert.That(index.MatchesDescription(1, "Second description"), Is.True);
            Assert.That(index.MatchesDescription(1, "First description"), Is.False);
        }

        [Test]
        public void ReplaceSnapshot_WithMissingName_ThrowsWithoutChangingExistingSnapshot()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            ItemTextIndex index = CreatePopulatedIndex(catalog);
            long revision = index.Revision;

            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot(
                        "fr-FR",
                        new Dictionary<int, string> { [1] = "Changed" },
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first description",
                            [2] = "Changed second description"
                        });
                }));

            AssertOriginalSnapshot(index, revision);
        }

        [Test]
        public void ReplaceSnapshot_WithMissingDescription_ThrowsWithoutChangingExistingSnapshot()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            ItemTextIndex index = CreatePopulatedIndex(catalog);
            long revision = index.Revision;

            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot(
                        "fr-FR",
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first",
                            [2] = "Changed second"
                        },
                        new Dictionary<int, string> { [1] = "Changed" });
                }));

            AssertOriginalSnapshot(index, revision);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ReplaceSnapshot_WithInvalidName_ThrowsWithoutChangingExistingSnapshot(string invalidName)
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            ItemTextIndex index = CreatePopulatedIndex(catalog);
            long revision = index.Revision;

            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot(
                        "fr-FR",
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first",
                            [2] = invalidName
                        },
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first description",
                            [2] = "Changed second description"
                        });
                }));

            AssertOriginalSnapshot(index, revision);
        }

        [Test]
        public void ReplaceSnapshot_WithNullDescription_ThrowsWithoutChangingExistingSnapshot()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            ItemTextIndex index = CreatePopulatedIndex(catalog);
            long revision = index.Revision;

            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot(
                        "fr-FR",
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first",
                            [2] = "Changed second"
                        },
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first description",
                            [2] = null
                        });
                }));

            AssertOriginalSnapshot(index, revision);
        }

        [Test]
        public void ReplaceSnapshot_WithNullNames_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot("en-US", null, new Dictionary<int, string> { [1] = string.Empty });
                }));
        }

        [Test]
        public void ReplaceSnapshot_WithNullDescriptions_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var index = new ItemTextIndex(catalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot("en-US", new Dictionary<int, string> { [1] = "First" }, null);
                }));
        }

        [Test]
        public void ReplaceSnapshot_WithInvalidCulture_ThrowsWithoutChangingExistingSnapshot()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"), new ItemCatalogEntry(2, "Second"));
            ItemTextIndex index = CreatePopulatedIndex(catalog);
            long revision = index.Revision;

            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    index.ReplaceSnapshot(
                        "   ",
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first",
                            [2] = "Changed second"
                        },
                        new Dictionary<int, string>
                        {
                            [1] = "Changed first description",
                            [2] = "Changed second description"
                        });
                }));

            AssertOriginalSnapshot(index, revision);
        }

        private static ItemTextIndex CreatePopulatedIndex(ItemCatalog catalog)
        {
            var index = new ItemTextIndex(catalog);
            index.ReplaceSnapshot(
                "en-US",
                new Dictionary<int, string>
                {
                    [1] = "First localized",
                    [2] = "Second localized"
                },
                new Dictionary<int, string>
                {
                    [1] = "First description",
                    [2] = "Second description"
                });

            return index;
        }

        private static void AssertOriginalSnapshot(ItemTextIndex index, long revision)
        {
            Assert.That(index.Revision, Is.EqualTo(revision));
            Assert.That(index.CultureName, Is.EqualTo("en-US"));
            Assert.That(index.GetName(1), Is.EqualTo("First localized"));
            Assert.That(index.GetName(2), Is.EqualTo("Second localized"));
            Assert.That(index.GetDescription(1), Is.EqualTo("First description"));
            Assert.That(index.GetDescription(2), Is.EqualTo("Second description"));
            Assert.That(index.MatchesDescription(1, "First description"), Is.True);
            Assert.That(index.MatchesDescription(2, "Second description"), Is.True);
        }

        private static ItemCatalog CreateCatalog(params ItemCatalogEntry[] entries)
        {
            return ItemCatalog.Create(entries);
        }
    }
}