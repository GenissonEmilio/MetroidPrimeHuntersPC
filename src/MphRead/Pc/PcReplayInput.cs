using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    // In-process verification input. It never sends events to the Windows desktop.
    internal sealed class PcReplayInput
    {
        private HashSet<Keys> _previous = new();
        private HashSet<Keys> _current = new();
        private HashSet<MouseButton> _previousMouse = new();
        private HashSet<MouseButton> _currentMouse = new();
        public Vector2 LookDelta { get; private set; }
        public bool KeyDown(Keys key) => _current.Contains(key);
        public bool KeyWasDown(Keys key) => _previous.Contains(key);
        public bool MouseDown(MouseButton button) => _currentMouse.Contains(button);
        public bool MouseWasDown(MouseButton button) => _previousMouse.Contains(button);
        public void PressMouse(MouseButton button) => _currentMouse.Add(button);
        public void Set(Vector2 look = default, params Keys[] keys)
        {
            _previous = _current;
            _current = new(keys);
            _previousMouse = _currentMouse;
            _currentMouse = new();
            LookDelta = look;
        }
    }
}
