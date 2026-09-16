using System;

namespace TerrarianCompendium.Navigation
{
    internal static class BrowserSelectionNavigation
    {
        public static bool IsItemSelected(BrowserDestination destination, int itemId)
        {
            return destination.IsItem && destination.ItemId == itemId;
        }

        public static bool IsArmorSetSelected(BrowserDestination destination, int armorSetId)
        {
            return destination.IsArmorSet && destination.ArmorSetId == armorSetId;
        }

        public static bool IsRecipeResultSelected(BrowserDestination destination, int itemId)
        {
            return destination is { Section: BrowserSection.Recipes, HasRecipeQuery: true } &&
                   destination.RecipeQueryItemId == itemId;
        }

        public static bool IsNpcSelected(BrowserDestination destination, int npcNetId)
        {
            return destination.IsNpc && destination.NpcNetId == npcNetId;
        }

        public static bool ToggleItem(BrowserNavigationState navigationState, int itemId)
        {
            if (navigationState == null)
                throw new ArgumentNullException(nameof(navigationState));

            BrowserDestination destination = IsItemSelected(navigationState.CurrentDestination, itemId)
                ? BrowserDestination.ForSection(BrowserSection.Items)
                : BrowserDestination.ForItem(itemId);

            return navigationState.Navigate(destination);
        }

        public static bool ToggleArmorSet(BrowserNavigationState navigationState, int armorSetId)
        {
            if (navigationState == null)
                throw new ArgumentNullException(nameof(navigationState));

            BrowserDestination destination = IsArmorSetSelected(navigationState.CurrentDestination, armorSetId)
                ? BrowserDestination.ForSection(BrowserSection.ArmorSets)
                : BrowserDestination.ForArmorSet(armorSetId);

            return navigationState.Navigate(destination);
        }

        public static bool ToggleRecipeResult(BrowserNavigationState navigationState, int itemId)
        {
            if (navigationState == null)
                throw new ArgumentNullException(nameof(navigationState));

            BrowserDestination destination = IsRecipeResultSelected(navigationState.CurrentDestination, itemId)
                ? BrowserDestination.ForSection(BrowserSection.Recipes)
                : BrowserDestination.ForRecipeQuery(itemId);

            return navigationState.Navigate(destination);
        }

        public static bool ToggleNpc(BrowserNavigationState navigationState, int npcNetId)
        {
            if (navigationState == null)
                throw new ArgumentNullException(nameof(navigationState));

            BrowserDestination destination = IsNpcSelected(navigationState.CurrentDestination, npcNetId)
                ? BrowserDestination.ForSection(BrowserSection.Bestiary)
                : BrowserDestination.ForNpc(npcNetId);

            return navigationState.Navigate(destination);
        }
    }
}