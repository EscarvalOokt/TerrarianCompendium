using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using CoreItemTooltip = TerrariaModder.Core.UI.Widgets.ItemTooltip;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaItemIcon : UIElement
    {
        private const int ResearchMarkerOffsetX = -2;
        private const int ResearchMarkerOffsetY = -3;
        private const float ResearchMarkerScale = 0.75f;
        private const string ResearchMarker = "•";

        internal static readonly Color4 ResearchAccent = new Color4(210, 140, 255);
        private readonly JourneyResearchState _journeyResearchState;

        private readonly ItemResearchBadgePresentation _researchBadgePresentation;
        private int _itemId;
        private bool _journeyDuplicationPressHandled;
        private bool _journeyRightDuplicationActive;
        private UIElement _journeyRightDuplicationHoverOwner;
        private bool _journeyRightDuplicationSkipRepeatOnce;

        public VanillaItemIcon() : this(null)
        {
        }

        public VanillaItemIcon(JourneyResearchState journeyResearchState)
        {
            _journeyResearchState = journeyResearchState;
            IgnoresMouseInteraction = false;
            OnLeftMouseDown += OnItemLeftMouseDown;
            OnRightMouseDown += OnItemRightMouseDown;

            if (journeyResearchState != null)
                _researchBadgePresentation = new ItemResearchBadgePresentation(journeyResearchState);
        }

        public int ItemId
        {
            get => _itemId;
            set
            {
                int itemId = Math.Max(0, value);

                if (_itemId == itemId)
                    return;

                _itemId = itemId;
                _journeyDuplicationPressHandled = false;
                _journeyRightDuplicationActive = false;
                _journeyRightDuplicationHoverOwner = null;
                _journeyRightDuplicationSkipRepeatOnce = false;

                if (_researchBadgePresentation == null)
                    return;

                _researchBadgePresentation.Bind(_itemId);
                _researchBadgePresentation.Synchronize();
            }
        }

        public bool IsMissing { get; set; }

        public bool ShowResearchBadge { get; set; } = true;

        public bool ShowTooltip { get; set; } = true;

        private bool IsResearchMarkerVisible => ShowResearchBadge && _researchBadgePresentation?.IsVisible == true;

        internal bool TryHandleJourneyDuplicationFromOwnerMouseDown(
            UIMouseEvent evt,
            UIElement hoverOwner,
            bool rightClick)
        {
            if (evt.Target == this)
                return _journeyDuplicationPressHandled;

            return BeginJourneyDuplicationPress(rightClick, hoverOwner);
        }

        internal bool ConsumeJourneyDuplicationClick()
        {
            bool handled = _journeyDuplicationPressHandled;
            _journeyDuplicationPressHandled = false;
            return handled;
        }

        public override void Update(GameTime gameTime)
        {
            _researchBadgePresentation?.Synchronize();
            base.Update(gameTime);
            UpdateJourneyRightDuplication();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_itemId <= 0)
                return;

            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            if (width <= 0 || height <= 0)
                return;

            AsyncItemIconRenderer.Draw(_itemId, x, y, width, height, IsMissing);
            DrawResearchMarker(x, y);

            if (ShowTooltip && IsMouseHovering)
            {
                CoreItemTooltip.Set(_itemId);
                RegisterResearchSupplementalTooltip();
            }
        }

        private void OnItemLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
        {
            _journeyDuplicationPressHandled = false;

            if (evt.Target == this)
                BeginJourneyDuplicationPress(rightClick: false, this);
        }

        private void OnItemRightMouseDown(UIMouseEvent evt, UIElement listeningElement)
        {
            _journeyDuplicationPressHandled = false;

            if (evt.Target == this)
                BeginJourneyDuplicationPress(rightClick: true, this);
        }

        private bool BeginJourneyDuplicationPress(bool rightClick, UIElement hoverOwner)
        {
            _journeyDuplicationPressHandled = false;
            _journeyRightDuplicationActive = false;
            _journeyRightDuplicationHoverOwner = null;
            _journeyRightDuplicationSkipRepeatOnce = false;

            if (_itemId <= 0 ||
                !WidgetInput.IsAltHeld ||
                WidgetInput.IsCtrlHeld ||
                WidgetInput.IsShiftHeld ||
                (rightClick ? WidgetInput.MouseLeft : WidgetInput.MouseRight))
            {
                return false;
            }

            _journeyDuplicationPressHandled =
                VanillaJourneyItemDuplication.TryDuplicate(_itemId, _journeyResearchState);

            if (_journeyDuplicationPressHandled && rightClick)
            {
                _journeyRightDuplicationActive = true;
                _journeyRightDuplicationHoverOwner = hoverOwner ?? this;
                _journeyRightDuplicationSkipRepeatOnce = true;
            }

            return _journeyDuplicationPressHandled;
        }

        private void UpdateJourneyRightDuplication()
        {
            if (!_journeyRightDuplicationActive)
                return;

            if (!WidgetInput.MouseRight ||
                !WidgetInput.IsAltHeld ||
                WidgetInput.IsCtrlHeld ||
                WidgetInput.IsShiftHeld ||
                WidgetInput.MouseLeft ||
                _journeyRightDuplicationHoverOwner?.IsMouseHovering != true)
            {
                _journeyRightDuplicationActive = false;
                _journeyRightDuplicationHoverOwner = null;
                _journeyRightDuplicationSkipRepeatOnce = false;
                return;
            }

            if (_journeyRightDuplicationSkipRepeatOnce)
            {
                _journeyRightDuplicationSkipRepeatOnce = false;
                return;
            }

            if (!VanillaJourneyItemDuplication.TryDuplicate(_itemId, _journeyResearchState))
            {
                _journeyRightDuplicationActive = false;
                _journeyRightDuplicationHoverOwner = null;
                _journeyRightDuplicationSkipRepeatOnce = false;
            }
        }

        internal void RegisterResearchSupplementalTooltip()
        {
            SupplementalTooltip.RegisterPrimaryItemTooltip(_itemId);

            if (!IsResearchMarkerVisible)
                return;

            SupplementalTooltip.RegisterLocalized(
                CompendiumTextKeys.ItemDetails.Researched,
                ResearchAccent,
                ResearchMarker + " ");
            SupplementalTooltip.RegisterLocalized(
                CompendiumTextKeys.ItemDetails.ResearchDuplicateStackHint,
                UIColors.TextDim);
            SupplementalTooltip.RegisterLocalized(
                CompendiumTextKeys.ItemDetails.ResearchDuplicateSingleHint,
                UIColors.TextDim);
        }

        private void DrawResearchMarker(int x, int y)
        {
            if (!IsResearchMarkerVisible)
                return;

            UIRenderer.DrawTextScaled(
                ResearchMarker,
                x + ResearchMarkerOffsetX,
                y + ResearchMarkerOffsetY,
                ResearchAccent.R,
                ResearchAccent.G,
                ResearchAccent.B,
                ResearchAccent.A,
                ResearchMarkerScale);
        }
    }
}