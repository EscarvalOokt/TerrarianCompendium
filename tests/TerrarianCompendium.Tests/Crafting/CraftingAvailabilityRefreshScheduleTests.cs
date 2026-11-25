using NUnit.Framework;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Navigation;

namespace TerrarianCompendium.Tests.Crafting
{
    [TestFixture]
    public sealed class CraftingAvailabilityRefreshScheduleTests
    {
        [Test]
        public void Update_RefreshesOnNewDemandAndAtTenTickIntervals()
        {
            var calls = 0;
            var schedule = new CraftingAvailabilityRefreshSchedule(() => calls++);
            var items = BrowserDestination.ForSection(BrowserSection.Items);
            var item = BrowserDestination.ForItem(1);
            var recipe = BrowserDestination.ForRecipe(0);

            schedule.Update(50, items, false, true);
            Assert.That(calls, Is.Zero);
            schedule.Update(51, items, true, false);
            schedule.Update(51, items, true, false);
            schedule.Update(60, items, true, false);
            Assert.That(calls, Is.EqualTo(1));
            schedule.Update(61, items, true, false);
            Assert.That(calls, Is.EqualTo(2));

            schedule.Update(62, item, false, false);
            schedule.Update(62, item, true, false);
            schedule.Update(62, recipe, true, false);
            Assert.That(calls, Is.EqualTo(5));
            schedule.Update(63, items, false, false);
            schedule.Update(64, recipe, false, false);
            Assert.That(calls, Is.EqualTo(6));
            schedule.Reset();
            schedule.Update(64, recipe, false, false);
            Assert.That(calls, Is.EqualTo(7));
        }
    }
}