using System;
using System.Collections.Generic;
using Escarval.TerrariaWindowKit;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;
using TerrariaModder.Core.Input;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaUiHost : IDisposable
    {
        private const string WindowId = "browser";
        private readonly string _keyInputBlockId;
        private readonly CompendiumLocalization _localization;
        private readonly int _minimumHeight;
        private readonly int _minimumWidth;
        private readonly string _modId;
        private readonly string _panelId;
        private readonly BrowserUiState _state;
        private readonly UserInterface _userInterface;
        private CoreWindowBackend _backend;
        private SharedCoordinator _coordinator;
        private bool _coreTextInputEnabled;
        private bool _disposed;
        private bool _earlyThirdPartyTickHandled;
        private bool _escapeReleasePending;
        private bool _keyInputBlockRegistered;
        private bool _stateAttached;
        private WindowHost _window;

        public VanillaUiHost(
            string panelId,
            CompendiumLocalization localization,
            Func<BrowserUiState> stateFactory,
            int minimumWidth,
            int minimumHeight)
        {
            if (string.IsNullOrWhiteSpace(panelId))
                throw new ArgumentException("Panel ID must not be empty.", nameof(panelId));

            if (stateFactory == null)
                throw new ArgumentNullException(nameof(stateFactory));
            if (minimumWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumWidth));
            if (minimumHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumHeight));

            _modId = panelId;
            _panelId = panelId + ":" + WindowId;
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _minimumWidth = minimumWidth;
            _minimumHeight = minimumHeight;
            _keyInputBlockId = panelId + ".keyboard";
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;

            try
            {
                _userInterface = new UserInterface();
                _state = stateFactory();
                _state.CloseRequested += Close;
                _state.InteractionStarted += BringToFront;
                _state.DragRequested += BeginDrag;
                _state.ResizeRequested += BeginResize;
                _state.PinToggleRequested += TogglePin;
                _state.SetIsResizingProvider(() => _window?.IsResizing == true);
                _state.SetIsPinnedProvider(() => _window?.IsPinned == true);
            }
            finally
            {
                UserInterface.ActiveInstance = previousActiveInterface;
            }
        }

        public bool IsOpen => _window?.IsOpen == true;

        public void Dispose()
        {
            Unregister();
        }

        public void Register()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VanillaUiHost));
            if (_window != null)
                return;

            try
            {
                _coordinator = SharedCoordinator.Connect();
                _backend = new CoreWindowBackend(_coordinator);
                Rectangle initialBounds = _state.InitialBounds;
                var options = new WindowOptions(
                    new WindowBounds(initialBounds.X, initialBounds.Y, initialBounds.Width, initialBounds.Height))
                {
                    MinimumWidth = _minimumWidth,
                    MinimumHeight = _minimumHeight,
                    Draggable = true,
                    Resizable = true,
                    Pinnable = true
                };
                WindowCallbacks callbacks = CreateWindowCallbacks(
                    _state,
                    Draw,
                    OnBoundsChanged,
                    OnClosed,
                    ReleaseTextFocus,
                    () => IsOpen && _stateAttached);
                _window = new WindowHost(_modId, WindowId, options, _backend, callbacks);
            }
            catch (Exception failure)
            {
                try
                {
                    SharedCoordinator.RunAll(
                    [
                        () => _window?.Dispose(),
                        () => _backend?.Dispose()
                    ]);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(failure, cleanup);
                }
                finally
                {
                    _window = null;
                    _backend = null;
                    _coordinator = null;
                }

                throw;
            }
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void BeginUpdateFrame()
        {
            _earlyThirdPartyTickHandled = false;
            ReleaseEscapeBlockIfPossible();
        }

        public void HandleThirdPartySoftwareTick()
        {
            if (_earlyThirdPartyTickHandled)
                return;

            _earlyThirdPartyTickHandled = true;

            if (!IsOpen)
            {
                ReleaseCoreTextInput();
                return;
            }

            if (UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId))
            {
                ReleaseCoreTextInput();
                return;
            }

            if (_state.IsWritingText)
            {
                MaintainCoreTextInput();
                return;
            }

            ReleaseCoreTextInput();
        }

        public void Update()
        {
            if (!IsOpen || !_stateAttached)
            {
                ReleaseEscapeBlockIfPossible();
                return;
            }

            GetUpdateMouse(out int mouseX, out int mouseY);
            int vanillaWheel = PlayerInput.ScrollWheelDeltaForUI;
            int capturedScroll = UIRenderer.ScrollWheel;
            var deliveredWheel = 0;
            var passThroughToVanilla = true;

            if (capturedScroll != 0 || vanillaWheel != 0)
            {
                IDictionary<string, object> selection = _coordinator.SelectWheel(mouseX, mouseY);
                var owner = selection["ownerId"] as string;
                var disposition = (int)selection["disposition"];
                passThroughToVanilla = owner == null && disposition == (int)WheelDisposition.PassThrough;

                if (string.Equals(owner, _window.Id, StringComparison.Ordinal) &&
                    disposition == (int)WheelDisposition.Deliver)
                {
                    deliveredWheel = capturedScroll;
                }
            }

            UserInterface previousActiveInterface = UserInterface.ActiveInstance;

            try
            {
                PlayerInput.SetZoom_UI();
                _userInterface.Use();

                bool blocked = _backend.IsInputBlockedAt(_window.Id, mouseX, mouseY);
                _state.SetExternalInputBlocked(blocked);

                bool updated = _window.TryUpdateContent(
                    mouseX,
                    mouseY,
                    () =>
                    {
                        PlayerInput.ScrollWheelDeltaForUI = deliveredWheel;
                        _userInterface.Update(Main.gameTimeCache);
                    });
                if (!updated)
                    ReleaseCoreTextInput();
            }
            finally
            {
                // The game's native UI receives its pending wheel on the next update.
                PlayerInput.ScrollWheelDeltaForUI = passThroughToVanilla ? vanillaWheel : 0;
                PlayerInput.SetZoom_Unscaled();
                UserInterface.ActiveInstance = previousActiveInterface;
                SyncKeyInputBlock();
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                DetachInterface();
                return;
            }

            _window.Close();
        }

        public void Unregister()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                SharedCoordinator.RunAll(
                [
                    () => _window?.Dispose(),
                    DetachInterface,
                    () => _backend?.Dispose()
                ]);
            }
            finally
            {
                _window = null;
                _backend = null;
                _coordinator = null;
            }
        }

        private void Open()
        {
            if (IsOpen)
                return;

            Register();
            if (!_window.Open())
                return;

            try
            {
                UserInterface previousActiveInterface = UserInterface.ActiveInstance;

                try
                {
                    PlayerInput.SetZoom_UI();
                    _userInterface.Use();
                    _stateAttached = true;
                    _userInterface.SetState(_state);
                }
                finally
                {
                    PlayerInput.SetZoom_Unscaled();
                    UserInterface.ActiveInstance = previousActiveInterface;
                }

                // Open need not change the already fitted geometry and trigger BoundsChanged.
                OnBoundsChanged(_window.Bounds);
                SyncKeyInputBlock();
            }
            catch (Exception failure)
            {
                try
                {
                    _window.Close();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(failure, cleanup);
                }

                throw;
            }
        }

        private void OnClosed(CloseReason reason)
        {
            DetachInterface();
        }

        private void DetachInterface()
        {
            bool wasAttached = _stateAttached;
            _stateAttached = false;
            _state.SetExternalInputBlocked(false);

            SharedCoordinator.RunAll(
            [
                ReleaseCoreTextInput,
                SupplementalTooltip.Clear,
                () =>
                {
                    if (!wasAttached)
                        return;

                    UserInterface previousActiveInterface = UserInterface.ActiveInstance;

                    try
                    {
                        PlayerInput.SetZoom_UI();
                        _userInterface.Use();
                        _userInterface.SetState(null);
                    }
                    finally
                    {
                        PlayerInput.SetZoom_Unscaled();
                        UserInterface.ActiveInstance = previousActiveInterface;
                    }
                },
                UnregisterKeyInputBlock
            ]);
        }

        private void ReleaseTextFocus()
        {
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;
            bool wasWritingText = _state.IsWritingText;

            try
            {
                if (wasWritingText)
                {
                    PlayerInput.SetZoom_UI();
                    _userInterface.Use();
                    _state.ReleaseTextFocus();
                }
            }
            finally
            {
                if (wasWritingText)
                    PlayerInput.SetZoom_Unscaled();

                UserInterface.ActiveInstance = previousActiveInterface;
                ReleaseCoreTextInput();
                SyncKeyInputBlock();
            }
        }

        private void BringToFront()
        {
            if (IsOpen)
                _window.Activate();
        }

        private void BeginDrag(int x, int y)
        {
            _window?.BeginDrag(x, y);
        }

        private void BeginResize(int x, int y)
        {
            _window?.BeginResize(x, y);
        }

        private void TogglePin()
        {
            if (IsOpen)
                _window.SetPinned(!_window.IsPinned);
        }

        private void OnBoundsChanged(WindowBounds bounds)
        {
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;
            int previousMouseX = Main.mouseX;
            int previousMouseY = Main.mouseY;
            int previousLastMouseX = Main.lastMouseX;
            int previousLastMouseY = Main.lastMouseY;
            int previousScreenWidth = Main.screenWidth;
            int previousScreenHeight = Main.screenHeight;

            try
            {
                PlayerInput.SetZoom_UI();
                _userInterface.Use();
                _state.ApplyBounds(new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
            }
            finally
            {
                Main.mouseX = previousMouseX;
                Main.mouseY = previousMouseY;
                Main.lastMouseX = previousLastMouseX;
                Main.lastMouseY = previousLastMouseY;
                Main.screenWidth = previousScreenWidth;
                Main.screenHeight = previousScreenHeight;
                UserInterface.ActiveInstance = previousActiveInterface;
            }
        }

        internal static WindowCallbacks CreateWindowCallbacks(
            IBrowserHostInputState state,
            Action draw,
            Action<WindowBounds> onBoundsChanged,
            Action<CloseReason> onClosed,
            Action releaseTextFocus,
            Func<bool> canRouteWheel)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (draw == null)
                throw new ArgumentNullException(nameof(draw));
            if (onBoundsChanged == null)
                throw new ArgumentNullException(nameof(onBoundsChanged));
            if (onClosed == null)
                throw new ArgumentNullException(nameof(onClosed));
            if (releaseTextFocus == null)
                throw new ArgumentNullException(nameof(releaseTextFocus));
            if (canRouteWheel == null)
                throw new ArgumentNullException(nameof(canRouteWheel));

            return new WindowCallbacks
            {
                Draw = draw,
                BoundsChanged = onBoundsChanged,
                Closed = onClosed,
                HasTextFocus = () => state.IsWritingText,
                ReleaseTextFocus = releaseTextFocus,
                HasPopup = () => state.HasOpenTransientSurface,
                ClosePopup = () => state.TryCloseTransientSurface(),
                WheelAt = (x, y) => canRouteWheel()
                    ? ToWheelDisposition(state.GetWheelRegion(x, y))
                    : (int)WheelDisposition.PassThrough
            };
        }

        private static int ToWheelDisposition(BrowserWheelRegion region)
        {
            return region switch
            {
                BrowserWheelRegion.Deliver => (int)WheelDisposition.Deliver,
                BrowserWheelRegion.CaptureOnly => (int)WheelDisposition.CaptureOnly,
                _ => (int)WheelDisposition.PassThrough
            };
        }

        private static void GetUpdateMouse(out int x, out int y)
        {
            float scale = Main.UIScale;

            if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
                scale = 1f;

            x = (int)(PlayerInput.MouseX / scale);
            y = (int)(PlayerInput.MouseY / scale);
        }

        private void Draw()
        {
            if (!IsOpen || !_stateAttached)
            {
                SupplementalTooltip.Clear();
                ReleaseCoreTextInput();
                return;
            }

            _window.UpdatePointer(Main.mouseX, Main.mouseY, Main.mouseLeft, WidgetInput.BlockInput);
            SyncCoreTextInput(UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId));
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;

            try
            {
                Tooltip.Clear();
                SupplementalTooltip.Clear();
                _userInterface.Draw(Main.spriteBatch, Main.gameTimeCache);

                if (!WidgetInput.BlockInput)
                {
                    Tooltip.DrawDeferred();
                    SupplementalTooltip.DrawDeferred(_localization);
                }
                else
                {
                    Tooltip.Clear();
                }
            }
            finally
            {
                SupplementalTooltip.Clear();
                UserInterface.ActiveInstance = previousActiveInterface;
                SyncKeyInputBlock();
                SyncCoreTextInput(UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId));
            }
        }

        private void SyncCoreTextInput(bool blockedByHigherPriorityPanel)
        {
            if (IsOpen && !blockedByHigherPriorityPanel && _state.IsWritingText)
            {
                MaintainCoreTextInput();
                return;
            }

            ReleaseCoreTextInput();
        }

        private void MaintainCoreTextInput()
        {
            UIRenderer.EnableTextInput();
            _coreTextInputEnabled = true;
        }

        private void ReleaseCoreTextInput()
        {
            if (!_coreTextInputEnabled)
                return;

            UIRenderer.DisableTextInput();
            _coreTextInputEnabled = false;
        }

        private void SyncKeyInputBlock()
        {
            bool writingText = IsOpen && _state.IsWritingText;

            if (writingText)
            {
                _escapeReleasePending = false;
                RegisterKeyInputBlock();
                return;
            }

            if (_escapeReleasePending)
            {
                if (InputState.IsKeyDown(KeyCode.Escape))
                    return;

                _escapeReleasePending = false;
            }
            else if (IsOpen && _keyInputBlockRegistered && InputState.IsKeyDown(KeyCode.Escape))
            {
                _escapeReleasePending = true;
                return;
            }

            UnregisterKeyInputBlock();
        }

        private void ReleaseEscapeBlockIfPossible()
        {
            if (!_escapeReleasePending || InputState.IsKeyDown(KeyCode.Escape))
                return;

            _escapeReleasePending = false;

            if (IsOpen && _state.IsWritingText)
                return;

            UnregisterKeyInputBlock();
        }

        private void RegisterKeyInputBlock()
        {
            if (_keyInputBlockRegistered)
                return;

            UIRenderer.RegisterKeyInputBlock(_keyInputBlockId);
            _keyInputBlockRegistered = true;
        }

        private void UnregisterKeyInputBlock()
        {
            _escapeReleasePending = false;

            if (!_keyInputBlockRegistered)
                return;

            UIRenderer.UnregisterKeyInputBlock(_keyInputBlockId);
            _keyInputBlockRegistered = false;
        }
    }
}