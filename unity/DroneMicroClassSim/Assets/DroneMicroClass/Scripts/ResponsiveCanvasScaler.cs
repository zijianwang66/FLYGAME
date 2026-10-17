using UnityEngine;
using UnityEngine.UI;

namespace DroneMicroClass
{
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasScaler : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float DesignAspect = 16f / 9f;
        private const float NarrowAspect = 4f / 3f;
        private const float WideAspect = 21f / 9f;

        private CanvasScaler scaler;
        private int lastWidth;
        private int lastHeight;

        public static void ApplyTo(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            CanvasScaler targetScaler = canvas.GetComponent<CanvasScaler>();
            if (targetScaler == null)
            {
                targetScaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            Configure(targetScaler);

            ResponsiveCanvasScaler adapter = canvas.GetComponent<ResponsiveCanvasScaler>();
            if (adapter == null)
            {
                adapter = canvas.gameObject.AddComponent<ResponsiveCanvasScaler>();
            }

            adapter.scaler = targetScaler;
            adapter.RememberScreenSize();
        }

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight)
            {
                return;
            }

            Apply();
        }

        private void Apply()
        {
            if (scaler == null)
            {
                scaler = GetComponent<CanvasScaler>();
            }

            Configure(scaler);
            RememberScreenSize();
        }

        private void RememberScreenSize()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }

        private static void Configure(CanvasScaler targetScaler)
        {
            targetScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            targetScaler.referenceResolution = ReferenceResolution;
            targetScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            targetScaler.matchWidthOrHeight = CalculateMatch(Screen.width, Screen.height);
            targetScaler.referencePixelsPerUnit = 100f;
        }

        private static float CalculateMatch(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return 0.5f;
            }

            float aspect = width / (float)height;
            if (aspect < DesignAspect)
            {
                float normalized = Mathf.InverseLerp(NarrowAspect, DesignAspect, aspect);
                return Mathf.Lerp(0f, 0.5f, normalized);
            }

            float wide = Mathf.InverseLerp(DesignAspect, WideAspect, aspect);
            return Mathf.Lerp(0.5f, 1f, wide);
        }
    }
}
