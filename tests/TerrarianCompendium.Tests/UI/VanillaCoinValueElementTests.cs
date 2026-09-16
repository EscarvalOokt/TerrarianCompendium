using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class VanillaCoinValueElementTests
    {
        [Test]
        public void Decompose_MixedValue_ReturnsPlatinumGoldSilverCopper()
        {
            var breakdown = VanillaCoinValueBreakdown.Decompose(1_020_304);

            Assert.Multiple(() =>
            {
                Assert.That(breakdown.Platinum, Is.EqualTo(1));
                Assert.That(breakdown.Gold, Is.EqualTo(2));
                Assert.That(breakdown.Silver, Is.EqualTo(3));
                Assert.That(breakdown.Copper, Is.EqualTo(4));
            });
        }

        [Test]
        public void Decompose_IntermediateZeroDenominations_RemainZero()
        {
            var breakdown = VanillaCoinValueBreakdown.Decompose(1_000_050);

            Assert.Multiple(() =>
            {
                Assert.That(breakdown.Platinum, Is.EqualTo(1));
                Assert.That(breakdown.Gold, Is.EqualTo(0));
                Assert.That(breakdown.Silver, Is.EqualTo(0));
                Assert.That(breakdown.Copper, Is.EqualTo(50));
            });
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Decompose_NonPositiveValue_NormalizesToZero(int value)
        {
            var breakdown = VanillaCoinValueBreakdown.Decompose(value);

            Assert.Multiple(() =>
            {
                Assert.That(breakdown.Platinum, Is.EqualTo(0));
                Assert.That(breakdown.Gold, Is.EqualTo(0));
                Assert.That(breakdown.Silver, Is.EqualTo(0));
                Assert.That(breakdown.Copper, Is.EqualTo(0));
            });
        }
    }
}