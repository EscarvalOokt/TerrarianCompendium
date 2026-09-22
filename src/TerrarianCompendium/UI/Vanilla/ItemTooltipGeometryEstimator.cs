using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using TerrariaModder.Core.UI;

namespace TerrarianCompendium.UI.Vanilla
{
    internal static class ItemTooltipGeometryEstimator
    {
        private const int MaxWidth = 350;
        private const int LineHeight = 16;
        private const int Padding = 8;
        private const int CursorOffset = 16;
        private const int EdgePadding = 4;
        private const int ConservativeHeight = 416;

        public static bool TryEstimateBounds(
            int itemId,
            int screenWidth,
            int screenHeight,
            int mouseX,
            int mouseY,
            out SupplementalTooltipBounds bounds)
        {
            bounds = default;

            if (itemId <= 0 ||
                ContentSamples.ItemsByType == null ||
                ContentSamples.ItemsByType.Count == 0 ||
                !ContentSamples.ItemsByType.ContainsKey(itemId))
            {
                return false;
            }

            var lines = new List<string>(24);

            if (!TryBuildLines(ContentSamples.ItemsByType[itemId], lines) || lines.Count == 0)
                return false;

            bounds = EstimateBounds(lines, screenWidth, screenHeight, mouseX, mouseY, UIRenderer.MeasureText);
            return true;
        }

        internal static SupplementalTooltipBounds EstimateBounds(
            IReadOnlyList<string> lines,
            int screenWidth,
            int screenHeight,
            int mouseX,
            int mouseY,
            Func<string, int> measureText)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (measureText == null)
                throw new ArgumentNullException(nameof(measureText));

            int maxContentWidth = MaxWidth - Padding * 2;
            var wrappedLineCount = 0;
            var contentWidth = 0;

            for (var index = 0; index < lines.Count; index++)
            {
                string line = lines[index] ?? string.Empty;

                if (line.Length == 0)
                {
                    wrappedLineCount++;
                    continue;
                }

                MeasureWrappedLine(line, maxContentWidth, measureText, ref wrappedLineCount, ref contentWidth);
            }

            int tooltipWidth = Math.Min(contentWidth + Padding * 2, MaxWidth);
            int tooltipHeight = wrappedLineCount * LineHeight + Padding * 2;
            int x = mouseX + CursorOffset;
            int y = mouseY + CursorOffset;

            if (x + tooltipWidth > screenWidth - EdgePadding)
                x = mouseX - tooltipWidth - EdgePadding;
            if (y + tooltipHeight > screenHeight - EdgePadding)
                y = mouseY - tooltipHeight - EdgePadding;

            x = Math.Max(EdgePadding, x);
            y = Math.Max(EdgePadding, y);

            return new SupplementalTooltipBounds(x, y, tooltipWidth, tooltipHeight);
        }

        internal static SupplementalTooltipBounds EstimateConservativeBounds(
            int screenWidth,
            int screenHeight,
            int mouseX,
            int mouseY)
        {
            int viewportWidth = Math.Max(1, screenWidth - EdgePadding * 2);
            int viewportHeight = Math.Max(1, screenHeight - EdgePadding * 2);
            int tooltipWidth = Math.Min(MaxWidth, viewportWidth);
            int tooltipHeight = Math.Min(ConservativeHeight, viewportHeight);
            int x = mouseX + CursorOffset;
            int y = mouseY + CursorOffset;

            if (x + tooltipWidth > screenWidth - EdgePadding)
                x = mouseX - tooltipWidth - EdgePadding;
            if (y + tooltipHeight > screenHeight - EdgePadding)
                y = mouseY - tooltipHeight - EdgePadding;

            x = Math.Max(EdgePadding, x);
            y = Math.Max(EdgePadding, y);

            return new SupplementalTooltipBounds(x, y, tooltipWidth, tooltipHeight);
        }

        private static bool TryBuildLines(Item item, List<string> lines)
        {
            int damage = item.damage;
            int crit = item.crit;
            int useAnimation = item.useAnimation;
            int useStyle = item.useStyle;
            float knockBack = item.knockBack;
            int defense = item.defense;
            int pick = item.pick;
            int axe = item.axe;
            int hammer = item.hammer;
            int tileBoost = item.tileBoost;
            int healLife = item.healLife;
            int healMana = item.healMana;
            int mana = item.mana;
            bool consumable = item.consumable;
            bool material = item.material;
            bool vanity = item.vanity;
            bool accessory = item.accessory;
            int headSlot = item.headSlot;
            int bodySlot = item.bodySlot;
            int legSlot = item.legSlot;
            int createTile = item.createTile;
            int createWall = item.createWall;
            int ammo = item.ammo;
            bool notAmmo = item.notAmmo;
            int buffTime = item.buffTime;
            bool melee = item.melee;
            bool ranged = item.ranged;
            bool magic = item.magic;
            bool summon = item.summon;
            int bait = item.bait;
            int fishingPole = item.fishingPole;
            bool questItem = item.questItem;

            string name;

            try
            {
                name = item.AffixName() ?? "???";
            }
            catch
            {
                name = "???";
            }

            lines.Add(name);

            if (damage > 0 && (!notAmmo || useStyle != 0))
            {
                string damageType;

                if (melee)
                    damageType = GetTip(2);
                else if (ranged)
                    damageType = GetTip(3);
                else if (magic)
                    damageType = GetTip(4);
                else if (summon)
                    damageType = GetTip(53);
                else
                    damageType = GetTip(55);

                AddLine(lines, damage + damageType);

                if (melee || ranged || magic)
                    AddLine(lines, crit + GetTip(5));

                if (useStyle != 0 && !summon)
                {
                    if (useAnimation <= 8)
                        AddLine(lines, GetTip(6));
                    else if (useAnimation <= 20)
                        AddLine(lines, GetTip(7));
                    else if (useAnimation <= 25)
                        AddLine(lines, GetTip(8));
                    else if (useAnimation <= 30)
                        AddLine(lines, GetTip(9));
                    else if (useAnimation <= 35)
                        AddLine(lines, GetTip(10));
                    else if (useAnimation <= 45)
                        AddLine(lines, GetTip(11));
                    else if (useAnimation <= 55)
                        AddLine(lines, GetTip(12));
                    else
                        AddLine(lines, GetTip(13));
                }

                if (knockBack == 0f)
                    AddLine(lines, GetTip(14));
                else if (knockBack <= 1.5f)
                    AddLine(lines, GetTip(15));
                else if (knockBack <= 3f)
                    AddLine(lines, GetTip(16));
                else if (knockBack <= 4f)
                    AddLine(lines, GetTip(17));
                else if (knockBack <= 6f)
                    AddLine(lines, GetTip(18));
                else if (knockBack <= 7f)
                    AddLine(lines, GetTip(19));
                else if (knockBack <= 9f)
                    AddLine(lines, GetTip(20));
                else if (knockBack <= 11f)
                    AddLine(lines, GetTip(21));
                else
                    AddLine(lines, GetTip(22));
            }

            if (fishingPole > 0)
                AddLine(lines, fishingPole + "% fishing power");
            if (bait > 0)
                AddLine(lines, bait + "% bait power");

            if (accessory || headSlot > 0 || bodySlot > 0 || legSlot > 0)
                AddLine(lines, GetTip(23));

            if (questItem)
                AddLine(lines, "Quest Item");

            if (vanity)
                AddLine(lines, GetTip(24));

            if (defense > 0)
                AddLine(lines, defense + GetTip(25));

            if (pick > 0)
                AddLine(lines, pick + GetTip(26));
            if (axe > 0)
                AddLine(lines, axe * 5 + GetTip(27));
            if (hammer > 0)
                AddLine(lines, hammer + GetTip(28));

            if (tileBoost != 0)
            {
                string sign = tileBoost > 0 ? "+" : string.Empty;
                AddLine(lines, sign + tileBoost + GetTip(54));
            }

            if (healLife > 0)
                AddLine(lines, "Restores " + healLife + " life");
            if (healMana > 0)
                AddLine(lines, "Restores " + healMana + " mana");

            if (mana > 0)
                AddLine(lines, "Uses " + mana + " mana");

            if (createWall > 0 || createTile > -1)
            {
                if (consumable)
                    AddLine(lines, GetTip(33));
            }
            else if (ammo > 0 && !notAmmo)
            {
                AddLine(lines, GetTip(34));
            }
            else if (consumable)
            {
                AddLine(lines, GetTip(35));
            }

            if (material)
                AddLine(lines, GetTip(36));

            try
            {
                ItemTooltip tooltip = item.ToolTip;

                if (tooltip != null)
                {
                    int lineCount = tooltip.Lines;

                    for (var index = 0; index < lineCount; index++)
                        AddLine(lines, tooltip.GetLine(index));
                }
            }
            catch
            {
                return false;
            }

            if (buffTime > 0)
            {
                int seconds = buffTime / 60;

                if (seconds < 60)
                    AddLine(lines, Math.Round(buffTime / 60.0) + " second duration");
                else
                    AddLine(lines, Math.Round(seconds / 60.0) + " minute duration");
            }

            return true;
        }

        private static void MeasureWrappedLine(
            string text,
            int maxContentWidth,
            Func<string, int> measureText,
            ref int lineCount,
            ref int contentWidth)
        {
            if (measureText(text) <= maxContentWidth)
            {
                AddMeasuredLine(text, measureText, ref lineCount, ref contentWidth);
                return;
            }

            string remaining = text;

            while (measureText(remaining) > maxContentWidth)
            {
                int breakAt = -1;

                for (int index = remaining.Length - 1; index > 0; index--)
                {
                    if (remaining[index] == ' ' && measureText(remaining.Substring(0, index)) <= maxContentWidth)
                    {
                        breakAt = index;
                        break;
                    }
                }

                if (breakAt <= 0)
                {
                    breakAt = remaining.Length;

                    for (var index = 1; index < remaining.Length; index++)
                    {
                        if (measureText(remaining.Substring(0, index)) > maxContentWidth)
                        {
                            breakAt = Math.Max(1, index - 1);
                            break;
                        }
                    }
                }

                AddMeasuredLine(remaining.Substring(0, breakAt), measureText, ref lineCount, ref contentWidth);
                remaining = remaining.Substring(breakAt).TrimStart();
            }

            if (remaining.Length > 0)
                AddMeasuredLine(remaining, measureText, ref lineCount, ref contentWidth);
        }

        private static void AddMeasuredLine(
            string line,
            Func<string, int> measureText,
            ref int lineCount,
            ref int contentWidth)
        {
            lineCount++;
            contentWidth = Math.Max(contentWidth, measureText(line));
        }

        private static void AddLine(List<string> lines, string text)
        {
            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }

        private static string GetTip(int index)
        {
            if (Lang.tip == null || index < 0 || index >= Lang.tip.Length)
                return string.Empty;

            try
            {
                return Lang.tip[index]?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}