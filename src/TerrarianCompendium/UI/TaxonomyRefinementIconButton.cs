using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class TaxonomyRefinementIconButton : UIElement
    {
        private const int IconPadding = 3;
        private const int ActiveBorderThickness = 2;
        private readonly string _centeredText;

        private readonly Action _clicked;
        private readonly VanillaItemIcon _icon;
        private readonly CompendiumLocalization _localization;
        private readonly OverlayElement _overlay;
        private readonly bool _showFacetState;
        private string _tooltip;

        private TaxonomyRefinementIconButton(
            int? itemId,
            string centeredText,
            string tooltip,
            ChecklistTaxonomyFacetState state,
            bool showFacetState,
            Action clicked,
            CompendiumLocalization localization)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                throw new ArgumentException("Tooltip must not be empty.", nameof(tooltip));

            ValidateState(state);
            _centeredText = centeredText;
            _tooltip = tooltip;
            _showFacetState = showFacetState;
            _clicked = clicked;
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            State = state;
            SetPadding(0f);

            if (itemId.HasValue)
            {
                _icon = new VanillaItemIcon
                {
                    ItemId = itemId.Value,
                    ShowTooltip = false,
                    IgnoresMouseInteraction = true
                };
                Append(_icon);
            }

            _overlay = new OverlayElement(this)
            {
                IgnoresMouseInteraction = true
            };
            Append(_overlay);
            OnLeftClick += OnClicked;
        }

        public string TooltipText
        {
            get => _tooltip;
            set => _tooltip = value ?? string.Empty;
        }

        public ChecklistTaxonomyFacetState State { get; set; }

        public static TaxonomyRefinementIconButton CreateNavigation(
            int itemId,
            string tooltip,
            Action clicked,
            CompendiumLocalization localization)
        {
            return new TaxonomyRefinementIconButton(
                itemId,
                null,
                tooltip,
                ChecklistTaxonomyFacetState.Off,
                false,
                clicked,
                localization);
        }

        public static TaxonomyRefinementIconButton CreateOther(
            string tooltip,
            Action clicked,
            CompendiumLocalization localization)
        {
            return new TaxonomyRefinementIconButton(
                null,
                "...",
                tooltip,
                ChecklistTaxonomyFacetState.Off,
                false,
                clicked,
                localization);
        }

        public static TaxonomyRefinementIconButton CreateFacet(
            int itemId,
            string displayName,
            ChecklistTaxonomyFacetState state,
            Action clicked,
            CompendiumLocalization localization)
        {
            return new TaxonomyRefinementIconButton(itemId, null, displayName, state, true, clicked, localization);
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int size = Math.Max(0, Math.Min((int)dimensions.Width, (int)dimensions.Height));

            if (_icon != null)
            {
                int iconSize = Math.Max(0, size - IconPadding * 2);
                _icon.Left.Set(IconPadding, 0f);
                _icon.Top.Set(IconPadding, 0f);
                _icon.Width.Set(iconSize, 0f);
                _icon.Height.Set(iconSize, 0f);
            }

            _overlay.Left.Set(0f, 0f);
            _overlay.Top.Set(0f, 0f);
            _overlay.Width.Set(size, 0f);
            _overlay.Height.Set(size, 0f);
            base.RecalculateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            ValidateState(State);
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);
            int size = Math.Min(width, height);

            if (size <= 0)
                return;

            UIRenderer.DrawRect(x, y, size, size, IsMouseHovering ? UIColors.ButtonHover : UIColors.Button);

            if (_icon == null)
                DrawCenteredText(x, y, size, _centeredText);

            if (IsMouseHovering)
                Tooltip.Set(
                    _showFacetState
                        ? _localization.Format(
                            CompendiumTextKeys.Common.StateSuffix,
                            _tooltip,
                            GetStateLabel(_localization, State))
                        : _tooltip);
        }

        private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (evt.Target != this)
                return;

            _clicked?.Invoke();
        }

        private static void DrawCenteredText(int x, int y, int size, string text)
        {
            int textWidth = UIRenderer.MeasureText(text);
            int textX = x + Math.Max(0, (size - textWidth) / 2);
            int textY = y + Math.Max(0, (size - 16) / 2);
            UIRenderer.DrawText(text, textX, textY, UIColors.Text);
        }

        private static void DrawBorder(int x, int y, int size, ChecklistTaxonomyFacetState state, bool showFacetState)
        {
            if (!showFacetState || state == ChecklistTaxonomyFacetState.Off)
            {
                UIRenderer.DrawRectOutline(x, y, size, size, UIColors.Border);
                return;
            }

            UIRenderer.DrawRectOutline(
                x,
                y,
                size,
                size,
                state == ChecklistTaxonomyFacetState.Include ? UIColors.Success : UIColors.Error,
                ActiveBorderThickness);
        }

        private static string GetStateLabel(CompendiumLocalization localization, ChecklistTaxonomyFacetState state)
        {
            switch (state)
            {
                case ChecklistTaxonomyFacetState.Off:
                    return localization.Get(CompendiumTextKeys.Common.Off);

                case ChecklistTaxonomyFacetState.Include:
                    return localization.Get(CompendiumTextKeys.Common.Include);

                case ChecklistTaxonomyFacetState.Exclude:
                    return localization.Get(CompendiumTextKeys.Common.Exclude);

                default:
                    throw new InvalidOperationException("Unsupported taxonomy facet state.");
            }
        }

        private static void ValidateState(ChecklistTaxonomyFacetState state)
        {
            if (state != ChecklistTaxonomyFacetState.Off &&
                state != ChecklistTaxonomyFacetState.Include &&
                state != ChecklistTaxonomyFacetState.Exclude)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported taxonomy facet state.");
            }
        }

        private sealed class OverlayElement(TaxonomyRefinementIconButton owner) : UIElement
        {
            private readonly TaxonomyRefinementIconButton _owner =
                owner ?? throw new ArgumentNullException(nameof(owner));

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                ValidateState(_owner.State);
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int size = Math.Max(0, Math.Min((int)dimensions.Width, (int)dimensions.Height));

                if (size <= 0)
                    return;

                DrawBorder(x, y, size, _owner.State, _owner._showFacetState);
            }
        }
    }
}