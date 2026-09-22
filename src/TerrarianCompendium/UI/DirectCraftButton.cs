using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class DirectCraftButton : UIElement
    {
        private const int OptionHeight = 32;
        private const int OptionGap = 2;
        private const int EmptyStateHeight = 18;

        private readonly VanillaIconButton _button;
        private readonly VanillaDirectCraftingService _craftingService;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly CompendiumLocalization _localization;
        private readonly CraftOptionsContent _optionsContent;
        private readonly RecipeStationDisplayIndex _stationDisplayIndex;
        private int _itemId = -1;
        private long _observedCraftingRevision = long.MinValue;
        private long _observedItemTextRevision = long.MinValue;
        private long _observedLocalizationRevision = long.MinValue;
        private VanillaPopover _popover;
        private UIElement _popoverHost;
        private int _preferredRuntimeIndex = -1;

        public DirectCraftButton(
            VanillaDirectCraftingService craftingService,
            ItemTextIndex itemTextIndex,
            RecipeStationDisplayIndex stationDisplayIndex,
            CompendiumLocalization localization,
            JourneyResearchState journeyResearchState = null)
        {
            _craftingService = craftingService;
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _stationDisplayIndex = stationDisplayIndex;
            SetPadding(0f);

            _button = new VanillaIconButton(DrawCraftIcon, TogglePopover);
            Append(_button);

            _optionsContent = new CraftOptionsContent(
                _craftingService,
                _localization,
                BuildRecipeTooltip,
                BuildRequirementPresentation,
                journeyResearchState);
            Clear();
        }

        public bool IsPopoverOpen => _popover?.IsOpen == true;

        public void AttachPopoverHost(UIElement host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (ReferenceEquals(_popoverHost, host))
                return;

            if (_popoverHost != null)
                throw new InvalidOperationException("Direct crafting popover host is already attached.");

            _popoverHost = host;
            _popover = new VanillaPopover(host, _button, _optionsContent);
        }

        public void Bind(int itemId, int preferredRuntimeIndex = -1)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            bool itemChanged = _itemId != itemId;

            if (itemChanged)
                TryClosePopover();

            _itemId = itemId;
            _preferredRuntimeIndex = preferredRuntimeIndex;
            SynchronizeAvailability(force: itemChanged);
            SynchronizeButtonActiveState();
        }

        public void Clear()
        {
            TryClosePopover();
            _itemId = -1;
            _preferredRuntimeIndex = -1;
            _observedCraftingRevision = long.MinValue;
            _observedItemTextRevision = long.MinValue;
            _observedLocalizationRevision = long.MinValue;
            _button.IsActive = false;
            _button.IsEnabled = false;
            _button.TooltipText = _localization.Get(CompendiumTextKeys.DirectCraft.AvailabilityUnavailable);
            _optionsContent.Bind(Array.Empty<RecipeCatalogEntry>(), preferredRuntimeIndex: -1, forceRebuild: true);
        }

        public bool TryClosePopover()
        {
            if (_popover?.IsOpen != true)
                return false;

            _popover.Close();
            _optionsContent.CancelCraftHold();
            _button.IsActive = false;
            return true;
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeAvailability(force: false);

            Player player = Main.LocalPlayer;

            if (!IsPopoverOpen || player == null || player.dead || player.ghost)
                _optionsContent.CancelCraftHold();

            SynchronizeButtonActiveState();
            base.Update(gameTime);
        }

        public override void RecalculateChildren()
        {
            _button.Left.Set(0f, 0f);
            _button.Top.Set(0f, 0f);
            _button.Width.Set(0f, 1f);
            _button.Height.Set(0f, 1f);
            base.RecalculateChildren();
        }

        public override void OnDeactivate()
        {
            TryClosePopover();
            base.OnDeactivate();
        }

        private void SynchronizeAvailability(bool force)
        {
            if (_itemId <= 0)
                return;

            long craftingRevision = _craftingService?.Revision ?? long.MinValue;
            long itemTextRevision = _itemTextIndex.Revision;
            bool itemTextChanged = _observedItemTextRevision != itemTextRevision;
            bool localizationChanged = _observedLocalizationRevision != _localization.Revision;

            if (!force && _observedCraftingRevision == craftingRevision && !itemTextChanged && !localizationChanged)
            {
                _optionsContent.SetPreferredRuntimeIndex(_preferredRuntimeIndex);
                return;
            }

            _observedCraftingRevision = craftingRevision;
            _observedItemTextRevision = itemTextRevision;
            _observedLocalizationRevision = _localization.Revision;

            IReadOnlyList<RecipeCatalogEntry> recipes = _craftingService == null
                ? Array.Empty<RecipeCatalogEntry>()
                : _craftingService.GetCraftableRecipesForItem(_itemId);

            bool hasCraftableRecipes = recipes.Count > 0;
            _button.IsEnabled = hasCraftableRecipes;

            if (_craftingService == null)
            {
                _button.TooltipText = _localization.Get(CompendiumTextKeys.DirectCraft.AvailabilityUnavailable);
            }
            else if (hasCraftableRecipes)
            {
                _button.TooltipText = recipes.Count == 1
                    ? _localization.Get(CompendiumTextKeys.DirectCraft.ChooseOne)
                    : _localization.Format(CompendiumTextKeys.DirectCraft.ChooseMany, recipes.Count);
            }
            else
            {
                _button.TooltipText = _localization.Get(CompendiumTextKeys.DirectCraft.CannotCraft);
            }

            _optionsContent.Bind(recipes, _preferredRuntimeIndex, force || itemTextChanged || localizationChanged);

            if (_popover?.IsOpen == true)
                _popover.Recalculate();
        }

        private void SynchronizeButtonActiveState()
        {
            _button.IsActive = IsPopoverOpen;
        }

        private void DrawCraftIcon(Rectangle bounds)
        {
            VanillaPresentationIcons.DrawCraft(bounds, _button.IsEnabled);
        }

        private void TogglePopover()
        {
            SynchronizeAvailability(force: false);

            if (!_button.IsEnabled)
                return;

            _popover?.Toggle();

            if (!IsPopoverOpen)
                _optionsContent.CancelCraftHold();

            SynchronizeButtonActiveState();
        }

        private string BuildRecipeTooltip(RecipeCatalogEntry recipe)
        {
            var builder = new StringBuilder();

            if (recipe.Ingredients.Count == 0)
            {
                builder.Append(_localization.Get(CompendiumTextKeys.DirectCraft.NoIngredients));
            }
            else
            {
                for (var index = 0; index < recipe.Ingredients.Count; index++)
                {
                    if (index > 0)
                        builder.Append(" + ");

                    RecipeIngredient ingredient = recipe.Ingredients[index];
                    string name = ResolveItemName(ingredient.DisplayItemId);

                    if (ingredient.Requirement.Kind == RecipeIngredientRequirementKind.RecipeGroup)
                    {
                        builder.Append(_localization.Get(CompendiumTextKeys.DirectCraft.Any));
                        builder.Append(' ');
                    }

                    builder.Append(name);
                    builder.Append(' ');
                    builder.Append(_localization.Format(CompendiumTextKeys.Common.Stack, ingredient.Stack));
                }
            }

            builder.Append(" → ");
            builder.Append(ResolveItemName(recipe.ResultItemId));
            builder.Append(' ');
            builder.Append(_localization.Format(CompendiumTextKeys.Common.Stack, recipe.ResultStack));
            builder.Append(" · ");
            builder.Append(BuildRequirementSummary(recipe));
            return builder.ToString();
        }

        private RequirementPresentation BuildRequirementPresentation(RecipeCatalogEntry recipe)
        {
            var stationItemId = 0;
            var requirements = new List<string>();
            RecipeEnvironmentRequirements environment = recipe.EnvironmentRequirements;

            if (environment.RequiredTileId.HasValue)
            {
                if (_stationDisplayIndex != null &&
                    _stationDisplayIndex.TryGetRepresentativeItemId(
                        environment.RequiredTileId.Value,
                        out int representativeStationItemId))
                {
                    stationItemId = representativeStationItemId;
                }
                else
                {
                    requirements.Add(_localization.Get(CompendiumTextKeys.Recipes.CraftingStation));
                }
            }

            AddRequirement(
                requirements,
                environment.RequiresWater,
                _localization.Get(CompendiumTextKeys.Recipes.Water));
            AddRequirement(
                requirements,
                environment.RequiresHoney,
                _localization.Get(CompendiumTextKeys.Recipes.Honey));
            AddRequirement(requirements, environment.RequiresLava, _localization.Get(CompendiumTextKeys.Recipes.Lava));
            AddRequirement(
                requirements,
                environment.RequiresSnowBiome,
                _localization.Get(CompendiumTextKeys.Recipes.SnowBiome));
            AddRequirement(
                requirements,
                environment.RequiresGraveyardBiome,
                _localization.Get(CompendiumTextKeys.Recipes.GraveyardBiome));
            AddRequirement(
                requirements,
                environment.RequiresMechdusa,
                _localization.Get(CompendiumTextKeys.Recipes.Mechdusa));
            AddRequirement(
                requirements,
                environment.RequiresTorchGodsFavor,
                _localization.Get(CompendiumTextKeys.Recipes.TorchGodsFavor));
            AddRequirement(requirements, recipe.IsAlchemy, _localization.Get(CompendiumTextKeys.Recipes.Alchemy));

            if (stationItemId <= 0 && requirements.Count == 0)
                return new RequirementPresentation(0, _localization.Get(CompendiumTextKeys.Recipes.ByHand));

            return new RequirementPresentation(stationItemId, string.Join(", ", requirements));
        }

        private string BuildRequirementSummary(RecipeCatalogEntry recipe)
        {
            var requirements = new List<string>();
            RecipeEnvironmentRequirements environment = recipe.EnvironmentRequirements;

            if (environment.RequiredTileId.HasValue)
            {
                string stationName = null;

                if (_stationDisplayIndex != null &&
                    _stationDisplayIndex.TryGetRepresentativeItemId(
                        environment.RequiredTileId.Value,
                        out int stationItemId))
                {
                    stationName = _itemTextIndex.GetName(stationItemId);
                }

                requirements.Add(
                    string.IsNullOrWhiteSpace(stationName)
                        ? _localization.Get(CompendiumTextKeys.Recipes.CraftingStation)
                        : stationName);
            }

            AddRequirement(
                requirements,
                environment.RequiresWater,
                _localization.Get(CompendiumTextKeys.Recipes.Water));
            AddRequirement(
                requirements,
                environment.RequiresHoney,
                _localization.Get(CompendiumTextKeys.Recipes.Honey));
            AddRequirement(requirements, environment.RequiresLava, _localization.Get(CompendiumTextKeys.Recipes.Lava));
            AddRequirement(
                requirements,
                environment.RequiresSnowBiome,
                _localization.Get(CompendiumTextKeys.Recipes.SnowBiome));
            AddRequirement(
                requirements,
                environment.RequiresGraveyardBiome,
                _localization.Get(CompendiumTextKeys.Recipes.GraveyardBiome));
            AddRequirement(
                requirements,
                environment.RequiresMechdusa,
                _localization.Get(CompendiumTextKeys.Recipes.Mechdusa));
            AddRequirement(
                requirements,
                environment.RequiresTorchGodsFavor,
                _localization.Get(CompendiumTextKeys.Recipes.TorchGodsFavor));
            AddRequirement(requirements, recipe.IsAlchemy, _localization.Get(CompendiumTextKeys.Recipes.Alchemy));

            return requirements.Count == 0
                ? _localization.Get(CompendiumTextKeys.Recipes.ByHand)
                : string.Join(", ", requirements);
        }

        private string ResolveItemName(int itemId)
        {
            string name = _itemTextIndex.GetName(itemId);
            return string.IsNullOrWhiteSpace(name)
                ? _localization.Format(CompendiumTextKeys.DirectCraft.FallbackItem, itemId)
                : name;
        }

        private static void AddRequirement(List<string> requirements, bool required, string label)
        {
            if (required)
                requirements.Add(label);
        }

        private readonly struct RequirementPresentation(int stationItemId, string text)
        {
            public int StationItemId { get; } = stationItemId;

            public string Text { get; } = text ?? string.Empty;
        }

        private sealed class CraftOptionsContent : UIElement, IVanillaPopoverContent
        {
            private readonly Action<int, RecipeOptionElement> _beginCraftHold;
            private readonly VanillaDirectCraftingService _craftingService;
            private readonly Action<int> _endCraftHold;
            private readonly JourneyResearchState _journeyResearchState;
            private readonly CompendiumLocalization _localization;
            private readonly List<OptionEntry> _options = new();
            private readonly Func<RecipeCatalogEntry, RequirementPresentation> _requirementResolver;
            private readonly VanillaScrollRegion _scroll;
            private readonly Func<RecipeCatalogEntry, string> _tooltipResolver;
            private bool _hasCraftedDuringHold;
            private RecipeOptionElement _heldOption;
            private bool _heldRecipeRepeatable;
            private int _heldRuntimeIndex = -1;

            public CraftOptionsContent(
                VanillaDirectCraftingService craftingService,
                CompendiumLocalization localization,
                Func<RecipeCatalogEntry, string> tooltipResolver,
                Func<RecipeCatalogEntry, RequirementPresentation> requirementResolver,
                JourneyResearchState journeyResearchState)
            {
                _craftingService = craftingService;
                _journeyResearchState = journeyResearchState;
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                _tooltipResolver = tooltipResolver ?? throw new ArgumentNullException(nameof(tooltipResolver));
                _requirementResolver =
                    requirementResolver ?? throw new ArgumentNullException(nameof(requirementResolver));
                _beginCraftHold = BeginCraftHold;
                _endCraftHold = EndCraftHold;
                SetPadding(0f);

                _scroll = new VanillaScrollRegion
                {
                    Width = StyleDimension.Fill,
                    Height = StyleDimension.Fill
                };
                Append(_scroll);
            }

            public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
            {
                int naturalContentWidth =
                    UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.DirectCraft.NoCraftableRecipes));

                for (var index = 0; index < _options.Count; index++)
                {
                    naturalContentWidth = Math.Max(naturalContentWidth, _options[index].Option.MeasureNaturalWidth());
                }

                int naturalWidth = VanillaScrollRegion.CalculateRequiredWidth(naturalContentWidth);
                int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
                int height = _options.Count == 0
                    ? EmptyStateHeight
                    : _options.Count * OptionHeight + Math.Max(0, _options.Count - 1) * OptionGap;
                return new VanillaPopoverContentSize(width, height);
            }

            public void Bind(IReadOnlyList<RecipeCatalogEntry> recipes, int preferredRuntimeIndex, bool forceRebuild)
            {
                if (recipes == null)
                    throw new ArgumentNullException(nameof(recipes));

                if (!forceRebuild && MatchesCurrentRecipes(recipes))
                {
                    SetPreferredRuntimeIndex(preferredRuntimeIndex);
                    return;
                }

                int heldRuntimeIndex = _heldRuntimeIndex;
                bool preserveHold = heldRuntimeIndex >= 0 && ContainsRuntimeIndex(recipes, heldRuntimeIndex);

                _scroll.Content.RemoveAllChildren();
                _options.Clear();
                _heldOption = null;

                if (recipes.Count == 0)
                {
                    CancelCraftHold();
                    var emptyState = new TextLabelElement(
                        _localization.Get(CompendiumTextKeys.DirectCraft.NoCraftableRecipes),
                        UIColors.TextDim)
                    {
                        IgnoresMouseInteraction = true,
                        Width = StyleDimension.Fill,
                        Height = new StyleDimension(EmptyStateHeight, 0f)
                    };
                    _scroll.Content.Append(emptyState);
                    _scroll.SetContentHeight(EmptyStateHeight);
                    return;
                }

                for (var index = 0; index < recipes.Count; index++)
                {
                    RecipeCatalogEntry recipe = recipes[index];
                    RequirementPresentation requirement = _requirementResolver(recipe);
                    var option = new RecipeOptionElement(
                        recipe,
                        requirement,
                        _tooltipResolver(recipe),
                        _localization,
                        _journeyResearchState,
                        _beginCraftHold,
                        _endCraftHold)
                    {
                        Left = new StyleDimension(0f, 0f),
                        Top = new StyleDimension(index * (OptionHeight + OptionGap), 0f),
                        Width = StyleDimension.Fill,
                        Height = new StyleDimension(OptionHeight, 0f)
                    };
                    option.SetActive(recipe.RuntimeIndex == preferredRuntimeIndex);
                    _options.Add(new OptionEntry(recipe.RuntimeIndex, option));
                    _scroll.Content.Append(option);

                    if (preserveHold && recipe.RuntimeIndex == heldRuntimeIndex)
                        _heldOption = option;
                }

                if (!preserveHold)
                    CancelCraftHold();

                int contentHeight = recipes.Count * OptionHeight + Math.Max(0, recipes.Count - 1) * OptionGap;
                _scroll.SetContentHeight(contentHeight);
            }

            public void SetPreferredRuntimeIndex(int preferredRuntimeIndex)
            {
                foreach (OptionEntry option in _options)
                    option.Option.SetActive(option.RuntimeIndex == preferredRuntimeIndex);
            }

            public void CancelCraftHold()
            {
                _heldRuntimeIndex = -1;
                _heldOption = null;
                _hasCraftedDuringHold = false;
                _heldRecipeRepeatable = false;
            }

            public override void Update(GameTime gameTime)
            {
                if (_heldRuntimeIndex >= 0)
                {
                    if (!Main.mouseLeft && !Main.mouseRight)
                        CancelCraftHold();
                    else
                        TryCraftHeldRecipe();
                }

                base.Update(gameTime);
            }

            private void BeginCraftHold(int runtimeIndex, RecipeOptionElement option)
            {
                if (_craftingService == null || option == null)
                    return;

                if (_heldRuntimeIndex >= 0 && _heldRuntimeIndex != runtimeIndex)
                    return;

                if (_heldRuntimeIndex < 0)
                {
                    _heldRuntimeIndex = runtimeIndex;
                    _heldOption = option;
                    _hasCraftedDuringHold = false;
                    _heldRecipeRepeatable = option.IsRepeatable;
                }

                TryCraftHeldRecipe();
            }

            private void EndCraftHold(int runtimeIndex)
            {
                if (_heldRuntimeIndex != runtimeIndex)
                    return;

                if (Main.mouseLeft || Main.mouseRight)
                    return;

                CancelCraftHold();
            }

            private void TryCraftHeldRecipe()
            {
                if (_heldRuntimeIndex < 0 || _heldOption == null || !_heldOption.IsMouseHovering)
                    return;

                if (!Main.mouseLeft && !Main.mouseRight)
                    return;

                if (_hasCraftedDuringHold && !_heldRecipeRepeatable)
                    return;

                if (Main.stackSplit > 1)
                    return;

                int quantity = Math.Max(1, Main.superFastStack + 1);

                if (!_craftingService.TryCraft(_heldRuntimeIndex, quantity))
                    return;

                _hasCraftedDuringHold = true;
            }

            private bool MatchesCurrentRecipes(IReadOnlyList<RecipeCatalogEntry> recipes)
            {
                if (_options.Count != recipes.Count)
                    return false;

                for (var index = 0; index < recipes.Count; index++)
                {
                    if (_options[index].RuntimeIndex != recipes[index].RuntimeIndex)
                        return false;
                }

                return true;
            }

            private static bool ContainsRuntimeIndex(IReadOnlyList<RecipeCatalogEntry> recipes, int runtimeIndex)
            {
                for (var index = 0; index < recipes.Count; index++)
                {
                    if (recipes[index].RuntimeIndex == runtimeIndex)
                        return true;
                }

                return false;
            }

            private readonly struct OptionEntry(int runtimeIndex, RecipeOptionElement option)
            {
                public int RuntimeIndex { get; } = runtimeIndex;

                public RecipeOptionElement Option { get; } = option;
            }
        }

        private sealed class RecipeOptionElement : UIElement
        {
            private const int HorizontalPadding = 6;
            private const int IconSize = 22;
            private const int TextHeight = 16;
            private const int TokenGap = 3;

            private readonly TextLabelElement _arrow;
            private readonly Action<int, RecipeOptionElement> _beginCraftHold;
            private readonly VanillaTextButton _buttonSurface;
            private readonly TextLabelElement _ellipsis;
            private readonly Action<int> _endCraftHold;
            private readonly List<TextLabelElement> _ingredientSeparators = new();
            private readonly List<IngredientVisual> _ingredientVisuals = new();
            private readonly CompendiumLocalization _localization;
            private readonly TextLabelElement _noIngredients;
            private readonly RecipeCatalogEntry _recipe;
            private readonly TextLabelElement _requirementDivider;
            private readonly TextLabelElement _requirementJoiner;
            private readonly TextLabelElement _requirementText;
            private readonly string _requirementTextValue;
            private readonly VanillaItemIcon _resultIcon;
            private readonly TextLabelElement _resultStack;
            private readonly VanillaItemIcon _stationIcon;
            private readonly string _tooltipText;
            private bool _isContentTruncated;

            public RecipeOptionElement(
                RecipeCatalogEntry recipe,
                RequirementPresentation requirement,
                string tooltipText,
                CompendiumLocalization localization,
                JourneyResearchState journeyResearchState,
                Action<int, RecipeOptionElement> beginCraftHold,
                Action<int> endCraftHold)
            {
                _recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                _beginCraftHold = beginCraftHold ?? throw new ArgumentNullException(nameof(beginCraftHold));
                _endCraftHold = endCraftHold ?? throw new ArgumentNullException(nameof(endCraftHold));
                _tooltipText = tooltipText ?? string.Empty;
                SetPadding(0f);

                _buttonSurface = new VanillaTextButton(string.Empty)
                {
                    Width = StyleDimension.Fill,
                    Height = StyleDimension.Fill
                };
                _buttonSurface.OnLeftMouseDown += HandleLeftMouseDown;
                _buttonSurface.OnRightMouseDown += HandleRightMouseDown;
                _buttonSurface.OnLeftMouseUp += HandleLeftMouseUp;
                _buttonSurface.OnRightMouseUp += HandleRightMouseUp;
                Append(_buttonSurface);

                for (var index = 0; index < recipe.Ingredients.Count; index++)
                {
                    RecipeIngredient ingredient = recipe.Ingredients[index];
                    var visual = new IngredientVisual(ingredient, _localization, journeyResearchState);
                    _ingredientVisuals.Add(visual);
                    visual.AppendTo(_buttonSurface);

                    if (index > 0)
                    {
                        TextLabelElement separator = CreateTextLabel("+");
                        _ingredientSeparators.Add(separator);
                        _buttonSurface.Append(separator);
                    }
                }

                _noIngredients = CreateTextLabel(
                    _localization.Get(CompendiumTextKeys.DirectCraft.NoIngredients),
                    UIColors.TextDim);
                _buttonSurface.Append(_noIngredients);

                _ellipsis = CreateTextLabel("…", UIColors.TextDim);
                _buttonSurface.Append(_ellipsis);

                _arrow = CreateTextLabel("→");
                _buttonSurface.Append(_arrow);

                _resultIcon = CreateItemIcon(recipe.ResultItemId, journeyResearchState, showResearchBadge: true);
                _buttonSurface.Append(_resultIcon);

                _resultStack = CreateTextLabel(
                    _localization.Format(CompendiumTextKeys.Common.Stack, recipe.ResultStack));
                _buttonSurface.Append(_resultStack);

                _requirementDivider = CreateTextLabel("·", UIColors.TextDim);
                _buttonSurface.Append(_requirementDivider);

                if (requirement.StationItemId > 0)
                {
                    _stationIcon = CreateItemIcon(
                        requirement.StationItemId,
                        journeyResearchState,
                        showResearchBadge: false);
                    _buttonSurface.Append(_stationIcon);
                }

                _requirementJoiner = CreateTextLabel("+", UIColors.TextDim);
                _buttonSurface.Append(_requirementJoiner);

                _requirementTextValue = requirement.Text;
                _requirementText = CreateTextLabel(_requirementTextValue, UIColors.TextDim);
                _buttonSurface.Append(_requirementText);

                IsRepeatable = ContentSamples.ItemsByType[recipe.ResultItemId].maxStack > 1;
            }

            public bool IsRepeatable { get; }

            public void SetActive(bool isActive)
            {
                _buttonSurface.IsActive = isActive;
            }

            public int MeasureNaturalWidth()
            {
                int width = HorizontalPadding;

                if (_recipe.Ingredients.Count == 0)
                {
                    width += UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.DirectCraft.NoIngredients));
                }
                else
                {
                    for (var index = 0; index < _ingredientVisuals.Count; index++)
                    {
                        if (index > 0)
                            width += UIRenderer.MeasureText("+") + TokenGap;

                        width += _ingredientVisuals[index].MeasureWidth();
                        width += TokenGap;
                    }
                }

                width += TokenGap;
                width += UIRenderer.MeasureText("→") + TokenGap;
                width += IconSize + TokenGap;
                width += UIRenderer.MeasureText(
                             _localization.Format(CompendiumTextKeys.Common.Stack, _recipe.ResultStack)) +
                         TokenGap;
                width += UIRenderer.MeasureText("·") + TokenGap;

                if (_stationIcon != null)
                {
                    width += IconSize + TokenGap;

                    if (!string.IsNullOrEmpty(_requirementTextValue))
                        width += UIRenderer.MeasureText("+") + TokenGap;
                }

                if (!string.IsNullOrEmpty(_requirementTextValue))
                    width += UIRenderer.MeasureText(_requirementTextValue);

                width += HorizontalPadding;
                return width;
            }

            public override void RecalculateChildren()
            {
                _buttonSurface.Left.Set(0f, 0f);
                _buttonSurface.Top.Set(0f, 0f);
                _buttonSurface.Width.Set(0f, 1f);
                _buttonSurface.Height.Set(0f, 1f);

                int width = Math.Max(0, (int)GetInnerDimensions().Width);
                LayoutContent(width);
                base.RecalculateChildren();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                if (!_isContentTruncated ||
                    !IsMouseHovering ||
                    IsMouseOverItemIcon() ||
                    string.IsNullOrEmpty(_tooltipText))
                {
                    return;
                }

                Tooltip.Set(_tooltipText);
            }

            private void LayoutContent(int width)
            {
                HideAllContent();
                _isContentTruncated = false;

                int right = Math.Max(HorizontalPadding, width - HorizontalPadding);
                int cursor = HorizontalPadding;
                int centerTextY = Math.Max(0, (OptionHeight - TextHeight) / 2);
                int centerIconY = Math.Max(0, (OptionHeight - IconSize) / 2);

                if (_recipe.Ingredients.Count == 0)
                {
                    int noIngredientsWidth =
                        UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.DirectCraft.NoIngredients));

                    if (cursor + noIngredientsWidth + CalculateTailReserve() > right)
                        _isContentTruncated = true;

                    PlaceText(_noIngredients, ref cursor, noIngredientsWidth, centerTextY);
                }
                else
                {
                    for (var index = 0; index < _ingredientVisuals.Count; index++)
                    {
                        IngredientVisual visual = _ingredientVisuals[index];
                        int separatorWidth = index == 0 ? 0 : UIRenderer.MeasureText("+") + TokenGap;
                        int unitWidth = visual.MeasureWidth();
                        int requiredWidth = separatorWidth + unitWidth;
                        int tailReserve = CalculateTailReserve();

                        if (cursor + requiredWidth + tailReserve > right)
                        {
                            _isContentTruncated = true;
                            break;
                        }

                        if (index > 0)
                        {
                            TextLabelElement separator = _ingredientSeparators[index - 1];
                            PlaceText(separator, ref cursor, UIRenderer.MeasureText("+"), centerTextY);
                            cursor += TokenGap;
                        }

                        visual.Layout(ref cursor, centerTextY, centerIconY);
                        cursor += TokenGap;
                    }

                    if (_isContentTruncated)
                    {
                        int ellipsisWidth = UIRenderer.MeasureText("…");
                        PlaceText(_ellipsis, ref cursor, ellipsisWidth, centerTextY);
                        cursor += TokenGap;
                    }
                }

                cursor += TokenGap;
                PlaceText(_arrow, ref cursor, UIRenderer.MeasureText("→"), centerTextY);
                cursor += TokenGap;

                PlaceIcon(_resultIcon, ref cursor, centerIconY);
                cursor += TokenGap;
                PlaceText(
                    _resultStack,
                    ref cursor,
                    UIRenderer.MeasureText(_localization.Format(CompendiumTextKeys.Common.Stack, _recipe.ResultStack)),
                    centerTextY);
                cursor += TokenGap;

                if (cursor >= right)
                {
                    _isContentTruncated = true;
                    return;
                }

                PlaceText(_requirementDivider, ref cursor, UIRenderer.MeasureText("·"), centerTextY);
                cursor += TokenGap;

                if (_stationIcon != null)
                {
                    if (cursor + IconSize > right)
                    {
                        _isContentTruncated = true;
                        return;
                    }

                    PlaceIcon(_stationIcon, ref cursor, centerIconY);
                    cursor += TokenGap;

                    if (!string.IsNullOrEmpty(_requirementTextValue))
                    {
                        int joinerWidth = UIRenderer.MeasureText("+");

                        if (cursor + joinerWidth > right)
                        {
                            _isContentTruncated = true;
                            return;
                        }

                        PlaceText(_requirementJoiner, ref cursor, joinerWidth, centerTextY);
                        cursor += TokenGap;
                    }
                }

                if (string.IsNullOrEmpty(_requirementTextValue))
                    return;

                int remainingWidth = Math.Max(0, right - cursor);

                if (remainingWidth <= 0)
                {
                    _isContentTruncated = true;
                    return;
                }

                _requirementText.Left.Set(cursor, 0f);
                _requirementText.Top.Set(centerTextY, 0f);
                _requirementText.Width.Set(remainingWidth, 0f);
                _requirementText.Height.Set(TextHeight, 0f);

                if (UIRenderer.MeasureText(_requirementTextValue) > remainingWidth)
                    _isContentTruncated = true;
            }

            private bool IsMouseOverItemIcon()
            {
                foreach (IngredientVisual visual in _ingredientVisuals)
                {
                    if (visual.IsIconHovering)
                        return true;
                }

                return _resultIcon.IsMouseHovering || _stationIcon?.IsMouseHovering == true;
            }

            private int CalculateTailReserve()
            {
                int width = UIRenderer.MeasureText("→") + TokenGap;
                width += IconSize + TokenGap;
                width += UIRenderer.MeasureText(
                             _localization.Format(CompendiumTextKeys.Common.Stack, _recipe.ResultStack)) +
                         TokenGap;
                width += UIRenderer.MeasureText("·") + TokenGap;

                if (_stationIcon != null)
                    width += IconSize;
                else if (!string.IsNullOrEmpty(_requirementTextValue))
                    width += Math.Min(UIRenderer.MeasureText(_requirementTextValue), 40);

                return width;
            }

            private void HideAllContent()
            {
                foreach (IngredientVisual visual in _ingredientVisuals)
                    visual.Hide();

                foreach (TextLabelElement separator in _ingredientSeparators)
                    Hide(separator);

                Hide(_noIngredients);
                Hide(_ellipsis);
                Hide(_arrow);
                Hide(_resultIcon);
                Hide(_resultStack);
                Hide(_requirementDivider);
                Hide(_stationIcon);
                Hide(_requirementJoiner);
                Hide(_requirementText);
            }

            private void HandleLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
            {
                if (evt.Target is VanillaItemIcon itemIcon &&
                    itemIcon.TryHandleJourneyDuplicationFromOwnerMouseDown(evt, _buttonSurface, rightClick: false))
                {
                    return;
                }

                _beginCraftHold(_recipe.RuntimeIndex, this);
            }

            private void HandleRightMouseDown(UIMouseEvent evt, UIElement listeningElement)
            {
                if (evt.Target is VanillaItemIcon itemIcon &&
                    itemIcon.TryHandleJourneyDuplicationFromOwnerMouseDown(evt, _buttonSurface, rightClick: true))
                {
                    return;
                }

                _beginCraftHold(_recipe.RuntimeIndex, this);
            }

            private void HandleLeftMouseUp(UIMouseEvent evt, UIElement listeningElement)
            {
                _endCraftHold(_recipe.RuntimeIndex);
            }

            private void HandleRightMouseUp(UIMouseEvent evt, UIElement listeningElement)
            {
                _endCraftHold(_recipe.RuntimeIndex);
            }

            private static TextLabelElement CreateTextLabel(string text)
            {
                return CreateTextLabel(text, UIColors.Text);
            }

            private static TextLabelElement CreateTextLabel(string text, Color4 color)
            {
                return new TextLabelElement(text, color)
                {
                    IgnoresMouseInteraction = true
                };
            }

            private static VanillaItemIcon CreateItemIcon(
                int itemId,
                JourneyResearchState journeyResearchState,
                bool showResearchBadge)
            {
                return new VanillaItemIcon(journeyResearchState)
                {
                    ItemId = itemId,
                    ShowResearchBadge = showResearchBadge,
                    ShowTooltip = true
                };
            }

            private static void PlaceIcon(UIElement icon, ref int cursor, int top)
            {
                if (icon == null)
                    return;

                icon.Left.Set(cursor, 0f);
                icon.Top.Set(top, 0f);
                icon.Width.Set(IconSize, 0f);
                icon.Height.Set(IconSize, 0f);
                cursor += IconSize;
            }

            private static void PlaceText(TextLabelElement label, ref int cursor, int width, int top)
            {
                if (label == null || width <= 0)
                    return;

                label.Left.Set(cursor, 0f);
                label.Top.Set(top, 0f);
                label.Width.Set(width, 0f);
                label.Height.Set(TextHeight, 0f);
                cursor += width;
            }

            private static void Hide(UIElement element)
            {
                if (element == null)
                    return;

                element.Width.Set(0f, 0f);
                element.Height.Set(0f, 0f);
            }

            private sealed class IngredientVisual
            {
                private readonly TextLabelElement _anyLabel;
                private readonly string _anyText;
                private readonly VanillaItemIcon _icon;
                private readonly TextLabelElement _stackLabel;
                private readonly string _stackText;

                public IngredientVisual(
                    RecipeIngredient ingredient,
                    CompendiumLocalization localization,
                    JourneyResearchState journeyResearchState)
                {
                    if (ingredient == null)
                        throw new ArgumentNullException(nameof(ingredient));
                    if (localization == null)
                        throw new ArgumentNullException(nameof(localization));

                    if (ingredient.Requirement.Kind == RecipeIngredientRequirementKind.RecipeGroup)
                    {
                        _anyText = localization.Get(CompendiumTextKeys.DirectCraft.Any);
                        _anyLabel = CreateTextLabel(_anyText, UIColors.TextDim);
                    }

                    _icon = CreateItemIcon(
                        ingredient.DisplayItemId,
                        journeyResearchState,
                        showResearchBadge: ingredient.Requirement.Kind != RecipeIngredientRequirementKind.RecipeGroup);
                    _stackText = localization.Format(CompendiumTextKeys.Common.Stack, ingredient.Stack);
                    _stackLabel = CreateTextLabel(_stackText);
                }

                public bool IsIconHovering => _icon.IsMouseHovering;

                public void AppendTo(UIElement parent)
                {
                    if (_anyLabel != null)
                        parent.Append(_anyLabel);

                    parent.Append(_icon);
                    parent.Append(_stackLabel);
                }

                public int MeasureWidth()
                {
                    int width = IconSize + TokenGap + UIRenderer.MeasureText(_stackText);

                    if (_anyLabel != null)
                        width += UIRenderer.MeasureText(_anyText) + TokenGap;

                    return width;
                }

                public void Layout(ref int cursor, int textTop, int iconTop)
                {
                    if (_anyLabel != null)
                    {
                        PlaceText(_anyLabel, ref cursor, UIRenderer.MeasureText(_anyText), textTop);
                        cursor += TokenGap;
                    }

                    PlaceIcon(_icon, ref cursor, iconTop);
                    cursor += TokenGap;
                    PlaceText(_stackLabel, ref cursor, UIRenderer.MeasureText(_stackText), textTop);
                }

                public void Hide()
                {
                    RecipeOptionElement.Hide(_anyLabel);
                    RecipeOptionElement.Hide(_icon);
                    RecipeOptionElement.Hide(_stackLabel);
                }
            }
        }
    }
}