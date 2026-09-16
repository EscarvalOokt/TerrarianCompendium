using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class RecipeFilterPopup : UIElement, IVanillaPopoverContent
    {
        private const string BestiaryTagAtlasPath = "Images/UI/Bestiary/Icon_Tags_Shadow";
        private const int BestiaryTagColumns = 16;
        private const int BestiaryTagRows = 5;
        private const int SnowBestiaryFrame = 5;
        private const int GraveyardBestiaryFrame = 35;
        private const int TorchGodExtraIndex = 211;
        private const int TorchGodAtlasColumns = 4;
        private const int TorchGodEnabledFrame = 1;

        private const int MaxGridColumns = 7;
        private const int PrimaryFilterHeight = 24;
        private const int SectionLabelHeight = 18;
        private const int IconSize = 30;
        private const int IconGap = 4;
        private const int GroupGap = 8;
        private const int BottomPadding = 2;

        private static Asset<Texture2D> _bestiaryTagAtlas;

        private readonly TextLabelElement _completionLabel;
        private readonly VanillaTextButton _craftableNowButton;
        private readonly EnvironmentFilterControl[] _environmentControls;
        private readonly TextLabelElement _environmentLabel;
        private readonly Action _filtersChanged;
        private readonly RecipeFilterState _filterState;
        private readonly VanillaIconButton _foundButton;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaIconButton _missingButton;
        private readonly VanillaTextButton _noRequirementsButton;
        private readonly VanillaIconButton _researchedButton;
        private readonly TextLabelElement _researchLabel;
        private readonly VanillaScrollRegion _scroll;
        private readonly StationFilterControl[] _stationControls;
        private readonly TextLabelElement _stationLabel;
        private readonly VanillaIconButton _unresearchedButton;
        private long _localizationRevision = -1;
        private long _observedItemTextRevision;

        public RecipeFilterPopup(
            ItemCatalog catalog,
            ItemTextIndex itemTextIndex,
            RecipeCatalog recipeCatalog,
            RecipeStationDisplayIndex stationDisplayIndex,
            RecipeFilterState filterState,
            bool journeyResearchAvailable,
            CompendiumLocalization localization,
            Action filtersChanged)
        {
            ItemCatalog resolvedCatalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _observedItemTextRevision = _itemTextIndex.Revision;
            RecipeCatalog resolvedRecipeCatalog =
                recipeCatalog ?? throw new ArgumentNullException(nameof(recipeCatalog));
            RecipeStationDisplayIndex resolvedStationDisplayIndex =
                stationDisplayIndex ?? throw new ArgumentNullException(nameof(stationDisplayIndex));
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _filtersChanged = filtersChanged;

            SetPadding(0f);
            RequestStaticAssetsAsync();

            _scroll = new VanillaScrollRegion();
            Append(_scroll);

            _noRequirementsButton = new VanillaTextButton(string.Empty, ToggleNoRequirements)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
            };
            _scroll.Content.Append(_noRequirementsButton);

            _craftableNowButton = new VanillaTextButton(string.Empty, ToggleCraftableNow)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
            };
            _scroll.Content.Append(_craftableNowButton);

            _completionLabel = new TextLabelElement(string.Empty);
            _scroll.Content.Append(_completionLabel);

            _missingButton = CreateCollectionIconButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found: false),
                ToggleMissing,
                string.Empty);
            _scroll.Content.Append(_missingButton);

            _foundButton = CreateCollectionIconButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found: true),
                ToggleFound,
                string.Empty);
            _scroll.Content.Append(_foundButton);

            if (journeyResearchAvailable)
            {
                _researchLabel = new TextLabelElement(string.Empty);
                _scroll.Content.Append(_researchLabel);

                _unresearchedButton = CreateCollectionIconButton(
                    bounds => VanillaPresentationIcons.DrawJourneyResearch(bounds, researched: false),
                    ToggleUnresearched,
                    string.Empty);
                _scroll.Content.Append(_unresearchedButton);

                _researchedButton = CreateCollectionIconButton(
                    bounds => VanillaPresentationIcons.DrawJourneyResearch(bounds, researched: true),
                    ToggleResearched,
                    string.Empty);
                _scroll.Content.Append(_researchedButton);
            }

            _environmentLabel = new TextLabelElement(string.Empty);
            _scroll.Content.Append(_environmentLabel);

            _environmentControls = CreateEnvironmentControls();

            foreach (EnvironmentFilterControl control in _environmentControls)
                _scroll.Content.Append(control.Button);

            _stationLabel = new TextLabelElement(string.Empty);
            _scroll.Content.Append(_stationLabel);

            StationFilterOption[] stationOptions = BuildStationFilterOptions(
                resolvedRecipeCatalog,
                resolvedStationDisplayIndex,
                resolvedCatalog,
                _itemTextIndex);
            _stationControls = new StationFilterControl[stationOptions.Length];

            for (var index = 0; index < stationOptions.Length; index++)
            {
                StationFilterOption option = stationOptions[index];
                var button = new VanillaIconButton(
                    bounds => DrawItemIcon(option.ItemId, bounds),
                    () => ToggleStation(option.RequiredTileId))
                {
                    ActiveBorderColor = UIColors.Success,
                    ActiveBorderThickness = 2,
                    TooltipText = option.Label
                };

                _stationControls[index] = new StationFilterControl(button, option.RequiredTileId, option.ItemId);
                _scroll.Content.Append(button);
            }

            SynchronizeLocalization(force: true);
            SynchronizeState();
        }

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            int naturalContentWidth = CalculateNaturalContentWidth();
            int naturalWidth = VanillaScrollRegion.CalculateRequiredWidth(naturalContentWidth);
            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int contentWidth = VanillaScrollRegion.CalculateContentWidth(width);
            int height = CalculateContentHeight(contentWidth);
            return new VanillaPopoverContentSize(width, height);
        }

        public void SynchronizeState()
        {
            SynchronizeLocalization();
            SynchronizeStationText();
            _noRequirementsButton.IsActive = _filterState.NoRequirementsOnly;
            _craftableNowButton.IsActive = _filterState.CraftableNowOnly;
            _missingButton.IsActive = _filterState.CompletionFilter == ChecklistCompletionFilter.Missing;
            _foundButton.IsActive = _filterState.CompletionFilter == ChecklistCompletionFilter.Found;

            if (_unresearchedButton != null)
            {
                _unresearchedButton.IsActive = _filterState.ResearchFilter == ChecklistResearchFilter.Unresearched;
            }

            if (_researchedButton != null)
                _researchedButton.IsActive = _filterState.ResearchFilter == ChecklistResearchFilter.Researched;

            for (var index = 0; index < _environmentControls.Length; index++)
            {
                EnvironmentFilterControl control = _environmentControls[index];
                control.Button.IsActive = _filterState.IsEnvironmentRequirementEnabled(control.Requirement);
            }

            for (var index = 0; index < _stationControls.Length; index++)
            {
                StationFilterControl control = _stationControls[index];
                control.Button.IsActive = _filterState.RequiredTileId == control.RequiredTileId;
            }
        }

        public override void RecalculateChildren()
        {
            _scroll.Left.Set(0f, 0f);
            _scroll.Top.Set(0f, 0f);
            _scroll.Width.Set(0f, 1f);
            _scroll.Height.Set(0f, 1f);

            base.RecalculateChildren();

            int width = _scroll.ContentWidth;
            var top = 0;
            int primaryGap = Math.Min(IconGap, Math.Max(0, width));
            int primaryAvailableWidth = Math.Max(0, width - primaryGap);
            int firstPrimaryWidth = primaryAvailableWidth / 2;
            int secondPrimaryWidth = Math.Max(0, primaryAvailableWidth - firstPrimaryWidth);

            _noRequirementsButton.Left.Set(0f, 0f);
            _noRequirementsButton.Top.Set(top, 0f);
            _noRequirementsButton.Width.Set(firstPrimaryWidth, 0f);
            _noRequirementsButton.Height.Set(PrimaryFilterHeight, 0f);

            int craftableLeft = firstPrimaryWidth + primaryGap;
            _craftableNowButton.Left.Set(craftableLeft, 0f);
            _craftableNowButton.Top.Set(top, 0f);
            _craftableNowButton.Width.Set(secondPrimaryWidth, 0f);
            _craftableNowButton.Height.Set(PrimaryFilterHeight, 0f);
            top += PrimaryFilterHeight + FilterPopupLayout.SectionGap;

            bool hasResearch = _researchLabel != null;
            int groupGap = hasResearch ? GroupGap : 0;
            int groupWidth = hasResearch ? Math.Max(0, (width - groupGap) / 2) : width;

            LayoutCollectionGroup(_completionLabel, _missingButton, _foundButton, 0, groupWidth, top);

            if (hasResearch)
            {
                int researchLeft = groupWidth + groupGap;
                LayoutCollectionGroup(
                    _researchLabel,
                    _unresearchedButton,
                    _researchedButton,
                    researchLeft,
                    Math.Max(0, width - researchLeft),
                    top);
            }

            top += SectionLabelHeight + FilterPopupLayout.ContentGap + IconSize + FilterPopupLayout.SectionGap;

            _environmentLabel.Left.Set(0f, 0f);
            _environmentLabel.Top.Set(top, 0f);
            _environmentLabel.Width.Set(width, 0f);
            _environmentLabel.Height.Set(SectionLabelHeight, 0f);
            top += SectionLabelHeight + FilterPopupLayout.ContentGap;

            top = LayoutIconGrid(_environmentControls, width, top) + FilterPopupLayout.SectionGap;

            _stationLabel.Left.Set(0f, 0f);
            _stationLabel.Top.Set(top, 0f);
            _stationLabel.Width.Set(width, 0f);
            _stationLabel.Height.Set(SectionLabelHeight, 0f);
            top += SectionLabelHeight + FilterPopupLayout.ContentGap;

            top = LayoutIconGrid(_stationControls, width, top);
            _scroll.SetContentHeight(top + BottomPadding);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _noRequirementsButton.Text = _localization.Get(CompendiumTextKeys.Recipes.ByHand);
            _noRequirementsButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.ByHandTooltip);
            _craftableNowButton.Text = _localization.Get(CompendiumTextKeys.Recipes.CraftableNow);
            _craftableNowButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.CraftableNowTooltip);
            _completionLabel.Text = _localization.Get(CompendiumTextKeys.Recipes.Completion);
            _missingButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.Missing);
            _foundButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.Found);
            if (_researchLabel != null)
                _researchLabel.Text = _localization.Get(CompendiumTextKeys.Recipes.Research);
            if (_unresearchedButton != null)
                _unresearchedButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.Unresearched);
            if (_researchedButton != null)
                _researchedButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.Researched);
            _environmentLabel.Text = _localization.Get(CompendiumTextKeys.Recipes.Environment);
            for (var index = 0; index < _environmentControls.Length; index++)
                _environmentControls[index].Button.TooltipText =
                    _localization.Get(_environmentControls[index].TooltipKey);
            _stationLabel.Text = _localization.Get(CompendiumTextKeys.Recipes.CraftingStations);
            Recalculate();
        }

        private void ToggleNoRequirements()
        {
            long revision = _filterState.Revision;
            _filterState.NoRequirementsOnly = !_filterState.NoRequirementsOnly;
            CompleteFilterChange(revision);
        }

        private void ToggleCraftableNow()
        {
            long revision = _filterState.Revision;
            _filterState.CraftableNowOnly = !_filterState.CraftableNowOnly;
            CompleteFilterChange(revision);
        }

        private void ToggleMissing()
        {
            ToggleCompletionFilter(ChecklistCompletionFilter.Missing);
        }

        private void ToggleFound()
        {
            ToggleCompletionFilter(ChecklistCompletionFilter.Found);
        }

        private void ToggleCompletionFilter(ChecklistCompletionFilter filter)
        {
            long revision = _filterState.Revision;
            _filterState.CompletionFilter = _filterState.CompletionFilter == filter
                ? ChecklistCompletionFilter.All
                : filter;
            CompleteFilterChange(revision);
        }

        private void ToggleUnresearched()
        {
            ToggleResearchFilter(ChecklistResearchFilter.Unresearched);
        }

        private void ToggleResearched()
        {
            ToggleResearchFilter(ChecklistResearchFilter.Researched);
        }

        private void ToggleResearchFilter(ChecklistResearchFilter filter)
        {
            long revision = _filterState.Revision;
            _filterState.ResearchFilter = _filterState.ResearchFilter == filter ? ChecklistResearchFilter.All : filter;
            CompleteFilterChange(revision);
        }

        private void ToggleEnvironmentRequirement(RecipeEnvironmentRequirementFilter requirement)
        {
            long revision = _filterState.Revision;
            bool enabled = !_filterState.IsEnvironmentRequirementEnabled(requirement);
            _filterState.SetEnvironmentRequirement(requirement, enabled);
            CompleteFilterChange(revision);
        }

        private void ToggleStation(int requiredTileId)
        {
            long revision = _filterState.Revision;
            _filterState.RequiredTileId = _filterState.RequiredTileId == requiredTileId ? null : requiredTileId;
            CompleteFilterChange(revision);
        }

        private void CompleteFilterChange(long previousRevision)
        {
            if (_filterState.Revision == previousRevision)
                return;

            SynchronizeState();
            _filtersChanged?.Invoke();
        }

        private EnvironmentFilterControl[] CreateEnvironmentControls()
        {
            return
            [
                CreateItemEnvironmentControl(
                    ItemID.WaterBucket,
                    CompendiumTextKeys.Recipes.Water,
                    RecipeEnvironmentRequirementFilter.Water),
                CreateItemEnvironmentControl(
                    ItemID.HoneyBucket,
                    CompendiumTextKeys.Recipes.Honey,
                    RecipeEnvironmentRequirementFilter.Honey),
                CreateItemEnvironmentControl(
                    ItemID.LavaBucket,
                    CompendiumTextKeys.Recipes.Lava,
                    RecipeEnvironmentRequirementFilter.Lava),
                CreateBestiaryEnvironmentControl(
                    SnowBestiaryFrame,
                    CompendiumTextKeys.Recipes.SnowBiome,
                    RecipeEnvironmentRequirementFilter.SnowBiome),
                CreateBestiaryEnvironmentControl(
                    GraveyardBestiaryFrame,
                    CompendiumTextKeys.Recipes.GraveyardBiome,
                    RecipeEnvironmentRequirementFilter.GraveyardBiome),
                CreateItemEnvironmentControl(
                    ItemID.MechdusaSummon,
                    CompendiumTextKeys.Recipes.Mechdusa,
                    RecipeEnvironmentRequirementFilter.Mechdusa),
                CreateTorchGodEnvironmentControl()
            ];
        }

        private EnvironmentFilterControl CreateItemEnvironmentControl(
            int itemId,
            string tooltipKey,
            RecipeEnvironmentRequirementFilter requirement)
        {
            var button = new VanillaIconButton(
                bounds => DrawItemIcon(itemId, bounds),
                () => ToggleEnvironmentRequirement(requirement))
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
                TooltipText = _localization.Get(tooltipKey)
            };

            return new EnvironmentFilterControl(button, requirement, tooltipKey);
        }

        private EnvironmentFilterControl CreateBestiaryEnvironmentControl(
            int frameIndex,
            string tooltipKey,
            RecipeEnvironmentRequirementFilter requirement)
        {
            var button = new VanillaIconButton(
                bounds => DrawBestiaryTagIcon(frameIndex, bounds),
                () => ToggleEnvironmentRequirement(requirement))
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
                TooltipText = _localization.Get(tooltipKey)
            };

            return new EnvironmentFilterControl(button, requirement, tooltipKey);
        }

        private EnvironmentFilterControl CreateTorchGodEnvironmentControl()
        {
            var button = new VanillaIconButton(
                DrawTorchGodIcon,
                () => ToggleEnvironmentRequirement(RecipeEnvironmentRequirementFilter.TorchGodsFavor))
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
                TooltipText = _localization.Get(CompendiumTextKeys.Recipes.TorchGodsFavor)
            };

            return new EnvironmentFilterControl(
                button,
                RecipeEnvironmentRequirementFilter.TorchGodsFavor,
                CompendiumTextKeys.Recipes.TorchGodsFavor);
        }

        private static VanillaIconButton CreateCollectionIconButton(
            Action<Rectangle> drawIcon,
            Action onClick,
            string tooltipText)
        {
            return new VanillaIconButton(drawIcon, onClick)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
                TooltipText = tooltipText ?? string.Empty
            };
        }

        private int CalculateNaturalContentWidth()
        {
            int primaryWidth =
                VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Recipes.ByHand)) +
                IconGap +
                VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Recipes.CraftableNow));
            int completionGroupWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Recipes.Completion)),
                IconSize * 2 + IconGap);
            int collectionWidth = completionGroupWidth;

            if (_researchLabel != null)
            {
                int researchGroupWidth = Math.Max(
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Recipes.Research)),
                    IconSize * 2 + IconGap);
                collectionWidth += GroupGap + researchGroupWidth;
            }

            int environmentWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Recipes.Environment)),
                CalculateGridWidth(Math.Min(MaxGridColumns, _environmentControls.Length)));
            int stationWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Recipes.CraftingStations)),
                CalculateGridWidth(Math.Min(MaxGridColumns, _stationControls.Length)));

            return Math.Max(Math.Max(primaryWidth, collectionWidth), Math.Max(environmentWidth, stationWidth));
        }

        private int CalculateContentHeight(int width)
        {
            int top = PrimaryFilterHeight + FilterPopupLayout.SectionGap;
            top += SectionLabelHeight + FilterPopupLayout.ContentGap + IconSize + FilterPopupLayout.SectionGap;
            top += SectionLabelHeight + FilterPopupLayout.ContentGap;
            top += CalculateGridHeight(_environmentControls.Length, width) + FilterPopupLayout.SectionGap;
            top += SectionLabelHeight + FilterPopupLayout.ContentGap;
            top += CalculateGridHeight(_stationControls.Length, width);
            return top + BottomPadding;
        }

        private static int CalculateGridHeight(int itemCount, int width)
        {
            if (itemCount <= 0)
                return 0;

            int columns = CalculateGridColumnCount(width);
            int rows = (itemCount + columns - 1) / columns;
            return rows * IconSize + Math.Max(0, rows - 1) * IconGap;
        }

        private static int CalculateGridWidth(int columns)
        {
            if (columns <= 0)
                return 0;

            return columns * IconSize + (columns - 1) * IconGap;
        }

        private static void LayoutCollectionGroup(
            TextLabelElement label,
            VanillaIconButton firstButton,
            VanillaIconButton secondButton,
            int left,
            int width,
            int top)
        {
            label.Left.Set(left, 0f);
            label.Top.Set(top, 0f);
            label.Width.Set(width, 0f);
            label.Height.Set(SectionLabelHeight, 0f);

            int buttonsTop = top + SectionLabelHeight + FilterPopupLayout.ContentGap;
            int pairWidth = IconSize * 2 + IconGap;
            int pairLeft = left + Math.Max(0, (width - pairWidth) / 2);

            firstButton.Left.Set(pairLeft, 0f);
            firstButton.Top.Set(buttonsTop, 0f);
            firstButton.Width.Set(IconSize, 0f);
            firstButton.Height.Set(IconSize, 0f);

            secondButton.Left.Set(pairLeft + IconSize + IconGap, 0f);
            secondButton.Top.Set(buttonsTop, 0f);
            secondButton.Width.Set(IconSize, 0f);
            secondButton.Height.Set(IconSize, 0f);
        }

        private static int LayoutIconGrid(EnvironmentFilterControl[] controls, int width, int top)
        {
            if (controls.Length == 0)
                return top;

            int columns = CalculateGridColumnCount(width);

            for (var index = 0; index < controls.Length; index++)
            {
                int column = index % columns;
                int row = index / columns;
                VanillaIconButton button = controls[index].Button;
                button.Left.Set(column * (IconSize + IconGap), 0f);
                button.Top.Set(top + row * (IconSize + IconGap), 0f);
                button.Width.Set(IconSize, 0f);
                button.Height.Set(IconSize, 0f);
            }

            int rowCount = (controls.Length + columns - 1) / columns;
            return top + rowCount * IconSize + Math.Max(0, rowCount - 1) * IconGap;
        }

        private static int LayoutIconGrid(StationFilterControl[] controls, int width, int top)
        {
            if (controls.Length == 0)
                return top;

            int columns = CalculateGridColumnCount(width);

            for (var index = 0; index < controls.Length; index++)
            {
                int column = index % columns;
                int row = index / columns;
                VanillaIconButton button = controls[index].Button;
                button.Left.Set(column * (IconSize + IconGap), 0f);
                button.Top.Set(top + row * (IconSize + IconGap), 0f);
                button.Width.Set(IconSize, 0f);
                button.Height.Set(IconSize, 0f);
            }

            int rowCount = (controls.Length + columns - 1) / columns;
            return top + rowCount * IconSize + Math.Max(0, rowCount - 1) * IconGap;
        }

        private static int CalculateGridColumnCount(int width)
        {
            int availableColumns = Math.Max(1, (Math.Max(0, width) + IconGap) / (IconSize + IconGap));

            return Math.Min(MaxGridColumns, availableColumns);
        }

        private static StationFilterOption[] BuildStationFilterOptions(
            RecipeCatalog recipeCatalog,
            RecipeStationDisplayIndex stationDisplayIndex,
            ItemCatalog catalog,
            ItemTextIndex itemTextIndex)
        {
            var optionsByTileId = new Dictionary<int, StationFilterOption>();

            foreach (RecipeCatalogEntry recipe in recipeCatalog.Recipes)
            {
                int? requiredTileId = recipe.EnvironmentRequirements.RequiredTileId;

                if (!requiredTileId.HasValue || optionsByTileId.ContainsKey(requiredTileId.Value))
                    continue;

                if (!stationDisplayIndex.TryGetRepresentativeItemId(requiredTileId.Value, out int itemId) ||
                    !catalog.TryGet(itemId, out ItemCatalogEntry item))
                {
                    continue;
                }

                optionsByTileId.Add(
                    requiredTileId.Value,
                    new StationFilterOption(requiredTileId.Value, itemId, itemTextIndex.GetName(item.Id)));
            }

            var options = new List<StationFilterOption>(optionsByTileId.Values);
            options.Sort(CompareStationFilterOptions);
            return options.ToArray();
        }

        private void SynchronizeStationText()
        {
            long itemTextRevision = _itemTextIndex.Revision;

            if (_observedItemTextRevision == itemTextRevision)
                return;

            _observedItemTextRevision = itemTextRevision;

            for (var index = 0; index < _stationControls.Length; index++)
            {
                StationFilterControl control = _stationControls[index];
                control.Button.TooltipText = _itemTextIndex.GetName(control.ItemId);
            }

            Array.Sort(_stationControls, CompareStationFilterControls);
            Recalculate();
        }

        private int CompareStationFilterControls(StationFilterControl left, StationFilterControl right)
        {
            int nameComparison = StringComparer.OrdinalIgnoreCase.Compare(
                _itemTextIndex.GetName(left.ItemId),
                _itemTextIndex.GetName(right.ItemId));

            if (nameComparison != 0)
                return nameComparison;

            return left.RequiredTileId.CompareTo(right.RequiredTileId);
        }

        private static int CompareStationFilterOptions(StationFilterOption left, StationFilterOption right)
        {
            int nameComparison = StringComparer.OrdinalIgnoreCase.Compare(left.Label, right.Label);

            if (nameComparison != 0)
                return nameComparison;

            return left.RequiredTileId.CompareTo(right.RequiredTileId);
        }

        private static void DrawItemIcon(int itemId, Rectangle bounds)
        {
            AsyncItemIconRenderer.Draw(itemId, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        private static void DrawBestiaryTagIcon(int frameIndex, Rectangle bounds)
        {
            RequestStaticAssetsAsync();

            if (_bestiaryTagAtlas == null || _bestiaryTagAtlas.State != AssetState.Loaded)
                return;

            int frameX = frameIndex % BestiaryTagColumns;
            int frameY = frameIndex / BestiaryTagColumns;
            DrawAtlasFrame(_bestiaryTagAtlas.Value, BestiaryTagColumns, BestiaryTagRows, frameX, frameY, bounds);
        }

        private static void DrawTorchGodIcon(Rectangle bounds)
        {
            if (TextureAssets.Extra == null || TextureAssets.Extra.Length <= TorchGodExtraIndex)
                return;

            Asset<Texture2D> asset = TextureAssets.Extra[TorchGodExtraIndex];

            if (asset == null)
                return;

            if (asset.State == AssetState.NotLoaded)
                Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);

            if (asset.State != AssetState.Loaded)
                return;

            DrawAtlasFrame(asset.Value, TorchGodAtlasColumns, 1, TorchGodEnabledFrame, 0, bounds);
        }

        private static void DrawAtlasFrame(
            Texture2D texture,
            int columns,
            int rows,
            int frameX,
            int frameY,
            Rectangle bounds)
        {
            DrawAtlasFrame(texture, columns, rows, frameX, frameY, bounds, Color.White);
        }

        private static void DrawAtlasFrame(
            Texture2D texture,
            int columns,
            int rows,
            int frameX,
            int frameY,
            Rectangle bounds,
            Color color)
        {
            if (texture == null || columns <= 0 || rows <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;

            if (frameWidth <= 0 || frameHeight <= 0)
                return;

            var source = new Rectangle(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
            DrawTextureFrame(texture, source, bounds, color);
        }

        private static void DrawTextureFrame(Texture2D texture, Rectangle source, Rectangle bounds, Color color)
        {
            if (texture == null || source.Width <= 0 || source.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            float scale = Math.Min((float)bounds.Width / source.Width, (float)bounds.Height / source.Height);
            int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
            int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
            var destination = new Rectangle(
                bounds.X + (bounds.Width - drawWidth) / 2,
                bounds.Y + (bounds.Height - drawHeight) / 2,
                drawWidth,
                drawHeight);

            Main.spriteBatch.Draw(texture, destination, source, color);
        }

        private static void RequestStaticAssetsAsync()
        {
            _bestiaryTagAtlas ??= Main.Assets.Request<Texture2D>(BestiaryTagAtlasPath, AssetRequestMode.AsyncLoad);

            if (TextureAssets.Extra == null || TextureAssets.Extra.Length <= TorchGodExtraIndex)
                return;

            Asset<Texture2D> torchAsset = TextureAssets.Extra[TorchGodExtraIndex];

            if (torchAsset is { State: AssetState.NotLoaded })
                Main.Assets.Request<Texture2D>(torchAsset.Name, AssetRequestMode.AsyncLoad);
        }

        private sealed class EnvironmentFilterControl(
            VanillaIconButton button,
            RecipeEnvironmentRequirementFilter requirement,
            string tooltipKey)
        {
            public VanillaIconButton Button { get; } = button ?? throw new ArgumentNullException(nameof(button));

            public RecipeEnvironmentRequirementFilter Requirement { get; } = requirement;

            public string TooltipKey { get; } = tooltipKey ?? string.Empty;
        }

        private sealed class StationFilterControl(VanillaIconButton button, int requiredTileId, int itemId)
        {
            public VanillaIconButton Button { get; } = button ?? throw new ArgumentNullException(nameof(button));

            public int ItemId { get; } = itemId;

            public int RequiredTileId { get; } = requiredTileId;
        }

        private readonly struct StationFilterOption(int requiredTileId, int itemId, string label)
        {
            public int RequiredTileId { get; } = requiredTileId;

            public int ItemId { get; } = itemId;

            public string Label { get; } = label ?? string.Empty;
        }
    }
}