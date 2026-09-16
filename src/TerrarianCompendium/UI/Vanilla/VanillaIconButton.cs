using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaIconButton : UIElement
    {
        private const int IconPadding = 3;
        private readonly Action<Rectangle, bool> _drawIcon;

        public VanillaIconButton(Action<Rectangle> drawIcon, Action onClick = null) : this(
            WrapDrawIcon(drawIcon),
            onClick)
        {
        }

        public VanillaIconButton(Action<Rectangle, bool> drawIcon, Action onClick = null)
        {
            _drawIcon = drawIcon ?? throw new ArgumentNullException(nameof(drawIcon));
            Clicked = onClick;
            OnLeftClick += OnClicked;
        }

        public Action Clicked { get; set; }

        public bool IsActive { get; set; }

        public bool IsEnabled { get; set; } = true;

        public Color4 ActiveBorderColor { get; set; } = UIColors.Accent;

        public int ActiveBorderThickness { get; set; } = 1;

        public string TooltipText { get; set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            if (width <= 0 || height <= 0)
                return;

            UIRenderer.DrawRect(
                x,
                y,
                width,
                height,
                !IsEnabled ? UIColors.Button :
                IsActive ? UIColors.ItemActiveBg :
                IsMouseHovering ? UIColors.ButtonHover : UIColors.Button);

            if (IsEnabled && IsActive)
            {
                UIRenderer.DrawRectOutline(x, y, width, height, ActiveBorderColor, Math.Max(1, ActiveBorderThickness));
            }
            else
            {
                UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);
            }

            int iconWidth = Math.Max(0, width - IconPadding * 2);
            int iconHeight = Math.Max(0, height - IconPadding * 2);

            if (iconWidth > 0 && iconHeight > 0)
                _drawIcon(new Rectangle(x + IconPadding, y + IconPadding, iconWidth, iconHeight), IsMouseHovering);

            if (IsMouseHovering && !string.IsNullOrEmpty(TooltipText))
                Tooltip.Set(TooltipText);
        }

        private static Action<Rectangle, bool> WrapDrawIcon(Action<Rectangle> drawIcon)
        {
            if (drawIcon == null)
                throw new ArgumentNullException(nameof(drawIcon));

            return (bounds, _) => drawIcon(bounds);
        }

        private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!IsEnabled || evt.Target != this)
                return;

            Clicked?.Invoke();
        }
    }
}