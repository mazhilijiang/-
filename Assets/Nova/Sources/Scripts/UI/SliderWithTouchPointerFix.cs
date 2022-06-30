using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Nova
{
    public class SliderWithTouchPointerFix : Slider
    {
        public override void OnPointerDown(PointerEventData _eventData)
        {
            var eventData = (ExtendedPointerEventData)_eventData;
            if (TouchPointerFix.SkipOrAdd(this, eventData))
            {
                return;
            }

            base.OnPointerDown(_eventData);
        }

        public override void OnDrag(PointerEventData _eventData)
        {
            var eventData = (ExtendedPointerEventData)_eventData;
            if (TouchPointerFix.Skip(eventData))
            {
                return;
            }

            base.OnDrag(_eventData);
        }
    }
}
