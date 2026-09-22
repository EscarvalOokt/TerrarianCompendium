using System;
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
    internal enum BrowserEscapeAction
    {
        None,
        CloseTransientSurface,
        CloseBrowser
    }

    internal sealed class VanillaUiHost : IDisposable
    {
        private readonly string _keyInputBlockId;
        private readonly CompendiumLocalization _localization;
        private readonly string _panelId;
        private readonly BrowserUiState _state;
        private readonly UserInterface _userInterface;
        private bool _boundsRegistered;
        private bool _coreTextInputEnabled;
        private bool _drawRegistered;
        private bool _earlyThirdPartyTickHandled;
        private bool _escapeReleasePending;
        private bool _isOpen;
        private bool _keyInputBlockRegistered;
        private BrowserEscapeAction _pendingEscapeAction;

        public VanillaUiHost(string panelId, CompendiumLocalization localization, Func<BrowserUiState> stateFactory)
        {
            if (string.IsNullOrWhiteSpace(panelId))
                throw new ArgumentException("Panel ID must not be empty.", nameof(panelId));

            if (stateFactory == null)
                throw new ArgumentNullException(nameof(stateFactory));

            _panelId = panelId;
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _keyInputBlockId = panelId + ".keyboard";
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;

            try
            {
                _userInterface = new UserInterface();
                _state = stateFactory();
                _state.CloseRequested += Close;
                _state.InteractionStarted += BringToFront;
            }
            finally
            {
                UserInterface.ActiveInstance = previousActiveInterface;
            }
        }

        public bool IsOpen => _isOpen;

        public void Dispose()
        {
            Unregister();
        }

        public void Register()
        {
            if (_drawRegistered)
                return;

            UIRenderer.RegisterPanelDraw(_panelId, Draw);
            _drawRegistered = true;
        }

        public void Toggle()
        {
            if (_isOpen)
                Close();
            else
                Open();
        }

        public void BeginUpdateFrame()
        {
            _earlyThirdPartyTickHandled = false;
            ReleaseEscapeBlockIfPossible();
            _pendingEscapeAction = BrowserEscapeAction.None;

            if (!_isOpen || _escapeReleasePending || _state.IsWritingText)
                return;

            if (UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId))
                return;

            if (!InputState.IsKeyJustPressed(KeyCode.Escape))
                return;

            _pendingEscapeAction = ResolveEscapeAction(
                isWritingText: false,
                hasOpenTransientSurface: _state.HasOpenTransientSurface);

            if (_pendingEscapeAction == BrowserEscapeAction.None)
                return;

            RegisterKeyInputBlock();
            _escapeReleasePending = true;
        }

        public void HandleThirdPartySoftwareTick()
        {
            if (_earlyThirdPartyTickHandled)
                return;

            _earlyThirdPartyTickHandled = true;

            if (!_isOpen)
            {
                ReleaseCoreTextInput();
                return;
            }

            if (UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId))
            {
                _pendingEscapeAction = BrowserEscapeAction.None;
                ReleaseCoreTextInput();
                return;
            }

            if (_state.IsWritingText)
            {
                _pendingEscapeAction = BrowserEscapeAction.None;
                MaintainCoreTextInput();
                return;
            }

            ReleaseCoreTextInput();

            BrowserEscapeAction escapeAction = _pendingEscapeAction;
            _pendingEscapeAction = BrowserEscapeAction.None;

            switch (escapeAction)
            {
                case BrowserEscapeAction.None:
                    return;

                case BrowserEscapeAction.CloseTransientSurface:
                    _state.TryCloseTransientSurface();
                    return;

                case BrowserEscapeAction.CloseBrowser:
                    CloseCore(preserveEscapeBlock: true);
                    return;

                default:
                    throw new InvalidOperationException("Unsupported browser Escape action.");
            }
        }

        public void Update()
        {
            if (!_isOpen)
            {
                ReleaseEscapeBlockIfPossible();
                return;
            }

            int capturedScroll = UIRenderer.ScrollWheel;
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;
            var temporarilyRemovedOwnBounds = false;

            try
            {
                PlayerInput.SetZoom_UI();
                _userInterface.Use();

                bool blockedByHigherPriorityPanel = UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId);
                _state.SetExternalInputBlocked(blockedByHigherPriorityPanel);

                Rectangle bounds = _state.Bounds;
                bool pointerOverPanel = bounds.Contains(new Point(Main.mouseX, Main.mouseY));

                if (!blockedByHigherPriorityPanel && _boundsRegistered)
                {
                    UnregisterBounds();
                    temporarilyRemovedOwnBounds = true;
                }

                PlayerInput.ScrollWheelDeltaForUI =
                    !blockedByHigherPriorityPanel && pointerOverPanel ? capturedScroll : 0;

                _userInterface.Update(Main.gameTimeCache);
            }
            finally
            {
                PlayerInput.ScrollWheelDeltaForUI = 0;
                PlayerInput.SetZoom_Unscaled();
                UserInterface.ActiveInstance = previousActiveInterface;

                if (_isOpen && (temporarilyRemovedOwnBounds || !_boundsRegistered))
                    RegisterCurrentBounds();

                SyncKeyInputBlock();
            }
        }

        public void Close()
        {
            CloseCore(preserveEscapeBlock: false);
        }

        public void Unregister()
        {
            CloseCore(preserveEscapeBlock: false);

            if (!_drawRegistered)
                return;

            UIRenderer.UnregisterPanelDraw(_panelId);
            _drawRegistered = false;
        }

        internal static BrowserEscapeAction ResolveEscapeAction(bool isWritingText, bool hasOpenTransientSurface)
        {
            if (isWritingText)
                return BrowserEscapeAction.None;

            return hasOpenTransientSurface
                ? BrowserEscapeAction.CloseTransientSurface
                : BrowserEscapeAction.CloseBrowser;
        }

        private void Open()
        {
            if (_isOpen)
                return;

            if (!_drawRegistered)
                Register();

            UserInterface previousActiveInterface = UserInterface.ActiveInstance;

            try
            {
                PlayerInput.SetZoom_UI();
                _userInterface.Use();
                _userInterface.SetState(_state);
            }
            finally
            {
                PlayerInput.SetZoom_Unscaled();
                UserInterface.ActiveInstance = previousActiveInterface;
            }

            _isOpen = true;
            RegisterCurrentBounds();
            UIRenderer.BringToFront(_panelId);
            SyncKeyInputBlock();
        }

        private void CloseCore(bool preserveEscapeBlock)
        {
            _pendingEscapeAction = BrowserEscapeAction.None;
            ReleaseCoreTextInput();

            bool wasOpen = _isOpen;
            _isOpen = false;
            _state.SetExternalInputBlocked(false);

            if (wasOpen)
            {
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
            }

            UnregisterBounds();

            if (preserveEscapeBlock && _keyInputBlockRegistered)
            {
                _escapeReleasePending = true;
                return;
            }

            UnregisterKeyInputBlock();
        }

        private void BringToFront()
        {
            if (_isOpen)
                UIRenderer.BringToFront(_panelId);
        }

        private void Draw()
        {
            if (!_isOpen)
            {
                ReleaseCoreTextInput();
                return;
            }

            SyncCoreTextInput(UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId));
            UserInterface previousActiveInterface = UserInterface.ActiveInstance;

            try
            {
                Tooltip.Clear();
                SupplementalTooltip.Clear();
                _userInterface.Draw(Main.spriteBatch, Main.gameTimeCache);
                Tooltip.DrawDeferred();
                SupplementalTooltip.DrawDeferred(_localization);
            }
            finally
            {
                UserInterface.ActiveInstance = previousActiveInterface;
                RegisterCurrentBounds();
                SyncKeyInputBlock();
                SyncCoreTextInput(UIRenderer.ShouldBlockForHigherPriorityPanel(_panelId));
            }
        }

        private void RegisterCurrentBounds()
        {
            if (!_isOpen)
                return;

            Rectangle bounds = _state.Bounds;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            UIRenderer.RegisterPanelBounds(_panelId, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            _boundsRegistered = true;
        }

        private void UnregisterBounds()
        {
            if (!_boundsRegistered)
                return;

            UIRenderer.UnregisterPanelBounds(_panelId);
            _boundsRegistered = false;
        }

        private void SyncCoreTextInput(bool blockedByHigherPriorityPanel)
        {
            if (_isOpen && !blockedByHigherPriorityPanel && _state.IsWritingText)
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
            bool writingText = _isOpen && _state.IsWritingText;

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
            else if (_isOpen && _keyInputBlockRegistered && InputState.IsKeyDown(KeyCode.Escape))
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

            if (_isOpen && _state.IsWritingText)
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