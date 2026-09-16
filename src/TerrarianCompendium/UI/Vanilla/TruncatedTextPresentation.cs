using System;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaModder.Core.UI.Widgets;

namespace TerrarianCompendium.UI.Vanilla
{
    internal static class TruncatedTextPresentation
    {
        internal static string Truncate(string text, int availableWidth, out bool wasTruncated)
        {
            string fullText = text ?? string.Empty;
            string displayText = TextUtil.Truncate(fullText, Math.Max(0, availableWidth));
            wasTruncated = !string.Equals(displayText, fullText, StringComparison.Ordinal);
            return displayText;
        }

        internal static void ShowTooltipIfTruncated(
            string fullText,
            bool wasTruncated,
            int x,
            int y,
            int width,
            int height,
            bool ownerHovered)
        {
            if (!ownerHovered || !wasTruncated || string.IsNullOrEmpty(fullText) || width <= 0 || height <= 0)
            {
                return;
            }

            var bounds = new Rectangle(x, y, width, height);

            if (bounds.Contains(new Point(Main.mouseX, Main.mouseY)))
                Tooltip.Set(fullText);
        }
    }
}