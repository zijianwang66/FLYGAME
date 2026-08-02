using UnityEngine;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class DroneHud : MonoBehaviour
    {
        [SerializeField] private SimpleFlightController target;
        [SerializeField] private DroneFleetManager fleet;
        [SerializeField] private DroneCameraRig cameraRig;
        [SerializeField] private Text titleText;
        [SerializeField] private Text altitudeBadgeText;
        [SerializeField] private Text altitudeText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text attitudeText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text throttleText;
        [SerializeField] private Text parameterText;
        [SerializeField] private Text cameraModeText;
        [SerializeField] private Text aircraftSelectText;
        [SerializeField] private RawImage noseCameraView;
        [SerializeField] private Image throttleFill;
        [SerializeField] private DroneMiniMap miniMap;
        [SerializeField] private float noseCameraDisplayWidth = 480f;
        [SerializeField] private Vector3 displayCoordinateOrigin = new Vector3(325f, 48f, 468f);

        private const float TextUpdateInterval = 0.08f;
        private const float DefaultNoseCameraAspect = 16f / 9f;
        private float nextTextUpdateTime;
        private static Font chineseFont;
        private Text parameterValueText;
        private Text windLabelText;
        private Text windValueText;

        private void Awake()
        {
            ConfigureNoseCameraView(noseCameraView != null ? noseCameraView.texture : null);
            ConfigureFlightDataLayout();
        }

        private void OnEnable()
        {
            ConfigureFlightDataLayout();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RefreshRuntimeHudLayouts()
        {
            DroneHud[] huds = Object.FindObjectsByType<DroneHud>(FindObjectsSortMode.None);
            foreach (DroneHud hud in huds)
            {
                hud.ConfigureNoseCameraView(hud.noseCameraView != null ? hud.noseCameraView.texture : null);
                hud.ConfigureFlightDataLayout();
            }
        }

        public void Bind(SimpleFlightController newTarget)
        {
            target = newTarget;
            if (miniMap != null)
            {
                miniMap.Bind(newTarget);
            }
        }

        public void Bind(SimpleFlightController newTarget, DroneFleetManager newFleet, DroneCameraRig newCameraRig, RenderTexture noseCameraTexture)
        {
            target = newTarget;
            fleet = newFleet;
            cameraRig = newCameraRig;
            if (noseCameraView != null)
            {
                ConfigureNoseCameraView(noseCameraTexture);
            }

            if (miniMap != null)
            {
                miniMap.Bind(newTarget);
            }
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            if (throttleFill != null)
            {
                throttleFill.fillAmount = target.CurrentThrottle;
            }

            if (Time.unscaledTime < nextTextUpdateTime)
            {
                return;
            }

            nextTextUpdateTime = Time.unscaledTime + TextUpdateInterval;

            if (modeText != null)
            {
                modeText.text = "飞行姿态与位置参数";
            }

            if (parameterText != null)
            {
                float gimbalPitch = fleet != null
                    ? fleet.CurrentGimbalPitchDegrees
                    : cameraRig != null ? cameraRig.GimbalPitchDegrees : 0f;
                string gimbalPitchDisplay = fleet != null && !fleet.CurrentVariantSupportsGimbalPitch
                    ? "固定镜头"
                    : $"{gimbalPitch:+0.0;-0.0;0.0}°";
                parameterText.text =
                    "D（水平距离）\n" +
                    "H（相对高度）\n" +
                    "水平速度\n" +
                    "垂直速度\n" +
                    "俯仰角\n" +
                    "横滚角\n" +
                    "航向角\n" +
                    "云台俯仰";

                if (parameterValueText != null)
                {
                    parameterValueText.text =
                        $"{target.HorizontalDistanceFromHome:0.00} m\n" +
                        $"{target.HeightFromHome:0.00} m\n" +
                        $"{target.HorizontalSpeed:0.00} m/s\n" +
                        $"{target.VerticalSpeed:+0.00;-0.00;0.00} m/s\n" +
                        $"{target.PitchDegrees:+0.0;-0.0;0.0}°\n" +
                        $"{target.RollDegrees:+0.0;-0.0;0.0}°\n" +
                        $"{target.YawDegrees:000.0}°\n" +
                        gimbalPitchDisplay;
                }
            }

            UpdateEnvironmentDisplay();
        }

        private void ConfigureFlightDataLayout()
        {
            SetVisible(titleText, false);
            SetVisible(altitudeBadgeText, false);
            SetVisible(altitudeText, false);
            SetVisible(speedText, false);
            SetVisible(attitudeText, false);

            if (throttleFill != null && throttleFill.transform.parent != null)
            {
                throttleFill.transform.parent.gameObject.SetActive(false);
            }

            if (cameraModeText != null && cameraModeText.transform.parent != null)
            {
                cameraModeText.transform.parent.gameObject.SetActive(false);
            }

            Font readableChineseFont = GetChineseFont();
            EnsureEnvironmentPanel(readableChineseFont);
            if (modeText != null)
            {
                modeText.font = readableChineseFont != null ? readableChineseFont : modeText.font;
                modeText.fontSize = 22;
                modeText.fontStyle = FontStyle.Bold;
                modeText.color = new Color(0.72f, 0.96f, 1f, 1f);
                modeText.alignment = TextAnchor.UpperLeft;
                ConfigureTopLeftRect(modeText.rectTransform, new Vector2(18f, -16f), new Vector2(334f, 34f));
            }

            if (parameterText == null)
            {
                return;
            }

            parameterText.font = readableChineseFont != null ? readableChineseFont : parameterText.font;
            parameterText.fontSize = 16;
            parameterText.fontStyle = FontStyle.Normal;
            parameterText.color = new Color(0.92f, 0.96f, 0.97f, 1f);
            parameterText.alignment = TextAnchor.UpperLeft;
            parameterText.lineSpacing = 1.2f;
            ConfigureTopLeftRect(parameterText.rectTransform, new Vector2(18f, -62f), new Vector2(170f, 224f));

            if (parameterText.transform.parent is RectTransform panelRect)
            {
                panelRect.anchorMin = new Vector2(1f, 0f);
                panelRect.anchorMax = new Vector2(1f, 0f);
                panelRect.pivot = new Vector2(1f, 0f);
                panelRect.anchoredPosition = new Vector2(-24f, 24f);
                panelRect.sizeDelta = new Vector2(350f, 350f);

                Image panelImage = panelRect.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = new Color(0.035f, 0.055f, 0.065f, 0.84f);
                }

                parameterValueText = EnsureParameterValueText(panelRect, readableChineseFont);
                RemoveGimbalCenterButton(panelRect);
            }
        }

        private Text EnsureParameterValueText(RectTransform panelRect, Font font)
        {
            Transform existing = panelRect.Find("Parameter Values");
            Text values;
            if (existing != null)
            {
                values = existing.GetComponent<Text>();
            }
            else
            {
                GameObject valuesObject = new GameObject(
                    "Parameter Values",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                valuesObject.transform.SetParent(panelRect, false);
                values = valuesObject.GetComponent<Text>();
            }

            values.font = font != null ? font : parameterText.font;
            values.fontSize = 16;
            values.fontStyle = FontStyle.Normal;
            values.color = Color.white;
            values.alignment = TextAnchor.UpperRight;
            values.lineSpacing = parameterText.lineSpacing;
            values.raycastTarget = false;
            ConfigureTopLeftRect(values.rectTransform, new Vector2(198f, -62f), new Vector2(134f, 224f));
            return values;
        }

        private void EnsureEnvironmentPanel(Font font)
        {
            if (displayCoordinateOrigin.sqrMagnitude < 0.001f)
            {
                displayCoordinateOrigin = new Vector3(325f, 48f, 468f);
            }

            Transform existing = transform.Find("Environment Data Panel");
            GameObject panelObject;
            if (existing != null)
            {
                panelObject = existing.gameObject;
            }
            else
            {
                panelObject = new GameObject(
                    "Environment Data Panel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                panelObject.transform.SetParent(transform, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = Vector2.zero;
            panelRect.anchoredPosition = new Vector2(24f, 24f);
            panelRect.sizeDelta = new Vector2(350f, 330f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.035f, 0.055f, 0.065f, 0.84f);
            panelImage.raycastTarget = false;

            Text title = EnsurePanelText(panelRect, "Title", font, 22, FontStyle.Bold, TextAnchor.UpperLeft);
            title.color = new Color(0.72f, 0.96f, 1f, 1f);
            title.text = "风况与飞行坐标";
            ConfigureTopLeftRect(title.rectTransform, new Vector2(18f, -16f), new Vector2(314f, 34f));

            windLabelText = EnsurePanelText(panelRect, "Labels", font, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            windLabelText.lineSpacing = 1.18f;
            windLabelText.text =
                "风况预设\n" +
                "风向（来向）\n" +
                "平均风速\n" +
                "当前风速\n" +
                "阵风强度\n" +
                "湍流频率\n" +
                "GPS 定点\n" +
                "坐标 X\n" +
                "坐标 Y\n" +
                "坐标 Z";
            ConfigureTopLeftRect(windLabelText.rectTransform, new Vector2(18f, -62f), new Vector2(156f, 244f));

            windValueText = EnsurePanelText(panelRect, "Values", font, 16, FontStyle.Normal, TextAnchor.UpperRight);
            windValueText.lineSpacing = windLabelText.lineSpacing;
            ConfigureTopLeftRect(windValueText.rectTransform, new Vector2(180f, -62f), new Vector2(152f, 244f));
        }

        private static Text EnsurePanelText(
            RectTransform panelRect,
            string objectName,
            Font font,
            int fontSize,
            FontStyle style,
            TextAnchor alignment)
        {
            Transform existing = panelRect.Find(objectName);
            Text text;
            if (existing != null)
            {
                text = existing.GetComponent<Text>();
            }
            else
            {
                GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(panelRect, false);
                text = textObject.GetComponent<Text>();
            }

            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.96f, 0.97f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private static void RemoveGimbalCenterButton(RectTransform panelRect)
        {
            Transform button = panelRect.Find("Gimbal Center Button");
            if (button == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(button.gameObject);
            }
            else
            {
                Object.DestroyImmediate(button.gameObject);
            }
        }

        private void UpdateEnvironmentDisplay()
        {
            if (windValueText == null || target == null)
            {
                return;
            }

            WindSystem wind = WindSystem.Active;
            Vector3 localOffset = target.transform.position - target.HomePosition;
            Vector3 displayPosition = displayCoordinateOrigin + localOffset;
            string presetName = wind != null ? wind.PresetDisplayName : "无风";
            string windMode = wind != null && wind.IsFixedWind ? "固定" : "动态";
            float direction = wind != null ? wind.CurrentDirectionDegrees : 0f;
            float averageSpeed = wind != null ? wind.AverageWindSpeed : 0f;
            float currentSpeed = wind != null ? wind.CurrentWindSpeed : 0f;
            float gust = wind != null ? wind.GustStrength : 0f;
            float turbulence = wind != null ? wind.TurbulenceFrequency : 0f;

            windValueText.text =
                $"{presetName} · {windMode}\n" +
                $"{direction:000}°\n" +
                $"{averageSpeed:0.00} m/s\n" +
                $"{currentSpeed:0.00} m/s\n" +
                $"{gust:0.00} m/s\n" +
                $"{turbulence:0.00} Hz\n" +
                $"{(target.GpsPositionHoldEnabled ? "开启" : "关闭")}\n" +
                $"{displayPosition.x:0.00} m\n" +
                $"{displayPosition.y:0.00} m\n" +
                $"{displayPosition.z:0.00} m";
        }

        private static void SetVisible(Graphic graphic, bool visible)
        {
            if (graphic != null)
            {
                graphic.gameObject.SetActive(visible);
            }
        }

        private static void ConfigureTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Font GetChineseFont()
        {
            if (chineseFont == null)
            {
                chineseFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS" },
                    20);
            }

            return chineseFont;
        }

        private void ConfigureNoseCameraView(Texture texture)
        {
            if (noseCameraView == null)
            {
                return;
            }

            if (texture != null)
            {
                noseCameraView.texture = texture;
            }

            noseCameraView.color = Color.white;
            float aspect = texture != null && texture.height > 0
                ? (float)texture.width / texture.height
                : DefaultNoseCameraAspect;

            RectTransform viewRect = noseCameraView.rectTransform;
            viewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, noseCameraDisplayWidth);

            AspectRatioFitter aspectFitter = noseCameraView.GetComponent<AspectRatioFitter>();
            if (aspectFitter == null)
            {
                aspectFitter = noseCameraView.gameObject.AddComponent<AspectRatioFitter>();
            }

            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspectFitter.aspectRatio = aspect;

            if (viewRect.parent is RectTransform panelRect)
            {
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, noseCameraDisplayWidth + 40f);
                panelRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    noseCameraDisplayWidth / aspect + 75f);

                Image panelImage = panelRect.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = new Color(0f, 0f, 0f, 0.42f);
                }
            }
        }
    }
}
