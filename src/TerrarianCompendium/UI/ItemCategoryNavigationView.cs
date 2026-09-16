using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ItemCategoryNavigationView : UIElement
    {
        public const int PreferredWidth = 58;

        private const int IconSize = 30;
        private const int ControlGap = 4;
        private readonly CompendiumLocalization _localization;

        private readonly Action<ItemNavigationNodeId> _nodeSelected;
        private readonly Action<ItemNavigationNodeId> _otherSelected;
        private readonly VanillaScrollRegion _scroll;
        private ItemNavigationNodeId _currentNodeId = ItemNavigationNodeId.AllItems;

        private bool _dirty = true;
        private bool _hasOther;
        private bool _isOther;
        private int _lastContentWidth = -1;
        private long _localizationRevision = -1;
        private ItemNavigationNodeId? _parentTarget;

        public ItemCategoryNavigationView(
            CompendiumLocalization localization,
            Action<ItemNavigationNodeId> nodeSelected,
            Action<ItemNavigationNodeId> otherSelected)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _nodeSelected = nodeSelected ?? throw new ArgumentNullException(nameof(nodeSelected));
            _otherSelected = otherSelected ?? throw new ArgumentNullException(nameof(otherSelected));
            SetPadding(0f);

            _scroll = new VanillaScrollRegion
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill
            };
            Append(_scroll);
        }

        public void Synchronize(
            ItemNavigationNodeId currentNodeId,
            bool isOther,
            ItemNavigationNodeId? parentTarget,
            bool hasOther)
        {
            if (_currentNodeId == currentNodeId &&
                _isOther == isOther &&
                _parentTarget == parentTarget &&
                _hasOther == hasOther)
            {
                return;
            }

            _currentNodeId = currentNodeId;
            _isOther = isOther;
            _parentTarget = parentTarget;
            _hasOther = hasOther;
            _dirty = true;
            _scroll.ResetScroll();
        }

        public override void RecalculateChildren()
        {
            base.RecalculateChildren();
            TryRebuildContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (_localizationRevision != _localization.Revision)
            {
                _localizationRevision = _localization.Revision;
                _dirty = true;
            }

            TryRebuildContent();
            base.Update(gameTime);
        }

        private void TryRebuildContent()
        {
            int contentWidth = _scroll.ContentWidth;

            if (contentWidth <= 0)
                return;

            if (!_dirty && _lastContentWidth == contentWidth)
                return;

            _dirty = false;
            _lastContentWidth = contentWidth;
            _scroll.Content.RemoveAllChildren();

            if (_currentNodeId == ItemNavigationNodeId.AllItems)
            {
                _scroll.SetContentHeight(_scroll.ViewportHeight);
                _scroll.Content.Recalculate();
                return;
            }

            ItemNavigationNodeDefinition currentNode = ItemTaxonomyDefinitions.GetNavigationNode(_currentNodeId);
            var cursor = 0;

            string currentName = _localization.Get(currentNode.DisplayNameKey);
            var current = new CurrentCategoryElement(
                _isOther ? null : _currentNodeId,
                _isOther ? _localization.Format(CompendiumTextKeys.Common.OtherFor, currentName) : currentName);
            AppendCentered(current, cursor, IconSize, IconSize, contentWidth);
            cursor += IconSize + ControlGap;

            if (_parentTarget.HasValue)
            {
                ItemNavigationNodeId parentTarget = _parentTarget.Value;
                string parentName =
                    _localization.Get(ItemTaxonomyDefinitions.GetNavigationNode(parentTarget).DisplayNameKey);
                var parentButton = new VanillaTextButton("↑", () => _nodeSelected(parentTarget))
                {
                    TooltipText = _localization.Format(CompendiumTextKeys.Common.UpTo, parentName)
                };
                AppendCentered(parentButton, cursor, IconSize, IconSize, contentWidth);
                cursor += IconSize + ControlGap;
            }

            if (!_isOther)
            {
                IReadOnlyList<ItemNavigationNodeDefinition> children =
                    ItemTaxonomyDefinitions.GetChildren(_currentNodeId);

                for (var index = 0; index < children.Count; index++)
                {
                    ItemNavigationNodeDefinition child = children[index];
                    ItemNavigationNodeId childId = child.Id;
                    var button = TaxonomyRefinementIconButton.CreateNavigation(
                        ItemTaxonomyIconDefinitions.GetNavigationItemId(childId),
                        _localization.Get(child.DisplayNameKey),
                        () => _nodeSelected(childId),
                        _localization);
                    AppendCentered(button, cursor, IconSize, IconSize, contentWidth);
                    cursor += IconSize + ControlGap;
                }

                if (_hasOther)
                {
                    ItemNavigationNodeId parentNodeId = _currentNodeId;
                    var otherButton = TaxonomyRefinementIconButton.CreateOther(
                        _localization.Format(CompendiumTextKeys.Common.OtherFor, currentName),
                        () => _otherSelected(parentNodeId),
                        _localization);
                    AppendCentered(otherButton, cursor, IconSize, IconSize, contentWidth);
                    cursor += IconSize + ControlGap;
                }
            }

            int contentHeight = Math.Max(0, cursor - ControlGap);
            _scroll.SetContentHeight(Math.Max(contentHeight, _scroll.ViewportHeight));
            _scroll.Content.Recalculate();
        }

        private void AppendCentered(UIElement element, int top, int width, int height, int contentWidth)
        {
            int left = Math.Max(0, (contentWidth - width) / 2);
            element.Left.Set(left, 0f);
            element.Top.Set(top, 0f);
            element.Width.Set(width, 0f);
            element.Height.Set(height, 0f);
            _scroll.Content.Append(element);
        }

        private sealed class CurrentCategoryElement : UIElement
        {
            private readonly ItemNavigationIcon _icon;
            private readonly string _tooltip;

            public CurrentCategoryElement(ItemNavigationNodeId? nodeId, string tooltip)
            {
                _tooltip = tooltip ?? string.Empty;
                SetPadding(0f);

                if (nodeId.HasValue)
                {
                    _icon = new ItemNavigationIcon(nodeId.Value)
                    {
                        IgnoresMouseInteraction = true
                    };
                    Append(_icon);
                }
            }

            public override void RecalculateChildren()
            {
                if (_icon != null)
                {
                    _icon.Left.Set(0f, 0f);
                    _icon.Top.Set(0f, 0f);
                    _icon.Width = StyleDimension.Fill;
                    _icon.Height = StyleDimension.Fill;
                }

                base.RecalculateChildren();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int size = Math.Max(0, Math.Min((int)dimensions.Width, (int)dimensions.Height));

                if (size <= 0)
                    return;

                if (_icon == null)
                {
                    const string text = "...";
                    int textWidth = UIRenderer.MeasureText(text);
                    UIRenderer.DrawText(
                        text,
                        x + Math.Max(0, (size - textWidth) / 2),
                        y + Math.Max(0, (size - 16) / 2),
                        UIColors.Text);
                }

                if (IsMouseHovering && _tooltip.Length > 0)
                    Tooltip.Set(_tooltip);
            }
        }
    }
}