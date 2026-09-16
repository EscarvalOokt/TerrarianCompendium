using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaTextButton : UIElement
    {
        private const int HorizontalPadding = 6;
        private string _text;

        public VanillaTextButton(string text, Action onClick = null)
        {
            Text = text;
            Clicked = onClick;
            OnLeftClick += OnClicked;
        }

        public Action Clicked { get; set; }

        public bool IsActive { get; set; }

        public bool IsEnabled { get; set; } = true;

        public Color4 ActiveBorderColor { get; set; } = UIColors.Accent;

        public int ActiveBorderThickness { get; set; } = 1;

        public string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }

        public string TooltipText { get; set; }

        internal static int MeasureNaturalWidth(string text)
        {
            int textWidth = Math.Max(0, TextUtil.MeasureWidth(text ?? string.Empty));
            long width = textWidth + HorizontalPadding * 2L;
            return width >= int.MaxValue ? int.MaxValue : (int)width;
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

            int availableWidth = Math.Max(0, width - HorizontalPadding * 2);
            string displayText = TruncatedTextPresentation.Truncate(Text, availableWidth, out bool wasTruncated);

            if (displayText.Length > 0)
            {
                int textWidth = TextUtil.MeasureWidth(displayText);
                int textX = x + Math.Max(HorizontalPadding, (width - textWidth) / 2);
                int textY = y + Math.Max(0, (height - 16) / 2);
                UIRenderer.DrawText(displayText, textX, textY, IsEnabled ? UIColors.Text : UIColors.TextDim);
            }

            if (IsMouseHovering)
            {
                string tooltipText = wasTruncated ? Text : TooltipText;

                if (!string.IsNullOrEmpty(tooltipText))
                    Tooltip.Set(tooltipText);
            }
        }

        private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!IsEnabled || evt.Target != this)
                return;

            Clicked?.Invoke();
        }
    }
}