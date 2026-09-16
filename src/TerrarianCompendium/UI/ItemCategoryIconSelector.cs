using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI
{
    internal sealed class ItemCategoryIconSelector : UIElement
    {
        private const int IconGap = 4;

        private readonly CategoryIconButton[] _buttons;
        private readonly int _iconSize;

        public ItemCategoryIconSelector(
            int iconSize,
            CompendiumLocalization localization,
            Action<ItemNavigationNodeId> selectionChanged)
        {
            if (iconSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(iconSize), iconSize, "Icon size must be positive.");

            _iconSize = iconSize;
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));
            SelectionChanged = selectionChanged;
            Height.Set(iconSize, 0f);
            SetPadding(0f);

            IReadOnlyList<ItemNavigationNodeDefinition> roots =
                ItemTaxonomyDefinitions.GetChildren(ItemNavigationNodeId.AllItems);
            _buttons = new CategoryIconButton[roots.Count];

            for (var index = 0; index < roots.Count; index++)
            {
                var button = new CategoryIconButton(roots[index].Id, iconSize, localization, OnCategoryClicked);
                _buttons[index] = button;
                Append(button);
            }
        }

        public ItemNavigationNodeId? ActiveRoot { get; set; }

        public Action<ItemNavigationNodeId> SelectionChanged { get; set; }

        public static int RequiredWidth(int iconSize)
        {
            if (iconSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(iconSize), iconSize, "Icon size must be positive.");

            int categoryCount = ItemTaxonomyDefinitions.GetChildren(ItemNavigationNodeId.AllItems).Count;
            return categoryCount == 0 ? 0 : categoryCount * iconSize + (categoryCount - 1) * IconGap;
        }

        public override void RecalculateChildren()
        {
            int width = Math.Max(0, (int)GetInnerDimensions().Width);
            int requiredWidth = RequiredWidth(_iconSize);
            int startX = Math.Max(0, (width - requiredWidth) / 2);

            for (var index = 0; index < _buttons.Length; index++)
            {
                CategoryIconButton button = _buttons[index];
                button.Left.Set(startX + index * (_iconSize + IconGap), 0f);
                button.Top.Set(0f, 0f);
                button.Width.Set(_iconSize, 0f);
                button.Height.Set(_iconSize, 0f);
                button.IsActive = ActiveRoot.HasValue && ActiveRoot.Value == button.NodeId;
            }

            base.RecalculateChildren();
        }

        public override void Update(GameTime gameTime)
        {
            for (var index = 0; index < _buttons.Length; index++)
                _buttons[index].IsActive = ActiveRoot.HasValue && ActiveRoot.Value == _buttons[index].NodeId;

            base.Update(gameTime);
        }

        private void OnCategoryClicked(ItemNavigationNodeId nodeId)
        {
            SelectionChanged?.Invoke(nodeId);
        }

        private sealed class CategoryIconButton : UIElement
        {
            private readonly Action<ItemNavigationNodeId> _clicked;
            private readonly ItemNavigationIcon _icon;
            private readonly CompendiumLocalization _localization;
            private readonly int _size;

            public CategoryIconButton(
                ItemNavigationNodeId nodeId,
                int size,
                CompendiumLocalization localization,
                Action<ItemNavigationNodeId> clicked)
            {
                NodeId = nodeId;
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                _size = size;
                _clicked = clicked;
                _icon = new ItemNavigationIcon(nodeId)
                {
                    IgnoresMouseInteraction = true
                };
                Append(_icon);
                OnLeftClick += OnClicked;
            }

            public ItemNavigationNodeId NodeId { get; }

            public bool IsActive { get; set; }

            public override void RecalculateChildren()
            {
                _icon.Left.Set(0f, 0f);
                _icon.Top.Set(0f, 0f);
                _icon.Width.Set(_size, 0f);
                _icon.Height.Set(_size, 0f);
                base.RecalculateChildren();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int size = Math.Min(_size, Math.Min((int)dimensions.Width, (int)dimensions.Height));

                if (size <= 0)
                    return;

                UIRenderer.DrawRect(
                    x,
                    y,
                    size,
                    size,
                    IsActive ? UIColors.ItemActiveBg : IsMouseHovering ? UIColors.ButtonHover : UIColors.Button);
                UIRenderer.DrawRectOutline(x, y, size, size, IsActive ? UIColors.Accent : UIColors.Border);

                if (IsMouseHovering)
                    Tooltip.Set(_localization.Get(ItemTaxonomyDefinitions.GetNavigationNode(NodeId).DisplayNameKey));
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (evt.Target == this)
                    _clicked?.Invoke(NodeId);
            }
        }
    }
}