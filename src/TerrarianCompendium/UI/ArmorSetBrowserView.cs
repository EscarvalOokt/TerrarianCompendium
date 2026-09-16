using System;
using Microsoft.Xna.Framework;
using Terraria.UI;
using TerrarianCompendium.ArmorSets;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ArmorSetBrowserView : UIElement
    {
        private readonly VirtualArmorSetGrid _grid;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaScrollRegion _scroll;
        private long _localizationRevision = -1;

        public ArmorSetBrowserView(
            ArmorSetCatalog catalog,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            BrowserNavigationState resolvedNavigationState = navigationState ??
                                                             throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            SetPadding(0f);

            _scroll = new VanillaScrollRegion();
            Append(_scroll);

            _grid = new VirtualArmorSetGrid(
                _scroll,
                () => catalog.Entries,
                armorSetId => BrowserSelectionNavigation.ToggleArmorSet(resolvedNavigationState, armorSetId),
                armorSetId => BrowserSelectionNavigation.IsArmorSetSelected(
                    resolvedNavigationState.CurrentDestination,
                    armorSetId),
                string.Empty);
            _scroll.Content.Append(_grid);
            SynchronizeLocalization(force: true);
        }

        public bool IsWritingText => false;

        public bool HasOpenTransientSurface => false;

        public bool TryCloseTransientSurface()
        {
            return false;
        }


        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            base.Update(gameTime);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _grid.EmptyStateText = _localization.Get(CompendiumTextKeys.ArmorSets.EmptyState);
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            _scroll.Left.Set(0f, 0f);
            _scroll.Top.Set(0f, 0f);
            _scroll.Width.Set(Math.Max(0, dimensions.Width), 0f);
            _scroll.Height.Set(Math.Max(0, dimensions.Height), 0f);
            base.RecalculateChildren();
        }
    }
}