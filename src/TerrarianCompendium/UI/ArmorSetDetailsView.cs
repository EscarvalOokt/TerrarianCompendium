using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ArmorSetDetailsView : UIElement
    {
        private const int ContentPadding = 6;
        private const int IdentityHeight = 40;
        private const int IconSize = 36;
        private const int IdentityTextGap = 6;
        private const int RowHeight = 18;
        private const int RelationIconSize = 30;
        private const int RelationIconGap = 2;
        private const int RoleLabelHeight = 18;
        private const int VariantTitleHeight = 18;
        private const int VariantGap = 6;
        private const int TextHeight = 16;

        private readonly List<VanillaItemRelationButton> _bodyVariantButtons = new();
        private readonly List<VanillaItemRelationButton> _headVariantButtons = new();
        private readonly VanillaItemIcon _icon;
        private readonly List<VanillaItemRelationButton> _legVariantButtons = new();
        private readonly CompendiumLocalization _localization;
        private readonly ArmorSetDetailsModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly List<VariantButtons> _variantButtons = new();
        private int _armorSetId = -1;
        private ArmorSetDetailsProjection _projection;
        private bool _refreshRequired = true;

        public ArmorSetDetailsView(
            ArmorSetDetailsModel model,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            Width = StyleDimension.Fill;
            SetPadding(0f);

            _icon = new VanillaItemIcon();
            Append(_icon);
            HideIcon();
        }

        public int ContentHeight =>
            _projection == null
                ? ContentPadding * 2 + TextHeight
                : CalculateContentHeight(_projection, GetAvailableContentWidth());

        public void ShowArmorSet(int armorSetId)
        {
            if (_armorSetId == armorSetId)
            {
                Refresh();
                return;
            }

            _armorSetId = armorSetId;
            _refreshRequired = true;
            Refresh();
        }

        public void Clear()
        {
            _armorSetId = -1;
            _projection = null;
            _refreshRequired = false;
            HideIcon();
            HideRoleVariantButtons();
            HideUnusedVariantButtons(0);
            Height.Set(ContentPadding * 2 + TextHeight, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            Refresh();
            base.Update(gameTime);
        }

        public override void RecalculateChildren()
        {
            if (_projection != null)
            {
                if (_projection.UsesRoleVariantPresentation)
                    LayoutRoleVariantButtons();
                else
                    LayoutExactVariantButtons();
            }

            Height.Set(ContentHeight, 0f);
            base.RecalculateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            int x = (int)dimensions.X + ContentPadding;
            int y = (int)dimensions.Y + ContentPadding;
            int width = Math.Max(0, (int)dimensions.Width - ContentPadding * 2);

            if (width <= 0)
                return;

            if (_projection == null)
            {
                DrawTextRow(_localization.Get(CompendiumTextKeys.ArmorSets.Missing), x, y, width, UIColors.TextDim);
                return;
            }

            DrawIdentityText(_projection, x, y, width);
            int cursor = y + IdentityHeight;

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.ArmorSets.SetBonus), x, width, ref cursor);
            DrawMultilineText(_projection.BonusDescription, x, width, ref cursor);

            if (_projection.UsesRoleVariantPresentation)
            {
                DrawRoleVariantSection(
                    _localization.Get(CompendiumTextKeys.ArmorSets.Head),
                    _projection.HeadVariants.Count,
                    x,
                    width,
                    ref cursor);
                DrawRoleVariantSection(
                    _localization.Get(CompendiumTextKeys.ArmorSets.Body),
                    _projection.BodyVariants.Count,
                    x,
                    width,
                    ref cursor);
                DrawRoleVariantSection(
                    _localization.Get(CompendiumTextKeys.ArmorSets.Legs),
                    _projection.LegVariants.Count,
                    x,
                    width,
                    ref cursor);
                return;
            }

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.ArmorSets.Variants), x, width, ref cursor);

            for (var index = 0; index < _projection.Variants.Count; index++)
            {
                DrawTextRow(
                    _localization.Format(CompendiumTextKeys.ArmorSets.Variant, index + 1),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += VariantTitleHeight;
                DrawRoleLabels(x, cursor, width);
                cursor += RoleLabelHeight + RelationIconSize + VariantGap;
            }
        }

        private void Refresh()
        {
            ArmorSetDetailsProjection nextProjection = null;

            if (_armorSetId > 0)
                _model.TryGetProjection(_armorSetId, out nextProjection);

            if (!_refreshRequired && ReferenceEquals(_projection, nextProjection))
                return;

            _refreshRequired = false;
            _projection = nextProjection;
            RebuildChildren();
        }

        private void RebuildChildren()
        {
            if (_projection == null)
            {
                HideIcon();
                HideRoleVariantButtons();
                HideUnusedVariantButtons(0);
                Height.Set(ContentPadding * 2 + TextHeight, 0f);
                Recalculate();
                return;
            }

            _icon.ItemId = _projection.RepresentativeItemId;
            _icon.IsMissing = false;
            _icon.Left.Set(ContentPadding, 0f);
            _icon.Top.Set(ContentPadding, 0f);
            _icon.Width.Set(IconSize, 0f);
            _icon.Height.Set(IconSize, 0f);

            if (_projection.UsesRoleVariantPresentation)
            {
                BindRoleVariantButtons(_headVariantButtons, _projection.HeadVariants);
                BindRoleVariantButtons(_bodyVariantButtons, _projection.BodyVariants);
                BindRoleVariantButtons(_legVariantButtons, _projection.LegVariants);
                HideUnusedVariantButtons(0);
            }
            else
            {
                HideRoleVariantButtons();
                EnsureVariantButtonCapacity(_projection.Variants.Count);

                for (var index = 0; index < _projection.Variants.Count; index++)
                    _variantButtons[index].Bind(_projection.Variants[index]);

                HideUnusedVariantButtons(_projection.Variants.Count);
            }

            Recalculate();
        }

        private void LayoutRoleVariantButtons()
        {
            int contentWidth = GetAvailableContentWidth();
            int top = GetContentTopAfterBonus(_projection);

            top = LayoutRoleVariantSection(_headVariantButtons, _projection.HeadVariants.Count, top, contentWidth);
            top = LayoutRoleVariantSection(_bodyVariantButtons, _projection.BodyVariants.Count, top, contentWidth);
            LayoutRoleVariantSection(_legVariantButtons, _projection.LegVariants.Count, top, contentWidth);
        }

        private int LayoutRoleVariantSection(
            IReadOnlyList<VanillaItemRelationButton> buttons,
            int count,
            int top,
            int contentWidth)
        {
            if (count <= 0)
                return top;

            int contentTop = top + DetailsSectionLayout.HeaderHeight;
            int columns = CalculateRelationColumns(contentWidth);

            for (var index = 0; index < count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                VanillaItemRelationButton button = buttons[index];
                button.Left.Set(ContentPadding + column * (RelationIconSize + RelationIconGap), 0f);
                button.Top.Set(contentTop + row * (RelationIconSize + RelationIconGap), 0f);
                button.Width.Set(RelationIconSize, 0f);
                button.Height.Set(RelationIconSize, 0f);
            }

            return contentTop + CalculateRelationGridHeight(count, contentWidth);
        }

        private void LayoutExactVariantButtons()
        {
            int contentWidth = GetAvailableContentWidth();
            int variantsTop = GetExactVariantsContentTop(_projection);
            int roleWidth = Math.Max(1, contentWidth / 3);

            for (var index = 0; index < _projection.Variants.Count; index++)
            {
                int rowTop = variantsTop + index * GetExactVariantHeight() + VariantTitleHeight + RoleLabelHeight;
                VariantButtons buttons = _variantButtons[index];
                LayoutButton(buttons.Head, ContentPadding, rowTop, roleWidth);
                LayoutButton(buttons.Body, ContentPadding + roleWidth, rowTop, roleWidth);
                LayoutButton(buttons.Legs, ContentPadding + roleWidth * 2, rowTop, contentWidth - roleWidth * 2);
            }
        }

        private void LayoutButton(VanillaItemRelationButton button, int columnLeft, int top, int columnWidth)
        {
            if (button == null)
                return;

            int left = columnLeft + Math.Max(0, (columnWidth - RelationIconSize) / 2);
            button.Left.Set(left, 0f);
            button.Top.Set(top, 0f);
            button.Width.Set(RelationIconSize, 0f);
            button.Height.Set(RelationIconSize, 0f);
        }

        private void BindRoleVariantButtons(
            List<VanillaItemRelationButton> buttons,
            IReadOnlyList<ArmorSetDetailsItemReference> references)
        {
            EnsureRoleVariantButtonCapacity(buttons, references.Count);

            for (var index = 0; index < references.Count; index++)
            {
                ArmorSetDetailsItemReference reference = references[index];
                buttons[index].Bind(reference.ItemId, !reference.IsFound);
            }

            HideUnusedRoleVariantButtons(buttons, references.Count);
        }

        private void EnsureRoleVariantButtonCapacity(List<VanillaItemRelationButton> buttons, int count)
        {
            while (buttons.Count < count)
            {
                var button = new VanillaItemRelationButton(NavigateToItem);
                buttons.Add(button);
                Append(button);
            }
        }

        private static void HideUnusedRoleVariantButtons(List<VanillaItemRelationButton> buttons, int startIndex)
        {
            for (int index = startIndex; index < buttons.Count; index++)
                buttons[index].Hide();
        }

        private void HideRoleVariantButtons()
        {
            HideUnusedRoleVariantButtons(_headVariantButtons, 0);
            HideUnusedRoleVariantButtons(_bodyVariantButtons, 0);
            HideUnusedRoleVariantButtons(_legVariantButtons, 0);
        }

        private void EnsureVariantButtonCapacity(int count)
        {
            while (_variantButtons.Count < count)
            {
                var buttons = new VariantButtons(NavigateToItem);
                _variantButtons.Add(buttons);
                Append(buttons.Head);
                Append(buttons.Body);
                Append(buttons.Legs);
            }
        }

        private void HideUnusedVariantButtons(int startIndex)
        {
            for (int index = startIndex; index < _variantButtons.Count; index++)
                _variantButtons[index].Hide();
        }

        private void NavigateToItem(int itemId)
        {
            if (itemId > 0)
                _navigationState.Navigate(BrowserDestination.ForItem(itemId));
        }

        private void HideIcon()
        {
            _icon.ItemId = 0;
            _icon.Width.Set(0f, 0f);
            _icon.Height.Set(0f, 0f);
        }

        private int GetAvailableContentWidth()
        {
            return Math.Max(0, (int)GetInnerDimensions().Width - ContentPadding * 2);
        }

        private static int CalculateContentHeight(ArmorSetDetailsProjection projection, int contentWidth)
        {
            int height = ContentPadding * 2 +
                         IdentityHeight +
                         DetailsSectionLayout.HeaderHeight +
                         Math.Max(RowHeight, CountTextLines(projection.BonusDescription) * RowHeight);

            if (!projection.UsesRoleVariantPresentation)
            {
                return height + DetailsSectionLayout.HeaderHeight + projection.Variants.Count * GetExactVariantHeight();
            }

            height += CalculateRoleVariantSectionHeight(projection.HeadVariants.Count, contentWidth);
            height += CalculateRoleVariantSectionHeight(projection.BodyVariants.Count, contentWidth);
            height += CalculateRoleVariantSectionHeight(projection.LegVariants.Count, contentWidth);
            return height;
        }

        private static int CalculateRoleVariantSectionHeight(int count, int contentWidth)
        {
            if (count <= 0)
                return 0;

            return DetailsSectionLayout.HeaderHeight + CalculateRelationGridHeight(count, contentWidth);
        }

        private static int GetContentTopAfterBonus(ArmorSetDetailsProjection projection)
        {
            return ContentPadding +
                   IdentityHeight +
                   DetailsSectionLayout.HeaderHeight +
                   Math.Max(RowHeight, CountTextLines(projection.BonusDescription) * RowHeight);
        }

        private static int GetExactVariantsContentTop(ArmorSetDetailsProjection projection)
        {
            return GetContentTopAfterBonus(projection) + DetailsSectionLayout.HeaderHeight;
        }

        private static int GetExactVariantHeight()
        {
            return VariantTitleHeight + RoleLabelHeight + RelationIconSize + VariantGap;
        }

        private static int CalculateRelationColumns(int contentWidth)
        {
            return Math.Max(1, (Math.Max(0, contentWidth) + RelationIconGap) / (RelationIconSize + RelationIconGap));
        }

        private static int CalculateRelationGridHeight(int count, int contentWidth)
        {
            if (count <= 0)
                return 0;

            int columns = CalculateRelationColumns(contentWidth);
            int rows = (count + columns - 1) / columns;
            return rows * RelationIconSize + Math.Max(0, rows - 1) * RelationIconGap;
        }

        private void DrawIdentityText(ArmorSetDetailsProjection projection, int x, int y, int width)
        {
            int textX = x + IconSize + IdentityTextGap;
            int textWidth = Math.Max(0, width - IconSize - IdentityTextGap);
            DrawTextRow(
                _localization.Get(CompendiumTextKeys.ArmorSets.ArmorSet),
                textX,
                y + 2,
                textWidth,
                UIColors.TextTitle);
            DrawTextRow(
                _localization.Format(CompendiumTextKeys.ArmorSets.SetId, projection.ArmorSetId),
                textX,
                y + 20,
                textWidth,
                UIColors.TextDim);
        }

        private void DrawRoleVariantSection(string role, int count, int x, int width, ref int cursor)
        {
            if (count <= 0)
                return;

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(
                count > 1 ? _localization.Format(CompendiumTextKeys.ArmorSets.RoleVariants, role) : role,
                x,
                width,
                ref cursor);
            cursor += CalculateRelationGridHeight(count, width);
        }

        private void DrawRoleLabels(int x, int y, int width)
        {
            int roleWidth = Math.Max(1, width / 3);
            DrawCenteredText(_localization.Get(CompendiumTextKeys.ArmorSets.Head), x, y, roleWidth);
            DrawCenteredText(_localization.Get(CompendiumTextKeys.ArmorSets.Body), x + roleWidth, y, roleWidth);
            DrawCenteredText(
                _localization.Get(CompendiumTextKeys.ArmorSets.Legs),
                x + roleWidth * 2,
                y,
                Math.Max(0, width - roleWidth * 2));
        }

        private static void DrawCenteredText(string text, int x, int y, int width)
        {
            string display = TruncatedTextPresentation.Truncate(text, width, out _);

            if (display.Length == 0)
                return;

            int textWidth = UIRenderer.MeasureText(display);
            UIRenderer.DrawText(display, x + Math.Max(0, (width - textWidth) / 2), y + 1, UIColors.TextDim);
        }

        private void DrawMultilineText(string text, int x, int width, ref int cursor)
        {
            string[] lines = SplitLines(text);

            if (lines.Length == 0)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.ArmorSets.NoBonusDescription),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += RowHeight;
                return;
            }

            foreach (string line in lines)
            {
                DrawTextRow(line, x, cursor, width, UIColors.Text);
                cursor += RowHeight;
            }
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split(['\n'], StringSplitOptions.RemoveEmptyEntries);
        }

        private static int CountTextLines(string text)
        {
            return SplitLines(text).Length;
        }

        private static void DrawSectionSeparator(int x, int width, ref int cursor)
        {
            cursor += DetailsSectionLayout.SeparatorSpacing;

            if (width > 0)
                UIRenderer.DrawRect(x, cursor, width, DetailsSectionLayout.DividerHeight, UIColors.Divider);

            cursor += DetailsSectionLayout.DividerHeight + DetailsSectionLayout.SeparatorSpacing;
        }

        private void DrawSectionTitle(string title, int x, int width, ref int cursor)
        {
            string displayTitle = TruncatedTextPresentation.Truncate(title, width, out bool wasTruncated);

            if (displayTitle.Length > 0)
            {
                int textY = cursor + Math.Max(0, (DetailsSectionLayout.TitleHeight - TextHeight) / 2);
                UIRenderer.DrawText(displayTitle, x, textY, UIColors.TextTitle);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    title,
                    wasTruncated,
                    x,
                    cursor,
                    width,
                    DetailsSectionLayout.TitleHeight,
                    IsMouseHovering);
            }

            cursor += DetailsSectionLayout.TitleHeight + DetailsSectionLayout.ContentGap;
        }

        private void DrawTextRow(string text, int x, int y, int width, Color4 color)
        {
            if (width <= 0)
                return;

            string displayText = TruncatedTextPresentation.Truncate(text ?? string.Empty, width, out bool wasTruncated);

            if (displayText.Length == 0)
                return;

            UIRenderer.DrawText(displayText, x, y + 1, color);
            TruncatedTextPresentation.ShowTooltipIfTruncated(
                text ?? string.Empty,
                wasTruncated,
                x,
                y,
                width,
                RowHeight,
                IsMouseHovering);
        }

        private sealed class VariantButtons(Action<int> clicked)
        {
            public VanillaItemRelationButton Head { get; } = new(clicked);

            public VanillaItemRelationButton Body { get; } = new(clicked);

            public VanillaItemRelationButton Legs { get; } = new(clicked);

            public void Bind(ArmorSetDetailsVariant variant)
            {
                Bind(Head, variant.Head);
                Bind(Body, variant.Body);
                Bind(Legs, variant.Legs);
            }

            public void Hide()
            {
                Head.Hide();
                Body.Hide();
                Legs.Hide();
            }

            private static void Bind(VanillaItemRelationButton button, ArmorSetDetailsItemReference reference)
            {
                if (reference == null)
                {
                    button.Hide();
                    return;
                }

                button.Bind(reference.ItemId, !reference.IsFound);
            }
        }
    }
}