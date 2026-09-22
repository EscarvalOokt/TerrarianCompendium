using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Details;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ItemDetailsView : UIElement
    {
        private const int ContentPadding = 6;
        private const int IdentityHeight = 40;
        private const int IconSize = 36;
        private const int IdentityTextGap = 6;
        private const int ResearchIndicatorSize = 28;
        private const int CraftingActionSize = 28;
        private const int CraftButtonWidth = 28;
        private const int RowHeight = 18;
        private const int RelationTopGap = 4;
        private const int SourceGroupGap = 6;
        private const int RelationIconSize = 30;
        private const int RelationIconGap = 2;
        private const int TextHeight = 16;

        private const string FishingAndSeparatorText = "+";

        private readonly List<VanillaItemRelationButton> _armorSetButtons = new();
        private readonly VanillaBestiaryFilterCatalog _bestiaryFilterCatalog;
        private readonly DirectCraftButton _craftButton;
        private readonly VanillaIconButton _craftingRecipesButton;
        private readonly List<NpcRelationButton> _droppedByButtons = new();
        private readonly List<TextLabelElement> _fishingOrSeparators = new();
        private readonly List<FishingVariantRow> _fishingVariantRows = new();
        private readonly VanillaItemIcon _icon;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly CompendiumLocalization _localization;
        private readonly ItemDetailsModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly List<VanillaItemRelationButton> _openableContentButtons = new();
        private readonly List<VanillaItemRelationButton> _openableSourceButtons = new();
        private readonly List<NpcRelationButton> _purchasableButtons = new();
        private readonly RecipeFilterState _recipeFilterState;
        private readonly ResearchStatusIndicator _researchIndicator;
        private readonly List<VanillaIconValueElement> _statElements = new();
        private readonly VanillaIconButton _stationRecipesButton;
        private readonly VanillaCoinValueElement _valueElement;
        private readonly List<WorldLootSourceIcon> _worldSourceIcons = new();
        private int _itemId = -1;
        private long _localizationRevision = -1;
        private ItemDetailsProjection _projection;
        private bool _refreshRequired = true;

        private IReadOnlyList<ItemDetailsStatPresentation> _statPresentations =
            Array.Empty<ItemDetailsStatPresentation>();

        public ItemDetailsView(
            ItemDetailsModel model,
            BrowserNavigationState navigationState,
            ItemTextIndex itemTextIndex,
            RecipeStationDisplayIndex stationDisplayIndex,
            VanillaDirectCraftingService directCraftingService,
            JourneyResearchState journeyResearchState = null,
            RecipeFilterState recipeFilterState = null,
            VanillaBestiaryFilterCatalog bestiaryFilterCatalog = null,
            CompendiumLocalization localization = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _journeyResearchState = journeyResearchState;
            _recipeFilterState = recipeFilterState;
            _bestiaryFilterCatalog = bestiaryFilterCatalog;
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            Width = StyleDimension.Fill;
            SetPadding(0f);

            _icon = new VanillaItemIcon(_journeyResearchState);
            Append(_icon);

            _valueElement = new VanillaCoinValueElement();
            Append(_valueElement);

            _researchIndicator = new ResearchStatusIndicator(_localization);
            Append(_researchIndicator);

            _craftingRecipesButton = new VanillaIconButton(
                (bounds, hovered) => VanillaPresentationIcons.DrawCraftToggle(bounds, 0, 1, hovered),
                NavigateToCraftingRecipes);
            Append(_craftingRecipesButton);

            _craftButton = new DirectCraftButton(
                directCraftingService,
                itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex)),
                stationDisplayIndex,
                _localization,
                _journeyResearchState);
            Append(_craftButton);

            _stationRecipesButton = new VanillaIconButton(
                (bounds, hovered) => VanillaPresentationIcons.DrawCraftToggle(bounds, 4, 5, hovered),
                NavigateToStationRecipes)
            {
                TooltipText = _localization.Get(CompendiumTextKeys.ItemDetails.StationRecipesTooltip)
            };
            Append(_stationRecipesButton);

            HideIcon();
            _valueElement.Hide();
            HideUnusedStatElements(0);
            HideResearchIndicator();
            HideCraftingRecipesButton();
            HideCraftButton();
            HideStationRecipesButton();
        }

        public int ContentHeight =>
            _projection == null
                ? ContentPadding * 2 + TextHeight
                : CalculateContentHeight(_projection, GetAvailableContentWidth());

        public bool HasOpenTransientSurface => _craftButton.IsPopoverOpen;

        public void AttachPopoverHost(UIElement host)
        {
            _craftButton.AttachPopoverHost(host);
        }

        public bool TryCloseTransientSurface()
        {
            return _craftButton.TryClosePopover();
        }

        public void ShowItem(int itemId)
        {
            if (_itemId == itemId)
            {
                Refresh();
                return;
            }

            _itemId = itemId;
            _refreshRequired = true;
            Refresh();
        }

        public void Clear()
        {
            _itemId = -1;
            _projection = null;
            _refreshRequired = false;
            HideIcon();
            _valueElement.Hide();
            _statPresentations = Array.Empty<ItemDetailsStatPresentation>();
            HideUnusedStatElements(0);
            HideResearchIndicator();
            HideCraftingRecipesButton();
            HideCraftButton();
            HideStationRecipesButton();
            HideUnusedArmorSetButtons(0);
            HideUnusedNpcRelationButtons(_droppedByButtons, 0);
            HideUnusedNpcRelationButtons(_purchasableButtons, 0);
            HideUnusedWorldSourceIcons(0);
            HideUnusedOpenableSourceButtons(0);
            HideUnusedFishingVariantRows(0);
            HideUnusedFishingOrSeparators(0);
            HideUnusedOpenableContentButtons(0);
            Height.Set(ContentPadding * 2 + TextHeight, 0f);
        }

        public int GetContentHeight(int itemId)
        {
            if (!_model.TryGetProjection(itemId, out ItemDetailsProjection projection))
                return ContentPadding * 2 + TextHeight;

            return CalculateContentHeight(projection, GetAvailableContentWidth());
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            Refresh();
            base.Update(gameTime);
        }

        public override void OnDeactivate()
        {
            _craftButton.TryClosePopover();
            base.OnDeactivate();
        }

        public override void RecalculateChildren()
        {
            if (_projection != null)
                LayoutDynamicChildren();

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
                DrawTextRow(_localization.Get(CompendiumTextKeys.ItemDetails.MissingItem), x, y, width);
                return;
            }

            DrawIdentityText(_projection, x, y, width);
            int cursor = y + IdentityHeight;

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.ItemDetails.Stats), x, width, ref cursor);
            cursor += CalculateStatsAreaHeight(_projection, width);

            if (!string.IsNullOrWhiteSpace(_projection.Description))
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawSectionTitle(_localization.Get(CompendiumTextKeys.ItemDetails.Description), x, width, ref cursor);
                DrawDescription(_projection.Description, x, width, ref cursor);
            }

            if (_projection.ArmorSets.Count > 0)
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawSectionTitle(
                    _localization.Get(CompendiumTextKeys.ItemDetails.ArmorSetsSection),
                    x,
                    width,
                    ref cursor);
                cursor += CalculateRelationGridHeight(_projection.ArmorSets.Count, width);
            }

            if (HasSourcesSection(_projection))
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawSectionTitle(_localization.Get(CompendiumTextKeys.ItemDetails.Sources), x, width, ref cursor);
                DrawSources(_projection, x, width, ref cursor);
            }

            if (HasPossibleDropsSection(_projection))
            {
                DrawSectionSeparator(x, width, ref cursor);
                DrawSectionTitle(_localization.Get(CompendiumTextKeys.ItemDetails.PossibleDrops), x, width, ref cursor);
                cursor += CalculateRelationGridHeight(_projection.OpenableContents.Count, width);
            }

            if (!HasRecipeStationSection(_projection))
                return;

            DrawSectionSeparator(x, width, ref cursor);
            DrawSectionTitle(_localization.Get(CompendiumTextKeys.ItemDetails.RecipesSection), x, width, ref cursor);
            DrawRecipeStation(x, width, ref cursor);
        }

        private void DrawSources(ItemDetailsProjection projection, int x, int width, ref int cursor)
        {
            var hasPreviousGroup = false;

            if (projection.HasRecipeRelations)
            {
                AddGroupGap(ref cursor, ref hasPreviousGroup);
                DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.Crafting), x, width, ref cursor);
                cursor += RelationTopGap + CraftingActionSize;
            }

            if (projection.DroppedByNpcSources.Count > 0)
            {
                AddGroupGap(ref cursor, ref hasPreviousGroup);
                DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.NpcDrops), x, width, ref cursor);
                cursor += RelationTopGap + CalculateRelationGridHeight(projection.DroppedByNpcSources.Count, width);
            }

            if (projection.PurchasableFromMerchants.Count > 0)
            {
                AddGroupGap(ref cursor, ref hasPreviousGroup);
                DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.Purchasable), x, width, ref cursor);
                cursor += RelationTopGap +
                          CalculateRelationGridHeight(projection.PurchasableFromMerchants.Count, width);
            }

            if (projection.WorldSources.Count > 0)
            {
                AddGroupGap(ref cursor, ref hasPreviousGroup);
                DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.WorldSources), x, width, ref cursor);
                cursor += RelationTopGap + CalculateRelationGridHeight(projection.WorldSources.Count, width);
            }

            if (projection.OpenableSources.Count > 0)
            {
                AddGroupGap(ref cursor, ref hasPreviousGroup);
                DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.OpenableItems), x, width, ref cursor);
                cursor += RelationTopGap + CalculateRelationGridHeight(projection.OpenableSources.Count, width);
            }

            if (projection.FishingVariants.Count > 0)
            {
                AddGroupGap(ref cursor, ref hasPreviousGroup);
                DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.FishingSection), x, width, ref cursor);
                cursor += RelationTopGap + CalculateFishingVariantsHeight(projection.FishingVariants, width);
            }
        }

        private void DrawRecipeStation(int x, int width, ref int cursor)
        {
            DrawGroupLabel(_localization.Get(CompendiumTextKeys.ItemDetails.CraftingStation), x, width, ref cursor);
            cursor += RelationTopGap + CraftingActionSize;
        }

        private void SynchronizeLocalization()
        {
            if (_localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _stationRecipesButton.TooltipText = _localization.Get(CompendiumTextKeys.ItemDetails.StationRecipesTooltip);

            foreach (TextLabelElement separator in _fishingOrSeparators)
                separator.Text = _localization.Get(CompendiumTextKeys.Common.Or);

            _refreshRequired = true;
        }

        private void Refresh()
        {
            ItemDetailsProjection nextProjection = null;

            if (_itemId > 0)
                _model.TryGetProjection(_itemId, out nextProjection);

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
                _valueElement.Hide();
                _statPresentations = Array.Empty<ItemDetailsStatPresentation>();
                HideUnusedStatElements(0);
                HideResearchIndicator();
                HideCraftingRecipesButton();
                HideCraftButton();
                HideStationRecipesButton();
                HideUnusedArmorSetButtons(0);
                HideUnusedNpcRelationButtons(_droppedByButtons, 0);
                HideUnusedNpcRelationButtons(_purchasableButtons, 0);
                HideUnusedWorldSourceIcons(0);
                HideUnusedOpenableSourceButtons(0);
                HideUnusedFishingVariantRows(0);
                HideUnusedFishingOrSeparators(0);
                HideUnusedOpenableContentButtons(0);
                Height.Set(ContentPadding * 2 + TextHeight, 0f);
                Recalculate();
                return;
            }

            _icon.ItemId = _projection.ItemId;
            _icon.IsMissing = !_projection.IsFound;
            _icon.Left.Set(ContentPadding, 0f);
            _icon.Top.Set(ContentPadding, 0f);
            _icon.Width.Set(IconSize, 0f);
            _icon.Height.Set(IconSize, 0f);

            _researchIndicator.Bind(_projection.ResearchStatus);

            _valueElement.Bind(
                _projection.Value,
                leadingItemId: 0,
                showCopperWhenZero: false,
                tooltipText: _localization.Get(CompendiumTextKeys.ItemDetails.BaseValueTooltip));

            _statPresentations = ItemDetailsPresentation.BuildStats(_projection, _localization);
            EnsureStatElementCapacity(_statPresentations.Count);
            for (var index = 0; index < _statPresentations.Count; index++)
                BindStatElement(_statElements[index], _statPresentations[index]);
            HideUnusedStatElements(_statPresentations.Count);

            EnsureArmorSetButtonCapacity(_projection.ArmorSets.Count);
            for (var index = 0; index < _projection.ArmorSets.Count; index++)
            {
                ItemDetailsArmorSetReference reference = _projection.ArmorSets[index];
                _armorSetButtons[index].Bind(reference.RepresentativeItemId, isMissing: false, reference.ArmorSetId);
            }

            HideUnusedArmorSetButtons(_projection.ArmorSets.Count);

            EnsureNpcRelationButtonCapacity(_droppedByButtons, _projection.DroppedByNpcSources.Count);
            for (var index = 0; index < _projection.DroppedByNpcSources.Count; index++)
            {
                ItemDetailsNpcSourceReference reference = _projection.DroppedByNpcSources[index];
                _droppedByButtons[index].Bind(reference.NpcNetId, reference.Name);
            }

            HideUnusedNpcRelationButtons(_droppedByButtons, _projection.DroppedByNpcSources.Count);

            EnsureNpcRelationButtonCapacity(_purchasableButtons, _projection.PurchasableFromMerchants.Count);
            for (var index = 0; index < _projection.PurchasableFromMerchants.Count; index++)
            {
                ItemDetailsNpcSourceReference reference = _projection.PurchasableFromMerchants[index];
                _purchasableButtons[index].Bind(reference.NpcNetId, reference.Name);
            }

            HideUnusedNpcRelationButtons(_purchasableButtons, _projection.PurchasableFromMerchants.Count);

            EnsureWorldSourceIconCapacity(_projection.WorldSources.Count);
            for (var index = 0; index < _projection.WorldSources.Count; index++)
                _worldSourceIcons[index].Bind(_projection.WorldSources[index]);

            HideUnusedWorldSourceIcons(_projection.WorldSources.Count);

            EnsureOpenableSourceButtonCapacity(_projection.OpenableSources.Count);
            for (var index = 0; index < _projection.OpenableSources.Count; index++)
            {
                ItemDetailsOpenableSourceReference reference = _projection.OpenableSources[index];
                _openableSourceButtons[index].Bind(reference.ItemId, !reference.IsFound);
            }

            HideUnusedOpenableSourceButtons(_projection.OpenableSources.Count);

            EnsureFishingVariantRowCapacity(_projection.FishingVariants.Count);
            for (var index = 0; index < _projection.FishingVariants.Count; index++)
                _fishingVariantRows[index].Bind(_projection.FishingVariants[index]);

            HideUnusedFishingVariantRows(_projection.FishingVariants.Count);

            int fishingOrSeparatorCount = Math.Max(0, _projection.FishingVariants.Count - 1);
            EnsureFishingOrSeparatorCapacity(fishingOrSeparatorCount);
            HideUnusedFishingOrSeparators(fishingOrSeparatorCount);

            EnsureOpenableContentButtonCapacity(_projection.OpenableContents.Count);
            for (var index = 0; index < _projection.OpenableContents.Count; index++)
            {
                ItemDetailsOpenableContentReference reference = _projection.OpenableContents[index];
                _openableContentButtons[index].Bind(reference.ItemId, !reference.IsFound);
            }

            HideUnusedOpenableContentButtons(_projection.OpenableContents.Count);

            BindCraftButton();
            Recalculate();
        }

        private void BindCraftButton()
        {
            if (_projection == null || _projection.ProducingRecipeCount <= 0)
            {
                _craftButton.Clear();
                return;
            }

            _craftButton.Bind(_projection.ItemId);
        }

        private void LayoutDynamicChildren()
        {
            int contentWidth = GetAvailableContentWidth();
            LayoutResearchIndicator(contentWidth);
            LayoutStats(contentWidth);
            int columns = CalculateRelationColumns(contentWidth);

            if (_projection.ArmorSets.Count > 0)
            {
                LayoutItemRelationGrid(
                    _armorSetButtons,
                    _projection.ArmorSets.Count,
                    GetArmorSetsContentTop(_projection, contentWidth),
                    columns);
            }

            int sourceCursor = GetSourcesContentTop(_projection, contentWidth);
            var hasPreviousSourceGroup = false;

            if (_projection.HasRecipeRelations)
            {
                AddGroupGap(ref sourceCursor, ref hasPreviousSourceGroup);
                sourceCursor += RowHeight + RelationTopGap;

                _craftingRecipesButton.TooltipText = _localization.Get(CompendiumTextKeys.ItemDetails.RecipesSection);
                _craftingRecipesButton.Left.Set(ContentPadding, 0f);
                _craftingRecipesButton.Top.Set(sourceCursor, 0f);
                _craftingRecipesButton.Width.Set(CraftingActionSize, 0f);
                _craftingRecipesButton.Height.Set(CraftingActionSize, 0f);

                if (_projection.ProducingRecipeCount > 0)
                {
                    _craftButton.Left.Set(ContentPadding + Math.Max(0, contentWidth - CraftButtonWidth), 0f);
                    _craftButton.Top.Set(sourceCursor, 0f);
                    _craftButton.Width.Set(CraftButtonWidth, 0f);
                    _craftButton.Height.Set(CraftingActionSize, 0f);
                }
                else
                {
                    HideCraftButton();
                }

                sourceCursor += CraftingActionSize;
            }
            else
            {
                HideCraftingRecipesButton();
                HideCraftButton();
            }

            if (_projection.DroppedByNpcSources.Count > 0)
            {
                AddGroupGap(ref sourceCursor, ref hasPreviousSourceGroup);
                sourceCursor += RowHeight + RelationTopGap;
                LayoutNpcGrid(_droppedByButtons, _projection.DroppedByNpcSources.Count, sourceCursor, columns);
                sourceCursor += CalculateRelationGridHeight(_projection.DroppedByNpcSources.Count, contentWidth);
            }

            if (_projection.PurchasableFromMerchants.Count > 0)
            {
                AddGroupGap(ref sourceCursor, ref hasPreviousSourceGroup);
                sourceCursor += RowHeight + RelationTopGap;
                LayoutNpcGrid(_purchasableButtons, _projection.PurchasableFromMerchants.Count, sourceCursor, columns);
                sourceCursor += CalculateRelationGridHeight(_projection.PurchasableFromMerchants.Count, contentWidth);
            }

            if (_projection.WorldSources.Count > 0)
            {
                AddGroupGap(ref sourceCursor, ref hasPreviousSourceGroup);
                sourceCursor += RowHeight + RelationTopGap;
                LayoutWorldSourceGrid(_worldSourceIcons, _projection.WorldSources.Count, sourceCursor, columns);
                sourceCursor += CalculateRelationGridHeight(_projection.WorldSources.Count, contentWidth);
            }

            if (_projection.OpenableSources.Count > 0)
            {
                AddGroupGap(ref sourceCursor, ref hasPreviousSourceGroup);
                sourceCursor += RowHeight + RelationTopGap;
                LayoutItemRelationGrid(
                    _openableSourceButtons,
                    _projection.OpenableSources.Count,
                    sourceCursor,
                    columns);
                sourceCursor += CalculateRelationGridHeight(_projection.OpenableSources.Count, contentWidth);
            }

            if (_projection.FishingVariants.Count > 0)
            {
                AddGroupGap(ref sourceCursor, ref hasPreviousSourceGroup);
                sourceCursor += RowHeight + RelationTopGap;
                LayoutFishingVariantRows(
                    _fishingVariantRows,
                    _fishingOrSeparators,
                    _projection.FishingVariants,
                    sourceCursor,
                    contentWidth);
            }

            if (_projection.OpenableContents.Count > 0)
            {
                LayoutItemRelationGrid(
                    _openableContentButtons,
                    _projection.OpenableContents.Count,
                    GetPossibleDropsContentTop(_projection, contentWidth),
                    columns);
            }

            if (_projection.CraftingStationRequiredTileId.HasValue)
            {
                int stationCursor = GetRecipeStationContentTop(_projection, contentWidth) + RowHeight + RelationTopGap;
                _stationRecipesButton.Left.Set(ContentPadding, 0f);
                _stationRecipesButton.Top.Set(stationCursor, 0f);
                _stationRecipesButton.Width.Set(CraftingActionSize, 0f);
                _stationRecipesButton.Height.Set(CraftingActionSize, 0f);
            }
            else
            {
                HideStationRecipesButton();
            }
        }

        private void LayoutStats(int contentWidth)
        {
            int statsTop = ContentPadding + IdentityHeight + GetSectionHeaderHeight();
            _valueElement.Left.Set(ContentPadding, 0f);
            _valueElement.Top.Set(statsTop, 0f);
            _valueElement.Width.Set(contentWidth, 0f);
            _valueElement.Height.Set(VanillaCoinValueElement.RowHeight, 0f);

            int gridTop = statsTop + VanillaCoinValueElement.RowHeight;

            if (_statPresentations.Count > 0)
                gridTop += VanillaIconValueLayout.RowGap;

            for (var index = 0; index < _statPresentations.Count; index++)
            {
                VanillaIconValueCellBounds bounds = VanillaIconValueLayout.CalculateCellBounds(
                    index,
                    _statPresentations.Count,
                    contentWidth);
                VanillaIconValueElement element = _statElements[index];
                element.Left.Set(ContentPadding + bounds.X, 0f);
                element.Top.Set(gridTop + bounds.Y, 0f);
                element.Width.Set(bounds.Width, 0f);
                element.Height.Set(bounds.Height, 0f);
            }
        }

        private void BindStatElement(VanillaIconValueElement element, ItemDetailsStatPresentation presentation)
        {
            switch (presentation.Kind)
            {
                case ItemDetailsStatKind.Rarity:
                    int rarity = _projection.Rarity;
                    element.Bind(
                        VanillaPresentationIcons.DrawBestiaryRankLight,
                        presentation.ValueText,
                        presentation.TooltipText,
                        () => ItemDetailsPresentation.GetRarityColor(rarity));
                    break;
                case ItemDetailsStatKind.Damage:
                    BindItemTextureStat(element, ItemID.IronBroadsword, presentation);
                    break;
                case ItemDetailsStatKind.Defense:
                    element.Bind(
                        VanillaPresentationIcons.DrawDefenseCounterIcon,
                        presentation.ValueText,
                        presentation.TooltipText);
                    break;
                case ItemDetailsStatKind.Knockback:
                    BindItemTextureStat(element, ItemID.CobaltShield, presentation);
                    break;
                case ItemDetailsStatKind.CriticalHitChance:
                    BindItemTextureStat(element, ItemID.DestroyerEmblem, presentation);
                    break;
                case ItemDetailsStatKind.UseTime:
                    BindItemTextureStat(element, ItemID.Stopwatch, presentation);
                    break;
                case ItemDetailsStatKind.TagDamage:
                    BindItemTextureStat(element, ItemID.BlandWhip, presentation);
                    break;
                case ItemDetailsStatKind.PickPower:
                    BindItemTextureStat(element, ItemID.IronPickaxe, presentation);
                    break;
                case ItemDetailsStatKind.AxePower:
                    BindItemTextureStat(element, ItemID.IronAxe, presentation);
                    break;
                case ItemDetailsStatKind.HammerPower:
                    BindItemTextureStat(element, ItemID.IronHammer, presentation);
                    break;
                case ItemDetailsStatKind.FishingPower:
                    BindItemTextureStat(element, ItemID.WoodFishingPole, presentation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(presentation),
                        presentation.Kind,
                        "Unsupported Item Details stat kind.");
            }
        }

        private static void BindItemTextureStat(
            VanillaIconValueElement element,
            int itemId,
            ItemDetailsStatPresentation presentation)
        {
            AsyncItemIconRenderer.RequestAsync(itemId);
            element.Bind(
                bounds => AsyncItemIconRenderer.Draw(itemId, bounds.X, bounds.Y, bounds.Width, bounds.Height),
                presentation.ValueText,
                presentation.TooltipText);
        }

        private void EnsureStatElementCapacity(int count)
        {
            while (_statElements.Count < count)
            {
                var element = new VanillaIconValueElement();
                _statElements.Add(element);
                Append(element);
            }
        }

        private void HideUnusedStatElements(int startIndex)
        {
            for (int index = startIndex; index < _statElements.Count; index++)
                _statElements[index].Hide();
        }

        private int CalculateStatsAreaHeight(ItemDetailsProjection projection, int contentWidth)
        {
            int statCount = ItemDetailsPresentation.BuildStats(projection, _localization).Count;
            int height = VanillaCoinValueElement.RowHeight;

            if (statCount > 0)
                height += VanillaIconValueLayout.RowGap +
                          VanillaIconValueLayout.CalculateHeight(statCount, contentWidth);

            return height;
        }

        private void LayoutResearchIndicator(int contentWidth)
        {
            if (!ShouldShowResearchIndicator(_projection.ResearchStatus))
            {
                HideResearchIndicator();
                return;
            }

            _researchIndicator.Left.Set(ContentPadding + Math.Max(0, contentWidth - ResearchIndicatorSize), 0f);
            _researchIndicator.Top.Set(ContentPadding + Math.Max(0, (IdentityHeight - ResearchIndicatorSize) / 2), 0f);
            _researchIndicator.Width.Set(ResearchIndicatorSize, 0f);
            _researchIndicator.Height.Set(ResearchIndicatorSize, 0f);
        }

        private static void LayoutItemRelationGrid(
            IReadOnlyList<VanillaItemRelationButton> buttons,
            int count,
            int top,
            int columns)
        {
            for (var index = 0; index < count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                VanillaItemRelationButton button = buttons[index];
                button.Left.Set(ContentPadding + column * (RelationIconSize + RelationIconGap), 0f);
                button.Top.Set(top + row * (RelationIconSize + RelationIconGap), 0f);
                button.Width.Set(RelationIconSize, 0f);
                button.Height.Set(RelationIconSize, 0f);
            }
        }

        private static void LayoutNpcGrid(IReadOnlyList<NpcRelationButton> buttons, int count, int top, int columns)
        {
            for (var index = 0; index < count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                NpcRelationButton button = buttons[index];
                button.Left.Set(ContentPadding + column * (RelationIconSize + RelationIconGap), 0f);
                button.Top.Set(top + row * (RelationIconSize + RelationIconGap), 0f);
                button.Width.Set(RelationIconSize, 0f);
                button.Height.Set(RelationIconSize, 0f);
            }
        }

        private static void LayoutWorldSourceGrid(
            IReadOnlyList<WorldLootSourceIcon> icons,
            int count,
            int top,
            int columns)
        {
            for (var index = 0; index < count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                WorldLootSourceIcon icon = icons[index];
                icon.Left.Set(ContentPadding + column * (RelationIconSize + RelationIconGap), 0f);
                icon.Top.Set(top + row * (RelationIconSize + RelationIconGap), 0f);
                icon.Width.Set(RelationIconSize, 0f);
                icon.Height.Set(RelationIconSize, 0f);
            }
        }

        private void LayoutFishingVariantRows(
            IReadOnlyList<FishingVariantRow> rows,
            IReadOnlyList<TextLabelElement> orSeparators,
            IReadOnlyList<ItemDetailsFishingVariantReference> variants,
            int top,
            int contentWidth)
        {
            int availableWidth = Math.Max(0, contentWidth);
            int cursorTop = top;
            int orTextWidth = UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.Common.Or));

            for (var index = 0; index < variants.Count; index++)
            {
                if (index > 0)
                {
                    cursorTop += SourceGroupGap;

                    TextLabelElement orSeparator = orSeparators[index - 1];
                    orSeparator.Left.Set(ContentPadding, 0f);
                    orSeparator.Top.Set(cursorTop, 0f);
                    orSeparator.Width.Set(orTextWidth, 0f);
                    orSeparator.Height.Set(TextHeight, 0f);

                    cursorTop += TextHeight + SourceGroupGap;
                }

                ItemDetailsFishingVariantReference variant = variants[index];
                int rowWidth = CalculateFishingVariantWidth(variant, availableWidth);
                int rowHeight = CalculateFishingVariantHeight(variant, rowWidth);
                rows[index].Layout(ContentPadding, cursorTop, rowWidth, rowHeight);
                cursorTop += rowHeight;
            }
        }

        private void NavigateToCraftingRecipes()
        {
            if (_projection == null || !_projection.RecipeDataAvailable || !_projection.HasRecipeRelations)
                return;

            _navigationState.Navigate(BrowserDestination.ForRecipeQuery(_projection.ItemId));
        }

        private void NavigateToStationRecipes()
        {
            int? requiredTileId = _projection?.CraftingStationRequiredTileId;

            if (!requiredTileId.HasValue || _recipeFilterState == null)
                return;

            _recipeFilterState.RequiredTileId = requiredTileId.Value;
            _navigationState.Navigate(BrowserDestination.ForSection(BrowserSection.Recipes));
        }

        private void NavigateToArmorSet(int armorSetId)
        {
            if (armorSetId > 0)
                _navigationState.Navigate(BrowserDestination.ForArmorSet(armorSetId));
        }

        private void EnsureArmorSetButtonCapacity(int count)
        {
            while (_armorSetButtons.Count < count)
            {
                var button = new VanillaItemRelationButton(NavigateToArmorSet);
                _armorSetButtons.Add(button);
                Append(button);
            }
        }

        private void HideUnusedArmorSetButtons(int startIndex)
        {
            for (int index = startIndex; index < _armorSetButtons.Count; index++)
                _armorSetButtons[index].Hide();
        }

        private void NavigateToNpcSource(int npcNetId)
        {
            _navigationState.Navigate(BrowserDestination.ForNpc(npcNetId));
        }

        private void EnsureNpcRelationButtonCapacity(List<NpcRelationButton> buttons, int count)
        {
            while (buttons.Count < count)
            {
                var button = new NpcRelationButton(NavigateToNpcSource);
                buttons.Add(button);
                Append(button);
            }
        }

        private static void HideUnusedNpcRelationButtons(List<NpcRelationButton> buttons, int startIndex)
        {
            for (int index = startIndex; index < buttons.Count; index++)
                buttons[index].Hide();
        }

        private void EnsureWorldSourceIconCapacity(int count)
        {
            while (_worldSourceIcons.Count < count)
            {
                var icon = new WorldLootSourceIcon(_localization);
                _worldSourceIcons.Add(icon);
                Append(icon);
            }
        }

        private void HideUnusedWorldSourceIcons(int startIndex)
        {
            for (int index = startIndex; index < _worldSourceIcons.Count; index++)
                _worldSourceIcons[index].Hide();
        }

        private void EnsureFishingVariantRowCapacity(int count)
        {
            while (_fishingVariantRows.Count < count)
            {
                var row = new FishingVariantRow(_bestiaryFilterCatalog, _localization);
                _fishingVariantRows.Add(row);
                Append(row);
            }
        }

        private void HideUnusedFishingVariantRows(int startIndex)
        {
            for (int index = startIndex; index < _fishingVariantRows.Count; index++)
                _fishingVariantRows[index].Hide();
        }

        private void EnsureFishingOrSeparatorCapacity(int count)
        {
            while (_fishingOrSeparators.Count < count)
            {
                var separator = new TextLabelElement(_localization.Get(CompendiumTextKeys.Common.Or), UIColors.Text)
                {
                    IgnoresMouseInteraction = true
                };
                _fishingOrSeparators.Add(separator);
                Append(separator);
            }
        }

        private void HideUnusedFishingOrSeparators(int startIndex)
        {
            for (int index = startIndex; index < _fishingOrSeparators.Count; index++)
            {
                _fishingOrSeparators[index].Width.Set(0f, 0f);
                _fishingOrSeparators[index].Height.Set(0f, 0f);
            }
        }

        private void NavigateToOpenableSource(int itemId)
        {
            if (itemId > 0)
                _navigationState.Navigate(BrowserDestination.ForItem(itemId));
        }

        private void EnsureOpenableSourceButtonCapacity(int count)
        {
            while (_openableSourceButtons.Count < count)
            {
                var button = new VanillaItemRelationButton(NavigateToOpenableSource, _journeyResearchState);
                _openableSourceButtons.Add(button);
                Append(button);
            }
        }

        private void HideUnusedOpenableSourceButtons(int startIndex)
        {
            for (int index = startIndex; index < _openableSourceButtons.Count; index++)
                _openableSourceButtons[index].Hide();
        }

        private void EnsureOpenableContentButtonCapacity(int count)
        {
            while (_openableContentButtons.Count < count)
            {
                var button = new VanillaItemRelationButton(NavigateToOpenableSource, _journeyResearchState);
                _openableContentButtons.Add(button);
                Append(button);
            }
        }

        private void HideUnusedOpenableContentButtons(int startIndex)
        {
            for (int index = startIndex; index < _openableContentButtons.Count; index++)
                _openableContentButtons[index].Hide();
        }

        private void HideIcon()
        {
            _icon.ItemId = 0;
            _icon.Width.Set(0f, 0f);
            _icon.Height.Set(0f, 0f);
        }

        private void HideResearchIndicator()
        {
            _researchIndicator.Width.Set(0f, 0f);
            _researchIndicator.Height.Set(0f, 0f);
        }

        private void HideCraftingRecipesButton()
        {
            _craftingRecipesButton.Width.Set(0f, 0f);
            _craftingRecipesButton.Height.Set(0f, 0f);
        }

        private void HideCraftButton()
        {
            _craftButton.Clear();
            _craftButton.Width.Set(0f, 0f);
            _craftButton.Height.Set(0f, 0f);
        }

        private void HideStationRecipesButton()
        {
            _stationRecipesButton.Width.Set(0f, 0f);
            _stationRecipesButton.Height.Set(0f, 0f);
        }

        private int GetAvailableContentWidth()
        {
            return Math.Max(0, (int)GetInnerDimensions().Width - ContentPadding * 2);
        }

        private int CalculateContentHeight(ItemDetailsProjection projection, int contentWidth)
        {
            int height = ContentPadding * 2 +
                         IdentityHeight +
                         GetSectionHeaderHeight() +
                         CalculateStatsAreaHeight(projection, contentWidth);

            if (!string.IsNullOrWhiteSpace(projection.Description))
                height += GetSectionHeaderHeight() + GetDescriptionRowCount(projection.Description) * RowHeight;

            if (projection.ArmorSets.Count > 0)
                height += GetSectionHeaderHeight() +
                          CalculateRelationGridHeight(projection.ArmorSets.Count, contentWidth);

            if (HasSourcesSection(projection))
                height += GetSectionHeaderHeight() + GetSourcesAreaHeight(projection, contentWidth);

            if (HasPossibleDropsSection(projection))
            {
                height += GetSectionHeaderHeight() +
                          CalculateRelationGridHeight(projection.OpenableContents.Count, contentWidth);
            }

            if (HasRecipeStationSection(projection))
                height += GetSectionHeaderHeight() + GetRecipeStationAreaHeight();

            return height;
        }

        private static bool HasSourcesSection(ItemDetailsProjection projection)
        {
            return projection.HasRecipeRelations ||
                   projection.DroppedByNpcSources.Count > 0 ||
                   projection.PurchasableFromMerchants.Count > 0 ||
                   projection.WorldSources.Count > 0 ||
                   projection.OpenableSources.Count > 0 ||
                   projection.FishingVariants.Count > 0;
        }

        private static bool HasPossibleDropsSection(ItemDetailsProjection projection)
        {
            return projection.OpenableContents.Count > 0;
        }

        private static bool HasRecipeStationSection(ItemDetailsProjection projection)
        {
            return projection.CraftingStationRequiredTileId.HasValue;
        }

        private static int GetSourcesAreaHeight(ItemDetailsProjection projection, int contentWidth)
        {
            var height = 0;
            var hasGroup = false;
            AddGroupHeight(
                ref height,
                projection.HasRecipeRelations ? RowHeight + RelationTopGap + CraftingActionSize : 0,
                ref hasGroup);
            AddGroupHeight(
                ref height,
                projection.DroppedByNpcSources.Count > 0
                    ? RowHeight +
                      RelationTopGap +
                      CalculateRelationGridHeight(projection.DroppedByNpcSources.Count, contentWidth)
                    : 0,
                ref hasGroup);
            AddGroupHeight(
                ref height,
                projection.PurchasableFromMerchants.Count > 0
                    ? RowHeight +
                      RelationTopGap +
                      CalculateRelationGridHeight(projection.PurchasableFromMerchants.Count, contentWidth)
                    : 0,
                ref hasGroup);
            AddGroupHeight(
                ref height,
                projection.WorldSources.Count > 0
                    ? RowHeight +
                      RelationTopGap +
                      CalculateRelationGridHeight(projection.WorldSources.Count, contentWidth)
                    : 0,
                ref hasGroup);
            AddGroupHeight(
                ref height,
                projection.OpenableSources.Count > 0
                    ? RowHeight +
                      RelationTopGap +
                      CalculateRelationGridHeight(projection.OpenableSources.Count, contentWidth)
                    : 0,
                ref hasGroup);
            AddGroupHeight(
                ref height,
                projection.FishingVariants.Count > 0
                    ? RowHeight +
                      RelationTopGap +
                      CalculateFishingVariantsHeight(projection.FishingVariants, contentWidth)
                    : 0,
                ref hasGroup);
            return height;
        }

        private static int GetRecipeStationAreaHeight()
        {
            return RowHeight + RelationTopGap + CraftingActionSize;
        }

        private static void AddGroupHeight(ref int height, int groupHeight, ref bool hasGroup)
        {
            if (groupHeight <= 0)
                return;
            if (hasGroup)
                height += SourceGroupGap;
            height += groupHeight;
            hasGroup = true;
        }

        private static void AddGroupGap(ref int cursor, ref bool hasPreviousGroup)
        {
            if (hasPreviousGroup)
                cursor += SourceGroupGap;
            hasPreviousGroup = true;
        }

        private void DrawGroupLabel(string label, int x, int width, ref int cursor)
        {
            DrawTextRow(label, x, cursor, width, UIColors.TextTitle);
            cursor += RowHeight;
        }

        private int GetContentTopAfterStats(ItemDetailsProjection projection, int contentWidth)
        {
            int top = ContentPadding +
                      IdentityHeight +
                      GetSectionHeaderHeight() +
                      CalculateStatsAreaHeight(projection, contentWidth);
            if (!string.IsNullOrWhiteSpace(projection.Description))
                top += GetSectionHeaderHeight() + GetDescriptionRowCount(projection.Description) * RowHeight;
            return top;
        }

        private int GetArmorSetsContentTop(ItemDetailsProjection projection, int contentWidth)
        {
            return GetContentTopAfterStats(projection, contentWidth) + GetSectionHeaderHeight();
        }

        private int GetContentTopAfterArmorSets(ItemDetailsProjection projection, int contentWidth)
        {
            int top = GetContentTopAfterStats(projection, contentWidth);
            if (projection.ArmorSets.Count > 0)
                top += GetSectionHeaderHeight() + CalculateRelationGridHeight(projection.ArmorSets.Count, contentWidth);
            return top;
        }

        private int GetSourcesContentTop(ItemDetailsProjection projection, int contentWidth)
        {
            int top = GetContentTopAfterArmorSets(projection, contentWidth);
            return HasSourcesSection(projection) ? top + GetSectionHeaderHeight() : top;
        }

        private int GetContentTopAfterSources(ItemDetailsProjection projection, int contentWidth)
        {
            int top = GetContentTopAfterArmorSets(projection, contentWidth);
            if (HasSourcesSection(projection))
                top += GetSectionHeaderHeight() + GetSourcesAreaHeight(projection, contentWidth);
            return top;
        }

        private int GetPossibleDropsContentTop(ItemDetailsProjection projection, int contentWidth)
        {
            int top = GetContentTopAfterSources(projection, contentWidth);
            return HasPossibleDropsSection(projection) ? top + GetSectionHeaderHeight() : top;
        }

        private int GetRecipeStationContentTop(ItemDetailsProjection projection, int contentWidth)
        {
            int top = GetContentTopAfterSources(projection, contentWidth);
            if (HasPossibleDropsSection(projection))
            {
                top += GetSectionHeaderHeight() +
                       CalculateRelationGridHeight(projection.OpenableContents.Count, contentWidth);
            }

            return HasRecipeStationSection(projection) ? top + GetSectionHeaderHeight() : top;
        }

        private static int GetSectionHeaderHeight()
        {
            return DetailsSectionLayout.HeaderHeight;
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

        private static int CalculateFishingVariantsHeight(
            IReadOnlyList<ItemDetailsFishingVariantReference> variants,
            int contentWidth)
        {
            if (variants.Count == 0 || contentWidth <= 0)
                return 0;

            var height = 0;

            for (var index = 0; index < variants.Count; index++)
            {
                if (index > 0)
                    height += SourceGroupGap + TextHeight + SourceGroupGap;

                ItemDetailsFishingVariantReference variant = variants[index];
                int rowWidth = CalculateFishingVariantWidth(variant, contentWidth);
                height += CalculateFishingVariantHeight(variant, rowWidth);
            }

            return height;
        }

        private static int CalculateFishingVariantWidth(ItemDetailsFishingVariantReference variant, int contentWidth)
        {
            if (contentWidth <= 0)
                return 0;

            int iconCount = GetFishingVariantIconCount(variant);
            int naturalWidth = RelationIconSize;

            if (iconCount > 1)
            {
                naturalWidth += (iconCount - 1) * (CalculateFishingAndSeparatorWidth() + RelationIconSize);
            }

            return Math.Min(contentWidth, naturalWidth);
        }

        private static int CalculateFishingVariantHeight(ItemDetailsFishingVariantReference variant, int width)
        {
            if (width <= 0)
                return 0;

            int iconCount = GetFishingVariantIconCount(variant);
            int lineX = RelationIconSize;
            var rows = 1;
            int andUnitWidth = CalculateFishingAndSeparatorWidth() + RelationIconSize;

            for (var index = 1; index < iconCount; index++)
            {
                if (lineX > 0 && lineX + andUnitWidth > width)
                {
                    rows++;
                    lineX = 0;
                }

                lineX += andUnitWidth;
            }

            return rows * RelationIconSize + Math.Max(0, rows - 1) * RelationIconGap;
        }

        private static int GetFishingVariantIconCount(ItemDetailsFishingVariantReference variant)
        {
            int combinedLiquidExclusionCount = variant.ExcludesLavaAndHoney ? 1 : 0;
            return Math.Max(
                1,
                variant.Conditions.Count + variant.ExcludedConditions.Count + combinedLiquidExclusionCount);
        }

        private static int CalculateFishingAndSeparatorWidth()
        {
            return RelationIconGap + UIRenderer.MeasureText(FishingAndSeparatorText) + RelationIconGap;
        }

        private void DrawIdentityText(ItemDetailsProjection projection, int x, int y, int width)
        {
            int textX = x + IconSize + IdentityTextGap;
            int researchWidth = ShouldShowResearchIndicator(projection.ResearchStatus)
                ? ResearchIndicatorSize + IdentityTextGap
                : 0;
            int textWidth = Math.Max(0, width - IconSize - IdentityTextGap - researchWidth);
            string displayName = TruncatedTextPresentation.Truncate(
                projection.Name,
                textWidth,
                out bool nameWasTruncated);

            if (displayName.Length > 0)
            {
                int nameY = y + 2;
                UIRenderer.DrawText(displayName, textX, nameY, UIColors.TextTitle);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    projection.Name,
                    nameWasTruncated,
                    textX,
                    nameY,
                    textWidth,
                    TextHeight,
                    IsMouseHovering);
            }

            string fullIdText = _localization.Format(CompendiumTextKeys.Common.ItemId, projection.ItemId);
            string idText = TruncatedTextPresentation.Truncate(fullIdText, textWidth, out bool idWasTruncated);

            if (idText.Length > 0)
            {
                int idY = y + 20;
                UIRenderer.DrawText(idText, textX, idY, UIColors.TextDim);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    fullIdText,
                    idWasTruncated,
                    textX,
                    idY,
                    textWidth,
                    TextHeight,
                    IsMouseHovering);
            }
        }

        private void DrawDescription(string description, int x, int width, ref int cursor)
        {
            string[] lines = SplitDescriptionLines(description);
            int descriptionTop = cursor;
            var wasAnyLineTruncated = false;

            foreach (string line in lines)
            {
                string displayText = TruncatedTextPresentation.Truncate(line, width, out bool wasTruncated);
                wasAnyLineTruncated |= wasTruncated;

                if (displayText.Length > 0)
                    UIRenderer.DrawText(displayText, x, cursor + 1, UIColors.Text);

                cursor += RowHeight;
            }

            TruncatedTextPresentation.ShowTooltipIfTruncated(
                description,
                wasAnyLineTruncated,
                x,
                descriptionTop,
                width,
                lines.Length * RowHeight,
                IsMouseHovering);
        }

        private static int GetDescriptionRowCount(string description)
        {
            return SplitDescriptionLines(description).Length;
        }

        private static string[] SplitDescriptionLines(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Array.Empty<string>();

            return description.Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split(['\n'], StringSplitOptions.RemoveEmptyEntries);
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

        private static bool ShouldShowResearchIndicator(ItemResearchStatus status)
        {
            return status == ItemResearchStatus.Unresearched || status == ItemResearchStatus.Researched;
        }

        private sealed class FishingVariantRow : UIElement
        {
            private readonly List<TextLabelElement> _andSeparators = new();
            private readonly List<FishingConditionIcon> _conditionIcons = new();
            private readonly VanillaBestiaryFilterCatalog _filterCatalog;
            private readonly CompendiumLocalization _localization;
            private int _layoutWidth;
            private ItemDetailsFishingVariantReference _variant;

            public FishingVariantRow(VanillaBestiaryFilterCatalog filterCatalog, CompendiumLocalization localization)
            {
                _filterCatalog = filterCatalog;
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                SetPadding(0f);
            }

            public void Bind(ItemDetailsFishingVariantReference variant)
            {
                _variant = variant ?? throw new ArgumentNullException(nameof(variant));
                int conditionCount = _variant.Conditions.Count;
                int excludedConditionCount = _variant.ExcludedConditions.Count;
                int iconCount = GetFishingVariantIconCount(_variant);
                EnsureIconCapacity(iconCount);
                EnsureAndSeparatorCapacity(Math.Max(0, iconCount - 1));

                if (conditionCount == 0 && excludedConditionCount == 0 && !_variant.ExcludesLavaAndHoney)
                {
                    _conditionIcons[0].BindGeneralFishing();
                }
                else
                {
                    var iconIndex = 0;

                    for (var index = 0; index < conditionCount; index++)
                        _conditionIcons[iconIndex++].Bind(_variant.Conditions[index]);

                    if (_variant.ExcludesLavaAndHoney)
                        _conditionIcons[iconIndex++].BindExcludedLavaAndHoney();

                    for (var index = 0; index < excludedConditionCount; index++)
                    {
                        _conditionIcons[iconIndex++].Bind(_variant.ExcludedConditions[index], isExcluded: true);
                    }
                }

                for (int index = iconCount; index < _conditionIcons.Count; index++)
                    _conditionIcons[index].Hide();

                int separatorCount = Math.Max(0, iconCount - 1);
                for (int index = separatorCount; index < _andSeparators.Count; index++)
                    HideTextElement(_andSeparators[index]);
            }

            public void Layout(int left, int top, int width, int height)
            {
                _layoutWidth = Math.Max(0, width);
                Left.Set(left, 0f);
                Top.Set(top, 0f);
                Width.Set(_layoutWidth, 0f);
                Height.Set(Math.Max(0, height), 0f);
            }

            public void Hide()
            {
                _variant = null;
                _layoutWidth = 0;

                foreach (FishingConditionIcon icon in _conditionIcons)
                    icon.Hide();

                foreach (TextLabelElement separator in _andSeparators)
                    HideTextElement(separator);

                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
            }

            public override void RecalculateChildren()
            {
                if (_variant == null || _layoutWidth <= 0)
                {
                    base.RecalculateChildren();
                    return;
                }

                int iconCount = GetFishingVariantIconCount(_variant);
                var cursorX = 0;
                var cursorY = 0;

                LayoutConditionIcon(_conditionIcons[0], cursorX, cursorY);
                cursorX += RelationIconSize;

                int andSeparatorWidth = CalculateFishingAndSeparatorWidth();
                int andUnitWidth = andSeparatorWidth + RelationIconSize;
                int andTextWidth = UIRenderer.MeasureText(FishingAndSeparatorText);

                for (var index = 1; index < iconCount; index++)
                {
                    if (cursorX > 0 && cursorX + andUnitWidth > _layoutWidth)
                    {
                        cursorX = 0;
                        cursorY += RelationIconSize + RelationIconGap;
                    }

                    TextLabelElement separator = _andSeparators[index - 1];
                    separator.Left.Set(cursorX + RelationIconGap, 0f);
                    separator.Top.Set(cursorY + (RelationIconSize - TextHeight) / 2f, 0f);
                    separator.Width.Set(andTextWidth, 0f);
                    separator.Height.Set(TextHeight, 0f);

                    LayoutConditionIcon(_conditionIcons[index], cursorX + andSeparatorWidth, cursorY);
                    cursorX += andUnitWidth;
                }

                base.RecalculateChildren();
            }

            private static void LayoutConditionIcon(FishingConditionIcon icon, int left, int top)
            {
                icon.Left.Set(left, 0f);
                icon.Top.Set(top, 0f);
                icon.Width.Set(RelationIconSize, 0f);
                icon.Height.Set(RelationIconSize, 0f);
            }

            private static void HideTextElement(TextLabelElement element)
            {
                element.Width.Set(0f, 0f);
                element.Height.Set(0f, 0f);
            }

            private void EnsureIconCapacity(int count)
            {
                while (_conditionIcons.Count < count)
                {
                    var icon = new FishingConditionIcon(_filterCatalog, _localization);
                    _conditionIcons.Add(icon);
                    Append(icon);
                }
            }

            private void EnsureAndSeparatorCapacity(int count)
            {
                while (_andSeparators.Count < count)
                {
                    var separator = new TextLabelElement(FishingAndSeparatorText, UIColors.Text)
                    {
                        IgnoresMouseInteraction = true
                    };
                    _andSeparators.Add(separator);
                    Append(separator);
                }
            }
        }

        private sealed class ResearchStatusIndicator(CompendiumLocalization localization) : UIElement
        {
            private readonly CompendiumLocalization _localization =
                localization ?? throw new ArgumentNullException(nameof(localization));

            private bool _researched;
            private string _tooltipText;

            public void Bind(ItemResearchStatus status)
            {
                switch (status)
                {
                    case ItemResearchStatus.Unresearched:
                        _researched = false;
                        _tooltipText = _localization.Get(CompendiumTextKeys.ItemDetails.Unresearched);
                        break;
                    case ItemResearchStatus.Researched:
                        _researched = true;
                        _tooltipText = _localization.Get(CompendiumTextKeys.ItemDetails.Researched);
                        break;
                    default:
                        _researched = false;
                        _tooltipText = null;
                        break;
                }
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (string.IsNullOrEmpty(_tooltipText))
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var bounds = new Rectangle(
                    (int)dimensions.X,
                    (int)dimensions.Y,
                    Math.Max(0, (int)dimensions.Width),
                    Math.Max(0, (int)dimensions.Height));

                VanillaPresentationIcons.DrawJourneyResearch(bounds, _researched);

                if (IsMouseHovering)
                    Tooltip.Set(_tooltipText);
            }
        }

        private sealed class NpcRelationButton : UIElement
        {
            private const int IconPadding = 3;
            private readonly Action<int> _clicked;
            private bool _isBound;
            private string _name;
            private int _npcNetId;

            public NpcRelationButton(Action<int> clicked)
            {
                _clicked = clicked;
                OnLeftClick += OnClicked;
            }

            public void Bind(int npcNetId, string name)
            {
                bool changed = !_isBound || _npcNetId != npcNetId;
                _isBound = true;
                _npcNetId = npcNetId;
                _name = name ?? string.Empty;
                IgnoresMouseInteraction = false;

                if (changed)
                    AsyncNpcIconRenderer.RequestAsync(npcNetId);
            }

            public void Hide()
            {
                _isBound = false;
                _npcNetId = 0;
                _name = null;
                IgnoresMouseInteraction = true;
                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (!_isBound)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);

                UIRenderer.DrawRect(x, y, width, height, IsMouseHovering ? UIColors.ButtonHover : UIColors.ItemBg);
                UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);

                int iconWidth = Math.Max(0, width - IconPadding * 2);
                int iconHeight = Math.Max(0, height - IconPadding * 2);
                AsyncNpcIconRenderer.Draw(
                    spriteBatch,
                    _npcNetId,
                    x + IconPadding,
                    y + IconPadding,
                    iconWidth,
                    iconHeight);

                if (IsMouseHovering && !string.IsNullOrEmpty(_name))
                    Tooltip.Set(_name);
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (_isBound && evt.Target == this)
                    _clicked?.Invoke(_npcNetId);
            }
        }
    }
}