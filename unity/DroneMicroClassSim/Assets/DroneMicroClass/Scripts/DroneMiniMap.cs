using UnityEngine;

namespace DroneMicroClass
{
    public sealed class DroneMiniMap : MonoBehaviour
    {
        [SerializeField] private SimpleFlightController target;
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private RectTransform aircraftMarker;
        [SerializeField] private Vector2 worldCenter;
        [SerializeField] private Vector2 worldHalfExtents = new Vector2(20f, 13f);
        [SerializeField] private Vector2 contentViewportMin = new Vector2(48f / 512f, 121f / 512f);
        [SerializeField] private Vector2 contentViewportMax = new Vector2(464f / 512f, 391f / 512f);

        public void Configure(
            SimpleFlightController newTarget,
            RectTransform newMapArea,
            RectTransform newAircraftMarker,
            Vector2 newWorldHalfExtents,
            Vector2 newWorldCenter,
            Vector2 newContentViewportMin,
            Vector2 newContentViewportMax)
        {
            target = newTarget;
            mapArea = newMapArea;
            aircraftMarker = newAircraftMarker;
            worldHalfExtents = new Vector2(Mathf.Max(1f, newWorldHalfExtents.x), Mathf.Max(1f, newWorldHalfExtents.y));
            worldCenter = newWorldCenter;
            contentViewportMin = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, newContentViewportMin));
            contentViewportMax = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, newContentViewportMax));
            UpdateMarker();
        }

        public void Bind(SimpleFlightController newTarget)
        {
            target = newTarget;
            UpdateMarker();
        }

        private void LateUpdate()
        {
            UpdateMarker();
        }

        private void UpdateMarker()
        {
            if (target == null || aircraftMarker == null)
            {
                return;
            }

            RectTransform activeMapArea = mapArea != null ? mapArea : transform as RectTransform;
            if (activeMapArea == null)
            {
                return;
            }

            Vector3 position = target.transform.position;
            float normalizedX = Mathf.InverseLerp(-worldHalfExtents.x, worldHalfExtents.x, position.x - worldCenter.x);
            float normalizedY = Mathf.InverseLerp(-worldHalfExtents.y, worldHalfExtents.y, position.z - worldCenter.y);
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            Rect rect = activeMapArea.rect;
            Vector2 viewportMin = Vector2.Min(contentViewportMin, contentViewportMax);
            Vector2 viewportMax = Vector2.Max(contentViewportMin, contentViewportMax);
            float left = rect.xMin + rect.width * viewportMin.x;
            float right = rect.xMin + rect.width * viewportMax.x;
            float bottom = rect.yMin + rect.height * viewportMin.y;
            float top = rect.yMin + rect.height * viewportMax.y;

            aircraftMarker.anchoredPosition = new Vector2(Mathf.Lerp(left, right, normalizedX), Mathf.Lerp(bottom, top, normalizedY));
            aircraftMarker.localEulerAngles = new Vector3(0f, 0f, -target.transform.eulerAngles.y);
        }
    }
}
