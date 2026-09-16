using System;
using NUnit.Framework;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class CycleSelectorTests
    {
        [Test]
        public void GetNextIndex_FromMiddle_ReturnsNextIndex()
        {
            Assert.That(CycleSelectorIndex.GetNextIndex(1, 3), Is.EqualTo(2));
        }

        [Test]
        public void GetNextIndex_FromLast_WrapsToFirst()
        {
            Assert.That(CycleSelectorIndex.GetNextIndex(2, 3), Is.Zero);
        }

        [Test]
        public void GetPreviousIndex_FromMiddle_ReturnsPreviousIndex()
        {
            Assert.That(CycleSelectorIndex.GetPreviousIndex(1, 3), Is.Zero);
        }

        [Test]
        public void GetPreviousIndex_FromFirst_WrapsToLast()
        {
            Assert.That(CycleSelectorIndex.GetPreviousIndex(0, 3), Is.EqualTo(2));
        }

        [Test]
        public void SingleOption_AlwaysReturnsOnlyIndex()
        {
            Assert.That(CycleSelectorIndex.GetNextIndex(0, 1), Is.Zero);
            Assert.That(CycleSelectorIndex.GetPreviousIndex(0, 1), Is.Zero);
        }

        [Test]
        public void GetNextIndex_WithEmptyOptions_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => CycleSelectorIndex.GetNextIndex(0, 0)));
        }

        [Test]
        public void GetPreviousIndex_WithIndexOutsideRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => CycleSelectorIndex.GetPreviousIndex(3, 3)));
        }
    }
}