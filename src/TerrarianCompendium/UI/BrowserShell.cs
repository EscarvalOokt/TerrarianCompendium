using System;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class BrowserShell
    {
        private const int DefaultPanelWidth = 880;
        private const int DefaultPanelHeight = 480;
        private const int MinimumPanelWidth = 720;
        private const int MinimumPanelHeight = 360;
        private const int DetailsWidth = 230;

        private readonly VanillaUiHost _host;
        private readonly BrowserNavigationState _navigationState;

        public BrowserShell(
            ItemBrowserView itemBrowserView,
            ArmorSetBrowserView armorSetBrowserView,
            RecipeBrowserView recipeBrowserView,
            ShimmerBrowserView shimmerBrowserView,
            BestiaryBrowserView bestiaryBrowserView,
            AnglerBrowserView anglerBrowserView,
            ItemDetailsView itemDetailsView,
            ArmorSetDetailsView armorSetDetailsView,
            RecipeDetailsView recipeDetailsView,
            ShimmerDetailsView shimmerDetailsView,
            NpcDetailsView npcDetailsView,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            if (itemBrowserView == null)
                throw new ArgumentNullException(nameof(itemBrowserView));

            if (itemDetailsView == null)
                throw new ArgumentNullException(nameof(itemDetailsView));

            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _host = new VanillaUiHost(
                TerrarianCompendiumMod.ModId,
                localization,
                () => new BrowserUiState(
                    itemBrowserView,
                    armorSetBrowserView,
                    recipeBrowserView,
                    shimmerBrowserView,
                    bestiaryBrowserView,
                    anglerBrowserView,
                    itemDetailsView,
                    armorSetDetailsView,
                    recipeDetailsView,
                    shimmerDetailsView,
                    npcDetailsView,
                    navigationState,
                    localization,
                    TerrarianCompendiumMod.ModName,
                    DefaultPanelWidth,
                    DefaultPanelHeight,
                    MinimumPanelWidth,
                    MinimumPanelHeight));
        }

        public void Register()
        {
            _host.Register();
        }

        public void BeginUpdateFrame()
        {
            _host.BeginUpdateFrame();
        }

        public void HandleThirdPartySoftwareTick()
        {
            _host.HandleThirdPartySoftwareTick();
        }

        public void Update()
        {
            _host.Update();
        }

        public void Toggle()
        {
            _host.Toggle();
        }

        public void OpenItem(int itemId)
        {
            _navigationState.Navigate(BrowserDestination.ForItem(itemId));

            if (!_host.IsOpen)
                _host.Toggle();
        }

        public void OpenRecipeQuery(int itemId)
        {
            _navigationState.Navigate(BrowserDestination.ForRecipeQuery(itemId));

            if (!_host.IsOpen)
                _host.Toggle();
        }

        public void Close()
        {
            _host.Close();
        }

        public void Unregister()
        {
            _host.Unregister();
        }

        internal static int CalculateDetailsWidth(int availableWidth)
        {
            return Math.Min(DetailsWidth, Math.Max(0, availableWidth));
        }
    }
}