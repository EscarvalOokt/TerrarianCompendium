using System;
using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class SupplementalTooltipPlacementTests
    {
        [Test]
        public void EstimatePrimaryBounds_WhenTooltipFitsRightAndBelow_UsesMeasuredDimensions()
        {
            SupplementalTooltipBounds bounds = ItemTooltipGeometryEstimator.EstimateBounds(
                [
                    "Lead Anvil",
                    "Can be placed",
                    "Used to craft items from metal bars"
                ],
                screenWidth: 1920,
                screenHeight: 1080,
                mouseX: 400,
                mouseY: 300,
                measureText: MeasureEightPixelsPerCharacter);

            Assert.Multiple(() =>
            {
                Assert.That(bounds.X, Is.EqualTo(416));
                Assert.That(bounds.Y, Is.EqualTo(316));
                Assert.That(bounds.Width, Is.EqualTo(296));
                Assert.That(bounds.Height, Is.EqualTo(64));
            });
        }

        [Test]
        public void EstimatePrimaryBounds_WhenBottomWouldOverflow_FlipsAboveUsingMeasuredHeight()
        {
            SupplementalTooltipBounds bounds = ItemTooltipGeometryEstimator.EstimateBounds(
                [
                    "Archery Potion",
                    "Consumable",
                    "10% increased bow damage",
                    "20% increased arrow speed",
                    "8 minute duration"
                ],
                screenWidth: 1200,
                screenHeight: 800,
                mouseX: 500,
                mouseY: 740,
                measureText: MeasureEightPixelsPerCharacter);

            Assert.Multiple(() =>
            {
                Assert.That(bounds.Y, Is.EqualTo(640));
                Assert.That(bounds.Height, Is.EqualTo(96));
            });
        }

        [Test]
        public void EstimatePrimaryBounds_WhenRightWouldOverflow_FlipsLeftUsingMeasuredWidth()
        {
            SupplementalTooltipBounds bounds = ItemTooltipGeometryEstimator.EstimateBounds(
                ["12345678901234567890"],
                screenWidth: 1200,
                screenHeight: 800,
                mouseX: 1100,
                mouseY: 300,
                measureText: MeasureTenPixelsPerCharacter);

            Assert.Multiple(() =>
            {
                Assert.That(bounds.X, Is.EqualTo(880));
                Assert.That(bounds.Width, Is.EqualTo(216));
            });
        }

        [Test]
        public void EstimatePrimaryBounds_WhenLineExceedsContentWidth_WrapsLikeFrameworkTooltip()
        {
            SupplementalTooltipBounds bounds = ItemTooltipGeometryEstimator.EstimateBounds(
                ["1234567890123456789012345678901234567890"],
                screenWidth: 1200,
                screenHeight: 800,
                mouseX: 100,
                mouseY: 100,
                measureText: MeasureTenPixelsPerCharacter);

            Assert.Multiple(() =>
            {
                Assert.That(bounds.Width, Is.EqualTo(346));
                Assert.That(bounds.Height, Is.EqualTo(48));
                Assert.That(bounds.X, Is.EqualTo(116));
                Assert.That(bounds.Y, Is.EqualTo(116));
            });
        }

        [Test]
        public void EstimateConservativePrimaryBounds_WhenExactEstimationFails_UsesMaximumFrameworkGeometry()
        {
            SupplementalTooltipBounds bounds = ItemTooltipGeometryEstimator.EstimateConservativeBounds(
                screenWidth: 1200,
                screenHeight: 800,
                mouseX: 500,
                mouseY: 740);

            Assert.Multiple(() =>
            {
                Assert.That(bounds.X, Is.EqualTo(516));
                Assert.That(bounds.Y, Is.EqualTo(320));
                Assert.That(bounds.Width, Is.EqualTo(350));
                Assert.That(bounds.Height, Is.EqualTo(416));
            });
        }

        [Test]
        public void Resolve_WhenBelowFits_PrefersBelowPrimary()
        {
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 1200,
                screenHeight: 800,
                mouseX: 350,
                mouseY: 180,
                tooltipWidth: 200,
                tooltipHeight: 80,
                primaryBounds: new SupplementalTooltipBounds(400, 200, 300, 120),
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.Kind, Is.EqualTo(SupplementalTooltipPlacementKind.Below));
                Assert.That(position.X, Is.EqualTo(400));
                Assert.That(position.Y, Is.EqualTo(324));
            });
        }

        [Test]
        public void Resolve_WhenPrimaryFlippedAboveCursor_PlacesSupplementalBelowPrimary()
        {
            var primary = new SupplementalTooltipBounds(300, 300, 250, 196);
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 1200,
                screenHeight: 800,
                mouseX: 400,
                mouseY: 500,
                tooltipWidth: 200,
                tooltipHeight: 80,
                primaryBounds: primary,
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.Kind, Is.EqualTo(SupplementalTooltipPlacementKind.Below));
                Assert.That(position.X, Is.EqualTo(300));
                Assert.That(position.Y, Is.EqualTo(500));
                Assert.That(OverlapArea(position, 200, 80, primary), Is.Zero);
            });
        }

        [Test]
        public void Resolve_WhenBelowCannotFitAndAboveCan_UsesAboveWithoutClampingIntoPrimary()
        {
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 1000,
                screenHeight: 800,
                mouseX: 350,
                mouseY: 650,
                tooltipWidth: 200,
                tooltipHeight: 100,
                primaryBounds: new SupplementalTooltipBounds(300, 500, 300, 200),
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.Kind, Is.EqualTo(SupplementalTooltipPlacementKind.Above));
                Assert.That(position.X, Is.EqualTo(300));
                Assert.That(position.Y, Is.EqualTo(396));
            });
        }

        [Test]
        public void Resolve_WhenVerticalSidesCannotFit_UsesHorizontalSideWithoutOverlap()
        {
            var primary = new SupplementalTooltipBounds(250, 150, 300, 300);
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 900,
                screenHeight: 600,
                mouseX: 400,
                mouseY: 300,
                tooltipWidth: 200,
                tooltipHeight: 200,
                primaryBounds: primary,
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.Kind, Is.EqualTo(SupplementalTooltipPlacementKind.Right));
                Assert.That(position.X, Is.EqualTo(554));
                Assert.That(position.Y, Is.EqualTo(150));
                Assert.That(OverlapArea(position, 200, 200, primary), Is.Zero);
            });
        }

        [Test]
        public void Resolve_WhenNoNonOverlappingSideFits_UsesMinimumOverlapViewportPosition()
        {
            var primary = new SupplementalTooltipBounds(50, 50, 400, 300);
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 500,
                screenHeight: 400,
                mouseX: 250,
                mouseY: 200,
                tooltipWidth: 200,
                tooltipHeight: 150,
                primaryBounds: primary,
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.Kind, Is.EqualTo(SupplementalTooltipPlacementKind.MinimumOverlap));
                Assert.That(position.X, Is.EqualTo(4));
                Assert.That(position.Y, Is.EqualTo(4));
                Assert.That(OverlapArea(position, 200, 150, primary), Is.EqualTo(16016));
            });
        }

        [Test]
        public void Resolve_WhenMinimumOverlapTies_PrefersPositionThatDoesNotCoverCursor()
        {
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 500,
                screenHeight: 400,
                mouseX: 20,
                mouseY: 20,
                tooltipWidth: 200,
                tooltipHeight: 150,
                primaryBounds: new SupplementalTooltipBounds(50, 50, 400, 300),
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.Kind, Is.EqualTo(SupplementalTooltipPlacementKind.MinimumOverlap));
                Assert.That(position.X, Is.EqualTo(296));
                Assert.That(position.Y, Is.EqualTo(4));
            });
        }

        [Test]
        public void Resolve_TinyViewport_KeepsResultInsideAvailableArea()
        {
            SupplementalTooltipPosition position = SupplementalTooltipPlacement.Resolve(
                screenWidth: 10,
                screenHeight: 10,
                mouseX: 5,
                mouseY: 5,
                tooltipWidth: 100,
                tooltipHeight: 100,
                primaryBounds: new SupplementalTooltipBounds(4, 4, 1, 1),
                edgePadding: 4,
                gap: 4);

            Assert.Multiple(() =>
            {
                Assert.That(position.X, Is.EqualTo(4));
                Assert.That(position.Y, Is.EqualTo(4));
            });
        }

        private static int MeasureEightPixelsPerCharacter(string value)
        {
            return (value?.Length ?? 0) * 8;
        }

        private static int MeasureTenPixelsPerCharacter(string value)
        {
            return (value?.Length ?? 0) * 10;
        }

        private static long OverlapArea(
            SupplementalTooltipPosition position,
            int width,
            int height,
            SupplementalTooltipBounds other)
        {
            int left = position.X;
            int top = position.Y;
            int right = left + width;
            int bottom = top + height;
            int overlapWidth = Math.Max(0, Math.Min(right, other.Right) - Math.Max(left, other.Left));
            int overlapHeight = Math.Max(0, Math.Min(bottom, other.Bottom) - Math.Max(top, other.Top));

            return (long)overlapWidth * overlapHeight;
        }
    }
}