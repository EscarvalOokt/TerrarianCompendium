using NUnit.Framework;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Catalog
{
    [TestFixture]
    public sealed class VanillaItemCatalogInclusionPolicyTests
    {
        [Test]
        public void ShouldInclude_WithValidDefinition_ReturnsTrue()
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                42,
                42,
                "Valid Item",
                isDeprecated: false,
                shouldNotBeInInventory: false);

            Assert.That(result, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ShouldInclude_WithNonPositiveRequestedItemId_ReturnsFalse(int requestedItemId)
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                requestedItemId,
                requestedItemId,
                "Invalid Item",
                isDeprecated: false,
                shouldNotBeInInventory: false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldInclude_WithDeprecatedDefinition_ReturnsFalse()
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                42,
                42,
                "Deprecated Item",
                isDeprecated: true,
                shouldNotBeInInventory: false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldInclude_WithItemThatShouldNotBeInInventory_ReturnsFalse()
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                42,
                42,
                "Internal Item",
                isDeprecated: false,
                shouldNotBeInInventory: true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldInclude_WithBothNativeExclusionFlags_ReturnsFalse()
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                42,
                42,
                "Excluded Item",
                isDeprecated: true,
                shouldNotBeInInventory: true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldInclude_WithResolvedIdentityMismatch_ReturnsFalse()
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                42,
                41,
                "Mismatched Item",
                isDeprecated: false,
                shouldNotBeInInventory: false);

            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ShouldInclude_WithMissingName_ReturnsFalse(string name)
        {
            bool result = VanillaItemCatalogInclusionPolicy.ShouldInclude(
                42,
                42,
                name,
                isDeprecated: false,
                shouldNotBeInInventory: false);

            Assert.That(result, Is.False);
        }
    }
}