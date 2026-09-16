using System;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeCatalogTests
    {
        [Test]
        public void Create_WithValidEntries_BuildsCatalog()
        {
            RecipeCatalogEntry first = CreateEntry(0, 1);
            RecipeCatalogEntry second = CreateEntry(1, 2);

            var catalog = RecipeCatalog.Create(
                new[]
                {
                    first,
                    second
                });

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.Recipes[0], Is.SameAs(first));
            Assert.That(catalog.Recipes[1], Is.SameAs(second));
        }

        [Test]
        public void Create_WithUnorderedEntries_OrdersByRuntimeIndex()
        {
            var catalog = RecipeCatalog.Create(
                new[]
                {
                    CreateEntry(2, 3),
                    CreateEntry(0, 1),
                    CreateEntry(1, 2)
                });

            Assert.That(catalog.Recipes[0].RuntimeIndex, Is.EqualTo(0));
            Assert.That(catalog.Recipes[1].RuntimeIndex, Is.EqualTo(1));
            Assert.That(catalog.Recipes[2].RuntimeIndex, Is.EqualTo(2));
        }

        [Test]
        public void Create_WithDuplicateRuntimeIndex_Throws()
        {
            RecipeCatalogEntry[] entries =
            {
                CreateEntry(0, 1),
                CreateEntry(0, 2)
            };

            Assert.Throws<ArgumentException>((Action)(() => { RecipeCatalog.Create(entries); }));
        }

        [Test]
        public void Create_WithNullEntry_Throws()
        {
            RecipeCatalogEntry[] entries =
            {
                CreateEntry(0, 1),
                null
            };

            Assert.Throws<ArgumentException>((Action)(() => { RecipeCatalog.Create(entries); }));
        }

        [Test]
        public void Create_CopiesInputCollection()
        {
            RecipeCatalogEntry first = CreateEntry(0, 1);
            RecipeCatalogEntry second = CreateEntry(1, 2);
            RecipeCatalogEntry[] entries = { first };

            var catalog = RecipeCatalog.Create(entries);
            entries[0] = second;

            Assert.That(catalog.Recipes[0], Is.SameAs(first));
        }

        [Test]
        public void Contains_ReturnsExpectedResult()
        {
            var catalog = RecipeCatalog.Create(
                new[]
                {
                    CreateEntry(0, 1),
                    CreateEntry(2, 3)
                });

            Assert.That(catalog.Contains(0), Is.True);
            Assert.That(catalog.Contains(1), Is.False);
        }

        [Test]
        public void TryGet_WithExistingRuntimeIndex_ReturnsMatchingEntry()
        {
            RecipeCatalogEntry expectedEntry = CreateEntry(2, 3);

            var catalog = RecipeCatalog.Create(
                new[]
                {
                    CreateEntry(0, 1),
                    expectedEntry
                });

            bool found = catalog.TryGet(2, out RecipeCatalogEntry actualEntry);

            Assert.That(found, Is.True);
            Assert.That(actualEntry, Is.SameAs(expectedEntry));
        }

        [Test]
        public void TryGet_WithMissingRuntimeIndex_ReturnsFalse()
        {
            var catalog = RecipeCatalog.Create(
                new[]
                {
                    CreateEntry(0, 1)
                });

            bool found = catalog.TryGet(1, out RecipeCatalogEntry entry);

            Assert.That(found, Is.False);
            Assert.That(entry, Is.Null);
        }

        private static RecipeCatalogEntry CreateEntry(int runtimeIndex, int resultItemId)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                Array.Empty<RecipeIngredient>(),
                new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                isAlchemy: false);
        }
    }
}