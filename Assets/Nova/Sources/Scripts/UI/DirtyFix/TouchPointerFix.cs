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
    /// Pointer up and drag events may not be invoked, so we need to save each pointer down event
    /// and invoke the corresponding events.
    /// TODO: The events should be invoked before Update
    /// </summary>
    public class TouchPointerFix : MonoBehaviour
    {
        // The values in PointerEventData may change after the event is processed, so we need to save them
        private class SavedEventData
        {
            private readonly PointerEventData.InputButton button;
            private readonly int clickCount;
            private readonly float clickTime;
            private readonly Vector2 delta;
            private readonly bool dragging;
            private readonly bool eligibleForClick;
            private readonly GameObject pointerClick;
            private readonly RaycastResult pointerCurrentRaycast;
            private readonly GameObject pointerDrag;
            private readonly GameObject pointerEnter;
            private readonly int pointerId;
            private readonly GameObject pointerPress;
            private readonly RaycastResult pointerPressRaycast;
            private readonly Vector2 pressPosition;
            private readonly GameObject rawPointerPress;
            private readonly Vector2 scrollDelta;
            private readonly bool useDragThreshold;

            private readonly InputControl control;
            private readonly InputDevice device;
            private readonly UIPointerType pointerType;
            private readonly int touchId;

            public SavedEventData(ExtendedPointerEventData eventData)
            {
                button = eventData.button;
                clickCount = eventData.clickCount;
                clickTime = eventData.clickTime;
                delta = eventData.delta;
                dragging = eventData.dragging;
                eligibleForClick = eventData.eligibleForClick;
                pointerClick = eventData.pointerClick;
                pointerCurrentRaycast = eventData.pointerCurrentRaycast;
                pointerDrag = eventData.pointerDrag;
                pointerEnter = eventData.pointerEnter;
                pointerId = eventData.pointerId;
                pointerPress = eventData.pointerPress;
                pointerPressRaycast = eventData.pointerPressRaycast;
                pressPosition = eventData.pressPosition;
                rawPointerPress = eventData.rawPointerPress;
                scrollDelta = eventData.scrollDelta;
                useDragThreshold = eventData.useDragThreshold;

                control = eventData.control;
                device = eventData.device;
                pointerType = eventData.pointerType;
                touchId = eventData.touchId;
            }

            public static explicit operator ExtendedPointerEventData(SavedEventData savedData)
            {
                var eventData = new ExtendedPointerEventData(EventSystem.current)
                {
                    button = savedData.button,
                    clickCount = savedData.clickCount,
                    clickTime = savedData.clickTime,
                    delta = savedData.delta,
                    dragging = savedData.dragging,
                    eligibleForClick = savedData.eligibleForClick,
                    pointerClick = savedData.pointerClick,
                    pointerCurrentRaycast = savedData.pointerCurrentRaycast,
                    pointerDrag = savedData.pointerDrag,
                    pointerEnter = savedData.pointerEnter,
                    pointerId = savedData.pointerId,
                    pointerPress = savedData.pointerPress,
                    pointerPressRaycast = savedData.pointerPressRaycast,
                    pressPosition = savedData.pressPosition,
                    rawPointerPress = savedData.rawPointerPress,
                    scrollDelta = savedData.scrollDelta,
                    useDragThreshold = savedData.useDragThreshold,

                    position = RealInput.pointerPosition,

                    control = savedData.control,
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

        public static bool SkipOrAddDrag(IDragHandler handler, ExtendedPointerEventData eventData)
        {
            if (Application.isMobilePlatform || Current == null)
            {
                return false;
            }

            if (eventData.pointerType == UIPointerType.Touch)
            {
                Current.AddDrag(handler, eventData);
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

            if (dragging != null)
            {
                if (Touch.activeTouches.Count == 0)
                {
                    InvokeEndDrag();
                    dragging = null;
                }
                else
                {
                    // TODO: The drag event may be already invoked, so the new event may be duplicate
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
                InvokeEndDrag();
                dragging = dragHandler;
                draggingEvent = savedData;
                draggingPosition = eventData.position;
            }
        }

        private void AddDrag(IDragHandler handler, ExtendedPointerEventData eventData)
        {
            InvokeEndDrag();
            dragging = handler;
            draggingEvent = new SavedEventData(eventData);
            draggingPosition = eventData.position;
        }

        private void InvokeEndDrag()
        {
            if (dragging is IEndDragHandler handler)
            {
                handler.OnEndDrag((ExtendedPointerEventData)draggingEvent);
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
        }
    }
}
