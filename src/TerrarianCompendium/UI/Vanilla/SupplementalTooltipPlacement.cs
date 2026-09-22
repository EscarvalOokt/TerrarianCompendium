using System;

namespace TerrarianCompendium.UI.Vanilla
{
    internal enum SupplementalTooltipPlacementKind
    {
        Below,
        Above,
        Right,
        Left,
        MinimumOverlap
    }

    internal readonly struct SupplementalTooltipBounds(int x, int y, int width, int height)
    {
        public int X { get; } = x;

        public int Y { get; } = y;

        public int Width { get; } = width;

        public int Height { get; } = height;

        public int Left => X;

        public int Top => Y;

        public int Right => X + Width;

        public int Bottom => Y + Height;
    }

    internal readonly struct SupplementalTooltipPosition(int x, int y, SupplementalTooltipPlacementKind kind)
    {
        public int X { get; } = x;

        public int Y { get; } = y;

        public SupplementalTooltipPlacementKind Kind { get; } = kind;
    }

    internal static class SupplementalTooltipPlacement
    {
        public static SupplementalTooltipPosition Resolve(
            int screenWidth,
            int screenHeight,
            int mouseX,
            int mouseY,
            int tooltipWidth,
            int tooltipHeight,
            SupplementalTooltipBounds primaryBounds,
            int edgePadding,
            int gap)
        {
            edgePadding = Math.Max(0, edgePadding);
            gap = Math.Max(0, gap);

            int viewportWidth = Math.Max(1, screenWidth - edgePadding * 2);
            int viewportHeight = Math.Max(1, screenHeight - edgePadding * 2);
            int width = Math.Min(Math.Max(1, tooltipWidth), viewportWidth);
            int height = Math.Min(Math.Max(1, tooltipHeight), viewportHeight);
            int viewportRight = edgePadding + viewportWidth;
            int viewportBottom = edgePadding + viewportHeight;

            int alignedX = Clamp(primaryBounds.Left, edgePadding, viewportRight - width);
            int alignedY = Clamp(primaryBounds.Top, edgePadding, viewportBottom - height);

            Candidate[] preferred =
            [
                new Candidate(
                    new SupplementalTooltipBounds(alignedX, primaryBounds.Bottom + gap, width, height),
                    SupplementalTooltipPlacementKind.Below),
                new Candidate(
                    new SupplementalTooltipBounds(alignedX, primaryBounds.Top - gap - height, width, height),
                    SupplementalTooltipPlacementKind.Above),
                new Candidate(
                    new SupplementalTooltipBounds(primaryBounds.Right + gap, alignedY, width, height),
                    SupplementalTooltipPlacementKind.Right),
                new Candidate(
                    new SupplementalTooltipBounds(primaryBounds.Left - gap - width, alignedY, width, height),
                    SupplementalTooltipPlacementKind.Left)
            ];

            for (var index = 0; index < preferred.Length; index++)
            {
                Candidate candidate = preferred[index];

                if (IsInsideViewport(candidate.Bounds, edgePadding, edgePadding, viewportRight, viewportBottom) &&
                    IntersectionArea(candidate.Bounds, primaryBounds) == 0)
                {
                    return new SupplementalTooltipPosition(candidate.Bounds.X, candidate.Bounds.Y, candidate.Kind);
                }
            }

            Candidate[] fallback =
            [
                ClampCandidate(preferred[0], edgePadding, edgePadding, viewportRight, viewportBottom),
                ClampCandidate(preferred[1], edgePadding, edgePadding, viewportRight, viewportBottom),
                ClampCandidate(preferred[2], edgePadding, edgePadding, viewportRight, viewportBottom),
                ClampCandidate(preferred[3], edgePadding, edgePadding, viewportRight, viewportBottom),
                new Candidate(
                    new SupplementalTooltipBounds(edgePadding, edgePadding, width, height),
                    SupplementalTooltipPlacementKind.MinimumOverlap),
                new Candidate(
                    new SupplementalTooltipBounds(viewportRight - width, edgePadding, width, height),
                    SupplementalTooltipPlacementKind.MinimumOverlap),
                new Candidate(
                    new SupplementalTooltipBounds(edgePadding, viewportBottom - height, width, height),
                    SupplementalTooltipPlacementKind.MinimumOverlap),
                new Candidate(
                    new SupplementalTooltipBounds(viewportRight - width, viewportBottom - height, width, height),
                    SupplementalTooltipPlacementKind.MinimumOverlap)
            ];

            Candidate best = fallback[0];
            long bestOverlap = IntersectionArea(best.Bounds, primaryBounds);
            bool bestCoversCursor = Contains(best.Bounds, mouseX, mouseY);

            for (var index = 1; index < fallback.Length; index++)
            {
                Candidate candidate = fallback[index];
                long overlap = IntersectionArea(candidate.Bounds, primaryBounds);
                bool coversCursor = Contains(candidate.Bounds, mouseX, mouseY);

                if (overlap > bestOverlap)
                    continue;
                if (overlap == bestOverlap && coversCursor && !bestCoversCursor)
                    continue;
                if (overlap == bestOverlap && coversCursor == bestCoversCursor)
                    continue;

                best = candidate;
                bestOverlap = overlap;
                bestCoversCursor = coversCursor;
            }

            return new SupplementalTooltipPosition(
                best.Bounds.X,
                best.Bounds.Y,
                SupplementalTooltipPlacementKind.MinimumOverlap);
        }

        private static Candidate ClampCandidate(
            Candidate candidate,
            int viewportLeft,
            int viewportTop,
            int viewportRight,
            int viewportBottom)
        {
            SupplementalTooltipBounds bounds = candidate.Bounds;
            int x = Clamp(bounds.X, viewportLeft, viewportRight - bounds.Width);
            int y = Clamp(bounds.Y, viewportTop, viewportBottom - bounds.Height);

            return new Candidate(new SupplementalTooltipBounds(x, y, bounds.Width, bounds.Height), candidate.Kind);
        }

        private static bool IsInsideViewport(
            SupplementalTooltipBounds bounds,
            int viewportLeft,
            int viewportTop,
            int viewportRight,
            int viewportBottom)
        {
            return bounds.Left >= viewportLeft &&
                   bounds.Top >= viewportTop &&
                   bounds.Right <= viewportRight &&
                   bounds.Bottom <= viewportBottom;
        }

        private static long IntersectionArea(SupplementalTooltipBounds left, SupplementalTooltipBounds right)
        {
            int width = Math.Max(0, Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left));
            int height = Math.Max(0, Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Top, right.Top));

            return (long)width * height;
        }

        private static bool Contains(SupplementalTooltipBounds bounds, int x, int y)
        {
            return x >= bounds.Left && x < bounds.Right && y >= bounds.Top && y < bounds.Bottom;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (maximum < minimum)
                return minimum;
            if (value <= minimum)
                return minimum;
            if (value >= maximum)
                return maximum;

            return value;
        }

        private readonly struct Candidate(SupplementalTooltipBounds bounds, SupplementalTooltipPlacementKind kind)
        {
            public SupplementalTooltipBounds Bounds { get; } = bounds;

            public SupplementalTooltipPlacementKind Kind { get; } = kind;
        }
    }
}