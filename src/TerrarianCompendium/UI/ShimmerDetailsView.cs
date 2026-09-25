using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Details;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.Shimmer;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ShimmerDetailsView : UIElement
    {
        private const int ContentPadding = 6;
        private const int IdentityHeight = 40;
        private const int IdentityIconSize = 36;
        private const int ItemRowHeight = 28;
        private const int ItemButtonSize = 26;
        private const int ItemTextGap = 4;
        private const int RowHeight = 18;
        private const int RecipeButtonHeight = 24;
        private const int TextHeight = 16;
        private const int VariantControlGap = 4;
        private const int VariantSelectorHeight = 24;

        private readonly VanillaItemRelationButton _inputButton;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly CompendiumLocalization _localization;
        private readonly ShimmerDetailsModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly List<VanillaItemRelationButton> _outputButtons = new();
        private readonly VanillaTextButton _recipeButton;
        private readonly VanillaItemRelationButton _resultButton;
        private readonly CycleSelector _variantSelector;
        private long _localizationRevision = -1;
        private ShimmerDetailsProjection _projection;
        private int _queryItemId = -1;
        private bool _refreshRequired = true;
        private int _selectedVariantIndex;

        public ShimmerDetailsView(
            ShimmerDetailsModel model,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization,
            JourneyResearchState journeyResearchState = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _journeyResearchState = journeyResearchState;
            Width = StyleDimension.Fill;
            SetPadding(0f);

            _resultButton = new VanillaItemRelationButton(NavigateToItem, _journeyResearchState);
            Append(_resultButton);

            _variantSelector = new CycleSelector(
                [_localization.Format(CompendiumTextKeys.Shimmer.DetailsVariant, 1, 1)],
                0,
                OnVariantSelected);
            Append(_variantSelector);
            HideVariantSelector();

            _inputButton = new VanillaItemRelationButton(NavigateToItem, _journeyResearchState);
            Append(_inputButton);
            _inputButton.Hide();

            _recipeButton = new VanillaTextButton(string.Empty, NavigateToRecipe);
            Append(_recipeButton);
            HideRecipeButton();
        }

        public int ContentHeight { get; private set; } = ContentPadding * 2 + TextHeight;

        public bool HasOpenTransientSurface => false;

        public bool TryCloseTransientSurface()
        {
            return false;
        }

        public void ShowQuery(int itemId)
        {
            if (_queryItemId == itemId)
            {
                Refresh();
                return;
            }

            _queryItemId = itemId;
            _selectedVariantIndex = 0;
            _refreshRequired = true;
            Refresh();
        }

        public void Clear()
        {
            _queryItemId = -1;
            _selectedVariantIndex = 0;
            _projection = null;
            _refreshRequired = false;
            _resultButton.Hide();
            _inputButton.Hide();
            HideVariantSelector();
            HideUnusedOutputButtons(0);
            HideRecipeButton();
            ContentHeight = ContentPadding * 2 + TextHeight;
            Height.Set(ContentHeight, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            if (_localizationRevision != _localization.Revision)
            {
                _localizationRevision = _localization.Revision;
                _refreshRequired = true;
            }

            Refresh();
            base.Update(gameTime);
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
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.Shimmer.DetailsMissing),
                    x,
                    y,
                    width,
                    UIColors.TextDim);
                return;
            }

            DrawIdentity(x, y, width);
            int cursor = y + IdentityHeight;
            ShimmerDetailsVariantProjection variant = GetSelectedVariant();

            if (variant == null)
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawTextRow(
                    _localization.Get(
                        _projection.HasProducingVariants
                            ? CompendiumTextKeys.Shimmer.DetailsNoMatchingVariants
                            : CompendiumTextKeys.Shimmer.DetailsNoProducingVariants),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                return;
            }

            if (ShouldShowVariantSelector())
                cursor += VariantControlGap + VariantSelectorHeight;

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.Shimmer.DetailsTransformation), x, width, ref cursor);
            DrawLabelValue(
                _localization.Get(CompendiumTextKeys.Shimmer.DetailsType),
                _localization.Get(
                    variant.IsDirect
                        ? CompendiumTextKeys.Shimmer.FilterTransform
                        : CompendiumTextKeys.Shimmer.FilterDecraft),
                x,
                width,
                ref cursor);
            DrawLabelValue(
                _localization.Get(CompendiumTextKeys.Shimmer.DetailsStatus),
                _localization.Get(
                    variant.IsProgressionLocked
                        ? CompendiumTextKeys.Shimmer.FilterLocked
                        : CompendiumTextKeys.Shimmer.FilterUnlocked),
                x,
                width,
                ref cursor);

            if (CountConditions(variant) > 0)
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawSectionTitle(_localization.Get(CompendiumTextKeys.Shimmer.DetailsConditions), x, width, ref cursor);
                DrawConditions(variant, x, width, ref cursor);
            }

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.Shimmer.DetailsInput), x, width, ref cursor);
            DrawItemRowText(
                variant.Input,
                x + ItemButtonSize + ItemTextGap,
                cursor,
                width - ItemButtonSize - ItemTextGap);
            cursor += ItemRowHeight;

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(
                _localization.Get(
                    variant.IsDirect
                        ? CompendiumTextKeys.Shimmer.DetailsResult
                        : CompendiumTextKeys.Shimmer.DetailsReturns),
                x,
                width,
                ref cursor);

            foreach (ShimmerDetailsItemReference output in variant.Outputs)
            {
                DrawItemRowText(output, x + ItemButtonSize + ItemTextGap, cursor, width - ItemButtonSize - ItemTextGap);
                cursor += ItemRowHeight;
            }

            if (variant.IsDecraft && variant.IsAlchemy)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.Shimmer.DetailsAlchemyNotice),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += RowHeight;
            }

            if (variant.IsDecraft && variant.DecraftingRecipeRuntimeIndex.HasValue)
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawSectionTitle(_localization.Get(CompendiumTextKeys.Shimmer.DetailsRecipe), x, width, ref cursor);
            }
        }

        private void Refresh()
        {
            ShimmerDetailsProjection nextProjection = null;

            if (_queryItemId > 0)
                _model.TryGetProjection(_queryItemId, out nextProjection);

            if (!_refreshRequired && ReferenceEquals(_projection, nextProjection))
                return;

            _refreshRequired = false;
            _projection = nextProjection;

            if (_projection == null || _projection.MatchingVariantCount == 0)
                _selectedVariantIndex = 0;
            else if (_selectedVariantIndex >= _projection.MatchingVariantCount)
                _selectedVariantIndex = _projection.MatchingVariantCount - 1;

            RebuildChildren();
        }

        private void RebuildChildren()
        {
            if (_projection == null)
            {
                _resultButton.Hide();
                _inputButton.Hide();
                HideVariantSelector();
                HideUnusedOutputButtons(0);
                HideRecipeButton();
                ContentHeight = ContentPadding * 2 + TextHeight;
                Height.Set(ContentHeight, 0f);
                Recalculate();
                return;
            }

            _resultButton.Bind(_projection.Result.ItemId, !_projection.Result.IsFound);
            _resultButton.Left.Set(ContentPadding, 0f);
            _resultButton.Top.Set(ContentPadding, 0f);
            _resultButton.Width.Set(IdentityIconSize, 0f);
            _resultButton.Height.Set(IdentityIconSize, 0f);

            ShimmerDetailsVariantProjection variant = GetSelectedVariant();
            if (variant == null)
            {
                _inputButton.Hide();
                HideVariantSelector();
                HideUnusedOutputButtons(0);
                HideRecipeButton();
                ContentHeight = CalculateContentHeight(_projection, null);
                Height.Set(ContentHeight, 0f);
                Recalculate();
                return;
            }

            SynchronizeVariantSelector();

            int cursor = ContentPadding + IdentityHeight;
            if (ShouldShowVariantSelector())
                cursor += VariantControlGap + VariantSelectorHeight;

            cursor += DetailsSectionLayout.HeaderHeight + RowHeight * 2;

            int conditionCount = CountConditions(variant);
            if (conditionCount > 0)
                cursor += DetailsSectionLayout.HeaderHeight + conditionCount * RowHeight;

            cursor += DetailsSectionLayout.HeaderHeight;
            _inputButton.Bind(variant.Input.ItemId, !variant.Input.IsFound);
            _inputButton.Left.Set(ContentPadding, 0f);
            _inputButton.Top.Set(cursor, 0f);
            _inputButton.Width.Set(ItemButtonSize, 0f);
            _inputButton.Height.Set(ItemButtonSize, 0f);
            cursor += ItemRowHeight;

            cursor += DetailsSectionLayout.HeaderHeight;
            EnsureOutputButtonCapacity(variant.Outputs.Count);

            for (var index = 0; index < variant.Outputs.Count; index++)
            {
                ShimmerDetailsItemReference output = variant.Outputs[index];
                VanillaItemRelationButton button = _outputButtons[index];
                button.Bind(output.ItemId, !output.IsFound);
                button.Left.Set(ContentPadding, 0f);
                button.Top.Set(cursor + index * ItemRowHeight, 0f);
                button.Width.Set(ItemButtonSize, 0f);
                button.Height.Set(ItemButtonSize, 0f);
            }

            HideUnusedOutputButtons(variant.Outputs.Count);
            cursor += variant.Outputs.Count * ItemRowHeight;

            if (variant.IsDecraft && variant.IsAlchemy)
                cursor += RowHeight;

            if (variant.IsDecraft && variant.DecraftingRecipeRuntimeIndex.HasValue)
            {
                cursor += DetailsSectionLayout.HeaderHeight;
                _recipeButton.Text = _localization.Get(CompendiumTextKeys.Shimmer.DetailsOpenRecipe);
                _recipeButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.DetailsOpenRecipeTooltip);
                _recipeButton.Left.Set(ContentPadding, 0f);
                _recipeButton.Top.Set(cursor, 0f);
                _recipeButton.Width.Set(-ContentPadding * 2f, 1f);
                _recipeButton.Height.Set(RecipeButtonHeight, 0f);
            }
            else
            {
                HideRecipeButton();
            }

            ContentHeight = CalculateContentHeight(_projection, variant);
            Height.Set(ContentHeight, 0f);
            Recalculate();
        }

        private void SynchronizeVariantSelector()
        {
            if (!ShouldShowVariantSelector())
            {
                HideVariantSelector();
                return;
            }

            var options = new string[_projection.MatchingVariantCount];
            for (var index = 0; index < options.Length; index++)
            {
                options[index] = _localization.Format(
                    CompendiumTextKeys.Shimmer.DetailsVariant,
                    index + 1,
                    options.Length);
            }

            _variantSelector.SetOptions(options, _selectedVariantIndex);
            _variantSelector.Left.Set(ContentPadding, 0f);
            _variantSelector.Top.Set(ContentPadding + IdentityHeight + VariantControlGap, 0f);
            _variantSelector.Width.Set(-ContentPadding * 2f, 1f);
            _variantSelector.Height.Set(VariantSelectorHeight, 0f);
        }

        private void DrawIdentity(int x, int y, int width)
        {
            int textX = x + IdentityIconSize + ItemTextGap;
            int textWidth = Math.Max(0, width - IdentityIconSize - ItemTextGap);
            DrawTextRow(_projection.Result.Name, textX, y, textWidth, UIColors.TextTitle);

            if (_projection.TotalVariantCount > 0)
            {
                DrawTextRow(
                    _localization.Format(
                        CompendiumTextKeys.Shimmer.DetailsVariantCount,
                        _projection.MatchingVariantCount,
                        _projection.TotalVariantCount),
                    textX,
                    y + RowHeight,
                    textWidth,
                    UIColors.TextDim);
            }
        }

        private void DrawConditions(ShimmerDetailsVariantProjection variant, int x, int width, ref int cursor)
        {
            ShimmerProgressionRequirement requirements = variant.ProgressionRequirement;

            if ((requirements & ShimmerProgressionRequirement.PostSkeletron) != 0)
                DrawBossRequirement(Lang.GetNPCNameValue(NPCID.SkeletronHead), x, width, ref cursor);
            if ((requirements & ShimmerProgressionRequirement.PostGolem) != 0)
                DrawBossRequirement(Lang.GetNPCNameValue(NPCID.Golem), x, width, ref cursor);
            if ((requirements & ShimmerProgressionRequirement.PostMoonLord) != 0)
                DrawBossRequirement(Lang.GetNPCNameValue(NPCID.MoonLordCore), x, width, ref cursor);

            if (variant.WorldCondition == ShimmerWorldCondition.Crimson)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.Shimmer.DetailsCrimsonWorld),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += RowHeight;
            }
            else if (variant.WorldCondition == ShimmerWorldCondition.Corruption)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.Shimmer.DetailsCorruptionWorld),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += RowHeight;
            }

            if (variant.MoonPhase.HasValue)
            {
                string moonPhaseName = ResolveMoonPhaseName(variant.MoonPhase.Value);
                DrawTextRow(
                    _localization.Format(CompendiumTextKeys.Shimmer.DetailsMoonPhase, moonPhaseName),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += RowHeight;
            }
        }

        private void DrawBossRequirement(string bossName, int x, int width, ref int cursor)
        {
            DrawTextRow(
                _localization.Format(CompendiumTextKeys.Shimmer.DetailsAfterBoss, bossName),
                x,
                cursor,
                width,
                UIColors.TextDim);
            cursor += RowHeight;
        }

        private static string ResolveMoonPhaseName(int moonPhase)
        {
            string key = moonPhase switch
            {
                0 => "GameUI.FullMoon",
                1 => "GameUI.WaningGibbous",
                2 => "GameUI.ThirdQuarter",
                3 => "GameUI.WaningCrescent",
                4 => "GameUI.NewMoon",
                5 => "GameUI.WaxingCrescent",
                6 => "GameUI.FirstQuarter",
                7 => "GameUI.WaxingGibbous",
                _ => null
            };

            return key == null ? moonPhase.ToString() : Language.GetTextValue(key);
        }

        private void DrawItemRowText(ShimmerDetailsItemReference item, int x, int y, int width)
        {
            string text = item.Stack > 1
                ? item.Name + " " + _localization.Format(CompendiumTextKeys.Common.Stack, item.Stack)
                : item.Name;
            DrawTextRow(text, x, y + Math.Max(0, (ItemRowHeight - TextHeight) / 2), width, UIColors.Text);
        }

        private void DrawLabelValue(string label, string value, int x, int width, ref int cursor)
        {
            DrawTextRow(label + ": " + value, x, cursor, width, UIColors.Text);
            cursor += RowHeight;
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
            if (width <= 0 || string.IsNullOrWhiteSpace(text))
                return;

            string display = TruncatedTextPresentation.Truncate(text, width, out bool wasTruncated);
            if (display.Length == 0)
                return;

            UIRenderer.DrawText(display, x, y, color);
            TruncatedTextPresentation.ShowTooltipIfTruncated(
                text,
                wasTruncated,
                x,
                y,
                width,
                TextHeight,
                IsMouseHovering);
        }

        private void NavigateToItem(int itemId)
        {
            if (itemId > 0)
                _navigationState.Navigate(BrowserDestination.ForItem(itemId));
        }

        private void NavigateToRecipe()
        {
            ShimmerDetailsVariantProjection variant = GetSelectedVariant();
            if (variant?.DecraftingRecipeRuntimeIndex is { } runtimeIndex)
                _navigationState.Navigate(BrowserDestination.ForRecipe(runtimeIndex));
        }

        private void OnVariantSelected(int selectedIndex)
        {
            if (_projection == null || selectedIndex < 0 || selectedIndex >= _projection.MatchingVariantCount)
                return;

            if (_selectedVariantIndex == selectedIndex)
                return;

            _selectedVariantIndex = selectedIndex;
            RebuildChildren();
        }

        private ShimmerDetailsVariantProjection GetSelectedVariant()
        {
            if (_projection == null || _projection.MatchingVariantCount == 0)
                return null;

            if (_selectedVariantIndex < 0 || _selectedVariantIndex >= _projection.MatchingVariantCount)
                _selectedVariantIndex = 0;

            return _projection.MatchingVariants[_selectedVariantIndex];
        }

        private bool ShouldShowVariantSelector()
        {
            return _projection is { MatchingVariantCount: > 1 };
        }

        private void EnsureOutputButtonCapacity(int count)
        {
            while (_outputButtons.Count < count)
            {
                var button = new VanillaItemRelationButton(NavigateToItem, _journeyResearchState);
                _outputButtons.Add(button);
                Append(button);
            }
        }

        private void HideUnusedOutputButtons(int startIndex)
        {
            for (int index = startIndex; index < _outputButtons.Count; index++)
                _outputButtons[index].Hide();
        }

        private void HideVariantSelector()
        {
            _variantSelector.Width.Set(0f, 0f);
            _variantSelector.Height.Set(0f, 0f);
        }

        private void HideRecipeButton()
        {
            _recipeButton.Width.Set(0f, 0f);
            _recipeButton.Height.Set(0f, 0f);
        }

        private static int CalculateContentHeight(
            ShimmerDetailsProjection projection,
            ShimmerDetailsVariantProjection variant)
        {
            int height = ContentPadding * 2 + IdentityHeight;

            if (variant == null)
                return height + DetailsSectionLayout.SeparatorHeight + RowHeight;

            if (projection.MatchingVariantCount > 1)
                height += VariantControlGap + VariantSelectorHeight;

            height += DetailsSectionLayout.HeaderHeight + RowHeight * 2;

            int conditionCount = CountConditions(variant);
            if (conditionCount > 0)
                height += DetailsSectionLayout.HeaderHeight + conditionCount * RowHeight;

            height += DetailsSectionLayout.HeaderHeight + ItemRowHeight;
            height += DetailsSectionLayout.HeaderHeight + variant.Outputs.Count * ItemRowHeight;

            if (variant.IsDecraft && variant.IsAlchemy)
                height += RowHeight;

            if (variant.IsDecraft && variant.DecraftingRecipeRuntimeIndex.HasValue)
                height += DetailsSectionLayout.HeaderHeight + RecipeButtonHeight;

            return height;
        }

        private static int CountConditions(ShimmerDetailsVariantProjection variant)
        {
            var count = 0;
            ShimmerProgressionRequirement requirements = variant.ProgressionRequirement;

            if ((requirements & ShimmerProgressionRequirement.PostSkeletron) != 0)
                count++;
            if ((requirements & ShimmerProgressionRequirement.PostGolem) != 0)
                count++;
            if ((requirements & ShimmerProgressionRequirement.PostMoonLord) != 0)
                count++;
            if (variant.WorldCondition != ShimmerWorldCondition.Any)
                count++;
            if (variant.MoonPhase.HasValue)
                count++;

            return count;
        }
    }
}