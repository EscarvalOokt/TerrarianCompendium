using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class BrowserUiState : UIState
    {
        private const int SectionTabsHeight = 24;
        private const int HistoryButtonSize = 24;
        private const int HistoryButtonGap = 2;
        private const int HistoryTabsGap = 6;
        private const int SectionTabGap = 2;
        private const int LayoutSpacing = 2;
        private const int DetailsGap = 8;
        private const int DetailsTopInset = 3;
        private const int ContentInset = 4;

        private readonly ArmorSetBrowserView _armorSetBrowserView;
        private readonly VanillaTextButton _armorSetsTab;
        private readonly MessageElement _armorSetsUnavailableState;
        private readonly VanillaTextButton _backButton;
        private readonly BestiaryBrowserView _bestiaryBrowserView;
        private readonly VanillaTextButton _bestiaryTab;
        private readonly MessageElement _bestiaryUnavailableState;

        private readonly BrowserContentLayout _contentLayout;
        private readonly BrowserDetailsSurface _detailsSurface;
        private readonly VanillaTextButton _forwardButton;
        private readonly ItemBrowserView _itemBrowserView;
        private readonly VanillaTextButton _itemsTab;
        private readonly CompendiumLocalization _localization;
        private readonly BrowserNavigationState _navigationState;
        private readonly VanillaBrowserPanel _panel;
        private readonly RecipeBrowserView _recipeBrowserView;
        private readonly VanillaTextButton _recipesTab;
        private readonly MessageElement _recipesUnavailableState;
        private readonly UIElement _sectionHost;
        private BrowserSection _activeSection;
        private UIElement _activeSectionContent;
        private bool _hasActiveSection;
        private bool _isActive;
        private long _localizationRevision = -1;

        public BrowserUiState(
            ItemBrowserView itemBrowserView,
            ArmorSetBrowserView armorSetBrowserView,
            RecipeBrowserView recipeBrowserView,
            BestiaryBrowserView bestiaryBrowserView,
            ItemDetailsView itemDetailsView,
            ArmorSetDetailsView armorSetDetailsView,
            RecipeDetailsView recipeDetailsView,
            NpcDetailsView npcDetailsView,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization,
            string title,
            int defaultWidth,
            int defaultHeight,
            int minimumWidth,
            int minimumHeight)
        {
            _itemBrowserView = itemBrowserView ?? throw new ArgumentNullException(nameof(itemBrowserView));
            _armorSetBrowserView = armorSetBrowserView;
            _recipeBrowserView = recipeBrowserView;
            _bestiaryBrowserView = bestiaryBrowserView;
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _detailsSurface = new BrowserDetailsSurface(
                navigationState,
                itemDetailsView ?? throw new ArgumentNullException(nameof(itemDetailsView)),
                armorSetDetailsView,
                recipeDetailsView,
                npcDetailsView,
                localization);

            _panel = new VanillaBrowserPanel(title, defaultWidth, defaultHeight, minimumWidth, minimumHeight);
            _panel.CloseRequested += () => CloseRequested?.Invoke();
            _panel.InteractionStarted += () => InteractionStarted?.Invoke();
            Append(_panel);

            _backButton = new VanillaTextButton("←", NavigateBack);
            _forwardButton = new VanillaTextButton("→", NavigateForward);
            _itemsTab = new VanillaTextButton(string.Empty, () => NavigateToSection(BrowserSection.Items));
            _armorSetsTab = new VanillaTextButton(string.Empty, () => NavigateToSection(BrowserSection.ArmorSets));
            _recipesTab = new VanillaTextButton(string.Empty, () => NavigateToSection(BrowserSection.Recipes));
            _bestiaryTab = new VanillaTextButton(string.Empty, () => NavigateToSection(BrowserSection.Bestiary));

            _sectionHost = new UIElement
            {
                OverflowHidden = true
            };
            _sectionHost.SetPadding(0f);

            _armorSetsUnavailableState = new MessageElement(string.Empty);
            _recipesUnavailableState = new MessageElement(string.Empty);
            _bestiaryUnavailableState = new MessageElement(string.Empty);

            _contentLayout = new BrowserContentLayout(
                _backButton,
                _forwardButton,
                _itemsTab,
                _armorSetsTab,
                _recipesTab,
                _bestiaryTab,
                _sectionHost,
                _detailsSurface)
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill
            };
            _contentLayout.SetPadding(0f);
            _panel.ContentRoot.Append(_contentLayout);

            SynchronizeLocalization(force: true);
            SynchronizeSection(force: true);
            SynchronizeHistoryButtons();
        }

        public Rectangle Bounds => _panel.Bounds;

        public bool IsWritingText =>
            _itemBrowserView.IsWritingText ||
            _armorSetBrowserView?.IsWritingText == true ||
            _recipeBrowserView?.IsWritingText == true ||
            _bestiaryBrowserView?.IsWritingText == true;

        public bool HasOpenTransientSurface
        {
            get
            {
                if (_detailsSurface.HasOpenTransientSurface)
                    return true;

                if (!_hasActiveSection)
                    return false;

                switch (_activeSection)
                {
                    case BrowserSection.Items:
                        return _itemBrowserView.HasOpenTransientSurface;

                    case BrowserSection.ArmorSets:
                        return _armorSetBrowserView?.HasOpenTransientSurface == true;

                    case BrowserSection.Recipes:
                        return _recipeBrowserView?.HasOpenTransientSurface == true;

                    case BrowserSection.Bestiary:
                        return _bestiaryBrowserView?.HasOpenTransientSurface == true;

                    default:
                        return false;
                }
            }
        }

        public bool TryCloseTransientSurface()
        {
            if (_detailsSurface.TryCloseTransientSurface())
                return true;

            if (!_hasActiveSection)
                return false;

            switch (_activeSection)
            {
                case BrowserSection.Items:
                    return _itemBrowserView.TryCloseTransientSurface();

                case BrowserSection.ArmorSets:
                    return _armorSetBrowserView?.TryCloseTransientSurface() == true;

                case BrowserSection.Recipes:
                    return _recipeBrowserView?.TryCloseTransientSurface() == true;

                case BrowserSection.Bestiary:
                    return _bestiaryBrowserView?.TryCloseTransientSurface() == true;

                default:
                    return false;
            }
        }

        public event Action CloseRequested;

        public event Action InteractionStarted;

        public void SetExternalInputBlocked(bool blocked)
        {
            _panel.SetExternalInputBlocked(blocked);
        }

        public override void OnActivate()
        {
            _isActive = true;
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            _isActive = false;
            _panel.SetExternalInputBlocked(false);
            base.OnDeactivate();
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            SynchronizeSection();
            SynchronizeHistoryButtons();
            base.Update(gameTime);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            long revision = _localization.Revision;
            if (!force && _localizationRevision == revision)
                return;

            _localizationRevision = revision;
            _backButton.TooltipText = _localization.Get(CompendiumTextKeys.Common.Back);
            _forwardButton.TooltipText = _localization.Get(CompendiumTextKeys.Common.Forward);
            _itemsTab.Text = _localization.Get(CompendiumTextKeys.Browser.ItemsSection);
            _armorSetsTab.Text = _localization.Get(CompendiumTextKeys.Browser.ArmorSetsSection);
            _recipesTab.Text = _localization.Get(CompendiumTextKeys.Browser.RecipesSection);
            _bestiaryTab.Text = _localization.Get(CompendiumTextKeys.Browser.BestiarySection);
            _panel.CloseButton.TooltipText = _localization.Get(CompendiumTextKeys.Common.Close);
            _armorSetsUnavailableState.SetText(_localization.Get(CompendiumTextKeys.Browser.ArmorSetsUnavailable));
            _recipesUnavailableState.SetText(_localization.Get(CompendiumTextKeys.Browser.RecipesUnavailable));
            _bestiaryUnavailableState.SetText(_localization.Get(CompendiumTextKeys.Browser.BestiaryUnavailable));
            _contentLayout.Recalculate();
        }

        private void NavigateBack()
        {
            if (!_navigationState.GoBack())
                return;

            SynchronizeSection();
            SynchronizeHistoryButtons();
        }

        private void NavigateForward()
        {
            if (!_navigationState.GoForward())
                return;

            SynchronizeSection();
            SynchronizeHistoryButtons();
        }

        private void NavigateToSection(BrowserSection section)
        {
            if (_navigationState.CurrentDestination.Section == section)
                return;

            _navigationState.Navigate(BrowserDestination.ForSection(section));
            SynchronizeSection();
            SynchronizeHistoryButtons();
        }

        private void SynchronizeHistoryButtons()
        {
            _backButton.IsEnabled = _navigationState.CanGoBack;
            _forwardButton.IsEnabled = _navigationState.CanGoForward;
        }

        private void SynchronizeSection(bool force = false)
        {
            BrowserSection section = _navigationState.CurrentDestination.Section;

            if (!force && _hasActiveSection && _activeSection == section)
            {
                SynchronizeTabState(section);
                return;
            }

            if (_hasActiveSection && _isActive && _activeSectionContent != null)
                _activeSectionContent.Deactivate();

            _sectionHost.RemoveAllChildren();
            UIElement content = GetSectionContent(section);
            content.Left.Set(0f, 0f);
            content.Top.Set(0f, 0f);
            content.Width.Set(0f, 1f);
            content.Height.Set(0f, 1f);
            _sectionHost.Append(content);

            if (_isActive)
                content.Activate();

            _activeSection = section;
            _activeSectionContent = content;
            _hasActiveSection = true;
            SynchronizeTabState(section);
            _contentLayout.Recalculate();
        }

        private UIElement GetSectionContent(BrowserSection section)
        {
            switch (section)
            {
                case BrowserSection.Items:
                    return _itemBrowserView;

                case BrowserSection.ArmorSets:
                    return _armorSetBrowserView != null ? _armorSetBrowserView : _armorSetsUnavailableState;

                case BrowserSection.Recipes:
                    return _recipeBrowserView != null ? _recipeBrowserView : _recipesUnavailableState;

                case BrowserSection.Bestiary:
                    return _bestiaryBrowserView != null ? _bestiaryBrowserView : _bestiaryUnavailableState;

                default:
                    throw new ArgumentOutOfRangeException(nameof(section), section, "Unsupported browser section.");
            }
        }

        private void SynchronizeTabState(BrowserSection section)
        {
            _itemsTab.IsActive = section == BrowserSection.Items;
            _armorSetsTab.IsActive = section == BrowserSection.ArmorSets;
            _recipesTab.IsActive = section == BrowserSection.Recipes;
            _bestiaryTab.IsActive = section == BrowserSection.Bestiary;
        }

        private sealed class BrowserContentLayout : UIElement
        {
            private readonly VanillaTextButton _backButton;
            private readonly BrowserDetailsSurface _detailsSurface;
            private readonly VanillaTextButton _forwardButton;
            private readonly UIElement _sectionHost;
            private readonly VanillaTextButton[] _tabs;

            public BrowserContentLayout(
                VanillaTextButton backButton,
                VanillaTextButton forwardButton,
                VanillaTextButton itemsTab,
                VanillaTextButton armorSetsTab,
                VanillaTextButton recipesTab,
                VanillaTextButton bestiaryTab,
                UIElement sectionHost,
                BrowserDetailsSurface detailsSurface)
            {
                _backButton = backButton ?? throw new ArgumentNullException(nameof(backButton));
                _forwardButton = forwardButton ?? throw new ArgumentNullException(nameof(forwardButton));
                _tabs =
                [
                    itemsTab ?? throw new ArgumentNullException(nameof(itemsTab)),
                    armorSetsTab ?? throw new ArgumentNullException(nameof(armorSetsTab)),
                    recipesTab ?? throw new ArgumentNullException(nameof(recipesTab)),
                    bestiaryTab ?? throw new ArgumentNullException(nameof(bestiaryTab))
                ];
                _sectionHost = sectionHost ?? throw new ArgumentNullException(nameof(sectionHost));
                _detailsSurface = detailsSurface ?? throw new ArgumentNullException(nameof(detailsSurface));
                SetPadding(0f);

                Append(_backButton);
                Append(_forwardButton);

                foreach (VanillaTextButton tab in _tabs)
                    Append(tab);

                Append(_sectionHost);
                Append(_detailsSurface);
            }

            public override void RecalculateChildren()
            {
                CalculatedStyle inner = GetInnerDimensions();
                int width = Math.Max(0, (int)inner.Width);
                int height = Math.Max(0, (int)inner.Height);

                int contentX = ContentInset;
                int contentY = ContentInset;
                int contentWidth = Math.Max(0, width - ContentInset * 2);
                LayoutElement(_backButton, contentX, contentY, HistoryButtonSize, HistoryButtonSize);
                LayoutElement(
                    _forwardButton,
                    contentX + HistoryButtonSize + HistoryButtonGap,
                    contentY,
                    HistoryButtonSize,
                    HistoryButtonSize);

                int tabX = contentX + HistoryButtonSize * 2 + HistoryButtonGap + HistoryTabsGap;
                int tabRight = contentX + contentWidth;
                int tabContentWidth = Math.Max(0, tabRight - tabX);
                int totalTabGap = Math.Max(0, _tabs.Length - 1) * SectionTabGap;
                int tabButtonsWidth = Math.Max(0, tabContentWidth - totalTabGap);
                int tabBaseWidth = _tabs.Length == 0 ? 0 : tabButtonsWidth / _tabs.Length;

                for (var index = 0; index < _tabs.Length; index++)
                {
                    int tabWidth = index == _tabs.Length - 1 ? Math.Max(0, tabRight - tabX) : tabBaseWidth;
                    LayoutElement(_tabs[index], tabX, contentY, tabWidth, SectionTabsHeight);
                    tabX += tabWidth;

                    if (index < _tabs.Length - 1)
                        tabX += SectionTabGap;
                }

                contentY += SectionTabsHeight + LayoutSpacing;
                int contentHeight = Math.Max(0, height - contentY - ContentInset);
                int detailsWidth = BrowserShell.CalculateDetailsWidth(contentWidth);
                int detailsGap = Math.Min(DetailsGap, Math.Max(0, contentWidth - detailsWidth));
                int sectionWidth = Math.Max(0, contentWidth - detailsGap - detailsWidth);
                int detailsHeight = Math.Max(0, contentHeight - DetailsTopInset);

                LayoutElement(_sectionHost, contentX, contentY, sectionWidth, contentHeight);
                LayoutElement(
                    _detailsSurface,
                    contentX + sectionWidth + detailsGap,
                    contentY + DetailsTopInset,
                    detailsWidth,
                    detailsHeight);

                base.RecalculateChildren();
            }

            private static void LayoutElement(UIElement element, int x, int y, int width, int height)
            {
                element.Left.Set(x, 0f);
                element.Top.Set(y, 0f);
                element.Width.Set(Math.Max(0, width), 0f);
                element.Height.Set(Math.Max(0, height), 0f);
            }
        }

        private sealed class MessageElement : UIElement
        {
            private const int TextHeight = 16;
            private string _text;

            public MessageElement(string text)
            {
                SetText(text);
                IgnoresMouseInteraction = true;
            }

            public void SetText(string text)
            {
                _text = text ?? string.Empty;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);
                string display = TextUtil.Truncate(_text, width);

                if (display.Length == 0)
                    return;

                int textWidth = TextUtil.MeasureWidth(display);
                int x = (int)dimensions.X + Math.Max(0, (width - textWidth) / 2);
                int y = (int)dimensions.Y + Math.Max(0, (height - TextHeight) / 2);
                UIRenderer.DrawText(display, x, y, UIColors.TextDim);
            }
        }
    }
}