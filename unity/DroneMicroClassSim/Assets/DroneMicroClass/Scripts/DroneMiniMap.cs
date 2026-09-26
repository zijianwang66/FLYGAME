using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class DroneMiniMap : MonoBehaviour
    {
        private const string FigureEightSceneName = "8字飞行";
        private const string RectangleSceneName = "矩形飞行训练";
        private const int RuntimeMapTextureSize = 512;

        [SerializeField] private SimpleFlightController target;
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private RectTransform aircraftMarker;
        [SerializeField] private Vector2 worldCenter;
        [SerializeField] private Vector2 worldHalfExtents = new Vector2(20f, 13f);
        [SerializeField] private Vector2 contentViewportMin = new Vector2(48f / 512f, 121f / 512f);
        [SerializeField] private Vector2 contentViewportMax = new Vector2(464f / 512f, 391f / 512f);

        private Texture2D runtimeRouteTexture;
        private Sprite runtimeRouteSprite;

        private void Awake()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != FigureEightSceneName && sceneName != RectangleSceneName)
            {
                gameObject.SetActive(false);
                return;
            }

            ApplyGridLayout();
            if (sceneName == RectangleSceneName)
            {
                ApplyRectangleRouteGraphic();
            }
        }

        private void OnDestroy()
        {
            if (runtimeRouteSprite != null)
            {
                Destroy(runtimeRouteSprite);
            }

            if (runtimeRouteTexture != null)
            {
                Destroy(runtimeRouteTexture);
            }
        }

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
            ApplyGridLayout();
            UpdateMarker();
        }

        private void ApplyGridLayout()
        {
            if (transform is RectTransform panelRect)
            {
                panelRect.anchorMin = new Vector2(1f, 1f);
                panelRect.anchorMax = panelRect.anchorMin;
                panelRect.pivot = new Vector2(1f, 1f);
                panelRect.anchoredPosition = new Vector2(-24f, -24f);
                panelRect.sizeDelta = new Vector2(350f, 250f);
            }

            if (mapArea != null)
            {
                mapArea.anchorMin = new Vector2(0.5f, 0.5f);
                mapArea.anchorMax = mapArea.anchorMin;
                mapArea.pivot = new Vector2(0.5f, 0.5f);
                mapArea.anchoredPosition = Vector2.zero;
                mapArea.sizeDelta = new Vector2(230f, 230f);
            }
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

        private void ApplyRectangleRouteGraphic()
        {
            Image trackImage = mapArea != null ? mapArea.GetComponent<Image>() : null;
            if (trackImage == null)
            {
                Transform track = transform.Find("Mini Map Track");
                trackImage = track != null ? track.GetComponent<Image>() : null;
            }

            if (trackImage == null)
            {
                return;
            }

            runtimeRouteTexture = CreateRectangleRouteTexture();
            runtimeRouteSprite = Sprite.Create(
                runtimeRouteTexture,
                new Rect(0f, 0f, runtimeRouteTexture.width, runtimeRouteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeRouteSprite.name = "Runtime Rectangle Mini Map";
            trackImage.sprite = runtimeRouteSprite;
            trackImage.color = Color.white;
            trackImage.preserveAspect = true;
        }

        private Texture2D CreateRectangleRouteTexture()
        {
            Texture2D texture = new Texture2D(RuntimeMapTextureSize, RuntimeMapTextureSize, TextureFormat.RGBA32, false)
            {
                name = "Runtime Rectangle Mini Map Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[RuntimeMapTextureSize * RuntimeMapTextureSize];
            Color32 clear = new Color32(17, 32, 42, 220);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            texture.SetPixels32(pixels);
            Color32 grid = new Color32(255, 255, 255, 34);
            for (float x = -20f; x <= 20f; x += 5f)
            {
                DrawWorldLine(texture, new Vector2(x, -13f), new Vector2(x, 13f), 1, grid);
            }

            for (float y = -10f; y <= 10f; y += 5f)
            {
                DrawWorldLine(texture, new Vector2(-20f, y), new Vector2(20f, y), 1, grid);
            }

            DrawWorldRectangle(texture, new Vector2(20f, 13f), 5, new Color32(75, 230, 115, 220), false);
            DrawWorldRectangle(texture, new Vector2(15f, 8f), 4, new Color32(255, 176, 24, 225), false);
            DrawWorldRectangle(texture, new Vector2(15f, 8f), 5, new Color32(30, 170, 255, 240), true);
            DrawWorldRectangle(texture, new Vector2(1.5f, 1f), 3, new Color32(30, 220, 255, 225), false, new Vector2(0f, -11.6f));

            Vector2[] checkpoints =
            {
                new Vector2(7.5f, -8f), new Vector2(15f, -8f), new Vector2(15f, 0f),
                new Vector2(15f, 8f), new Vector2(7.5f, 8f), new Vector2(0f, 8f),
                new Vector2(-7.5f, 8f), new Vector2(-15f, 8f), new Vector2(-15f, 0f),
                new Vector2(-15f, -8f), new Vector2(-7.5f, -8f), new Vector2(0f, -8f)
            };
            foreach (Vector2 checkpoint in checkpoints)
            {
                DrawWorldDisc(texture, checkpoint, 0.45f, new Color32(255, 198, 30, 245));
            }

            DrawWorldDisc(texture, new Vector2(0f, -8f), 0.55f, new Color32(30, 230, 255, 255));
            texture.Apply(false, false);
            return texture;
        }

        private void DrawWorldRectangle(
            Texture2D texture,
            Vector2 halfExtents,
            int thickness,
            Color32 color,
            bool dashed,
            Vector2 center = default)
        {
            Vector2 bottomLeft = center + new Vector2(-halfExtents.x, -halfExtents.y);
            Vector2 bottomRight = center + new Vector2(halfExtents.x, -halfExtents.y);
            Vector2 topRight = center + new Vector2(halfExtents.x, halfExtents.y);
            Vector2 topLeft = center + new Vector2(-halfExtents.x, halfExtents.y);
            DrawWorldSegment(texture, bottomLeft, bottomRight, thickness, color, dashed);
            DrawWorldSegment(texture, bottomRight, topRight, thickness, color, dashed);
            DrawWorldSegment(texture, topRight, topLeft, thickness, color, dashed);
            DrawWorldSegment(texture, topLeft, bottomLeft, thickness, color, dashed);
        }

        private void DrawWorldSegment(Texture2D texture, Vector2 start, Vector2 end, int thickness, Color32 color, bool dashed)
        {
            if (!dashed)
            {
                DrawWorldLine(texture, start, end, thickness, color);
                return;
            }

            float length = Vector2.Distance(start, end);
            const float dashLength = 1.25f;
            const float gapLength = 0.65f;
            for (float distance = 0f; distance < length; distance += dashLength + gapLength)
            {
                float from = distance / length;
                float to = Mathf.Min(distance + dashLength, length) / length;
                DrawWorldLine(texture, Vector2.Lerp(start, end, from), Vector2.Lerp(start, end, to), thickness, color);
            }
        }

        private void DrawWorldLine(Texture2D texture, Vector2 worldStart, Vector2 worldEnd, int thickness, Color32 color)
        {
            Vector2 start = WorldToPixel(worldStart);
            Vector2 end = WorldToPixel(worldEnd);
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end)));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(start, end, i / (float)steps);
                DrawPixelDisc(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), thickness, color);
            }
        }

        private void DrawWorldDisc(Texture2D texture, Vector2 center, float radius, Color32 color)
        {
            Vector2 pixelCenter = WorldToPixel(center);
            Vector2 pixelEdge = WorldToPixel(center + Vector2.right * radius);
            int pixelRadius = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(pixelEdge.x - pixelCenter.x)));
            DrawPixelDisc(texture, Mathf.RoundToInt(pixelCenter.x), Mathf.RoundToInt(pixelCenter.y), pixelRadius, color);
        }

        private Vector2 WorldToPixel(Vector2 world)
        {
            Vector2 min = Vector2.Min(contentViewportMin, contentViewportMax) * RuntimeMapTextureSize;
            Vector2 max = Vector2.Max(contentViewportMin, contentViewportMax) * RuntimeMapTextureSize;
            float x = Mathf.InverseLerp(-worldHalfExtents.x, worldHalfExtents.x, world.x - worldCenter.x);
            float y = Mathf.InverseLerp(-worldHalfExtents.y, worldHalfExtents.y, world.y - worldCenter.y);
            return new Vector2(Mathf.Lerp(min.x, max.x, x), Mathf.Lerp(min.y, max.y, y));
        }

        private static void DrawPixelDisc(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
        {
            int radiusSq = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radiusSq)
                    {
                        continue;
                    }

                    int pixelX = centerX + x;
                    int pixelY = centerY + y;
                    if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
                    {
                        texture.SetPixel(pixelX, pixelY, color);
                    }
                }
            }
        }
    }
}
