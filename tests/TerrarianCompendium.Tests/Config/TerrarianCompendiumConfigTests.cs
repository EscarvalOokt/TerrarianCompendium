using NUnit.Framework;
using TerrarianCompendium.Config;

namespace TerrarianCompendium.Tests.Config
{
    [TestFixture]
    public sealed class TerrarianCompendiumConfigTests
    {
        [Test]
        public void NewConfig_StorageDiscoveryIsEnabledByDefault()
        {
            var config = new TerrarianCompendiumConfig();

            Assert.That(config.StorageDiscoveryEnabled, Is.True);
        }

        [Test]
        public void NewConfig_InventoryItemNavigationIsEnabledByDefault()
        {
            var config = new TerrarianCompendiumConfig();

            Assert.That(config.InventoryItemNavigationEnabled, Is.True);
        }

        [Test]
        public void InventoryItemNavigation_CanBeDisabledIndependently()
        {
            var config = new TerrarianCompendiumConfig
            {
                InventoryItemNavigationEnabled = false
            };

            Assert.That(config.InventoryItemNavigationEnabled, Is.False);
            Assert.That(config.StorageDiscoveryEnabled, Is.True);
        }

        [Test]
        public void Version_IsThree()
        {
            var config = new TerrarianCompendiumConfig();

            Assert.That(config.Version, Is.EqualTo(3));
        }
    }
}