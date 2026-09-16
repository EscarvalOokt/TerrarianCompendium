using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.Tests.Bestiary
{
    [TestFixture]
    public sealed class NpcCatalogTests
    {
        [Test]
        public void Create_WithValidEntries_OrdersByBestiaryOrder()
        {
            var third = new NpcCatalogEntry(30, 3);
            var first = new NpcCatalogEntry(10, 1);
            var second = new NpcCatalogEntry(20, 2);

            var catalog = NpcCatalog.Create([third, first, second]);

            Assert.That(catalog.Count, Is.EqualTo(3));
            Assert.That(catalog.Entries[0], Is.SameAs(first));
            Assert.That(catalog.Entries[1], Is.SameAs(second));
            Assert.That(catalog.Entries[2], Is.SameAs(third));
        }

        [Test]
        public void Create_WithNegativeNetId_PreservesEntry()
        {
            var entry = new NpcCatalogEntry(-65, 0);

            var catalog = NpcCatalog.Create([entry]);

            Assert.That(catalog.Contains(-65), Is.True);
            Assert.That(catalog.TryGet(-65, out NpcCatalogEntry actualEntry), Is.True);
            Assert.That(actualEntry, Is.SameAs(entry));
        }

        [Test]
        public void Create_WithDuplicateNetId_Throws()
        {
            NpcCatalogEntry[] entries =
            [
                new NpcCatalogEntry(10, 0),
                new NpcCatalogEntry(10, 1)
            ];

            Assert.Throws<ArgumentException>((Action)(() => { NpcCatalog.Create(entries); }));
        }

        [Test]
        public void Create_WithDuplicateBestiaryOrder_Throws()
        {
            NpcCatalogEntry[] entries =
            [
                new NpcCatalogEntry(10, 0),
                new NpcCatalogEntry(20, 0)
            ];

            Assert.Throws<ArgumentException>((Action)(() => { NpcCatalog.Create(entries); }));
        }

        [Test]
        public void Create_CopiesInputCollection()
        {
            var entries = new List<NpcCatalogEntry>
            {
                new NpcCatalogEntry(10, 0)
            };

            var catalog = NpcCatalog.Create(entries);
            entries.Add(new NpcCatalogEntry(20, 1));

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.Contains(20), Is.False);
        }

        [Test]
        public void TryGet_WithExistingNetId_ReturnsMatchingEntry()
        {
            var expected = new NpcCatalogEntry(20, 1);
            var catalog = NpcCatalog.Create(
            [
                new NpcCatalogEntry(10, 0),
                expected
            ]);

            bool found = catalog.TryGet(20, out NpcCatalogEntry actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void TryGet_WithMissingNetId_ReturnsFalse()
        {
            var catalog = NpcCatalog.Create(
            [
                new NpcCatalogEntry(10, 0)
            ]);

            bool found = catalog.TryGet(20, out NpcCatalogEntry entry);

            Assert.That(found, Is.False);
            Assert.That(entry, Is.Null);
        }

        [Test]
        public void NpcCatalogEntry_WithNegativeBestiaryOrder_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => { _ = new NpcCatalogEntry(10, -1); }));
        }
    }
}