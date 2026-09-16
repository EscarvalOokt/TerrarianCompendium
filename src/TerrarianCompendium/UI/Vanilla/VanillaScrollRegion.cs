using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaScrollRegion : UIElement
    {
        private const int ScrollbarWidth = 20;
        private const int ScrollbarGap = 4;
        private const int ScrollbarCapInset = 6;
        private const int ScrollbarReservedWidth = ScrollbarWidth + ScrollbarGap;

        private readonly UIElement _content;
        private readonly UIScrollbar _scrollbar;
        private readonly UIElement _viewport;
        private float _contentHeight;

        public VanillaScrollRegion(bool alwaysShowScrollbar = false)
        {
            SetPadding(0f);

            _viewport = new UIElement
            {
                Width = new StyleDimension(-ScrollbarReservedWidth, 1f),
                Height = StyleDimension.Fill,
                OverflowHidden = true
            };
            _viewport.SetPadding(0f);
            Append(_viewport);

            _content = new UIElement
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Empty,
                MaxHeight = new StyleDimension(float.MaxValue, 0f)
            };
            _content.SetPadding(0f);
            _viewport.Append(_content);

            _scrollbar = new UIScrollbar
            {
                Left = new StyleDimension(-ScrollbarWidth, 1f),
                Top = new StyleDimension(ScrollbarCapInset, 0f),
                Width = new StyleDimension(ScrollbarWidth, 0f),
                Height = new StyleDimension(-ScrollbarCapInset * 2, 1f),
                AutoHide = !alwaysShowScrollbar
            };
            Append(_scrollbar);
        }

        public UIElement Content => _content;

        public int ContentWidth => Math.Max(0, (int)_viewport.GetInnerDimensions().Width);

        public int ViewportHeight => Math.Max(0, (int)_viewport.GetInnerDimensions().Height);

        public int ScrollOffset => Math.Max(0, (int)Math.Round(_scrollbar.ViewPosition));

        internal static int CalculateRequiredWidth(int contentWidth)
        {
            long width = (long)Math.Max(0, contentWidth) + ScrollbarReservedWidth;
            return width >= int.MaxValue ? int.MaxValue : (int)width;
        }

        internal static int CalculateContentWidth(int regionWidth)
        {
            return Math.Max(0, regionWidth - ScrollbarReservedWidth);
        }

        public void ResetScroll()
        {
            _scrollbar.ViewPosition = 0f;
            ApplyScrollPosition();
        }

        public void SetContentHeight(float contentHeight)
        {
            _contentHeight = Math.Max(0f, contentHeight);
            _content.Height.Set(_contentHeight, 0f);
            UpdateScrollbar();
            ApplyScrollPosition();
        }

        public override void Recalculate()
        {
            base.Recalculate();
            UpdateScrollbar();
            ApplyScrollPosition();
        }

        public override void Update(GameTime gameTime)
        {
            ApplyScrollPosition();
            base.Update(gameTime);
            ApplyScrollPosition();
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            _scrollbar.ViewPosition -= evt.ScrollWheelValue;
            ApplyScrollPosition();
            base.ScrollWheel(evt);
        }

        private void UpdateScrollbar()
        {
            float viewportHeight = Math.Max(0f, _viewport.GetInnerDimensions().Height);
            _scrollbar.SetView(viewportHeight, Math.Max(viewportHeight, _contentHeight));
        }

        private void ApplyScrollPosition()
        {
            _content.Top.Set(-_scrollbar.ViewPosition, 0f);
            _content.Recalculate();
        }
    }
}