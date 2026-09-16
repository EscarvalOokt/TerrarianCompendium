using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class TextLabelElement : UIElement
    {
        private readonly Color4 _color;
        private string _text;

        public TextLabelElement(string text) : this(text, UIColors.TextTitle)
        {
        }

        public TextLabelElement(string text, Color4 color)
        {
            _color = color;
            Text = text;
        }

        public string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);

            if (width <= 0 || _text.Length == 0)
                return;

            string display = TruncatedTextPresentation.Truncate(_text, width, out _);

            if (display.Length > 0)
                UIRenderer.DrawText(display, x, y + 1, _color);
        }
    }
}