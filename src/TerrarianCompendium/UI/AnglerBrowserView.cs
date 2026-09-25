using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Angler;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class AnglerBrowserView : UIElement
    {
        private const int ContentPadding = 6;
        private const int ColumnGap = 10;
        private const int LeftColumnNumerator = 9;
        private const int ColumnDenominator = 20;
        private const int QuestIconSize = 44;
        private const int IconTextGap = 6;
        private const int TextRowHeight = 18;
        private const int SectionTitleHeight = 18;
        private const int SectionGap = 8;
        private const int MilestoneCardHeight = 40;
        private const int MilestoneCardGap = 3;
        private const int RewardSectionGap = 8;

        private readonly ChecklistState _checklistState;
        private readonly VanillaItemRelationButton _currentQuestButton;
        private readonly TextLabelElement _currentQuestName;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly CompendiumLocalization _localization;

        private readonly List<MilestoneCardElement> _milestoneCards = new();
        private readonly TextLabelElement _milestonesTitle;
        private readonly AnglerQuestModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly TextLabelElement _progressText;
        private readonly TextLabelElement _questTitle;
        private readonly List<RewardGroupElement> _rewardGroups = new();
        private readonly AnglerRewardInfoPopoverContent _rewardInfoContent;
        private readonly VanillaPopover _rewardInfoPopover;
        private readonly TextLabelElement _rewardsTitle;
        private readonly VanillaScrollRegion _scroll;
        private readonly TextLabelElement _statusText;
        private readonly TextLabelElement _unavailableText;

        private bool _hasProjection;
        private long _observedChecklistRevision = -1;
        private long _observedItemTextRevision = -1;
        private long _observedLocalizationRevision = -1;
        private long _observedModelRevision = -1;
        private AnglerRewardGroupKind? _openInfoGroup;
        private AnglerRewardPagePresentation _presentation;

        public AnglerBrowserView(
            AnglerQuestModel model,
            ItemTextIndex itemTextIndex,
            ChecklistState checklistState,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization,
            JourneyResearchState journeyResearchState = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _journeyResearchState = journeyResearchState;
            SetPadding(0f);

            _scroll = new VanillaScrollRegion();
            Append(_scroll);

            _questTitle = CreateLabel(UIColors.TextTitle);
            _currentQuestName = CreateLabel(UIColors.TextTitle);
            _statusText = CreateLabel(UIColors.TextDim);
            _progressText = CreateLabel(UIColors.Text);
            _milestonesTitle = CreateLabel(UIColors.TextTitle);
            _rewardsTitle = CreateLabel(UIColors.TextTitle);
            _unavailableText = CreateLabel(UIColors.TextDim);

            _currentQuestButton = new VanillaItemRelationButton(NavigateToItem, _journeyResearchState);
            _rewardInfoContent = new AnglerRewardInfoPopoverContent();
            _rewardInfoPopover = new VanillaPopover(this, _questTitle, _rewardInfoContent);

            Refresh(force: true);
        }

        public bool IsWritingText => false;

        public bool HasOpenTransientSurface => _rewardInfoPopover.IsOpen;

        public bool TryCloseTransientSurface()
        {
            if (!_rewardInfoPopover.IsOpen)
                return false;

            _rewardInfoPopover.Close();
            _openInfoGroup = null;
            return true;
        }

        public override void Update(GameTime gameTime)
        {
            Refresh();
            base.Update(gameTime);

            if (_rewardInfoPopover.IsOpen)
                _rewardInfoPopover.Recalculate();
        }

        public override void OnDeactivate()
        {
            TryCloseTransientSurface();
            base.OnDeactivate();
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);
            _scroll.Left.Set(0f, 0f);
            _scroll.Top.Set(0f, 0f);
            _scroll.Width.Set(width, 0f);
            _scroll.Height.Set(height, 0f);

            int contentWidth = VanillaScrollRegion.CalculateContentWidth(width);
            _scroll.SetContentHeight(LayoutContent(contentWidth));
            base.RecalculateChildren();
        }

        private void Refresh(bool force = false)
        {
            long modelRevision = _model.Revision;
            long itemTextRevision = _itemTextIndex.Revision;
            long checklistRevision = _checklistState.Revision;
            long localizationRevision = _localization.Revision;

            if (!force &&
                _observedModelRevision == modelRevision &&
                _observedItemTextRevision == itemTextRevision &&
                _observedChecklistRevision == checklistRevision &&
                _observedLocalizationRevision == localizationRevision)
            {
                return;
            }

            bool modelChanged = _observedModelRevision != modelRevision;
            _observedModelRevision = modelRevision;
            _observedItemTextRevision = itemTextRevision;
            _observedChecklistRevision = checklistRevision;
            _observedLocalizationRevision = localizationRevision;

            TryCloseTransientSurface();
            _scroll.Content.RemoveAllChildren();
            _milestoneCards.Clear();
            _rewardGroups.Clear();
            _presentation = null;

            _questTitle.Text = _localization.Get(CompendiumTextKeys.Angler.QuestTitle);
            _milestonesTitle.Text = _localization.Get(CompendiumTextKeys.Angler.Milestones);
            _rewardsTitle.Text = _localization.Get(CompendiumTextKeys.Angler.RewardsTitle);

            _scroll.Content.Append(_questTitle);

            if (!_model.TryGetProjection(out AnglerQuestProjection projection))
            {
                _hasProjection = false;
                _currentQuestButton.Hide();
                _currentQuestName.Text = string.Empty;
                _statusText.Text = string.Empty;
                _progressText.Text = string.Empty;
                _unavailableText.Text = _localization.Get(CompendiumTextKeys.Angler.Unavailable);
                _scroll.Content.Append(_unavailableText);
            }
            else
            {
                _hasProjection = true;
                int questItemId = projection.QuestItem.Id;
                _currentQuestButton.Bind(questItemId, !_checklistState.IsFound(questItemId));
                _currentQuestName.Text = _itemTextIndex.GetName(questItemId);
                _statusText.Text = _localization.Get(
                    projection.FinishedToday
                        ? CompendiumTextKeys.Angler.QuestCompletedToday
                        : CompendiumTextKeys.Angler.QuestNotCompletedToday);
                _progressText.Text = _localization.Format(
                    CompendiumTextKeys.Angler.CompletedQuests,
                    projection.QuestsFinished);
                _unavailableText.Text = string.Empty;
                _presentation = AnglerRewardPresentation.Build(projection, _model.RewardCatalog, _localization);

                _scroll.Content.Append(_currentQuestButton);
                _scroll.Content.Append(_currentQuestName);
                _scroll.Content.Append(_statusText);
                _scroll.Content.Append(_progressText);
                _scroll.Content.Append(_milestonesTitle);
                _scroll.Content.Append(_rewardsTitle);

                var milestoneQuestLabelWidth = 0;

                foreach (AnglerMilestonePresentation milestone in _presentation.Milestones)
                {
                    string questText = _localization.Format(
                        CompendiumTextKeys.Angler.MilestoneQuest,
                        milestone.CompletedQuests);
                    milestoneQuestLabelWidth = Math.Max(milestoneQuestLabelWidth, UIRenderer.MeasureText(questText));
                }

                foreach (AnglerMilestonePresentation milestone in _presentation.Milestones)
                {
                    string questText = _localization.Format(
                        CompendiumTextKeys.Angler.MilestoneQuest,
                        milestone.CompletedQuests);
                    var card = new MilestoneCardElement(
                        milestone,
                        _itemTextIndex.GetName(milestone.ItemId),
                        questText,
                        milestoneQuestLabelWidth,
                        _checklistState,
                        NavigateToItem,
                        _journeyResearchState);
                    _milestoneCards.Add(card);
                    _scroll.Content.Append(card);
                }

                foreach (AnglerRewardGroupPresentation group in _presentation.RewardGroups)
                {
                    var element = new RewardGroupElement(
                        group,
                        _checklistState,
                        NavigateToItem,
                        ToggleRewardInfo,
                        _localization.Get(CompendiumTextKeys.Angler.RewardInfoTooltip),
                        _journeyResearchState);
                    _rewardGroups.Add(element);
                    _scroll.Content.Append(element);
                }
            }

            if (modelChanged)
                _scroll.ResetScroll();

            Recalculate();
        }

        private int LayoutContent(int contentWidth)
        {
            int availableWidth = Math.Max(0, contentWidth - ContentPadding * 2);

            if (!_hasProjection)
            {
                LayoutElement(_questTitle, ContentPadding, ContentPadding, availableWidth, SectionTitleHeight);
                LayoutElement(
                    _unavailableText,
                    ContentPadding,
                    ContentPadding + SectionTitleHeight + SectionGap,
                    availableWidth,
                    TextRowHeight);
                return ContentPadding * 2 + SectionTitleHeight + SectionGap + TextRowHeight;
            }

            int resolvedColumnGap = Math.Min(ColumnGap, availableWidth);
            int columnsWidth = Math.Max(0, availableWidth - resolvedColumnGap);
            int leftWidth = columnsWidth * LeftColumnNumerator / ColumnDenominator;
            int rightWidth = Math.Max(0, columnsWidth - leftWidth);
            int leftX = ContentPadding;
            int rightX = leftX + leftWidth + resolvedColumnGap;

            int leftHeight = LayoutProgressColumn(leftX, ContentPadding, leftWidth);
            int rightHeight = LayoutRewardsColumn(rightX, ContentPadding, rightWidth);
            return Math.Max(leftHeight, rightHeight) + ContentPadding;
        }

        private int LayoutProgressColumn(int x, int y, int width)
        {
            int cursor = y;
            LayoutElement(_questTitle, x, cursor, width, SectionTitleHeight);
            cursor += SectionTitleHeight + SectionGap;

            LayoutElement(_currentQuestButton, x, cursor, QuestIconSize, QuestIconSize);
            int textX = x + QuestIconSize + IconTextGap;
            int textWidth = Math.Max(0, width - QuestIconSize - IconTextGap);
            LayoutElement(_currentQuestName, textX, cursor + 4, textWidth, TextRowHeight);
            LayoutElement(_statusText, textX, cursor + 4 + TextRowHeight, textWidth, TextRowHeight);
            cursor += QuestIconSize + SectionGap;

            LayoutElement(_progressText, x, cursor, width, TextRowHeight);
            cursor += TextRowHeight + SectionGap;

            LayoutElement(_milestonesTitle, x, cursor, width, SectionTitleHeight);
            cursor += SectionTitleHeight + 4;

            for (var index = 0; index < _milestoneCards.Count; index++)
            {
                MilestoneCardElement card = _milestoneCards[index];
                LayoutElement(card, x, cursor, width, MilestoneCardHeight);
                cursor += MilestoneCardHeight;

                if (index < _milestoneCards.Count - 1)
                    cursor += MilestoneCardGap;
            }

            return cursor;
        }

        private int LayoutRewardsColumn(int x, int y, int width)
        {
            int cursor = y;
            LayoutElement(_rewardsTitle, x, cursor, width, SectionTitleHeight);
            cursor += SectionTitleHeight + SectionGap;

            for (var index = 0; index < _rewardGroups.Count; index++)
            {
                RewardGroupElement group = _rewardGroups[index];
                int height = group.CalculateHeight(width);
                LayoutElement(group, x, cursor, width, height);
                cursor += height;

                if (index < _rewardGroups.Count - 1)
                    cursor += RewardSectionGap;
            }

            return cursor;
        }

        private void ToggleRewardInfo(AnglerRewardGroupPresentation group, UIElement anchor)
        {
            if (group == null || anchor == null)
                return;

            if (_rewardInfoPopover.IsOpen && _openInfoGroup == group.Kind)
            {
                TryCloseTransientSurface();
                return;
            }

            _rewardInfoContent.Bind(group.InfoText);
            _rewardInfoPopover.SetAnchor(anchor);
            _openInfoGroup = group.Kind;

            if (_rewardInfoPopover.IsOpen)
                _rewardInfoPopover.Recalculate();
            else
                _rewardInfoPopover.Open();
        }

        private void NavigateToItem(int itemId)
        {
            if (itemId <= 0)
                return;

            TryCloseTransientSurface();
            _navigationState.Navigate(BrowserDestination.ForItem(itemId));
        }

        private static TextLabelElement CreateLabel(Color4 color)
        {
            return new TextLabelElement(string.Empty, color)
            {
                IgnoresMouseInteraction = true
            };
        }

        private static void LayoutElement(UIElement element, int x, int y, int width, int height)
        {
            element.Left.Set(x, 0f);
            element.Top.Set(y, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }

        private sealed class MilestoneCardElement : UIElement
        {
            private const int CardPadding = 3;
            private const int MarkerWidth = 16;
            private const int QuestItemGap = 8;
            private const int ItemSize = 32;
            private const int ItemTextGap = 6;

            private readonly VanillaItemRelationButton _itemButton;
            private readonly string _itemName;
            private readonly int _questLabelWidth;
            private readonly string _questText;
            private readonly AnglerMilestonePresentationState _state;

            public MilestoneCardElement(
                AnglerMilestonePresentation presentation,
                string itemName,
                string questText,
                int questLabelWidth,
                ChecklistState checklistState,
                Action<int> itemClicked,
                JourneyResearchState journeyResearchState)
            {
                if (presentation == null)
                    throw new ArgumentNullException(nameof(presentation));
                if (checklistState == null)
                    throw new ArgumentNullException(nameof(checklistState));

                SetPadding(0f);
                _state = presentation.State;
                _itemName = itemName ?? string.Empty;
                _questText = questText ?? string.Empty;
                _questLabelWidth = Math.Max(0, questLabelWidth);
                _itemButton = new VanillaItemRelationButton(itemClicked, journeyResearchState);
                _itemButton.Bind(presentation.ItemId, !checklistState.IsFound(presentation.ItemId));
                Append(_itemButton);
            }

            public override void RecalculateChildren()
            {
                CalculatedStyle inner = GetInnerDimensions();
                int width = Math.Max(0, (int)inner.Width);
                int height = Math.Max(0, (int)inner.Height);
                int itemY = Math.Max(0, (height - ItemSize) / 2);
                int itemX = CardPadding + MarkerWidth + _questLabelWidth + QuestItemGap;
                _itemButton.Left.Set(itemX, 0f);
                _itemButton.Top.Set(itemY, 0f);
                _itemButton.Width.Set(Math.Min(ItemSize, Math.Max(0, width - itemX - CardPadding)), 0f);
                _itemButton.Height.Set(Math.Min(ItemSize, Math.Max(0, height - itemY)), 0f);
                base.RecalculateChildren();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);

                if (width <= 0 || height <= 0)
                    return;

                Color4 background = _state == AnglerMilestonePresentationState.Completed
                    ? UIColors.SectionBg
                    : UIColors.ItemBg;
                Color4 border = _state == AnglerMilestonePresentationState.Next ? UIColors.Accent : UIColors.Border;
                Color4 textColor = _state == AnglerMilestonePresentationState.Completed
                    ? UIColors.TextDim
                    : UIColors.Text;
                string marker = _state switch
                {
                    AnglerMilestonePresentationState.Completed => "✓",
                    AnglerMilestonePresentationState.Next => "→",
                    _ => string.Empty
                };

                UIRenderer.DrawRect(x, y, width, height, background);
                UIRenderer.DrawRectOutline(
                    x,
                    y,
                    width,
                    height,
                    border,
                    _state == AnglerMilestonePresentationState.Next ? 2 : 1);

                int textY = y + Math.Max(0, (height - TextRowHeight) / 2);

                if (marker.Length > 0)
                {
                    UIRenderer.DrawText(
                        marker,
                        x + CardPadding + Math.Max(0, (MarkerWidth - UIRenderer.MeasureText(marker)) / 2),
                        textY,
                        _state == AnglerMilestonePresentationState.Completed ? UIColors.Success : UIColors.Accent);
                }

                UIRenderer.DrawText(_questText, x + CardPadding + MarkerWidth, textY, textColor);

                int nameX = x + CardPadding + MarkerWidth + _questLabelWidth + QuestItemGap + ItemSize + ItemTextGap;
                int nameWidth = Math.Max(0, x + width - CardPadding - nameX);
                string displayName = TruncatedTextPresentation.Truncate(_itemName, nameWidth, out _);

                if (displayName.Length > 0)
                    UIRenderer.DrawText(displayName, nameX, textY, textColor);
            }
        }

        private sealed class RewardGroupElement : UIElement
        {
            private const int HeaderHeight = 22;
            private const int HeaderGap = 3;
            private const int InfoButtonSize = 20;
            private const int ItemSize = 36;
            private const int ItemGap = 3;
            private readonly VanillaTextButton _infoButton;
            private readonly List<VanillaItemRelationButton> _itemButtons = new();

            private readonly TextLabelElement _title;

            public RewardGroupElement(
                AnglerRewardGroupPresentation presentation,
                ChecklistState checklistState,
                Action<int> itemClicked,
                Action<AnglerRewardGroupPresentation, UIElement> infoRequested,
                string infoTooltip,
                JourneyResearchState journeyResearchState)
            {
                AnglerRewardGroupPresentation presentation1 =
                    presentation ?? throw new ArgumentNullException(nameof(presentation));
                SetPadding(0f);
                if (checklistState == null)
                    throw new ArgumentNullException(nameof(checklistState));
                if (infoRequested == null)
                    throw new ArgumentNullException(nameof(infoRequested));

                _title = new TextLabelElement(presentation1.Title, UIColors.TextTitle)
                {
                    IgnoresMouseInteraction = true
                };
                _infoButton = new VanillaTextButton("?", () => infoRequested(presentation1, _infoButton))
                {
                    TooltipText = infoTooltip
                };
                Append(_title);
                Append(_infoButton);

                foreach (int itemId in presentation1.ItemIds)
                {
                    var button = new VanillaItemRelationButton(itemClicked, journeyResearchState);
                    button.Bind(itemId, !checklistState.IsFound(itemId));
                    _itemButtons.Add(button);
                    Append(button);
                }
            }

            public int CalculateHeight(int width)
            {
                return HeaderHeight + HeaderGap + CalculateGridHeight(width);
            }

            public override void RecalculateChildren()
            {
                CalculatedStyle inner = GetInnerDimensions();
                int width = Math.Max(0, (int)inner.Width);
                int titleWidth = Math.Max(0, width - InfoButtonSize - ItemGap);
                _title.Left.Set(0f, 0f);
                _title.Top.Set(0f, 0f);
                _title.Width.Set(titleWidth, 0f);
                _title.Height.Set(HeaderHeight, 0f);
                _infoButton.Left.Set(Math.Max(0, width - InfoButtonSize), 0f);
                _infoButton.Top.Set(0f, 0f);
                _infoButton.Width.Set(InfoButtonSize, 0f);
                _infoButton.Height.Set(InfoButtonSize, 0f);

                int columns = CalculateColumns(width);
                int top = HeaderHeight + HeaderGap;

                for (var index = 0; index < _itemButtons.Count; index++)
                {
                    int row = index / columns;
                    int column = index % columns;
                    VanillaItemRelationButton button = _itemButtons[index];
                    button.Left.Set(column * (ItemSize + ItemGap), 0f);
                    button.Top.Set(top + row * (ItemSize + ItemGap), 0f);
                    button.Width.Set(ItemSize, 0f);
                    button.Height.Set(ItemSize, 0f);
                }

                base.RecalculateChildren();
            }

            private int CalculateColumns(int width)
            {
                if (_itemButtons.Count == 0 || width <= 0)
                    return 1;

                int fit = Math.Max(1, (width + ItemGap) / (ItemSize + ItemGap));
                return Math.Min(_itemButtons.Count, fit);
            }

            private int CalculateGridHeight(int width)
            {
                if (_itemButtons.Count == 0)
                    return 0;

                int columns = CalculateColumns(width);
                int rows = (_itemButtons.Count + columns - 1) / columns;
                return rows * ItemSize + Math.Max(0, rows - 1) * ItemGap;
            }
        }
    }
}