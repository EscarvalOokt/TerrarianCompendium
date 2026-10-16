using System;
using Escarval.TerrariaWindowKit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaBrowserPanel : UIElement
    {
        private const int HeaderHeight = 32;
        private const int ResizeHandleHitSize = CoreResizeGripRenderer.DefaultGripSize;
        private const int CloseButtonSize = 24;
        private const int CloseButtonInset = 6;
        private const int HeaderButtonGap = 4;
        private const int HeaderTitleInset = 10;
        private readonly PinButtonElement _pinButton;
        private readonly ResizeHandleElement _resizeHandle;
        private readonly string _title;
        private bool _externalInputBlocked;
        private int _pinButtonWidth;

        public VanillaBrowserPanel(
            string title,
            int defaultWidth,
            int defaultHeight,
            int minimumWidth,
            int minimumHeight)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Panel title must not be empty.", nameof(title));

            if (defaultWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(defaultWidth));

            if (defaultHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(defaultHeight));

            if (minimumWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumWidth));

            if (minimumHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumHeight));

            _title = title;

            CalculatedStyle viewport = UserInterface.ActiveInstance.GetDimensions();
            int viewportWidth = Math.Max(1, (int)viewport.Width);
            int viewportHeight = Math.Max(1, (int)viewport.Height);
            WindowBounds fittedSize = new WindowBounds(0, 0, defaultWidth, defaultHeight).Fit(
                viewportWidth,
                viewportHeight,
                minimumWidth,
                minimumHeight);
            int width = fittedSize.Width;
            int height = fittedSize.Height;

            int x = Math.Max(0, (viewportWidth - width) / 2);
            int y = Math.Max(0, (viewportHeight - height) / 2);
            InitialBounds = new Rectangle(x, y, width, height);
            Left.Set(x, 0f);
            Top.Set(y, 0f);
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            OverflowHidden = true;
            SetPadding(0f);

            HeaderRoot = new UIElement
            {
                Width = StyleDimension.Fill,
                Height = new StyleDimension(HeaderHeight, 0f)
            };
            HeaderRoot.SetPadding(0f);
            HeaderRoot.OnLeftMouseDown += OnHeaderLeftMouseDown;
            Append(HeaderRoot);

            CloseButton = new VanillaTextButton("×", () => CloseRequested?.Invoke())
            {
                Left = new StyleDimension(-(CloseButtonSize + CloseButtonInset), 1f),
                Top = new StyleDimension((HeaderHeight - CloseButtonSize) / 2f, 0f),
                Width = new StyleDimension(CloseButtonSize, 0f),
                Height = new StyleDimension(CloseButtonSize, 0f)
            };
            HeaderRoot.Append(CloseButton);

            _pinButton = new PinButtonElement(() => IsPinned?.Invoke() == true, () => _externalInputBlocked)
            {
                Top = new StyleDimension((HeaderHeight - CloseButtonSize) / 2f, 0f),
                Width = new StyleDimension(0f, 0f),
                Height = new StyleDimension(CloseButtonSize, 0f)
            };
            _pinButton.OnLeftClick += OnPinButtonLeftClick;
            HeaderRoot.Append(_pinButton);

            ContentRoot = new UIElement
            {
                Top = new StyleDimension(HeaderHeight, 0f),
                Width = StyleDimension.Fill,
                Height = new StyleDimension(-HeaderHeight, 1f),
                OverflowHidden = true
            };
            ContentRoot.SetPadding(0f);
            Append(ContentRoot);

            _resizeHandle = new ResizeHandleElement(() => IsResizing?.Invoke() == true, () => _externalInputBlocked)
            {
                Left = new StyleDimension(-ResizeHandleHitSize, 1f),
                Top = new StyleDimension(-ResizeHandleHitSize, 1f),
                Width = new StyleDimension(ResizeHandleHitSize, 0f),
                Height = new StyleDimension(ResizeHandleHitSize, 0f)
            };
            _resizeHandle.OnLeftMouseDown += OnResizeHandleLeftMouseDown;
            Append(_resizeHandle);
        }

        public VanillaTextButton CloseButton { get; }

        public UIElement ContentRoot { get; }

        public UIElement HeaderRoot { get; }

        public Rectangle Bounds => GetDimensions().ToRectangle();

        public Rectangle InitialBounds { get; }

        public Func<bool> IsResizing { get; set; }

        public Func<bool> IsPinned { get; set; }

        public event Action CloseRequested;

        public event Action PinToggleRequested;

        public event Action InteractionStarted;

        public event Action<int, int> DragRequested;

        public event Action<int, int> ResizeRequested;

        public void SetExternalInputBlocked(bool blocked)
        {
            _externalInputBlocked = blocked;
        }

        public void SetPinButtonLocalization(string pinText, string unpinText, string pinTooltip, string unpinTooltip)
        {
            _pinButton.PinText = pinText ?? string.Empty;
            _pinButton.UnpinText = unpinText ?? string.Empty;
            _pinButton.PinTooltip = pinTooltip;
            _pinButton.UnpinTooltip = unpinTooltip;

            var panelWidth = (int)GetDimensions().Width;
            UpdatePinButtonLayout(panelWidth > 0 ? panelWidth : InitialBounds.Width);
            Recalculate();
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            InteractionStarted?.Invoke();
            base.LeftMouseDown(evt);
        }

        public override void RightMouseDown(UIMouseEvent evt)
        {
            InteractionStarted?.Invoke();
            base.RightMouseDown(evt);
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

            UIRenderer.DrawPanel(x, y, width, height, UIColors.PanelBg);
            UIRenderer.DrawRect(x, y, width, HeaderHeight, UIColors.HeaderBg);
            int titleWidth = Math.Max(
                0,
                width -
                HeaderTitleInset -
                CloseButtonInset -
                CloseButtonSize -
                HeaderButtonGap -
                _pinButtonWidth -
                HeaderButtonGap);
            if (titleWidth > 0)
            {
                string title = TextUtil.Truncate(_title, titleWidth);
                if (title.Length > 0)
                    UIRenderer.DrawTextShadow(title, x + HeaderTitleInset, y + 8, UIColors.TextTitle);
            }
        }

        private void OnPinButtonLeftClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (_externalInputBlocked || WidgetInput.BlockInput || evt.Target != _pinButton)
                return;

            PinToggleRequested?.Invoke();
        }

        private void OnHeaderLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
        {
            if (_externalInputBlocked || evt.Target != HeaderRoot)
                return;

            DragRequested?.Invoke((int)Math.Round(evt.MousePosition.X), (int)Math.Round(evt.MousePosition.Y));
        }

        private void OnResizeHandleLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
        {
            if (_externalInputBlocked || evt.Target != _resizeHandle)
                return;

            ResizeRequested?.Invoke((int)Math.Round(evt.MousePosition.X), (int)Math.Round(evt.MousePosition.Y));
        }

        public void ApplyBounds(Rectangle bounds)
        {
            Left.Set(bounds.X, 0f);
            Top.Set(bounds.Y, 0f);
            Width.Set(bounds.Width, 0f);
            Height.Set(bounds.Height, 0f);
            UpdatePinButtonLayout(bounds.Width);
            Recalculate();
        }

        private void UpdatePinButtonLayout(int panelWidth)
        {
            int availableWidth = Math.Max(
                0,
                panelWidth - HeaderTitleInset - CloseButtonInset - CloseButtonSize - HeaderButtonGap);
            _pinButtonWidth = Math.Min(
                CorePinButtonRenderer.MeasureWidth(_pinButton.PinText, _pinButton.UnpinText),
                availableWidth);
            _pinButton.Left.Set(-(CloseButtonInset + CloseButtonSize + HeaderButtonGap + _pinButtonWidth), 1f);
            _pinButton.Width.Set(_pinButtonWidth, 0f);
        }

        private sealed class PinButtonElement(Func<bool> isPinned, Func<bool> isInputBlocked) : UIElement
        {
            private readonly Func<bool> _isInputBlocked =
                isInputBlocked ?? throw new ArgumentNullException(nameof(isInputBlocked));

            private readonly Func<bool> _isPinned = isPinned ?? throw new ArgumentNullException(nameof(isPinned));

            public string PinText { get; set; } = string.Empty;

            public string UnpinText { get; set; } = string.Empty;

            public string PinTooltip { get; set; }

            public string UnpinTooltip { get; set; }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);
                if (width <= 0 || height <= 0)
                    return;

                bool isPinned = _isPinned();
                bool hovered = IsMouseHovering && !_isInputBlocked() && !WidgetInput.BlockInput;
                CorePinButtonRenderer.Draw(
                    (int)dimensions.X,
                    (int)dimensions.Y,
                    width,
                    height,
                    isPinned,
                    hovered,
                    PinText,
                    UnpinText);

                if (hovered)
                {
                    string tooltip = isPinned ? UnpinTooltip : PinTooltip;
                    if (!string.IsNullOrEmpty(tooltip))
                        Tooltip.Set(tooltip);
                }
            }
        }

        private sealed class ResizeHandleElement(Func<bool> isResizing, Func<bool> isInputBlocked) : UIElement
        {
            private readonly Func<bool> _isInputBlocked =
                isInputBlocked ?? throw new ArgumentNullException(nameof(isInputBlocked));

            private readonly Func<bool> _isResizing = isResizing ?? throw new ArgumentNullException(nameof(isResizing));

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                bool hovered = IsMouseHovering && !_isInputBlocked() && !WidgetInput.BlockInput;
                CoreResizeGripRenderer.Draw(
                    (int)dimensions.X,
                    (int)dimensions.Y,
                    (int)dimensions.Width,
                    (int)dimensions.Height,
                    hovered,
                    _isResizing());
            }
        }
    }
}