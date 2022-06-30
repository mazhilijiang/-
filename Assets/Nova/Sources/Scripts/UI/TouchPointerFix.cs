using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Nova
{
    /// <summary>
    /// Disables mouse when any touch starts and for a short time after it ends.
    /// On Windows, there is a virtual mouse driven by the touch, and we need to disable it.
    /// The pointer up event and the drag event may not be invoked, so we need to save each pointer down event
    /// and invoke the corresponding pointer up and drag events.
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
                    position = RealInput.pointerPosition,

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
            if (Application.isMobilePlatform || Current == null || eventData.pointerType == UIPointerType.Touch)
            {
                return false;
            }

            return !Current.mouseEnabled;
        }

        public static bool SkipOrAdd(IPointerUpHandler handler, ExtendedPointerEventData eventData)
        {
            if (Application.isMobilePlatform || Current == null)
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

        private IDragHandler dragging;
        private SavedEventData draggingEvent;
        private Vector2 draggingPosition;

        private void Awake()
        {
            Current = this;
        }

        private void Update()
        {
            if (Application.isMobilePlatform || Mouse.current == null)
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
            else
            {
                if (dragging != null)
                {
                    // TODO: The drag event may be already invoked, so the new drag event may be duplicate
                    var pointerPosition = RealInput.pointerPosition;
                    if (pointerPosition != draggingPosition)
                    {
                        dragging.OnDrag((ExtendedPointerEventData)draggingEvent);
                        draggingPosition = pointerPosition;
                    }
                }
            }
        }

        private void AddPointerDown(IPointerUpHandler handler, ExtendedPointerEventData eventData)
        {
            if (pointerDownEvents.ContainsKey(handler))
            {
                handler.OnPointerUp((ExtendedPointerEventData)pointerDownEvents[handler]);
            }

            var savedData = new SavedEventData(eventData);
            pointerDownEvents[handler] = savedData;

            if (handler is IDragHandler dragHandler)
            {
                dragging = dragHandler;
                draggingEvent = savedData;
                draggingPosition = eventData.position;
            }
        }

        private void InvokeAllPointerUp()
        {
            if (pointerDownEvents.Count == 0)
            {
                return;
            }

            foreach (var pair in pointerDownEvents)
            {
                var eventData = (ExtendedPointerEventData)pair.Value;
                pair.Key.OnPointerUp(eventData);

                var selectable = (pair.Key as MonoBehaviour)?.gameObject.GetComponent<Selectable>();
                if (selectable != null)
                {
                    selectable.OnPointerUp(eventData);
                }
            }

            pointerDownEvents.Clear();

            dragging = null;
        }
    }
}
