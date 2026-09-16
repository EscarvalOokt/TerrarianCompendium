namespace TerrarianCompendium.Journey
{
    internal static class JourneyResearchAvailability
    {
        public static bool IsAvailable(bool worldIsJourney, bool characterIsJourney)
        {
            return worldIsJourney && characterIsJourney;
        }
    }
}