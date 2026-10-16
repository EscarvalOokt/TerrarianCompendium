using System;
using System.Collections.Generic;
using Escarval.TerrariaWindowKit;
using NUnit.Framework;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class VanillaUiHostCallbacksTests
    {
        [Test]
        public void FocusCallbacks_QueryLiveStateAndUseHostReleaseAction()
        {
            var state = new TestHostInputState();
            var releases = 0;
            WindowCallbacks callbacks = CreateCallbacks(
                state,
                releaseTextFocus: () =>
                {
                    releases++;
                    state.IsWritingText = false;
                });

            Assert.That(callbacks.HasTextFocus(), Is.False);
            state.IsWritingText = true;
            Assert.That(callbacks.HasTextFocus(), Is.True);

            callbacks.ReleaseTextFocus();

            Assert.That(releases, Is.EqualTo(1));
            Assert.That(callbacks.HasTextFocus(), Is.False);
        }

        [Test]
        public void PopupCallbacks_QueryLiveStateAndDelegateClosingToOwner()
        {
            var state = new TestHostInputState();
            WindowCallbacks callbacks = CreateCallbacks(state);

            Assert.That(callbacks.HasPopup(), Is.False);
            state.HasOpenTransientSurface = true;
            Assert.That(callbacks.HasPopup(), Is.True);

            callbacks.ClosePopup();

            Assert.That(state.PopupCloseCalls, Is.EqualTo(1));
            Assert.That(callbacks.HasPopup(), Is.False);

            state.HasOpenTransientSurface = true;
            Assert.That(callbacks.HasPopup(), Is.True);
        }

        [TestCase((int)BrowserWheelRegion.PassThrough, (int)WheelDisposition.PassThrough)]
        [TestCase((int)BrowserWheelRegion.Deliver, (int)WheelDisposition.Deliver)]
        [TestCase((int)BrowserWheelRegion.CaptureOnly, (int)WheelDisposition.CaptureOnly)]
        public void WheelCallback_MapsBrowserRegionToToolkitDisposition(int region, int expected)
        {
            var state = new TestHostInputState { WheelRegion = (BrowserWheelRegion)region };
            WindowCallbacks callbacks = CreateCallbacks(state);

            Assert.That(callbacks.WheelAt(180, 270), Is.EqualTo(expected));
            Assert.That(state.WheelQueries, Is.EqualTo(1));
            Assert.That(state.LastWheelX, Is.EqualTo(180));
            Assert.That(state.LastWheelY, Is.EqualTo(270));
        }

        [Test]
        public void WheelCallback_ClosedOrDetachedRootPassesThroughWithoutQueryingState()
        {
            var state = new TestHostInputState { WheelRegion = BrowserWheelRegion.Deliver };
            var isOpen = true;
            var isAttached = true;
            // These mutable captures intentionally verify live root state, not a cached snapshot.
            // ReSharper disable AccessToModifiedClosure
            WindowCallbacks callbacks = CreateCallbacks(state, canRouteWheel: () => isOpen && isAttached);
            // ReSharper restore AccessToModifiedClosure

            Assert.That(callbacks.WheelAt(10, 20), Is.EqualTo((int)WheelDisposition.Deliver));
            Assert.That(state.WheelQueries, Is.EqualTo(1));

            isOpen = false;
            Assert.That(callbacks.WheelAt(10, 20), Is.EqualTo((int)WheelDisposition.PassThrough));
            isOpen = true;
            isAttached = false;
            Assert.That(callbacks.WheelAt(10, 20), Is.EqualTo((int)WheelDisposition.PassThrough));
            Assert.That(state.WheelQueries, Is.EqualTo(1));

            isAttached = true;
            state.WheelRegion = BrowserWheelRegion.CaptureOnly;
            Assert.That(callbacks.WheelAt(30, 40), Is.EqualTo((int)WheelDisposition.CaptureOnly));
            Assert.That(state.WheelQueries, Is.EqualTo(2));
            Assert.That(state.LastWheelX, Is.EqualTo(30));
            Assert.That(state.LastWheelY, Is.EqualTo(40));
        }

        [Test]
        public void WheelCallback_UnknownRegionPassesThrough()
        {
            var state = new TestHostInputState { WheelRegion = (BrowserWheelRegion)99 };
            WindowCallbacks callbacks = CreateCallbacks(state);

            Assert.That(callbacks.WheelAt(10, 20), Is.EqualTo((int)WheelDisposition.PassThrough));
        }

        [Test]
        public void TransientClosing_UsesDetailsBeforeSectionAndStopsAfterOneSuccess()
        {
            var calls = new List<string>();
            var detailsOpen = true;
            var sectionOpen = true;
            Func<bool> closeDetails = () =>
            {
                calls.Add("details");
                bool wasOpen = detailsOpen;
                detailsOpen = false;
                return wasOpen;
            };
            Func<bool> closeSection = () =>
            {
                calls.Add("section");
                bool wasOpen = sectionOpen;
                sectionOpen = false;
                return wasOpen;
            };

            Assert.That(
                BrowserTransientClosing.CloseFirstAvailableTransientSurface(closeDetails, closeSection),
                Is.True);
            Assert.That(calls, Is.EqualTo(["details"]));
            Assert.That(sectionOpen, Is.True);

            Assert.That(
                BrowserTransientClosing.CloseFirstAvailableTransientSurface(closeDetails, closeSection),
                Is.True);
            Assert.That(calls, Is.EqualTo(["details", "details", "section"]));

            Assert.That(
                BrowserTransientClosing.CloseFirstAvailableTransientSurface(closeDetails, closeSection),
                Is.False);
            Assert.That(calls, Is.EqualTo(["details", "details", "section", "details", "section"]));
        }

        [Test]
        public void CleanupCallbacks_AreStillForwardedAfterRootBecomesInactive()
        {
            var state = new TestHostInputState
            {
                IsWritingText = true,
                HasOpenTransientSurface = true
            };
            var releaseCalls = 0;
            var closeCalls = 0;
            CloseReason? reason = null;
            WindowCallbacks callbacks = CreateCallbacks(
                state,
                releaseTextFocus: () =>
                {
                    releaseCalls++;
                    state.IsWritingText = false;
                },
                closed: value =>
                {
                    closeCalls++;
                    reason = value;
                },
                canRouteWheel: () => false);

            Assert.That(callbacks.WheelAt(10, 10), Is.EqualTo((int)WheelDisposition.PassThrough));
            callbacks.ReleaseTextFocus();
            callbacks.ClosePopup();
            callbacks.Closed(CloseReason.Escape);

            Assert.That(releaseCalls, Is.EqualTo(1));
            Assert.That(state.PopupCloseCalls, Is.EqualTo(1));
            Assert.That(closeCalls, Is.EqualTo(1));
            Assert.That(reason, Is.EqualTo(CloseReason.Escape));
            Assert.That(callbacks.HasTextFocus(), Is.False);
            Assert.That(callbacks.HasPopup(), Is.False);

            callbacks.ReleaseTextFocus();
            callbacks.ClosePopup();
            Assert.That(releaseCalls, Is.EqualTo(2));
            Assert.That(state.PopupCloseCalls, Is.EqualTo(2));
        }

        [Test]
        public void DrawAndBoundsCallbacks_UseProvidedHandlers()
        {
            var drawCount = 0;
            var boundChanges = 0;
            WindowBounds? lastBounds = null;
            WindowCallbacks callbacks = CreateCallbacks(
                new TestHostInputState(),
                draw: () => drawCount++,
                boundsChanged: bounds =>
                {
                    boundChanges++;
                    lastBounds = bounds;
                });
            var boundsToApply = new WindowBounds(20, 30, 720, 480);

            callbacks.Draw();
            callbacks.BoundsChanged(boundsToApply);

            Assert.That(drawCount, Is.EqualTo(1));
            Assert.That(boundChanges, Is.EqualTo(1));
            Assert.That(lastBounds, Is.EqualTo(boundsToApply));
        }

        [Test]
        public void BackendKeyboardAndWheelPriorityDefaults_AreNotOverridden()
        {
            WindowCallbacks callbacks = CreateCallbacks(new TestHostInputState());

            Assert.That((object)callbacks.CanReceiveKeyboard, Is.Null);
            Assert.That((object)callbacks.WheelInputBlocked, Is.Null);
        }

        private static WindowCallbacks CreateCallbacks(
            TestHostInputState state,
            Action releaseTextFocus = null,
            Action<CloseReason> closed = null,
            Action draw = null,
            Action<WindowBounds> boundsChanged = null,
            Func<bool> canRouteWheel = null)
        {
            return VanillaUiHost.CreateWindowCallbacks(
                state,
                draw ?? (() => { }),
                boundsChanged ?? (_ => { }),
                closed ?? (_ => { }),
                releaseTextFocus ?? (() => { }),
                canRouteWheel ?? (() => true));
        }

        private sealed class TestHostInputState : IBrowserHostInputState
        {
            public BrowserWheelRegion WheelRegion { get; set; }

            public int WheelQueries { get; private set; }

            public int LastWheelX { get; private set; }

            public int LastWheelY { get; private set; }

            public int PopupCloseCalls { get; private set; }
            public bool IsWritingText { get; set; }

            public bool HasOpenTransientSurface { get; set; }

            public BrowserWheelRegion GetWheelRegion(int x, int y)
            {
                WheelQueries++;
                LastWheelX = x;
                LastWheelY = y;
                return WheelRegion;
            }

            public bool TryCloseTransientSurface()
            {
                PopupCloseCalls++;
                bool wasOpen = HasOpenTransientSurface;
                HasOpenTransientSurface = false;
                return wasOpen;
            }
        }
    }
}