using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Nova
{
    /// <summary>
    /// Disables mouse when any touch starts and for a short time after it ends.
    /// On Windows, there is a virtual mouse driven by the touch, and we need to disable it.
    /// Also, the pointer up event is not invoked, so we need to save each pointer down event and invoke the
    /// corresponding pointer up event.
    /// TODO: OnPointerUp should be invoked before Update
    /// </summary>
    public class TouchPointerFix : MonoBehaviour
    {
        // The values in PointerEventData may change after the event is processed, so we need to save them
        // TODO: More properties of ExtendedPointerEventData
        private readonly struct SavedEventData
        {
            private readonly PointerEventData.InputButton button;
            private readonly RaycastResult pointerCurrentRaycast;
            private readonly GameObject pointerEnter;
            private readonly int pointerId;
            private readonly Vector2 position;

            private readonly InputDevice device;
            private readonly UIPointerType pointerType;
            private readonly int touchId;

            public SavedEventData(ExtendedPointerEventData eventData)
            {
                button = eventData.button;
                pointerCurrentRaycast = eventData.pointerCurrentRaycast;
                pointerEnter = eventData.pointerEnter;
                pointerId = eventData.pointerId;
                position = eventData.position;

                device = eventData.device;
                pointerType = eventData.pointerType;
                touchId = eventData.touchId;
            }

            public static explicit operator ExtendedPointerEventData(SavedEventData savedData)
            {
                var eventData = new ExtendedPointerEventData(EventSystem.current)
                {
                    button = savedData.button,
                    pointerCurrentRaycast = savedData.pointerCurrentRaycast,
                    pointerEnter = savedData.pointerEnter,
                    pointerId = savedData.pointerId,
                    position = savedData.position,

                    eligibleForClick = true,
                    pointerPress = savedData.pointerEnter,
                    pointerPressRaycast = savedData.pointerCurrentRaycast,
                    pressPosition = savedData.position,

                    device = savedData.device,
                    pointerType = savedData.pointerType,
                    touchId = savedData.touchId
                };

                return eventData;
            }
        }

        private static TouchPointerFix Current;

        public static bool Skip(ExtendedPointerEventData eventData)
        {
            if (Current == null || eventData.pointerType == UIPointerType.Touch)
            {
                return false;
            }

            return !Current.mouseEnabled;
        }

        public static bool SkipOrAdd(IPointerUpHandler handler, ExtendedPointerEventData eventData)
        {
            if (Current == null)
            {
                return false;
            }

            if (eventData.pointerType == UIPointerType.Touch)
            {
                Current.AddPointerDown(handler, eventData);
                return false;
            }
            else
            {
                return !Current.mouseEnabled;
            }
        }

        public float waitSeconds = 0.1f;

        private bool mouseEnabled = true;
        private float idleTime;

        private readonly Dictionary<IPointerUpHandler, SavedEventData> pointerDownEvents =
            new Dictionary<IPointerUpHandler, SavedEventData>();

        private void Awake()
        {
            Current = this;
        }

        private void Update()
        {
            if (Mouse.current == null)
            {
                return;
            }

            if (mouseEnabled)
            {
                // Disable mouse when touch is detected
                if (Touch.activeTouches.Count > 0)
                {
                    mouseEnabled = false;
                    idleTime = 0.0f;
                }
            }
            else
            {
                // Enable mouse after an idle time
                if (Touch.activeTouches.Count == 0)
                {
                    idleTime += Time.unscaledDeltaTime;
                    if (idleTime > waitSeconds)
                    {
                        mouseEnabled = true;
                    }
                }
            }

            if (Touch.activeTouches.Count == 0)
            {
                InvokeAllPointerUp();
            }
        }

        private void AddPointerDown(IPointerUpHandler handler, ExtendedPointerEventData eventData)
        {
            if (pointerDownEvents.ContainsKey(handler))
            {
                // TODO: Do we need to invoke OnPointerUp on all components?
                handler.OnPointerUp((ExtendedPointerEventData)pointerDownEvents[handler]);
            }

            pointerDownEvents[handler] = new SavedEventData(eventData);
        }

        private void InvokeAllPointerUp()
        {
            if (pointerDownEvents.Count == 0)
            {
                return;
            }

            foreach (var pair in pointerDownEvents)
            {
                var go = ((MonoBehaviour)pair.Key).gameObject;
                var eventData = (ExtendedPointerEventData)pair.Value;
                ExecuteEvents.Execute<IPointerUpHandler>(go, eventData, (x, y) => x.OnPointerUp((PointerEventData)y));
            }

            pointerDownEvents.Clear();
        }
    }
}
