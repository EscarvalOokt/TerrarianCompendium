using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class RecipeDetailsView : UIElement
    {
        private const int ContentPadding = 6;
        private const int IdentityHeight = 40;
        private const int IconSize = 36;
        private const int IdentityTextGap = 6;
        private const int FavoriteControlGap = 4;
        private const int FavoriteButtonHeight = 24;
        private const int FavoriteButtonWidth = 30;
        private const int VariantSelectorHeight = 24;
        private const int VariantControlGap = 2;
        private const int VariantSummaryHeight = 18;
        private const int CraftButtonTopGap = 4;
        private const int CraftButtonHeight = 28;
        private const int CraftButtonWidth = 56;
        private const int RowHeight = 18;
        private const int ItemRowHeight = 26;
        private const int ItemIconSize = 24;
        private const int ItemTextGap = 4;
        private const int AlternativeIndent = 10;
        private const int AlternativeRowHeight = 22;
        private const int AlternativeIconSize = 20;
        private const int TextHeight = 16;

        private readonly DirectCraftButton _craftButton;
        private readonly VanillaTextButton _favoriteButton;
        private readonly RecipeFavoriteState _favoriteState;
        private readonly List<bool> _iconNavigationEnabled = new();
        private readonly List<VanillaItemIcon> _icons = new();
        private readonly CompendiumLocalization _localization;
        private readonly RecipeDetailsModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly CycleSelector _variantSelector;
        private long _localizationRevision = -1;
        private int _preferredRuntimeIndex = -1;
        private RecipeDetailsProjection _projection;
        private int _queryItemId = -1;
        private RecipeQueryDetailsProjection _queryProjection;
        private bool _refreshRequired = true;
        private int _runtimeIndex = -1;

        public RecipeDetailsView(
            RecipeDetailsModel model,
            RecipeFavoriteState favoriteState,
            BrowserNavigationState navigationState,
            ItemTextIndex itemTextIndex,
            RecipeStationDisplayIndex stationDisplayIndex,
            VanillaDirectCraftingService directCraftingService,
            CompendiumLocalization localization)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _favoriteState = favoriteState ?? throw new ArgumentNullException(nameof(favoriteState));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            Width = StyleDimension.Fill;
            SetPadding(0f);

            _favoriteButton = new VanillaTextButton("☆", ToggleFavorite)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2
            };
            Append(_favoriteButton);
            HideFavoriteButton();

            _variantSelector = new CycleSelector(
                [_localization.Format(CompendiumTextKeys.Recipes.RecipeVariant, 1, 1)],
                0,
                OnVariantSelected);
            Append(_variantSelector);
            HideVariantSelector();

            _craftButton = new DirectCraftButton(
                directCraftingService,
                itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex)),
                stationDisplayIndex,
                _localization);
            Append(_craftButton);
            HideCraftButton();
        }

        public int ContentHeight { get; private set; } = ContentPadding * 2 + TextHeight;

        public bool HasOpenTransientSurface => _craftButton.IsPopoverOpen;

        public void AttachPopoverHost(UIElement host)
        {
            _craftButton.AttachPopoverHost(host);
        }

        public bool TryCloseTransientSurface()
        {
            return _craftButton.TryClosePopover();
        }

        public void ShowRecipe(int runtimeIndex)
        {
            if (_queryItemId < 0 && _runtimeIndex == runtimeIndex)
            {
                Refresh();
                return;
            }

            _queryItemId = -1;
            _preferredRuntimeIndex = -1;
            _runtimeIndex = runtimeIndex;
            _refreshRequired = true;
            Refresh();
        }

        public void ShowRecipeQuery(int resultItemId, int? preferredRuntimeIndex = null)
        {
            int preferred = preferredRuntimeIndex ?? -1;

            if (_queryItemId == resultItemId && _preferredRuntimeIndex == preferred)
            {
                Refresh();
                return;
            }

            _queryItemId = resultItemId;
            _preferredRuntimeIndex = preferred;
            _runtimeIndex = -1;
            _refreshRequired = true;
            Refresh();
        }

        public void Clear()
        {
            _queryItemId = -1;
            _preferredRuntimeIndex = -1;
            _runtimeIndex = -1;
            _queryProjection = null;
            _projection = null;
            _refreshRequired = false;
            HideFavoriteButton();
            HideVariantSelector();
            HideCraftButton();
            HideUnusedIcons(0);
            ContentHeight = ContentPadding * 2 + TextHeight;
            Height.Set(ContentHeight, 0f);
        }

        public int GetContentHeight(int runtimeIndex)
        {
            if (!_model.TryGetProjection(runtimeIndex, out RecipeDetailsProjection projection))
                return ContentPadding * 2 + TextHeight;

            return CalculateContentHeight(projection, null);
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

        public override void OnDeactivate()
        {
            _craftButton.TryClosePopover();
            base.OnDeactivate();
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
                string message = _localization.Get(
                    _queryProjection is { MatchingRecipeCount: 0 }
                        ? CompendiumTextKeys.Recipes.NoMatchingRecipe
                        : CompendiumTextKeys.Recipes.MissingRecipe);
                DrawTextRow(message, x, y, width, UIColors.TextDim);
                return;
            }

            DrawIdentityText(_projection, x, y, width);
            int cursor = y + IdentityHeight;

            if (ShouldShowVariantSelector())
                cursor += VariantControlGap + VariantSelectorHeight;

            if (ShouldShowVariantSummary())
            {
                cursor += VariantControlGap;
                DrawTextRow(
                    _localization.Format(
                        CompendiumTextKeys.Recipes.MatchingCount,
                        _queryProjection.MatchingRecipeCount,
                        _queryProjection.TotalRecipeCount),
                    x,
                    cursor,
                    width,
                    UIColors.TextDim);
                cursor += VariantSummaryHeight;
            }

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.Recipes.Crafting), x, width, ref cursor);
            DrawCrafting(_projection, x, width, ref cursor);

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.Recipes.Ingredients), x, width, ref cursor);
            DrawIngredientsText(_projection, x, width, ref cursor);

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.Recipes.Requirements), x, width, ref cursor);
            DrawRequirementsText(_projection, x, width, ref cursor);
        }

        private void Refresh()
        {
            RecipeQueryDetailsProjection nextQueryProjection = null;
            RecipeDetailsProjection nextProjection = null;
            int nextRuntimeIndex = _runtimeIndex;

            if (_queryItemId > 0)
            {
                if (_model.TryGetQueryProjection(_queryItemId, out nextQueryProjection))
                {
                    nextRuntimeIndex = ResolveQueryRuntimeIndex(nextQueryProjection);

                    if (nextRuntimeIndex >= 0)
                    {
                        NormalizeSelectedRecipeDestination(nextRuntimeIndex);
                        _model.TryGetProjection(nextRuntimeIndex, out nextProjection);
                    }
                }
                else
                {
                    nextRuntimeIndex = -1;
                }
            }
            else if (_runtimeIndex >= 0)
            {
                _model.TryGetProjection(_runtimeIndex, out nextProjection);
            }

            if (!_refreshRequired &&
                ReferenceEquals(_queryProjection, nextQueryProjection) &&
                ReferenceEquals(_projection, nextProjection) &&
                _runtimeIndex == nextRuntimeIndex)
            {
                return;
            }

            _refreshRequired = false;
            _queryProjection = nextQueryProjection;
            _projection = nextProjection;
            _runtimeIndex = nextRuntimeIndex;
            _preferredRuntimeIndex = -1;
            RebuildChildren();
        }

        private int ResolveQueryRuntimeIndex(RecipeQueryDetailsProjection queryProjection)
        {
            IReadOnlyList<int> indices = queryProjection.MatchingRecipeRuntimeIndices;

            if (indices.Count == 0)
                return -1;

            if (ContainsRuntimeIndex(indices, _runtimeIndex))
                return _runtimeIndex;

            if (ContainsRuntimeIndex(indices, _preferredRuntimeIndex))
                return _preferredRuntimeIndex;

            return indices[0];
        }

        private void RebuildChildren()
        {
            SynchronizeFavoriteButton();
            SynchronizeVariantSelector();
            SynchronizeCraftButton();
            ContentHeight = _projection == null
                ? ContentPadding * 2 + TextHeight
                : CalculateContentHeight(_projection, _queryProjection);
            Height.Set(ContentHeight, 0f);

            if (_projection == null)
            {
                HideUnusedIcons(0);
                Recalculate();
                return;
            }

            var iconIndex = 0;
            SetItemIcon(
                iconIndex++,
                _projection.Result,
                ContentPadding,
                ContentPadding,
                IconSize,
                navigationEnabled: true);

            int cursor = ContentPadding + IdentityHeight + GetVariantControlsHeight();
            cursor += GetSectionHeaderHeight();
            cursor += RowHeight + (_projection.IsAlchemy ? RowHeight : 0);
            cursor += CraftButtonTopGap + CraftButtonHeight;
            cursor += GetSectionHeaderHeight();

            foreach (RecipeDetailsIngredientProjection ingredient in _projection.Ingredients)
            {
                SetItemIcon(
                    iconIndex++,
                    ingredient.DisplayItem,
                    ContentPadding,
                    cursor + Math.Max(0, (ItemRowHeight - ItemIconSize) / 2),
                    ItemIconSize,
                    navigationEnabled: true);
                cursor += ItemRowHeight;

                if (!ingredient.IsRecipeGroup)
                    continue;

                foreach (RecipeDetailsItemReference validItem in ingredient.ValidItems)
                {
                    SetItemIcon(
                        iconIndex++,
                        validItem,
                        ContentPadding + AlternativeIndent,
                        cursor + Math.Max(0, (AlternativeRowHeight - AlternativeIconSize) / 2),
                        AlternativeIconSize,
                        navigationEnabled: true);
                    cursor += AlternativeRowHeight;
                }
            }

            if (_projection.Ingredients.Count == 0)
                cursor += RowHeight;

            cursor += GetSectionHeaderHeight();

            if (_projection.RequiresCraftingStation && _projection.CraftingStation != null)
            {
                SetItemIcon(
                    iconIndex++,
                    _projection.CraftingStation,
                    ContentPadding,
                    cursor + Math.Max(0, (ItemRowHeight - ItemIconSize) / 2),
                    ItemIconSize,
                    navigationEnabled: true);
            }

            HideUnusedIcons(iconIndex);
            Recalculate();
        }

        private void SynchronizeVariantSelector()
        {
            if (!ShouldShowVariantSelector())
            {
                HideVariantSelector();
                return;
            }

            IReadOnlyList<int> indices = _queryProjection.MatchingRecipeRuntimeIndices;
            var options = new string[indices.Count];
            var activeIndex = 0;

            for (var index = 0; index < indices.Count; index++)
            {
                options[index] = _localization.Format(
                    CompendiumTextKeys.Recipes.RecipeVariant,
                    index + 1,
                    indices.Count);

                if (indices[index] == _runtimeIndex)
                    activeIndex = index;
            }

            _variantSelector.SetOptions(options, activeIndex);
            _variantSelector.Left.Set(ContentPadding, 0f);
            _variantSelector.Top.Set(ContentPadding + IdentityHeight + VariantControlGap, 0f);
            _variantSelector.Width.Set(-ContentPadding * 2f, 1f);
            _variantSelector.Height.Set(VariantSelectorHeight, 0f);
        }

        private void HideVariantSelector()
        {
            _variantSelector.Width.Set(0f, 0f);
            _variantSelector.Height.Set(0f, 0f);
        }

        private void SynchronizeCraftButton()
        {
            if (_projection == null)
            {
                HideCraftButton();
                return;
            }

            _craftButton.Bind(_projection.Result.ItemId, _runtimeIndex);
            _craftButton.Left.Set(-ContentPadding - CraftButtonWidth, 1f);
            _craftButton.Top.Set(
                ContentPadding +
                IdentityHeight +
                GetVariantControlsHeight() +
                GetSectionHeaderHeight() +
                RowHeight +
                (_projection.IsAlchemy ? RowHeight : 0) +
                CraftButtonTopGap,
                0f);
            _craftButton.Width.Set(CraftButtonWidth, 0f);
            _craftButton.Height.Set(CraftButtonHeight, 0f);
        }

        private void HideCraftButton()
        {
            _craftButton.Clear();
            _craftButton.Width.Set(0f, 0f);
            _craftButton.Height.Set(0f, 0f);
        }

        private void SynchronizeFavoriteButton()
        {
            if (_projection == null)
            {
                HideFavoriteButton();
                return;
            }

            _favoriteButton.Left.Set(-ContentPadding - FavoriteButtonWidth, 1f);
            _favoriteButton.Top.Set(ContentPadding, 0f);
            _favoriteButton.Width.Set(FavoriteButtonWidth, 0f);
            _favoriteButton.Height.Set(FavoriteButtonHeight, 0f);
            _favoriteButton.IsActive = _projection.IsFavorite;
            _favoriteButton.Text = _projection.IsFavorite ? "★" : "☆";
            _favoriteButton.TooltipText = _localization.Get(
                _projection.IsFavorite
                    ? CompendiumTextKeys.Recipes.RemoveFavorite
                    : CompendiumTextKeys.Recipes.AddFavorite);
        }

        private void HideFavoriteButton()
        {
            _favoriteButton.IsActive = false;
            _favoriteButton.Width.Set(0f, 0f);
            _favoriteButton.Height.Set(0f, 0f);
        }

        private void ToggleFavorite()
        {
            if (_runtimeIndex < 0 || _projection == null)
                return;

            _favoriteState.Toggle(_runtimeIndex);
            _refreshRequired = true;
            Refresh();
        }

        private void OnVariantSelected(int selectedIndex)
        {
            if (_queryProjection == null ||
                selectedIndex < 0 ||
                selectedIndex >= _queryProjection.MatchingRecipeRuntimeIndices.Count)
            {
                return;
            }

            int runtimeIndex = _queryProjection.MatchingRecipeRuntimeIndices[selectedIndex];

            if (_runtimeIndex == runtimeIndex)
                return;

            BrowserDestination destination = _navigationState.CurrentDestination;

            if (_queryItemId > 0 &&
                destination is { Section: BrowserSection.Recipes, HasRecipeQuery: true } &&
                destination.RecipeQueryItemId == _queryItemId)
            {
                _navigationState.ReplaceCurrent(BrowserDestination.ForRecipe(runtimeIndex, _queryItemId));
            }

            _runtimeIndex = runtimeIndex;
            _preferredRuntimeIndex = -1;
            _refreshRequired = true;
            Refresh();
        }

        private void NormalizeSelectedRecipeDestination(int runtimeIndex)
        {
            BrowserDestination destination = _navigationState.CurrentDestination;

            if (!destination.IsRecipe ||
                !destination.HasRecipeQuery ||
                destination.RecipeQueryItemId != _queryItemId ||
                destination.RecipeRuntimeIndex == runtimeIndex)
            {
                return;
            }

            _navigationState.ReplaceCurrent(BrowserDestination.ForRecipe(runtimeIndex, _queryItemId));
        }

        private bool ShouldShowVariantSelector()
        {
            return _queryProjection is { MatchingRecipeCount: > 1 };
        }

        private bool ShouldShowVariantSummary()
        {
            return _queryProjection != null &&
                   _queryProjection.MatchingRecipeCount != _queryProjection.TotalRecipeCount;
        }

        private int GetVariantControlsHeight()
        {
            var height = 0;

            if (ShouldShowVariantSelector())
                height += VariantControlGap + VariantSelectorHeight;

            if (ShouldShowVariantSummary())
                height += VariantControlGap + VariantSummaryHeight;

            return height;
        }

        private static bool ContainsRuntimeIndex(IReadOnlyList<int> indices, int runtimeIndex)
        {
            if (runtimeIndex < 0)
                return false;

            for (var index = 0; index < indices.Count; index++)
            {
                if (indices[index] == runtimeIndex)
                    return true;
            }

            return false;
        }

        private void SetItemIcon(
            int index,
            RecipeDetailsItemReference item,
            int left,
            int top,
            int size,
            bool navigationEnabled)
        {
            while (_icons.Count <= index)
            {
                int createdIndex = _icons.Count;
                var icon = new VanillaItemIcon();
                icon.OnLeftClick += (evt, _) => OnItemIconClicked(createdIndex, evt);
                _icons.Add(icon);
                _iconNavigationEnabled.Add(false);
                Append(icon);
            }

            VanillaItemIcon itemIcon = _icons[index];
            itemIcon.ItemId = item.ItemId;
            itemIcon.IsMissing = item.IsMissing;
            itemIcon.Left.Set(left, 0f);
            itemIcon.Top.Set(top, 0f);
            itemIcon.Width.Set(size, 0f);
            itemIcon.Height.Set(size, 0f);
            _iconNavigationEnabled[index] = navigationEnabled;
        }

        private void HideUnusedIcons(int startIndex)
        {
            for (int index = startIndex; index < _icons.Count; index++)
            {
                VanillaItemIcon icon = _icons[index];
                icon.ItemId = 0;
                icon.IsMissing = false;
                icon.Width.Set(0f, 0f);
                icon.Height.Set(0f, 0f);
                _iconNavigationEnabled[index] = false;
            }
        }

        private void OnItemIconClicked(int index, UIMouseEvent evt)
        {
            if (index < 0 || index >= _icons.Count || !_iconNavigationEnabled[index])
                return;

            VanillaItemIcon icon = _icons[index];

            if (evt.Target != icon || icon.ItemId <= 0)
                return;

            _navigationState.Navigate(BrowserDestination.ForItem(icon.ItemId));
        }

        private int CalculateContentHeight(
            RecipeDetailsProjection projection,
            RecipeQueryDetailsProjection queryProjection)
        {
            int craftingRowsHeight =
                RowHeight + (projection.IsAlchemy ? RowHeight : 0) + CraftButtonTopGap + CraftButtonHeight;
            int ingredientRowsHeight = GetIngredientRowsHeight(projection);
            int requirementRowsHeight = GetRequirementRowsHeight(projection);
            var queryControlsHeight = 0;

            if (queryProjection != null)
            {
                if (queryProjection.MatchingRecipeCount > 1)
                    queryControlsHeight += VariantControlGap + VariantSelectorHeight;

                if (queryProjection.MatchingRecipeCount != queryProjection.TotalRecipeCount)
                    queryControlsHeight += VariantControlGap + VariantSummaryHeight;
            }

            return ContentPadding * 2 +
                   IdentityHeight +
                   queryControlsHeight +
                   GetSectionHeaderHeight() +
                   craftingRowsHeight +
                   GetSectionHeaderHeight() +
                   ingredientRowsHeight +
                   GetSectionHeaderHeight() +
                   requirementRowsHeight;
        }

        private void DrawIdentityText(RecipeDetailsProjection projection, int x, int y, int width)
        {
            int textX = x + IconSize + IdentityTextGap;
            int textWidth = Math.Max(0, width - IconSize - IdentityTextGap - FavoriteControlGap - FavoriteButtonWidth);
            string displayName = TruncatedTextPresentation.Truncate(
                projection.Result.Name,
                textWidth,
                out bool nameWasTruncated);

            if (displayName.Length > 0)
            {
                int nameY = y + 2;
                UIRenderer.DrawText(displayName, textX, nameY, UIColors.TextTitle);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    projection.Result.Name,
                    nameWasTruncated,
                    textX,
                    nameY,
                    textWidth,
                    TextHeight,
                    IsMouseHovering);
            }

            string fullStackText = _localization.Format(CompendiumTextKeys.Recipes.Makes, projection.ResultStack);
            string stackText = TruncatedTextPresentation.Truncate(fullStackText, textWidth, out bool stackWasTruncated);

            if (stackText.Length > 0)
            {
                int stackY = y + 20;
                UIRenderer.DrawText(stackText, textX, stackY, UIColors.TextDim);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    fullStackText,
                    stackWasTruncated,
                    textX,
                    stackY,
                    textWidth,
                    TextHeight,
                    IsMouseHovering);
            }
        }

        private void DrawCrafting(RecipeDetailsProjection projection, int x, int width, ref int cursor)
        {
            DrawTextRow(
                _localization.Format(
                    CompendiumTextKeys.Recipes.CraftableNowValue,
                    _localization.Get(
                        projection.IsCraftableNow ? CompendiumTextKeys.Common.Yes : CompendiumTextKeys.Common.No)),
                x,
                cursor,
                width,
                projection.IsCraftableNow ? UIColors.Success : UIColors.Text);
            cursor += RowHeight;

            if (projection.IsAlchemy)
            {
                DrawTextRow(_localization.Get(CompendiumTextKeys.Recipes.AlchemyYes), x, cursor, width);
                cursor += RowHeight;
            }

            cursor += CraftButtonTopGap + CraftButtonHeight;
        }

        private void DrawIngredientsText(RecipeDetailsProjection projection, int x, int width, ref int cursor)
        {
            if (projection.Ingredients.Count == 0)
            {
                DrawTextRow(_localization.Get(CompendiumTextKeys.Common.None), x, cursor, width, UIColors.TextDim);
                cursor += RowHeight;
                return;
            }

            foreach (RecipeDetailsIngredientProjection ingredient in projection.Ingredients)
            {
                string ingredientText = ingredient.IsRecipeGroup
                    ? _localization.Format(
                        CompendiumTextKeys.Recipes.IngredientAnyOf,
                        ingredient.DisplayItem.Name,
                        ingredient.Stack,
                        ingredient.ValidItems.Count)
                    : _localization.Format(
                        CompendiumTextKeys.Recipes.Ingredient,
                        ingredient.DisplayItem.Name,
                        ingredient.Stack);

                DrawItemRowText(ingredientText, x, cursor, width, ItemRowHeight, ItemIconSize, UIColors.Text);
                cursor += ItemRowHeight;

                if (!ingredient.IsRecipeGroup)
                    continue;

                foreach (RecipeDetailsItemReference validItem in ingredient.ValidItems)
                {
                    DrawItemRowText(
                        validItem.Name,
                        x + AlternativeIndent,
                        cursor,
                        Math.Max(0, width - AlternativeIndent),
                        AlternativeRowHeight,
                        AlternativeIconSize,
                        UIColors.TextDim);
                    cursor += AlternativeRowHeight;
                }
            }
        }

        private void DrawRequirementsText(RecipeDetailsProjection projection, int x, int width, ref int cursor)
        {
            var requirementCount = 0;

            if (projection.RequiresCraftingStation)
            {
                if (projection.CraftingStation != null)
                {
                    DrawItemRowText(
                        projection.CraftingStation.Name,
                        x,
                        cursor,
                        width,
                        ItemRowHeight,
                        ItemIconSize,
                        UIColors.Text);
                    cursor += ItemRowHeight;
                }
                else
                {
                    DrawTextRow(_localization.Get(CompendiumTextKeys.Recipes.CraftingStation), x, cursor, width);
                    cursor += RowHeight;
                }

                requirementCount++;
            }

            DrawRequirementIfNeeded(
                projection.RequiresWater,
                _localization.Get(CompendiumTextKeys.Recipes.Water),
                x,
                width,
                ref cursor,
                ref requirementCount);
            DrawRequirementIfNeeded(
                projection.RequiresHoney,
                _localization.Get(CompendiumTextKeys.Recipes.Honey),
                x,
                width,
                ref cursor,
                ref requirementCount);
            DrawRequirementIfNeeded(
                projection.RequiresLava,
                _localization.Get(CompendiumTextKeys.Recipes.Lava),
                x,
                width,
                ref cursor,
                ref requirementCount);
            DrawRequirementIfNeeded(
                projection.RequiresSnowBiome,
                _localization.Get(CompendiumTextKeys.Recipes.SnowBiome),
                x,
                width,
                ref cursor,
                ref requirementCount);
            DrawRequirementIfNeeded(
                projection.RequiresGraveyardBiome,
                _localization.Get(CompendiumTextKeys.Recipes.GraveyardBiome),
                x,
                width,
                ref cursor,
                ref requirementCount);
            DrawRequirementIfNeeded(
                projection.RequiresMechdusa,
                _localization.Get(CompendiumTextKeys.Recipes.Mechdusa),
                x,
                width,
                ref cursor,
                ref requirementCount);
            DrawRequirementIfNeeded(
                projection.RequiresTorchGodsFavor,
                _localization.Get(CompendiumTextKeys.Recipes.TorchGodsFavor),
                x,
                width,
                ref cursor,
                ref requirementCount);

            if (requirementCount != 0)
                return;

            DrawTextRow(
                _localization.Get(CompendiumTextKeys.Recipes.NoSpecialRequirements),
                x,
                cursor,
                width,
                UIColors.TextDim);
            cursor += RowHeight;
        }

        private void DrawRequirementIfNeeded(
            bool required,
            string text,
            int x,
            int width,
            ref int cursor,
            ref int requirementCount)
        {
            if (!required)
                return;

            DrawTextRow(text, x, cursor, width);
            cursor += RowHeight;
            requirementCount++;
        }

        private void DrawItemRowText(string text, int x, int y, int width, int height, int iconSize, Color4 textColor)
        {
            int textX = x + iconSize + ItemTextGap;
            int textWidth = Math.Max(0, x + width - textX);
            string displayText = TruncatedTextPresentation.Truncate(text, textWidth, out bool wasTruncated);
            int textY = y + Math.Max(0, (height - TextHeight) / 2);

            if (displayText.Length > 0)
            {
                UIRenderer.DrawText(displayText, textX, textY, textColor);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    text,
                    wasTruncated,
                    textX,
                    y,
                    textWidth,
                    height,
                    IsMouseHovering);
            }
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

        private void DrawTextRow(string text, int x, int y, int width)
        {
            DrawTextRow(text, x, y, width, UIColors.Text);
        }

        private void DrawTextRow(string text, int x, int y, int width, Color4 color)
        {
            if (width <= 0)
                return;

            string displayText = TruncatedTextPresentation.Truncate(text, width, out bool wasTruncated);

            if (displayText.Length > 0)
            {
                UIRenderer.DrawText(displayText, x, y + 1, color);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    text,
                    wasTruncated,
                    x,
                    y,
                    width,
                    RowHeight,
                    IsMouseHovering);
            }
        }

        private static int GetIngredientRowsHeight(RecipeDetailsProjection projection)
        {
            if (projection.Ingredients.Count == 0)
                return RowHeight;

            var height = 0;

            foreach (RecipeDetailsIngredientProjection ingredient in projection.Ingredients)
            {
                height += ItemRowHeight;

                if (ingredient.IsRecipeGroup)
                    height += ingredient.ValidItems.Count * AlternativeRowHeight;
            }

            return height;
        }

        private static int GetRequirementRowsHeight(RecipeDetailsProjection projection)
        {
            var height = 0;
            var count = 0;

            if (projection.RequiresCraftingStation)
            {
                height += projection.CraftingStation != null ? ItemRowHeight : RowHeight;
                count++;
            }

            AddRequirementHeight(projection.RequiresWater, ref height, ref count);
            AddRequirementHeight(projection.RequiresHoney, ref height, ref count);
            AddRequirementHeight(projection.RequiresLava, ref height, ref count);
            AddRequirementHeight(projection.RequiresSnowBiome, ref height, ref count);
            AddRequirementHeight(projection.RequiresGraveyardBiome, ref height, ref count);
            AddRequirementHeight(projection.RequiresMechdusa, ref height, ref count);
            AddRequirementHeight(projection.RequiresTorchGodsFavor, ref height, ref count);

            return count == 0 ? RowHeight : height;
        }

        private static void AddRequirementHeight(bool required, ref int height, ref int count)
        {
            if (!required)
                return;

            height += RowHeight;
            count++;
        }

        private static int GetSectionHeaderHeight()
        {
            return DetailsSectionLayout.HeaderHeight;
        }
    }
}