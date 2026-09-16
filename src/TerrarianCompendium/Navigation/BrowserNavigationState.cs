using System.Collections.Generic;

namespace TerrarianCompendium.Navigation
{
    internal sealed class BrowserNavigationState
    {
        private readonly List<BrowserDestination> _history =
        [
            BrowserDestination.ForSection(BrowserSection.Items)
        ];

        private int _historyIndex;
        private long _revision;

        public BrowserDestination CurrentDestination => _history[_historyIndex];

        public bool CanGoBack => _historyIndex > 0;

        public bool CanGoForward => _historyIndex < _history.Count - 1;

        public long Revision => _revision;

        public bool Navigate(BrowserDestination destination)
        {
            if (CurrentDestination == destination)
                return false;

            if (CanGoForward)
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);

            _history.Add(destination);
            _historyIndex++;
            _revision++;

            return true;
        }

        public bool ReplaceCurrent(BrowserDestination destination)
        {
            if (CurrentDestination == destination)
                return false;

            _history[_historyIndex] = destination;

            if (_historyIndex > 0 && _history[_historyIndex - 1] == destination)
            {
                _history.RemoveAt(_historyIndex);
                _historyIndex--;
            }

            if (_historyIndex < _history.Count - 1 && _history[_historyIndex + 1] == destination)
                _history.RemoveAt(_historyIndex + 1);

            _revision++;
            return true;
        }

        public bool GoBack()
        {
            if (!CanGoBack)
                return false;

            _historyIndex--;
            _revision++;

            return true;
        }

        public bool GoForward()
        {
            if (!CanGoForward)
                return false;

            _historyIndex++;
            _revision++;

            return true;
        }
    }
}