using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using TerrariaModder.Core.UI;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaBrowserPanel : UIElement
    {
        private const int HeaderHeight = 32;
        private const int ResizeHandleHitSize = 18;
        private const int ResizeHandleVisualSize = 8;
        private const int ResizeHandleInset = 3;
        private const int CloseButtonSize = 24;
        private const int CloseButtonInset = 6;
        private readonly int _minimumHeight;
        private readonly int _minimumWidth;
        private readonly DraggablePanelResizeBehavior _resizeBehavior;

        private readonly ResizeHandleElement _resizeHandle;
        private readonly string _title;
        private Vector2 _dragPointerStart;
        private float _dragPositionStartX;
        private float _dragPositionStartY;
        private bool _externalInputBlocked;
        private bool _isDragging;

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
            _minimumWidth = minimumWidth;
            _minimumHeight = minimumHeight;
            _resizeBehavior = new DraggablePanelResizeBehavior(minimumWidth, minimumHeight);

            CalculatedStyle viewport = UserInterface.ActiveInstance.GetDimensions();
            int viewportWidth = Math.Max(1, (int)viewport.Width);
            int viewportHeight = Math.Max(1, (int)viewport.Height);
            int width = DraggablePanelResizeBehavior.ClampDimension(defaultWidth, minimumWidth, viewportWidth);
            int height = DraggablePanelResizeBehavior.ClampDimension(defaultHeight, minimumHeight, viewportHeight);

            Left.Set(Math.Max(0, (viewportWidth - width) / 2), 0f);
            Top.Set(Math.Max(0, (viewportHeight - height) / 2), 0f);
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

            ContentRoot = new UIElement
            {
                Top = new StyleDimension(HeaderHeight, 0f),
                Width = StyleDimension.Fill,
                Height = new StyleDimension(-HeaderHeight, 1f),
                OverflowHidden = true
            };
            ContentRoot.SetPadding(0f);
            Append(ContentRoot);

            _resizeHandle = new ResizeHandleElement(() => _resizeBehavior.IsResizing)
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

        public event Action CloseRequested;

        public event Action InteractionStarted;

        public void SetExternalInputBlocked(bool blocked)
        {
            _externalInputBlocked = blocked;

            if (blocked)
                CancelPointerOperation();
        }

        public override void Update(GameTime gameTime)
        {
            EnsureWithinViewport();

            if (_externalInputBlocked)
            {
                CancelPointerOperation();
                base.Update(gameTime);
                return;
            }

            if (_isDragging && !Main.mouseLeft)
                _isDragging = false;

            if (_resizeBehavior.IsResizing && !Main.mouseLeft)
                _resizeBehavior.Cancel();

            if (_isDragging)
                UpdateDragging();
            else if (_resizeBehavior.IsResizing)
                UpdateResizing();

            base.Update(gameTime);
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

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            CancelPointerOperation();
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
            UIRenderer.DrawTextShadow(_title, x + 10, y + 8, UIColors.TextTitle);
        }

        private void EnsureWithinViewport()
        {
            CalculatedStyle viewport = UserInterface.ActiveInstance.GetDimensions();
            int viewportWidth = Math.Max(1, (int)viewport.Width);
            int viewportHeight = Math.Max(1, (int)viewport.Height);
            CalculatedStyle dimensions = GetDimensions();
            int currentWidth = Math.Max(1, (int)Math.Round(dimensions.Width));
            int currentHeight = Math.Max(1, (int)Math.Round(dimensions.Height));
            int width = DraggablePanelResizeBehavior.ClampDimension(currentWidth, _minimumWidth, viewportWidth);
            int height = DraggablePanelResizeBehavior.ClampDimension(currentHeight, _minimumHeight, viewportHeight);

            float maxX = Math.Max(0, viewportWidth - width);
            float maxY = Math.Max(0, viewportHeight - height);
            float left = Math.Max(0f, Math.Min(dimensions.X, maxX));
            float top = Math.Max(0f, Math.Min(dimensions.Y, maxY));

            if (width == currentWidth &&
                height == currentHeight &&
                Math.Abs(left - dimensions.X) < 0.01f &&
                Math.Abs(top - dimensions.Y) < 0.01f)
            {
                return;
            }

            Width.Set(width, 0f);
            Height.Set(height, 0f);
            Left.Set(left, 0f);
            Top.Set(top, 0f);
            Recalculate();
        }

        private void OnHeaderLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
        {
            if (_externalInputBlocked || evt.Target != HeaderRoot)
                return;

            CalculatedStyle dimensions = GetDimensions();
            _resizeBehavior.Cancel();
            _isDragging = true;
            _dragPointerStart = evt.MousePosition;
            _dragPositionStartX = dimensions.X;
            _dragPositionStartY = dimensions.Y;
        }

        private void OnResizeHandleLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
        {
            if (_externalInputBlocked || evt.Target != _resizeHandle)
                return;

            CalculatedStyle dimensions = GetDimensions();
            _isDragging = false;
            _resizeBehavior.Begin(
                (int)Math.Round(evt.MousePosition.X),
                (int)Math.Round(evt.MousePosition.Y),
                Math.Max(1, (int)Math.Round(dimensions.Width)),
                Math.Max(1, (int)Math.Round(dimensions.Height)));
        }

        private void UpdateDragging()
        {
            Vector2 pointer = UserInterface.ActiveInstance.MousePosition;
            CalculatedStyle viewport = UserInterface.ActiveInstance.GetDimensions();
            CalculatedStyle dimensions = GetDimensions();
            float x = _dragPositionStartX + pointer.X - _dragPointerStart.X;
            float y = _dragPositionStartY + pointer.Y - _dragPointerStart.Y;
            float maxX = Math.Max(0f, viewport.Width - dimensions.Width);
            float maxY = Math.Max(0f, viewport.Height - dimensions.Height);

            Left.Set(Math.Max(0f, Math.Min(x, maxX)), 0f);
            Top.Set(Math.Max(0f, Math.Min(y, maxY)), 0f);
            Recalculate();
        }

        private void UpdateResizing()
        {
            Vector2 pointer = UserInterface.ActiveInstance.MousePosition;
            CalculatedStyle viewport = UserInterface.ActiveInstance.GetDimensions();

            if (!_resizeBehavior.TryUpdate(
                    (int)Math.Round(pointer.X),
                    (int)Math.Round(pointer.Y),
                    Main.mouseLeft,
                    _externalInputBlocked,
                    Math.Max(1, (int)viewport.Width),
                    Math.Max(1, (int)viewport.Height),
                    out int width,
                    out int height))
            {
                return;
            }

            Width.Set(width, 0f);
            Height.Set(height, 0f);
            ClampPositionToViewport((int)viewport.Width, (int)viewport.Height, width, height);
            Recalculate();
        }

        private void ClampPositionToViewport(int viewportWidth, int viewportHeight, int width, int height)
        {
            CalculatedStyle dimensions = GetDimensions();
            float maxX = Math.Max(0, viewportWidth - width);
            float maxY = Math.Max(0, viewportHeight - height);

            Left.Set(Math.Max(0f, Math.Min(dimensions.X, maxX)), 0f);
            Top.Set(Math.Max(0f, Math.Min(dimensions.Y, maxY)), 0f);
        }

        private void CancelPointerOperation()
        {
            _isDragging = false;
            _resizeBehavior.Cancel();
        }

        private sealed class ResizeHandleElement(Func<bool> isResizing) : UIElement
        {
            private readonly Func<bool> _isResizing = isResizing ?? throw new ArgumentNullException(nameof(isResizing));

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                int visualX = (int)(dimensions.X + dimensions.Width) - ResizeHandleVisualSize - ResizeHandleInset;
                int visualY = (int)(dimensions.Y + dimensions.Height) - ResizeHandleVisualSize - ResizeHandleInset;

                UIRenderer.DrawRect(
                    visualX,
                    visualY,
                    ResizeHandleVisualSize,
                    ResizeHandleVisualSize,
                    _isResizing() || IsMouseHovering ? UIColors.Accent : UIColors.Border);
            }
        }
    }
}