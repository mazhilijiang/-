using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Nova
{
    /// <summary>
    /// Sets touch press state according to EnhancedTouch.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class TouchPressProcessor : InputProcessor<float>
    {
#if UNITY_EDITOR
        static TouchPressProcessor()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            InputSystem.RegisterProcessor<TouchPressProcessor>();
        }

        public override float Process(float value, InputControl control)
        {
            return Touch.activeTouches.Count > 0 ? 1.0f : 0.0f;
        }
    }
}
