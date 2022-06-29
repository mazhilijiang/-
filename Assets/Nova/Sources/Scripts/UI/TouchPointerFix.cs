using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
            public readonly PointerEventData.InputButton button;
            public readonly RaycastResult pointerCurrentRaycast;
            public readonly GameObject pointerEnter;
            public readonly int pointerId;
            public readonly Vector2 position;

            public readonly InputDevice device;
            public readonly UIPointerType pointerType;
            public readonly int touchId;

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
                var eventData = new ExtendedPointerEventData(EventSystem.current);

                eventData.button = savedData.button;
                eventData.pointerCurrentRaycast = savedData.pointerCurrentRaycast;
                eventData.pointerEnter = savedData.pointerEnter;
                eventData.pointerId = savedData.pointerId;
                eventData.position = savedData.position;

                eventData.eligibleForClick = true;
                eventData.pointerPress = savedData.pointerEnter;
                eventData.pointerPressRaycast = savedData.pointerCurrentRaycast;
                eventData.pressPosition = savedData.position;

                eventData.device = savedData.device;
                eventData.pointerType = savedData.pointerType;
                eventData.touchId = savedData.touchId;

                return eventData;
            }

            public override string ToString()
            {
                return "SavedEventData:\n" +
                    $"button: {button}\n" +
                    $"pointerCurrentRaycast: {pointerCurrentRaycast}\n" +
                    $"pointerEnter: {pointerEnter}\n" +
                    $"pointerId: {pointerId}\n" +
                    $"position: {position}\n" +
                    $"device: {device}\n" +
                    $"pointerType: {pointerType}\n" +
                    $"touchId: {touchId}";
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
                    // Debug.Log("Disable mouse");
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
                        // Debug.Log("Enable mouse");
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
                InvokePointerUp(handler, pointerDownEvents[handler]);
            }

            // Debug.Log($"AddPointerDown {Utils.GetPath((MonoBehaviour)handler)}\n{eventData}");
            pointerDownEvents[handler] = new SavedEventData(eventData);
        }

        private static void InvokePointerUp(IPointerUpHandler handler, SavedEventData savedData)
        {
            var mb = (MonoBehaviour)handler;
            var selectable = mb.GetComponent<Selectable>();
            var eventData = (ExtendedPointerEventData)savedData;
            // Debug.Log($"InvokePointerUp {Utils.GetPath(mb)}\n{selectable}\n\n{savedData}\n\n{eventData}");
            handler.OnPointerUp(eventData);
            if (selectable != null)
            {
                selectable.OnPointerUp(eventData);
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
                InvokePointerUp(pair.Key, pair.Value);
            }

            pointerDownEvents.Clear();
        }
    }
}
