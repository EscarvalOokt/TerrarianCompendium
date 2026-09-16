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

        public BrowserShell(
            ItemBrowserView itemBrowserView,
            ArmorSetBrowserView armorSetBrowserView,
            RecipeBrowserView recipeBrowserView,
            BestiaryBrowserView bestiaryBrowserView,
            ItemDetailsView itemDetailsView,
            ArmorSetDetailsView armorSetDetailsView,
            RecipeDetailsView recipeDetailsView,
            NpcDetailsView npcDetailsView,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            if (itemBrowserView == null)
                throw new ArgumentNullException(nameof(itemBrowserView));

            if (itemDetailsView == null)
                throw new ArgumentNullException(nameof(itemDetailsView));

            if (navigationState == null)
                throw new ArgumentNullException(nameof(navigationState));

            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            _host = new VanillaUiHost(
                TerrarianCompendiumMod.ModId,
                () => new BrowserUiState(
                    itemBrowserView,
                    armorSetBrowserView,
                    recipeBrowserView,
                    bestiaryBrowserView,
                    itemDetailsView,
                    armorSetDetailsView,
                    recipeDetailsView,
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