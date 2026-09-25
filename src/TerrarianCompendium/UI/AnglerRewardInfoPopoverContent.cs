using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class AnglerRewardInfoPopoverContent : UIElement, IVanillaPopoverContent
    {
        private const int MaximumWidth = 360;
        private const int LineHeight = 18;

        private string _text = string.Empty;

        public AnglerRewardInfoPopoverContent()
        {
            SetPadding(0f);
            IgnoresMouseInteraction = true;
        }

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            int width = Math.Min(Math.Max(0, availableWidth), MaximumWidth);

            if (width <= 0 || string.IsNullOrWhiteSpace(_text))
                return new VanillaPopoverContentSize(width, 0);

            IReadOnlyList<string> lines = SupplementalTooltip.WrapText(_text, width, UIRenderer.MeasureText);
            return new VanillaPopoverContentSize(width, lines.Count * LineHeight);
        }

        public void Bind(string text)
        {
            _text = text ?? string.Empty;
            Recalculate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            int width = Math.Max(0, (int)GetInnerDimensions().Width);

            if (width <= 0 || string.IsNullOrWhiteSpace(_text))
                return;

            IReadOnlyList<string> lines = SupplementalTooltip.WrapText(_text, width, UIRenderer.MeasureText);
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;

            for (var index = 0; index < lines.Count; index++)
                UIRenderer.DrawText(lines[index], x, y + index * LineHeight, UIColors.Text);
        }
    }
}