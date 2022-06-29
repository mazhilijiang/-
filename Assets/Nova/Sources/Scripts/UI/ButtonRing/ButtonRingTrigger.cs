using UnityEngine;

namespace Nova
{
    public class ButtonRingTrigger : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Canvas currentCanvas;
        private RectTransform backgroundBlur;
        private ButtonRing buttonRing;

        public bool buttonShowing { get; private set; }
        public bool holdOpen { get; private set; }

        public float sectorRadius => buttonRing.sectorRadius;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            currentCanvas = GetComponentInParent<Canvas>();
            backgroundBlur = transform.Find("BackgroundBlur").GetComponent<RectTransform>();
            buttonRing = GetComponentInChildren<ButtonRing>();
        }

        private void ForceHideChildren()
        {
            buttonShowing = false;
            backgroundBlur.gameObject.SetActive(false);
            buttonRing.gameObject.SetActive(false);
        }

        public void Show(bool holdOpen)
        {
            targetPosition = lastMousePosition ?? RealInput.mousePosition;
            NoShowIfMouseMoved();

            if (buttonShowing)
            {
                return;
            }

            this.holdOpen = holdOpen;

            AdjustAnchorPosition();
            buttonShowing = true;
            backgroundBlur.gameObject.SetActive(true);
            buttonRing.gameObject.SetActive(true);

            if (holdOpen)
            {
                buttonRing.BeginEntryAnimation();
            }
        }

        public void Hide(bool triggerAction)
        {
            NoShowIfMouseMoved();

            if (!buttonShowing)
            {
                return;
            }

            holdOpen = false;

            buttonShowing = false;
            if (!triggerAction)
            {
                buttonRing.SuppressNextAction();
            }

            backgroundBlur.gameObject.SetActive(false);
            buttonRing.gameObject.SetActive(false);
        }

        private void AdjustAnchorPosition()
        {
            rectTransform.anchoredPosition = currentCanvas.ScreenToCanvasPosition(targetPosition);
            Vector2 v = currentCanvas.ViewportToCanvasPosition(Vector3.one) * 2.0f;
            backgroundBlur.offsetMin = -v;
            backgroundBlur.offsetMax = v;
        }

        private Vector2? lastMousePosition = null;
        private Vector2 targetPosition;

        public void ShowIfMouseMoved()
        {
            lastMousePosition = RealInput.mousePosition;
        }

        public void NoShowIfMouseMoved()
        {
            lastMousePosition = null;
        }

        private bool isFirstCalled = true;

        private void LateUpdate()
        {
            if (lastMousePosition != null &&
                (RealInput.mousePosition - lastMousePosition.Value).magnitude > sectorRadius * 0.5f)
            {
                Show(false);
            }

            // have to use late update
            // wait for all background sectors fully initialized
            if (!isFirstCalled) return;
            ForceHideChildren();
            isFirstCalled = false;
        }
    }
}
