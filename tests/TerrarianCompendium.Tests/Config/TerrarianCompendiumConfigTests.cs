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
        public void Version_IsTwo()
        {
            var config = new TerrarianCompendiumConfig();

            Assert.That(config.Version, Is.EqualTo(2));
        }
    }
}