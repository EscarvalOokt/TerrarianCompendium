using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class NpcDetailsView : UIElement
    {
        private const int ContentPadding = 6;
        private const int IdentityHeight = 40;
        private const int IconSize = 36;
        private const int IdentityTextGap = 6;
        private const int RowHeight = 18;
        private const int TextHeight = 16;
        private const int DifficultyButtonSize = 30;
        private const int ControlGap = 4;
        private const int MetadataIconSize = 30;
        private const int MetadataIconGap = 4;
        private const int MetadataIconColumns = 6;
        private const int StockIconSize = 30;
        private const int StockIconGap = 2;
        private const int StockHeaderControlGap = 4;
        private const int StockHeaderRowHeight = 24;
        private readonly VanillaIconButton _classicButton;
        private readonly VanillaCoinValueElement _coinValueElement;
        private readonly VanillaIconButton _expertButton;
        private readonly VanillaBestiaryFilterCatalog _filterCatalog;
        private readonly NpcIdentityIcon _icon;
        private readonly List<DebuffImmunityIcon> _immunityIcons = new();
        private readonly CompendiumLocalization _localization;
        private readonly List<LootRowElement> _lootRowElements = new();
        private readonly VanillaIconButton _masterButton;
        private readonly NpcDetailsModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly List<VanillaBestiaryConditionIcon> _spawnConditionIcons = new();
        private readonly List<VanillaItemRelationButton> _stockButtons = new();
        private readonly VanillaTextButton _stockConditionsButton;
        private readonly MerchantStockConditionsPopoverContent _stockConditionsContent;

        private int _coinsContentTop;
        private int _difficultyContentTop;
        private NpcDifficultyMode _difficultyMode = NpcDifficultyMode.Classic;
        private bool _hasNpc;
        private int _immunityContentTop;
        private long _localizationRevision = -1;
        private int _lootContentTop;
        private int _npcNetId;
        private NpcDetailsProjection _projection;
        private bool _refreshRequired = true;
        private int _spawnContentTop;
        private int _statsContentTop;
        private VanillaPopover _stockConditionsPopover;
        private int _stockContentTop;
        private UIElement _stockPopoverHost;

        public NpcDetailsView(
            NpcDetailsModel model,
            BrowserNavigationState navigationState,
            VanillaBestiaryFilterCatalog filterCatalog,
            CompendiumLocalization localization)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _filterCatalog = filterCatalog ?? throw new ArgumentNullException(nameof(filterCatalog));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            Width = StyleDimension.Fill;
            SetPadding(0f);

            _icon = new NpcIdentityIcon();
            Append(_icon);

            _coinValueElement = new VanillaCoinValueElement();
            Append(_coinValueElement);

            _classicButton = CreateDifficultyButton(NpcDifficultyMode.Classic);
            _expertButton = CreateDifficultyButton(NpcDifficultyMode.Expert);
            _masterButton = CreateDifficultyButton(NpcDifficultyMode.Master);
            Append(_classicButton);
            Append(_expertButton);
            Append(_masterButton);

            _stockConditionsButton = new VanillaTextButton(
                _localization.Get(CompendiumTextKeys.NpcDetails.Conditions),
                ToggleStockConditionsPopover)
            {
                TooltipText = _localization.Get(CompendiumTextKeys.NpcDetails.ConditionsTooltip)
            };
            Append(_stockConditionsButton);

            _stockConditionsContent = new MerchantStockConditionsPopoverContent(_localization);

            HideDynamicChildren();
            SynchronizeDifficultyButtons();
        }

        public int ContentHeight { get; private set; } = ContentPadding * 2 + TextHeight;

        public bool HasOpenTransientSurface => _stockConditionsPopover?.IsOpen == true;

        public void AttachPopoverHost(UIElement host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (ReferenceEquals(_stockPopoverHost, host))
                return;

            if (_stockPopoverHost != null)
                throw new InvalidOperationException("NPC stock conditions popover host is already attached.");

            _stockPopoverHost = host;
            _stockConditionsPopover = new VanillaPopover(host, _stockConditionsButton, _stockConditionsContent);
        }

        public bool TryCloseTransientSurface()
        {
            if (_stockConditionsPopover?.IsOpen != true)
                return false;

            _stockConditionsPopover.Close();
            _stockConditionsButton.IsActive = false;
            return true;
        }

        public void ShowNpc(int npcNetId)
        {
            if (_hasNpc && _npcNetId == npcNetId)
            {
                Refresh();
                return;
            }

            TryCloseTransientSurface();
            _hasNpc = true;
            _npcNetId = npcNetId;
            _refreshRequired = true;
            Refresh();
        }

        public void Clear()
        {
            TryCloseTransientSurface();
            _hasNpc = false;
            _npcNetId = 0;
            _projection = null;
            _refreshRequired = false;
            _stockConditionsContent.Bind(Array.Empty<NpcDetailsMerchantStockEntry>());
            HideDynamicChildren();
            ContentHeight = ContentPadding * 2 + TextHeight;
            Height.Set(ContentHeight, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            Refresh();
            base.Update(gameTime);
            _stockConditionsButton.IsActive = HasOpenTransientSurface;
        }

        public override void OnDeactivate()
        {
            TryCloseTransientSurface();
            base.OnDeactivate();
        }

        public override void RecalculateChildren()
        {
            if (_projection != null)
            {
                LayoutDifficultyButtons(_difficultyContentTop);
                LayoutSpawnConditions(_spawnContentTop);
                LayoutImmunities(_immunityContentTop);
                LayoutCoinValue(_coinsContentTop);

                if (_projection.MerchantStock.Count > 0)
                {
                    LayoutStock(_stockContentTop);
                    LayoutStockConditionsButton(_stockContentTop);
                }
            }

            base.RecalculateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            var viewY = (int)dimensions.Y;
            int x = (int)dimensions.X + ContentPadding;
            int identityY = viewY + ContentPadding;
            int width = Math.Max(0, (int)dimensions.Width - ContentPadding * 2);

            if (width <= 0)
                return;

            if (_projection == null)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.NpcDetails.Missing),
                    x,
                    identityY,
                    width,
                    UIColors.TextDim);
                return;
            }

            DrawIdentityText(_projection, x, identityY, width);

            DrawSectionHeaderAt(
                _localization.Get(CompendiumTextKeys.NpcDetails.Difficulty),
                x,
                viewY + _difficultyContentTop,
                width);
            DrawSectionHeaderAt(
                _localization.Get(CompendiumTextKeys.NpcDetails.Statistics),
                x,
                viewY + _statsContentTop,
                width);
            DrawStats(_projection, x, viewY + _statsContentTop, width);

            DrawSectionHeaderAt(
                _localization.Get(CompendiumTextKeys.NpcDetails.FoundIn),
                x,
                viewY + _spawnContentTop,
                width);

            if (_projection.SpawnConditions.Count == 0)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.NpcDetails.NoEnvironmentTags),
                    x,
                    viewY + _spawnContentTop,
                    width,
                    UIColors.TextDim);
            }

            DrawSectionHeaderAt(
                _localization.Get(CompendiumTextKeys.NpcDetails.BaseImmunities),
                x,
                viewY + _immunityContentTop,
                width);

            if (_projection.BaseDebuffImmunities.Count == 0)
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.Common.None),
                    x,
                    viewY + _immunityContentTop,
                    width,
                    UIColors.TextDim);

            DrawSectionHeaderAt(
                _localization.Get(CompendiumTextKeys.NpcDetails.Coins),
                x,
                viewY + _coinsContentTop,
                width);
            DrawCoins(_projection, x, viewY + _coinsContentTop, width);

            if (_projection.MerchantStock.Count > 0)
                DrawStockSectionHeaderAt(
                    _localization.Get(CompendiumTextKeys.NpcDetails.Stock),
                    x,
                    viewY + _stockContentTop,
                    width);

            DrawSectionHeaderAt(
                _localization.Get(CompendiumTextKeys.NpcDetails.Drops),
                x,
                viewY + _lootContentTop,
                width);

            if (_projection.LootRows.Count == 0)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.NpcDetails.NoDrops),
                    x,
                    viewY + _lootContentTop,
                    width,
                    UIColors.TextDim);
            }
        }

        private void Refresh()
        {
            NpcDetailsProjection nextProjection = null;

            if (_hasNpc)
                _model.TryGetProjection(_npcNetId, _difficultyMode, out nextProjection);

            if (!_refreshRequired && ReferenceEquals(_projection, nextProjection))
                return;

            _refreshRequired = false;
            _projection = nextProjection;
            RebuildChildren();
        }

        private void RebuildChildren()
        {
            SynchronizeDifficultyButtons();

            if (_projection == null)
            {
                HideDynamicChildren();
                ContentHeight = ContentPadding * 2 + TextHeight;
                Height.Set(ContentHeight, 0f);
                Recalculate();
                return;
            }

            _icon.Bind(_projection.NpcNetId, !_projection.IsEncountered);
            _icon.Left.Set(ContentPadding, 0f);
            _icon.Top.Set(ContentPadding, 0f);
            _icon.Width.Set(IconSize, 0f);
            _icon.Height.Set(IconSize, 0f);

            int cursor = ContentPadding + IdentityHeight;

            cursor = AdvanceSectionHeader(cursor);
            _difficultyContentTop = cursor;
            LayoutDifficultyButtons(cursor);
            cursor += DifficultyButtonSize;

            cursor = AdvanceSectionHeader(cursor);
            _statsContentTop = cursor;
            cursor += GetStatsHeight(_projection);

            cursor = AdvanceSectionHeader(cursor);
            _spawnContentTop = cursor;
            LayoutSpawnConditions(cursor);
            cursor += _projection.SpawnConditions.Count == 0
                ? RowHeight
                : GetMetadataIconGridHeight(_projection.SpawnConditions.Count);

            cursor = AdvanceSectionHeader(cursor);
            _immunityContentTop = cursor;
            LayoutImmunities(cursor);
            cursor += _projection.BaseDebuffImmunities.Count == 0
                ? RowHeight
                : GetMetadataIconGridHeight(_projection.BaseDebuffImmunities.Count);

            cursor = AdvanceSectionHeader(cursor);
            _coinsContentTop = cursor;

            if (_projection.Stats == null)
            {
                _coinValueElement.Hide();
            }
            else
            {
                int monetaryValue = Math.Max(0, (int)Math.Round(_projection.Stats.MonetaryValue));
                _coinValueElement.Bind(
                    monetaryValue,
                    leadingItemId: 0,
                    showCopperWhenZero: true,
                    tooltipText: _localization.Get(CompendiumTextKeys.NpcDetails.MonetaryValue));
            }

            cursor += VanillaCoinValueElement.RowHeight;

            if (_projection.MerchantStock.Count > 0)
            {
                cursor = AdvanceStockSectionHeader(cursor);
                _stockContentTop = cursor;
                EnsureStockButtonCapacity(_projection.MerchantStock.Count);

                for (var index = 0; index < _projection.MerchantStock.Count; index++)
                {
                    NpcDetailsItemReference item = _projection.MerchantStock[index].Item;
                    _stockButtons[index]
                        .Bind(
                            item.ItemId,
                            item.IsCollectionTracked && !item.IsFound,
                            item.IsNavigable ? item.ItemId : 0);
                }

                HideUnusedStockButtons(_projection.MerchantStock.Count);
                LayoutStock(_stockContentTop);
                LayoutStockConditionsButton(_stockContentTop);
                _stockConditionsContent.Bind(_projection.MerchantStock);
                cursor += CalculateStockGridHeight(_projection.MerchantStock.Count, GetAvailableContentWidth());

                if (HasOpenTransientSurface)
                    _stockConditionsPopover.Recalculate();
            }
            else
            {
                TryCloseTransientSurface();
                HideUnusedStockButtons(0);
                HideStockConditionsButton();
                _stockConditionsContent.Bind(Array.Empty<NpcDetailsMerchantStockEntry>());
            }

            cursor = AdvanceSectionHeader(cursor);
            _lootContentTop = cursor;
            EnsureLootRowCapacity(_projection.LootRows.Count);

            for (var index = 0; index < _projection.LootRows.Count; index++)
            {
                NpcDetailsLootRow row = _projection.LootRows[index];
                LootRowElement element = _lootRowElements[index];
                element.Bind(row);
                element.Left.Set(ContentPadding, 0f);
                element.Top.Set(cursor, 0f);
                element.Width.Set(-ContentPadding * 2f, 1f);
                element.Height.Set(element.RequiredHeight, 0f);
                cursor += element.RequiredHeight;
            }

            HideUnusedLootRows(_projection.LootRows.Count);

            if (_projection.LootRows.Count == 0)
                cursor += RowHeight;

            ContentHeight = cursor + ContentPadding;
            Height.Set(ContentHeight, 0f);
            Recalculate();
        }

        private VanillaIconButton CreateDifficultyButton(NpcDifficultyMode difficulty)
        {
            return new VanillaIconButton(
                bounds => VanillaPresentationIcons.DrawNpcDifficulty(bounds, difficulty),
                () => SelectDifficulty(difficulty))
            {
                TooltipText = GetDifficultyText(difficulty),
                ActiveBorderColor = UIColors.Accent,
                ActiveBorderThickness = 2
            };
        }

        private void SynchronizeLocalization()
        {
            if (_localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _classicButton.TooltipText = GetDifficultyText(NpcDifficultyMode.Classic);
            _expertButton.TooltipText = GetDifficultyText(NpcDifficultyMode.Expert);
            _masterButton.TooltipText = GetDifficultyText(NpcDifficultyMode.Master);
            _stockConditionsButton.Text = _localization.Get(CompendiumTextKeys.NpcDetails.Conditions);
            _stockConditionsButton.TooltipText = _localization.Get(CompendiumTextKeys.NpcDetails.ConditionsTooltip);
            _refreshRequired = true;

            if (HasOpenTransientSurface)
                _stockConditionsPopover.Recalculate();
        }

        private string GetDifficultyText(NpcDifficultyMode difficulty)
        {
            return difficulty switch
            {
                NpcDifficultyMode.Classic => _localization.Get(CompendiumTextKeys.NpcDetails.Classic),
                NpcDifficultyMode.Expert => _localization.Get(CompendiumTextKeys.NpcDetails.Expert),
                NpcDifficultyMode.Master => _localization.Get(CompendiumTextKeys.NpcDetails.Master),
                _ => difficulty.ToString()
            };
        }

        private void SelectDifficulty(NpcDifficultyMode difficulty)
        {
            if (_difficultyMode == difficulty)
                return;

            _difficultyMode = difficulty;
            _refreshRequired = true;
            SynchronizeDifficultyButtons();
            Refresh();
        }

        private void SynchronizeDifficultyButtons()
        {
            _classicButton.IsActive = _difficultyMode == NpcDifficultyMode.Classic;
            _expertButton.IsActive = _difficultyMode == NpcDifficultyMode.Expert;
            _masterButton.IsActive = _difficultyMode == NpcDifficultyMode.Master;
        }

        private void LayoutDifficultyButtons(int top)
        {
            int availableWidth = Math.Max(0, (int)GetInnerDimensions().Width - ContentPadding * 2);
            int groupWidth = DifficultyButtonSize * 3 + ControlGap * 2;
            int left = ContentPadding + Math.Max(0, (availableWidth - groupWidth) / 2);

            LayoutElement(_classicButton, left, top, DifficultyButtonSize, DifficultyButtonSize);
            LayoutElement(
                _expertButton,
                left + DifficultyButtonSize + ControlGap,
                top,
                DifficultyButtonSize,
                DifficultyButtonSize);
            LayoutElement(
                _masterButton,
                left + (DifficultyButtonSize + ControlGap) * 2,
                top,
                DifficultyButtonSize,
                DifficultyButtonSize);
        }

        private void LayoutCoinValue(int top)
        {
            if (_projection?.Stats == null)
            {
                _coinValueElement.Hide();
                return;
            }

            int availableWidth = Math.Max(0, (int)GetInnerDimensions().Width - ContentPadding * 2);
            LayoutElement(_coinValueElement, ContentPadding, top, availableWidth, VanillaCoinValueElement.RowHeight);
        }

        private void LayoutSpawnConditions(int top)
        {
            EnsureSpawnConditionCapacity(_projection.SpawnConditions.Count);

            for (var index = 0; index < _projection.SpawnConditions.Count; index++)
            {
                NpcBestiarySpawnCondition condition = _projection.SpawnConditions[index];
                VanillaBestiaryConditionIcon element = _spawnConditionIcons[index];
                int row = index / MetadataIconColumns;
                int column = index % MetadataIconColumns;
                element.Bind(condition.DisplayNameKey, condition.DisplayName);
                LayoutElement(
                    element,
                    ContentPadding + column * (MetadataIconSize + MetadataIconGap),
                    top + row * (MetadataIconSize + MetadataIconGap),
                    MetadataIconSize,
                    MetadataIconSize);
            }

            for (int index = _projection.SpawnConditions.Count; index < _spawnConditionIcons.Count; index++)
                _spawnConditionIcons[index].Hide();
        }

        private void LayoutImmunities(int top)
        {
            EnsureImmunityCapacity(_projection.BaseDebuffImmunities.Count);

            for (var index = 0; index < _projection.BaseDebuffImmunities.Count; index++)
            {
                NpcBestiaryDebuffImmunity immunity = _projection.BaseDebuffImmunities[index];
                DebuffImmunityIcon element = _immunityIcons[index];
                int row = index / MetadataIconColumns;
                int column = index % MetadataIconColumns;
                element.Bind(immunity);
                LayoutElement(
                    element,
                    ContentPadding + column * (MetadataIconSize + MetadataIconGap),
                    top + row * (MetadataIconSize + MetadataIconGap),
                    MetadataIconSize,
                    MetadataIconSize);
            }

            for (int index = _projection.BaseDebuffImmunities.Count; index < _immunityIcons.Count; index++)
                _immunityIcons[index].Hide();
        }

        private void LayoutStock(int top)
        {
            int contentWidth = GetAvailableContentWidth();
            int columns = CalculateStockColumns(contentWidth);

            for (var index = 0; index < _projection.MerchantStock.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                LayoutElement(
                    _stockButtons[index],
                    ContentPadding + column * (StockIconSize + StockIconGap),
                    top + row * (StockIconSize + StockIconGap),
                    StockIconSize,
                    StockIconSize);
            }
        }

        private void LayoutStockConditionsButton(int stockContentTop)
        {
            if (_projection?.MerchantStock.Count <= 0)
            {
                HideStockConditionsButton();
                return;
            }

            int availableWidth = GetAvailableContentWidth();
            int naturalWidth =
                VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.NpcDetails.Conditions));
            int buttonWidth = Math.Min(availableWidth, naturalWidth);
            int headerTop = stockContentTop - DetailsSectionLayout.ContentGap - StockHeaderRowHeight;
            LayoutElement(
                _stockConditionsButton,
                ContentPadding + Math.Max(0, availableWidth - buttonWidth),
                headerTop,
                buttonWidth,
                StockHeaderRowHeight);
        }

        private int GetAvailableContentWidth()
        {
            return Math.Max(0, (int)GetInnerDimensions().Width - ContentPadding * 2);
        }

        private static int CalculateStockColumns(int contentWidth)
        {
            return Math.Max(1, (Math.Max(0, contentWidth) + StockIconGap) / (StockIconSize + StockIconGap));
        }

        private static int CalculateStockGridHeight(int count, int contentWidth)
        {
            if (count <= 0)
                return 0;

            int columns = CalculateStockColumns(contentWidth);
            int rows = (count + columns - 1) / columns;
            return rows * StockIconSize + Math.Max(0, rows - 1) * StockIconGap;
        }

        private static void LayoutElement(UIElement element, int left, int top, int width, int height)
        {
            element.Left.Set(left, 0f);
            element.Top.Set(top, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }

        private static int AdvanceSectionHeader(int cursor)
        {
            return cursor + DetailsSectionLayout.HeaderHeight;
        }

        private static int AdvanceStockSectionHeader(int cursor)
        {
            return cursor +
                   DetailsSectionLayout.SeparatorHeight +
                   StockHeaderRowHeight +
                   DetailsSectionLayout.ContentGap;
        }

        private static int GetMetadataIconGridHeight(int count)
        {
            if (count <= 0)
                return 0;

            int rows = (count + MetadataIconColumns - 1) / MetadataIconColumns;
            return rows * MetadataIconSize + Math.Max(0, rows - 1) * MetadataIconGap;
        }

        private static int GetStatsHeight(NpcDetailsProjection projection)
        {
            int rows = projection.Stats == null ? 2 : 5;

            if (projection.RareSpawnRarityLevel.HasValue)
                rows++;

            return rows * RowHeight;
        }

        private void DrawStats(NpcDetailsProjection projection, int x, int y, int width)
        {
            int rowY = y;

            if (projection.Stats == null)
            {
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.NpcDetails.StatsUnavailable),
                    x,
                    rowY,
                    width,
                    UIColors.TextDim);
                rowY += RowHeight;
            }
            else
            {
                NpcBestiaryStatsSnapshot stats = projection.Stats;

                DrawTextRow(_localization.Format(CompendiumTextKeys.NpcDetails.Damage, stats.Damage), x, rowY, width);
                rowY += RowHeight;

                DrawTextRow(_localization.Format(CompendiumTextKeys.NpcDetails.MaxLife, stats.LifeMax), x, rowY, width);
                rowY += RowHeight;

                DrawTextRow(_localization.Format(CompendiumTextKeys.NpcDetails.Defense, stats.Defense), x, rowY, width);
                rowY += RowHeight;

                DrawTextRow(
                    _localization.Format(CompendiumTextKeys.NpcDetails.KnockbackTaken, stats.KnockbackResist * 100f),
                    x,
                    rowY,
                    width);
                rowY += RowHeight;
            }

            DrawTextRow(
                _localization.Format(
                    CompendiumTextKeys.NpcDetails.Rarity,
                    FormatRarity(projection.BestiaryRarityStars)),
                x,
                rowY,
                width);
            rowY += RowHeight;

            if (projection.RareSpawnRarityLevel.HasValue)
            {
                DrawTextRow(
                    _localization.Format(
                        CompendiumTextKeys.NpcDetails.RareCreature,
                        projection.RareSpawnRarityLevel.Value),
                    x,
                    rowY,
                    width,
                    UIColors.TextDim);
            }
        }

        private void DrawCoins(NpcDetailsProjection projection, int x, int y, int width)
        {
            if (projection.Stats == null)
                DrawTextRow(
                    _localization.Get(CompendiumTextKeys.NpcDetails.Unavailable),
                    x,
                    y,
                    width,
                    UIColors.TextDim);
        }

        private string FormatRarity(int stars)
        {
            return stars <= 0 ? _localization.Get(CompendiumTextKeys.Common.None) : new string('★', stars);
        }

        private void DrawIdentityText(NpcDetailsProjection projection, int x, int y, int width)
        {
            int textX = x + IconSize + IdentityTextGap;
            int textWidth = Math.Max(0, width - IconSize - IdentityTextGap);
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

            string fullIdText = _localization.Format(CompendiumTextKeys.Common.NpcId, projection.NpcNetId);
            string idText = TruncatedTextPresentation.Truncate(fullIdText, textWidth, out bool idWasTruncated);

            if (idText.Length == 0)
                return;

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

        private void DrawStockSectionHeaderAt(string title, int x, int contentY, int width)
        {
            int titleY = contentY - DetailsSectionLayout.ContentGap - StockHeaderRowHeight;
            int dividerY = titleY - DetailsSectionLayout.SeparatorHeight;

            if (width > 0)
            {
                UIRenderer.DrawRect(
                    x,
                    dividerY + DetailsSectionLayout.SeparatorSpacing,
                    width,
                    DetailsSectionLayout.DividerHeight,
                    UIColors.Divider);
            }

            int buttonWidth = Math.Min(
                width,
                VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.NpcDetails.Conditions)));
            int titleWidth = Math.Max(0, width - buttonWidth - StockHeaderControlGap);
            string displayTitle = TruncatedTextPresentation.Truncate(title, titleWidth, out bool wasTruncated);

            if (displayTitle.Length == 0)
                return;

            int textY = titleY + Math.Max(0, (StockHeaderRowHeight - TextHeight) / 2);
            UIRenderer.DrawText(displayTitle, x, textY, UIColors.TextTitle);
            TruncatedTextPresentation.ShowTooltipIfTruncated(
                title,
                wasTruncated,
                x,
                titleY,
                titleWidth,
                StockHeaderRowHeight,
                IsMouseHovering);
        }

        private void DrawSectionHeaderAt(string title, int x, int contentY, int width)
        {
            int titleY = contentY - DetailsSectionLayout.ContentGap - DetailsSectionLayout.TitleHeight;
            int dividerY = titleY - DetailsSectionLayout.SeparatorHeight;

            if (width > 0)
            {
                UIRenderer.DrawRect(
                    x,
                    dividerY + DetailsSectionLayout.SeparatorSpacing,
                    width,
                    DetailsSectionLayout.DividerHeight,
                    UIColors.Divider);
            }

            string displayTitle = TruncatedTextPresentation.Truncate(title, width, out bool wasTruncated);

            if (displayTitle.Length == 0)
                return;

            int textY = titleY + Math.Max(0, (DetailsSectionLayout.TitleHeight - TextHeight) / 2);
            UIRenderer.DrawText(displayTitle, x, textY, UIColors.TextTitle);
            TruncatedTextPresentation.ShowTooltipIfTruncated(
                title,
                wasTruncated,
                x,
                titleY,
                width,
                DetailsSectionLayout.TitleHeight,
                IsMouseHovering);
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

            if (displayText.Length == 0)
                return;

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

        private void EnsureSpawnConditionCapacity(int count)
        {
            while (_spawnConditionIcons.Count < count)
            {
                var element = new VanillaBestiaryConditionIcon(_filterCatalog);
                _spawnConditionIcons.Add(element);
                Append(element);
            }
        }

        private void EnsureImmunityCapacity(int count)
        {
            while (_immunityIcons.Count < count)
            {
                var element = new DebuffImmunityIcon();
                _immunityIcons.Add(element);
                Append(element);
            }
        }

        private void EnsureStockButtonCapacity(int count)
        {
            while (_stockButtons.Count < count)
            {
                var button = new VanillaItemRelationButton(NavigateToItem);
                _stockButtons.Add(button);
                Append(button);
            }
        }

        private void HideUnusedStockButtons(int startIndex)
        {
            for (int index = startIndex; index < _stockButtons.Count; index++)
                _stockButtons[index].Hide();
        }

        private void HideStockConditionsButton()
        {
            _stockConditionsButton.IsActive = false;
            _stockConditionsButton.Width.Set(0f, 0f);
            _stockConditionsButton.Height.Set(0f, 0f);
        }

        private void ToggleStockConditionsPopover()
        {
            if (_projection?.MerchantStock.Count <= 0 || _stockConditionsPopover == null)
                return;

            _stockConditionsPopover.Toggle();
            _stockConditionsButton.IsActive = _stockConditionsPopover.IsOpen;
        }

        private void EnsureLootRowCapacity(int count)
        {
            while (_lootRowElements.Count < count)
            {
                var row = new LootRowElement(NavigateToItem, _localization);
                _lootRowElements.Add(row);
                Append(row);
            }
        }

        private void HideUnusedLootRows(int startIndex)
        {
            for (int index = startIndex; index < _lootRowElements.Count; index++)
                _lootRowElements[index].Hide();
        }

        private void HideDynamicChildren()
        {
            _icon.Hide();
            _coinValueElement.Hide();
            _icon.Width.Set(0f, 0f);
            _icon.Height.Set(0f, 0f);
            _classicButton.Width.Set(0f, 0f);
            _classicButton.Height.Set(0f, 0f);
            _expertButton.Width.Set(0f, 0f);
            _expertButton.Height.Set(0f, 0f);
            _masterButton.Width.Set(0f, 0f);
            _masterButton.Height.Set(0f, 0f);

            foreach (VanillaBestiaryConditionIcon icon in _spawnConditionIcons)
                icon.Hide();

            foreach (DebuffImmunityIcon icon in _immunityIcons)
                icon.Hide();

            HideUnusedStockButtons(0);
            HideStockConditionsButton();
            HideUnusedLootRows(0);
        }

        private void NavigateToItem(int itemId)
        {
            if (itemId > 0)
                _navigationState.Navigate(BrowserDestination.ForItem(itemId));
        }

        private sealed class NpcIdentityIcon : UIElement
        {
            private bool _isBound;
            private int _npcNetId;
            private bool _silhouette;

            public void Bind(int npcNetId, bool silhouette)
            {
                bool changed = !_isBound || _npcNetId != npcNetId;
                _isBound = true;
                _npcNetId = npcNetId;
                _silhouette = silhouette;

                if (changed)
                    AsyncNpcIconRenderer.RequestAsync(npcNetId);
            }

            public void Hide()
            {
                _isBound = false;
                _npcNetId = 0;
                _silhouette = false;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (!_isBound)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                AsyncNpcIconRenderer.Draw(
                    spriteBatch,
                    _npcNetId,
                    (int)dimensions.X,
                    (int)dimensions.Y,
                    Math.Max(0, (int)dimensions.Width),
                    Math.Max(0, (int)dimensions.Height),
                    _silhouette);
            }
        }

        private sealed class DebuffImmunityIcon : UIElement
        {
            private NpcBestiaryDebuffImmunity _immunity;

            public void Bind(NpcBestiaryDebuffImmunity immunity)
            {
                _immunity = immunity ?? throw new ArgumentNullException(nameof(immunity));
                IgnoresMouseInteraction = false;
                AsyncBuffIconRenderer.RequestAsync(_immunity.BuffId);
            }

            public void Hide()
            {
                _immunity = null;
                IgnoresMouseInteraction = true;
                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_immunity == null)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);
                AsyncBuffIconRenderer.Draw(spriteBatch, _immunity.BuffId, x, y, width, height);

                if (IsMouseHovering)
                    Tooltip.Set(_immunity.Name);
            }
        }

        private sealed class LootRowElement : UIElement
        {
            private const int ItemIconSize = 24;
            private const int ItemTextGap = 4;
            private const int MainRowHeight = 30;
            private const int ConditionRowHeight = 16;
            private readonly VanillaItemIcon _itemIcon;
            private readonly CompendiumLocalization _localization;
            private readonly Action<int> _navigateToItem;
            private NpcDetailsLootRow _row;

            public LootRowElement(Action<int> navigateToItem, CompendiumLocalization localization)
            {
                _navigateToItem = navigateToItem;
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                _itemIcon = new VanillaItemIcon
                {
                    ShowTooltip = true
                };
                _itemIcon.OnLeftClick += OnItemClicked;
                Append(_itemIcon);
            }

            public int RequiredHeight =>
                _row == null ? 0 : MainRowHeight + _row.ConditionDescriptions.Count * ConditionRowHeight;

            public void Bind(NpcDetailsLootRow row)
            {
                _row = row ?? throw new ArgumentNullException(nameof(row));
                _itemIcon.ItemId = _row.Item.ItemId;
                _itemIcon.IsMissing = _row.Item.IsCollectionTracked && !_row.Item.IsFound;
                _itemIcon.IgnoresMouseInteraction = !_row.Item.IsNavigable;
                _itemIcon.Left.Set(0f, 0f);
                _itemIcon.Top.Set(Math.Max(0, (MainRowHeight - ItemIconSize) / 2), 0f);
                _itemIcon.Width.Set(ItemIconSize, 0f);
                _itemIcon.Height.Set(ItemIconSize, 0f);
            }

            public void Hide()
            {
                _row = null;
                _itemIcon.ItemId = 0;
                _itemIcon.IsMissing = false;
                _itemIcon.IgnoresMouseInteraction = true;
                _itemIcon.Width.Set(0f, 0f);
                _itemIcon.Height.Set(0f, 0f);
                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_row == null)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int textX = x + ItemIconSize + ItemTextGap;
                int textWidth = Math.Max(0, width - ItemIconSize - ItemTextGap);

                DrawRowText(_row.Item.Name, textX, y, textWidth, UIColors.Text);
                DrawRowText(FormatDrop(_row), textX, y + 15, textWidth, UIColors.TextDim);

                int conditionY = y + MainRowHeight;

                foreach (string description in _row.ConditionDescriptions)
                {
                    DrawRowText(description, x, conditionY, width, UIColors.TextDim);
                    conditionY += ConditionRowHeight;
                }
            }

            private void OnItemClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (_row?.Item is not { IsNavigable: true } item || evt.Target != _itemIcon)
                    return;

                _navigateToItem?.Invoke(item.ItemId);
            }

            private string FormatDrop(NpcDetailsLootRow row)
            {
                string stackText = row.StackMin == row.StackMax
                    ? _localization.Format(CompendiumTextKeys.Common.Stack, row.StackMin)
                    : _localization.Format(CompendiumTextKeys.Common.StackRange, row.StackMin, row.StackMax);
                string rateText = _localization.Format(CompendiumTextKeys.Common.Percentage, row.DropRate * 100f);
                return $"{stackText} • {rateText}";
            }

            private void DrawRowText(string text, int x, int y, int width, Color4 color)
            {
                if (width <= 0 || string.IsNullOrEmpty(text))
                    return;

                string display = TruncatedTextPresentation.Truncate(text, width, out bool truncated);

                if (display.Length == 0)
                    return;

                UIRenderer.DrawText(display, x, y, color);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    text,
                    truncated,
                    x,
                    y,
                    width,
                    TextHeight,
                    IsMouseHovering);
            }
        }
    }
}