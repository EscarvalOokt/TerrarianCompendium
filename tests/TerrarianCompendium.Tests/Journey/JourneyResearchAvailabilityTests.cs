using NUnit.Framework;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Tests.Journey
{
    [TestFixture]
    public sealed class JourneyResearchAvailabilityTests
    {
        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, true)]
        public void IsAvailable_RequiresJourneyWorldAndJourneyCharacter(
            bool worldIsJourney,
            bool characterIsJourney,
            bool expected)
        {
            Assert.That(
                JourneyResearchAvailability.IsAvailable(worldIsJourney, characterIsJourney),
                Is.EqualTo(expected));
        }
    }
}