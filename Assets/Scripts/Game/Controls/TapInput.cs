using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Controls
{
    public class TapInput : MonoBehaviour
    {
        public bool IsPressed { get; private set; }

        public event Action OnPressed;
        public event Action OnReleased;

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                IsPressed = true;
                OnPressed?.Invoke();
            }
            else if (pointer.press.wasReleasedThisFrame && IsPressed)
            {
                IsPressed = false;
                OnReleased?.Invoke();
            }
        }
    }
}
