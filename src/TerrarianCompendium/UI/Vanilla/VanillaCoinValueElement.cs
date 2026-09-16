using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrarianCompendium.UI.Vanilla
{
    internal readonly struct VanillaCoinValueBreakdown(int platinum, int gold, int silver, int copper)
    {
        private const int CopperPerSilver = 100;
        private const int CopperPerGold = CopperPerSilver * 100;
        private const int CopperPerPlatinum = CopperPerGold * 100;

        public int Platinum { get; } = platinum;
        public int Gold { get; } = gold;
        public int Silver { get; } = silver;
        public int Copper { get; } = copper;

        internal static VanillaCoinValueBreakdown Decompose(int value)
        {
            int remaining = Math.Max(0, value);
            int platinum = remaining / CopperPerPlatinum;
            remaining %= CopperPerPlatinum;
            int gold = remaining / CopperPerGold;
            remaining %= CopperPerGold;
            int silver = remaining / CopperPerSilver;
            int copper = remaining % CopperPerSilver;

            return new VanillaCoinValueBreakdown(platinum, gold, silver, copper);
        }
    }

    internal sealed class VanillaCoinValueElement : UIElement
    {
        public const int RowHeight = 24;

        private const int IconSize = 20;
        private const int IconTextGap = 2;
        private const int GroupGap = 4;
        private const int TextHeight = 16;

        private int _leadingItemId;
        private bool _showCopperWhenZero;
        private string _tooltipText = string.Empty;
        private int _value;

        public void Bind(int value, int leadingItemId, bool showCopperWhenZero, string tooltipText)
        {
            _value = Math.Max(0, value);
            _leadingItemId = Math.Max(0, leadingItemId);
            _showCopperWhenZero = showCopperWhenZero;
            _tooltipText = tooltipText ?? string.Empty;
            IgnoresMouseInteraction = false;

            if (_leadingItemId > 0)
                AsyncItemIconRenderer.RequestAsync(_leadingItemId);

            var breakdown = VanillaCoinValueBreakdown.Decompose(_value);

            if (breakdown.Platinum > 0)
                AsyncItemIconRenderer.RequestAsync(ItemID.PlatinumCoin);
            if (breakdown.Gold > 0)
                AsyncItemIconRenderer.RequestAsync(ItemID.GoldCoin);
            if (breakdown.Silver > 0)
                AsyncItemIconRenderer.RequestAsync(ItemID.SilverCoin);
            if (breakdown.Copper > 0 || (_value == 0 && _showCopperWhenZero))
                AsyncItemIconRenderer.RequestAsync(ItemID.CopperCoin);
        }

        public void Hide()
        {
            _value = 0;
            _leadingItemId = 0;
            _showCopperWhenZero = false;
            _tooltipText = string.Empty;
            IgnoresMouseInteraction = true;
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
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

            int right = x + width;
            int cursorX = x;

            if (_leadingItemId > 0)
            {
                if (!DrawIcon(_leadingItemId, ref cursorX, y, height, right))
                    return;

                cursorX += GroupGap;
            }

            var breakdown = VanillaCoinValueBreakdown.Decompose(_value);
            var hasDenomination = false;

            hasDenomination |= DrawCoinGroup(ItemID.PlatinumCoin, breakdown.Platinum, ref cursorX, y, height, right);
            hasDenomination |= DrawCoinGroup(ItemID.GoldCoin, breakdown.Gold, ref cursorX, y, height, right);
            hasDenomination |= DrawCoinGroup(ItemID.SilverCoin, breakdown.Silver, ref cursorX, y, height, right);
            hasDenomination |= DrawCoinGroup(ItemID.CopperCoin, breakdown.Copper, ref cursorX, y, height, right);

            if (!hasDenomination && _value == 0)
            {
                if (_showCopperWhenZero)
                {
                    DrawCoinGroup(ItemID.CopperCoin, 0, ref cursorX, y, height, right, force: true);
                }
                else if (_leadingItemId > 0)
                {
                    DrawAmountText("0", ref cursorX, y, height, right);
                }
            }

            if (IsMouseHovering && _tooltipText.Length > 0)
                Tooltip.Set(_tooltipText);
        }

        private static bool DrawCoinGroup(
            int itemId,
            int amount,
            ref int cursorX,
            int y,
            int height,
            int right,
            bool force = false)
        {
            if (amount <= 0 && !force)
                return false;

            var amountText = amount.ToString();
            int amountWidth = Math.Max(1, UIRenderer.MeasureText(amountText));
            int groupWidth = IconSize + IconTextGap + amountWidth;

            if (cursorX + groupWidth > right)
                return false;

            AsyncItemIconRenderer.Draw(itemId, cursorX, y + Math.Max(0, (height - IconSize) / 2), IconSize, IconSize);
            UIRenderer.DrawText(
                amountText,
                cursorX + IconSize + IconTextGap,
                y + Math.Max(0, (height - TextHeight) / 2),
                UIColors.Text);
            cursorX += groupWidth + GroupGap;
            return true;
        }

        private static bool DrawIcon(int itemId, ref int cursorX, int y, int height, int right)
        {
            if (cursorX + IconSize > right)
                return false;

            AsyncItemIconRenderer.Draw(itemId, cursorX, y + Math.Max(0, (height - IconSize) / 2), IconSize, IconSize);
            cursorX += IconSize;
            return true;
        }

        private static void DrawAmountText(string text, ref int cursorX, int y, int height, int right)
        {
            int width = Math.Max(1, UIRenderer.MeasureText(text));

            if (cursorX + width > right)
                return;

            UIRenderer.DrawText(text, cursorX, y + Math.Max(0, (height - TextHeight) / 2), UIColors.Text);
            cursorX += width;
        }
    }
}