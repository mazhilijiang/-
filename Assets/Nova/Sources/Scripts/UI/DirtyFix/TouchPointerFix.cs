using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Nova
{
    /// <summary>
    /// On Windows, there is a virtual mouse driven by the touch, and pointer up, pointer click, and drag events may not
    /// be triggered by the touch. So we remove touch press in UIInputModule.inputactions, and use this component to
    /// trigger the events.
    /// </summary>
    public class TouchPointerFix : MonoBehaviour
    {
        private void Awake()
        {
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp += OnFingerUp;
        }

        private void OnFingerDown(Finger finger)
        {
            Debug.Log($"OnFingerDown {finger}");
        }

        private void OnFingerUp(Finger finger)
        {
            Debug.Log($"OnFingerUp {finger}");
        }
    }
}
