using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossHop.Input
{
    /// <summary>Grid-aligned hop directions the player can request.</summary>
    public enum HopDirection { Forward, Back, Left, Right }

    /// <summary>
    /// Translates raw input into discrete hop requests and raises an event.
    /// On device: swipes (and a tap = forward). In the editor: WASD / arrow keys.
    /// Systems subscribe to <see cref="OnHop"/> so input has no gameplay knowledge.
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        [Tooltip("Minimum finger travel (screen pixels) to register a swipe vs. a tap.")]
        [SerializeField] private float swipeThreshold = 40f;

        /// <summary>Raised once per discrete hop request.</summary>
        public event Action<HopDirection> OnHop;

        private Vector2 _touchStart;
        private bool _tracking;

        private void Update()
        {
            ReadKeyboard();
            ReadTouch();
        }

        private void ReadKeyboard()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                OnHop?.Invoke(HopDirection.Forward);
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                OnHop?.Invoke(HopDirection.Back);
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
                OnHop?.Invoke(HopDirection.Left);
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
                OnHop?.Invoke(HopDirection.Right);
        }

        private void ReadTouch()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts == null) return;

            TouchControl touch = ts.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                _touchStart = touch.position.ReadValue();
                _tracking = true;
            }
            else if (touch.press.wasReleasedThisFrame && _tracking)
            {
                _tracking = false;
                ResolveSwipe(touch.position.ReadValue() - _touchStart);
            }
        }

        private void ResolveSwipe(Vector2 delta)
        {
            if (delta.magnitude < swipeThreshold)
            {
                // A tap means "go forward" — the one-thumb default action.
                OnHop?.Invoke(HopDirection.Forward);
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                OnHop?.Invoke(delta.x > 0 ? HopDirection.Right : HopDirection.Left);
            else
                OnHop?.Invoke(delta.y > 0 ? HopDirection.Forward : HopDirection.Back);
        }
    }
}
