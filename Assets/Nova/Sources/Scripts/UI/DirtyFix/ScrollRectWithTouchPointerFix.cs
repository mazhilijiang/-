using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Nova
{
    public class ScrollRectWithTouchPointerFix : ScrollRect
    {
        public override void OnBeginDrag(PointerEventData _eventData)
        {
            var eventData = (ExtendedPointerEventData)_eventData;
            if (TouchPointerFix.SkipOrAddDrag(this, eventData))
            {
                return;
            }

            base.OnBeginDrag(_eventData);
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

        public override void OnEndDrag(PointerEventData _eventData)
        {
            var eventData = (ExtendedPointerEventData)_eventData;
            if (TouchPointerFix.Skip(eventData))
            {
                return;
            }

            base.OnEndDrag(_eventData);
        }
    }
}
