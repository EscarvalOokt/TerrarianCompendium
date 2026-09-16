using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaPopover : UIElement
    {
        private const int AnchorGap = 4;
        private const int PanelPadding = 8;
        private readonly UIElement _anchor;
        private readonly IVanillaPopoverContent _contentMeasurer;
        private readonly UIElement _host;
        private readonly PopoverPanel _panel;

        public VanillaPopover(UIElement host, UIElement anchor, UIElement content)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            UIElement resolvedContent = content ?? throw new ArgumentNullException(nameof(content));
            _contentMeasurer = resolvedContent as IVanillaPopoverContent ??
                               throw new ArgumentException(
                                   "Content-sized popover content must implement IVanillaPopoverContent.",
                                   nameof(content));

            _panel = CreatePanel(resolvedContent);
            ConfigureRoot();
        }

        public bool IsOpen => Parent == _host;

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (IsOpen)
                return;

            if (Parent != null)
                Parent.RemoveChild(this);

            _host.Append(this);
            Recalculate();
        }

        public void Close()
        {
            if (Parent != null)
                Parent.RemoveChild(this);
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle hostDimensions = _host.GetInnerDimensions();
            CalculatedStyle anchorDimensions = _anchor.GetDimensions();
            int hostWidth = Math.Max(0, (int)hostDimensions.Width);
            int hostHeight = Math.Max(0, (int)hostDimensions.Height);
            var anchorX = (int)Math.Round(anchorDimensions.X - hostDimensions.X);
            var anchorY = (int)Math.Round(anchorDimensions.Y - hostDimensions.Y);
            int anchorWidth = Math.Max(0, (int)Math.Round(anchorDimensions.Width));
            int anchorHeight = Math.Max(0, (int)Math.Round(anchorDimensions.Height));

            int availableContentWidth = Math.Max(0, hostWidth - PanelPadding * 2);
            VanillaPopoverContentSize contentSize = _contentMeasurer.MeasurePopoverContent(availableContentWidth);
            VanillaPopoverBounds bounds = VanillaPopoverLayout.CalculateContentSizedPanelBounds(
                hostWidth,
                hostHeight,
                anchorX,
                anchorY,
                anchorWidth,
                anchorHeight,
                contentSize.Width,
                contentSize.Height,
                PanelPadding,
                AnchorGap);

            _panel.Left.Set(bounds.X, 0f);
            _panel.Top.Set(bounds.Y, 0f);
            _panel.Width.Set(bounds.Width, 0f);
            _panel.Height.Set(bounds.Height, 0f);

            base.RecalculateChildren();
        }

        private static PopoverPanel CreatePanel(UIElement content)
        {
            var panel = new PopoverPanel
            {
                OverflowHidden = true
            };
            panel.SetPadding(PanelPadding);

            content.Width = StyleDimension.Fill;
            content.Height = StyleDimension.Fill;
            panel.Append(content);
            return panel;
        }

        private void ConfigureRoot()
        {
            Width = StyleDimension.Fill;
            Height = StyleDimension.Fill;
            SetPadding(0f);
            OnLeftClick += OnBackdropClicked;
            Append(_panel);
        }

        private void OnBackdropClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (evt.Target == this)
                Close();
        }

        private sealed class PopoverPanel : UIElement
        {
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
                UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);
            }
        }
    }

    internal interface IVanillaPopoverContent
    {
        VanillaPopoverContentSize MeasurePopoverContent(int availableWidth);
    }

    internal readonly struct VanillaPopoverContentSize(int width, int height)
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    internal readonly struct VanillaPopoverBounds(int x, int y, int width, int height)
    {
        public int X { get; } = x;

        public int Y { get; } = y;

        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    internal static class VanillaPopoverLayout
    {
        public static VanillaPopoverBounds CalculateContentSizedPanelBounds(
            int hostWidth,
            int hostHeight,
            int anchorX,
            int anchorY,
            int anchorWidth,
            int anchorHeight,
            int contentWidth,
            int contentHeight,
            int panelPadding,
            int anchorGap)
        {
            hostWidth = Math.Max(0, hostWidth);
            hostHeight = Math.Max(0, hostHeight);
            anchorWidth = Math.Max(0, anchorWidth);
            anchorHeight = Math.Max(0, anchorHeight);
            contentWidth = Math.Max(0, contentWidth);
            contentHeight = Math.Max(0, contentHeight);
            panelPadding = Math.Max(0, panelPadding);
            anchorGap = Math.Max(0, anchorGap);

            int desiredWidth = AddPadding(contentWidth, panelPadding);
            int desiredHeight = AddPadding(contentHeight, panelPadding);
            int width = Math.Min(desiredWidth, hostWidth);

            int anchorRight = ClampToRange((long)anchorX + anchorWidth, 0, hostWidth);
            int left = Math.Max(0, Math.Min(anchorRight - width, hostWidth - width));

            int belowTop = ClampToRange((long)anchorY + anchorHeight + anchorGap, 0, hostHeight);
            int belowSpace = Math.Max(0, hostHeight - belowTop);

            int aboveBottom = ClampToRange((long)anchorY - anchorGap, 0, hostHeight);
            int aboveSpace = Math.Max(0, aboveBottom);

            bool openBelow;

            if (belowSpace >= desiredHeight)
                openBelow = true;
            else if (aboveSpace >= desiredHeight)
                openBelow = false;
            else
                openBelow = belowSpace >= aboveSpace;

            int availableHeight = openBelow ? belowSpace : aboveSpace;
            int height = Math.Min(desiredHeight, availableHeight);
            int top = openBelow ? belowTop : Math.Max(0, aboveBottom - height);

            top = Math.Max(0, Math.Min(top, hostHeight - height));

            return new VanillaPopoverBounds(left, top, width, height);
        }

        private static int AddPadding(int contentSize, int panelPadding)
        {
            long paddedSize = Math.Max(0, contentSize) + Math.Max(0, panelPadding) * 2L;

            return paddedSize >= int.MaxValue ? int.MaxValue : (int)paddedSize;
        }

        private static int ClampToRange(long value, int minimum, int maximum)
        {
            if (value <= minimum)
                return minimum;

            if (value >= maximum)
                return maximum;

            return (int)value;
        }
    }
}