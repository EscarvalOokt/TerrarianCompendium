using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class BrowserDetailsSurface : UIElement
    {
        private const int HeaderHeight = 24;
        private const int HeaderHorizontalPadding = 6;
        private const int BodyPadding = 6;
        private const int TextHeight = 16;

        private readonly ArmorSetDetailsView _armorSetDetailsView;
        private readonly MessageElement _armorSetUnavailableState;

        private readonly MessageElement _emptyState;
        private readonly ItemDetailsView _itemDetailsView;
        private readonly CompendiumLocalization _localization;
        private readonly BrowserNavigationState _navigationState;
        private readonly NpcDetailsView _npcDetailsView;
        private readonly MessageElement _npcUnavailableState;
        private readonly RecipeDetailsView _recipeDetailsView;
        private readonly MessageElement _recipeUnavailableState;
        private readonly VanillaScrollRegion _scroll;
        private UIElement _activeContent;
        private bool _isActive;
        private long _observedLocalizationRevision = -1;
        private long _observedNavigationRevision = -1;

        public BrowserDetailsSurface(
            BrowserNavigationState navigationState,
            ItemDetailsView itemDetailsView,
            ArmorSetDetailsView armorSetDetailsView,
            RecipeDetailsView recipeDetailsView,
            NpcDetailsView npcDetailsView,
            CompendiumLocalization localization)
        {
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _itemDetailsView = itemDetailsView ?? throw new ArgumentNullException(nameof(itemDetailsView));
            _armorSetDetailsView = armorSetDetailsView;
            _recipeDetailsView = recipeDetailsView;
            _npcDetailsView = npcDetailsView;
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            SetPadding(0f);

            _scroll = new VanillaScrollRegion
            {
                Left = new StyleDimension(1f, 0f),
                Top = new StyleDimension(HeaderHeight, 0f),
                Width = new StyleDimension(-2f, 1f),
                Height = new StyleDimension(-(HeaderHeight + 1f), 1f)
            };
            Append(_scroll);

            _emptyState = new MessageElement(string.Empty);
            _armorSetUnavailableState = new MessageElement(string.Empty);
            _recipeUnavailableState = new MessageElement(string.Empty);
            _npcUnavailableState = new MessageElement(string.Empty);

            _itemDetailsView.AttachPopoverHost(this);
            _recipeDetailsView?.AttachPopoverHost(this);
            _npcDetailsView?.AttachPopoverHost(this);
            SynchronizeLocalization(force: true);
            SynchronizeNavigation();
        }

        public bool HasOpenTransientSurface
        {
            get
            {
                if (ReferenceEquals(_activeContent, _itemDetailsView))
                    return _itemDetailsView.HasOpenTransientSurface;

                if (ReferenceEquals(_activeContent, _recipeDetailsView))
                    return _recipeDetailsView?.HasOpenTransientSurface == true;

                if (ReferenceEquals(_activeContent, _npcDetailsView))
                    return _npcDetailsView?.HasOpenTransientSurface == true;

                return false;
            }
        }

        public bool TryCloseTransientSurface()
        {
            if (ReferenceEquals(_activeContent, _itemDetailsView))
                return _itemDetailsView.TryCloseTransientSurface();

            if (ReferenceEquals(_activeContent, _recipeDetailsView))
                return _recipeDetailsView?.TryCloseTransientSurface() == true;

            if (ReferenceEquals(_activeContent, _npcDetailsView))
                return _npcDetailsView?.TryCloseTransientSurface() == true;

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            SynchronizeNavigation();
            base.Update(gameTime);
            SynchronizeContentHeight();
        }

        public override void OnActivate()
        {
            _isActive = true;
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            _isActive = false;
            _itemDetailsView.TryCloseTransientSurface();
            _recipeDetailsView?.TryCloseTransientSurface();
            _npcDetailsView?.TryCloseTransientSurface();
            base.OnDeactivate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            if (width <= 0 || height <= 0)
                return;

            UIRenderer.DrawRect(x, y, width, height, UIColors.SectionBg);
            UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);

            int headerHeight = Math.Min(HeaderHeight, height);
            int availableWidth = Math.Max(0, width - HeaderHorizontalPadding * 2);
            string displayText = TruncatedTextPresentation.Truncate(
                _localization.Get(CompendiumTextKeys.Browser.Details),
                availableWidth,
                out bool headerWasTruncated);

            if (displayText.Length > 0)
            {
                int textY = y + Math.Max(0, (headerHeight - TextHeight) / 2);
                UIRenderer.DrawText(displayText, x + HeaderHorizontalPadding, textY, UIColors.TextTitle);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    _localization.Get(CompendiumTextKeys.Browser.Details),
                    headerWasTruncated,
                    x + HeaderHorizontalPadding,
                    y,
                    availableWidth,
                    headerHeight,
                    IsMouseHovering);
            }

            if (height > HeaderHeight && width > 2)
                UIRenderer.DrawRect(x + 1, y + HeaderHeight - 1, width - 2, 1, UIColors.Divider);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            long revision = _localization.Revision;
            if (!force && _observedLocalizationRevision == revision)
                return;

            _observedLocalizationRevision = revision;
            _emptyState.SetMessage(_localization.Get(CompendiumTextKeys.Browser.NoSelection));
            _armorSetUnavailableState.SetMessage(
                _localization.Get(CompendiumTextKeys.Browser.ArmorSetDetailsUnavailable));
            _recipeUnavailableState.SetMessage(_localization.Get(CompendiumTextKeys.Browser.RecipeDetailsUnavailable));
            _npcUnavailableState.SetMessage(_localization.Get(CompendiumTextKeys.Browser.NpcDetailsUnavailable));
            Recalculate();
        }

        private void SynchronizeNavigation()
        {
            if (_observedNavigationRevision == _navigationState.Revision)
                return;

            _observedNavigationRevision = _navigationState.Revision;
            BrowserDestination destination = _navigationState.CurrentDestination;

            if (destination.IsSectionRoot)
            {
                SetActiveContent(_emptyState);
            }
            else if (destination.IsItem)
            {
                _itemDetailsView.ShowItem(destination.ItemId);
                SetActiveContent(_itemDetailsView);
            }
            else if (destination.IsArmorSet)
            {
                if (_armorSetDetailsView == null)
                {
                    SetActiveContent(_armorSetUnavailableState);
                }
                else
                {
                    _armorSetDetailsView.ShowArmorSet(destination.ArmorSetId);
                    SetActiveContent(_armorSetDetailsView);
                }
            }
            else if (destination.IsRecipeQuery)
            {
                if (_recipeDetailsView == null)
                {
                    SetActiveContent(_recipeUnavailableState);
                }
                else
                {
                    _recipeDetailsView.ShowRecipeQuery(destination.RecipeQueryItemId);
                    SetActiveContent(_recipeDetailsView);
                }
            }
            else if (destination.IsRecipe)
            {
                if (_recipeDetailsView == null)
                {
                    SetActiveContent(_recipeUnavailableState);
                }
                else
                {
                    if (destination.HasRecipeQuery)
                    {
                        _recipeDetailsView.ShowRecipeQuery(
                            destination.RecipeQueryItemId,
                            destination.RecipeRuntimeIndex);
                    }
                    else
                    {
                        _recipeDetailsView.ShowRecipe(destination.RecipeRuntimeIndex);
                    }

                    SetActiveContent(_recipeDetailsView);
                }
            }
            else if (destination.IsNpc)
            {
                if (_npcDetailsView == null)
                {
                    SetActiveContent(_npcUnavailableState);
                }
                else
                {
                    _npcDetailsView.ShowNpc(destination.NpcNetId);
                    SetActiveContent(_npcDetailsView);
                }
            }
            else
            {
                throw new InvalidOperationException("Unsupported browser destination.");
            }

            _scroll.ResetScroll();
            SynchronizeContentHeight();
        }

        private void SetActiveContent(UIElement content)
        {
            if (ReferenceEquals(_activeContent, content))
                return;

            if (_isActive && _activeContent != null)
                _activeContent.Deactivate();

            _scroll.Content.RemoveAllChildren();
            _activeContent = content;

            if (_activeContent == null)
                return;

            _activeContent.Left.Set(0f, 0f);
            _activeContent.Top.Set(0f, 0f);
            _activeContent.Width.Set(0f, 1f);
            _scroll.Content.Append(_activeContent);

            if (_isActive)
                _activeContent.Activate();
        }

        private void SynchronizeContentHeight()
        {
            int contentHeight;

            if (ReferenceEquals(_activeContent, _itemDetailsView))
                contentHeight = _itemDetailsView.ContentHeight;
            else if (ReferenceEquals(_activeContent, _armorSetDetailsView))
                contentHeight = _armorSetDetailsView.ContentHeight;
            else if (ReferenceEquals(_activeContent, _recipeDetailsView))
                contentHeight = _recipeDetailsView.ContentHeight;
            else if (ReferenceEquals(_activeContent, _npcDetailsView))
                contentHeight = _npcDetailsView.ContentHeight;
            else
                contentHeight = BodyPadding * 2 + TextHeight;

            if (_activeContent != null)
            {
                _activeContent.Width.Set(0f, 1f);
                _activeContent.Height.Set(contentHeight, 0f);
                _activeContent.Recalculate();
            }

            _scroll.SetContentHeight(Math.Max(contentHeight, _scroll.ViewportHeight));
        }

        private sealed class MessageElement : UIElement
        {
            private string _message;

            public MessageElement(string message)
            {
                SetMessage(message);
                Width = StyleDimension.Fill;
                Height = new StyleDimension(BodyPadding * 2 + TextHeight, 0f);
            }

            public void SetMessage(string message)
            {
                _message = message ?? string.Empty;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                int width = Math.Max(0, (int)dimensions.Width - BodyPadding * 2);
                string displayText = TruncatedTextPresentation.Truncate(_message, width, out bool wasTruncated);

                if (displayText.Length > 0)
                {
                    int textX = (int)dimensions.X + BodyPadding;
                    int textY = (int)dimensions.Y + BodyPadding;
                    UIRenderer.DrawText(displayText, textX, textY, UIColors.TextDim);
                    TruncatedTextPresentation.ShowTooltipIfTruncated(
                        _message,
                        wasTruncated,
                        textX,
                        textY,
                        width,
                        TextHeight,
                        IsMouseHovering);
                }
            }
        }
    }
}