using System;

namespace TerrarianCompendium.UI
{
    internal sealed class DraggablePanelResizeBehavior
    {
        private readonly int _minimumHeight;
        private readonly int _minimumWidth;
        private bool _isResizing;
        private int _resizeStartHeight;
        private int _resizeStartPointerX;
        private int _resizeStartPointerY;
        private int _resizeStartWidth;

        public DraggablePanelResizeBehavior(int minimumWidth, int minimumHeight)
        {
            if (minimumWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumWidth));

            if (minimumHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumHeight));

            _minimumWidth = minimumWidth;
            _minimumHeight = minimumHeight;
        }

        public bool IsResizing => _isResizing;

        public void Begin(int pointerX, int pointerY, int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            _isResizing = true;
            _resizeStartPointerX = pointerX;
            _resizeStartPointerY = pointerY;
            _resizeStartWidth = width;
            _resizeStartHeight = height;
        }

        public bool TryUpdate(
            int pointerX,
            int pointerY,
            bool pointerDown,
            bool blocked,
            int viewportWidth,
            int viewportHeight,
            out int width,
            out int height)
        {
            width = _resizeStartWidth;
            height = _resizeStartHeight;

            if (!_isResizing)
                return false;

            if (!pointerDown || blocked)
            {
                Cancel();
                return false;
            }

            int requestedWidth = _resizeStartWidth + pointerX - _resizeStartPointerX;
            int requestedHeight = _resizeStartHeight + pointerY - _resizeStartPointerY;
            width = ClampDimension(requestedWidth, _minimumWidth, viewportWidth);
            height = ClampDimension(requestedHeight, _minimumHeight, viewportHeight);

            return true;
        }

        public void Cancel()
        {
            _isResizing = false;
        }

        internal static int ClampDimension(int value, int minimum, int screenSize)
        {
            if (minimum <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimum));

            if (screenSize <= 0)
                return Math.Max(value, minimum);

            int effectiveMinimum = Math.Min(minimum, screenSize);

            return Math.Max(effectiveMinimum, Math.Min(value, screenSize));
        }
    }
}