using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class TrainingSceneSelectionController : MonoBehaviour
    {
        [SerializeField] private string loadingSceneName = "Loading";

        private readonly Color ink = new Color(0.04f, 0.2f, 0.38f, 1f);
        private readonly Color blue = new Color(0.08f, 0.44f, 0.92f, 1f);
        private readonly Color mutedBlue = new Color(0.14f, 0.4f, 0.62f, 0.88f);
        private Font font;

        private readonly struct SceneOption
        {
            public SceneOption(string title, string english, string description, string sceneName, string previewResource)
            {
                Title = title;
                English = english;
                Description = description;
                SceneName = sceneName;
                PreviewResource = previewResource;
            }

            public string Title { get; }
            public string English { get; }
            public string Description { get; }
            public string SceneName { get; }
            public string PreviewResource { get; }
        }

        private void Awake()
        {
            EnsureSceneCamera();
            EnsureEventSystem();
            font = CreateChineseFont();
            BuildUi();
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void BuildUi()
        {
            Canvas canvas = EnsureCanvas();
            ClearCanvas(canvas.transform);
            CreateBackground(canvas.transform);
            CreateHeader(canvas.transform);

            SceneOption[] options =
            {
                new SceneOption("矩形飞行训练", "RECTANGLE COURSE", "沿矩形航线依次通过检查点，完成一整圈规范飞行。", "矩形飞行训练", "Login/mode-training-preview"),
                new SceneOption("8字飞行训练", "FIGURE EIGHT COURSE", "沿8字航线保持高度与速度，完成连续转向训练。", "8字飞行", "Login/mode-training-preview1"),
                new SceneOption("校园自由飞行", "CAMPUS FREE FLIGHT", "在校园建筑环境中自由练习起飞、巡航和降落。", "学校", "Login/drone-bg"),
                new SceneOption("森林自由飞行", "FOREST FREE FLIGHT", "在森林与丘陵环境中练习低空飞行和抗风控制。", "forest", "Login/drone-bg1")
            };

            GameObject content = CreatePanel(
                canvas.transform,
                "Scene Selection Field",
                new Vector2(80f, -286f),
                new Vector2(1760f, 610f),
                new Color(0.94f, 0.985f, 1f, 0.68f));
            AddOutline(content, new Color(0.18f, 0.52f, 0.96f, 0.28f));

            const float cardWidth = 402f;
            const float cardGap = 28f;
            for (int i = 0; i < options.Length; i++)
            {
                CreateSceneCard(content.transform, options[i], new Vector2(28f + i * (cardWidth + cardGap), -28f), cardWidth);
            }

            Button backButton = CreateButton(canvas.transform, "返回主页", new Vector2(80f, 68f), new Vector2(170f, 52f));
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.zero;
            backRect.pivot = Vector2.zero;
            backButton.onClick.AddListener(ReturnToMainMenu);

            Text status = CreateText(
                canvas.transform,
                "当前状态：训练场景已就绪 ｜ 请选择训练项目",
                new Vector2(276f, 92f),
                new Vector2(650f, 28f),
                16,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.12f, 0.48f, 0.86f, 0.82f));
            RectTransform statusRect = status.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.zero;
            statusRect.pivot = Vector2.zero;
        }

        private void CreateBackground(Transform parent)
        {
            Texture2D backgroundTexture = Resources.Load<Texture2D>("Login/drone-bg");
            if (backgroundTexture != null)
            {
                RawImage background = CreateRawImage(parent, "Scene Selection Background", backgroundTexture, new Color(1f, 1f, 1f, 0.42f));
                StretchToFill(background.rectTransform);
            }

            StretchToFill(CreatePanel(parent, "Blue White Wash", Vector2.zero, Vector2.zero, new Color(0.88f, 0.96f, 1f, 0.86f)).GetComponent<RectTransform>());
            GameObject topBand = CreatePanel(parent, "Header White Field", Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.72f));
            RectTransform topRect = topBand.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 0.77f);
            topRect.anchorMax = Vector2.one;
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;
        }

        private void CreateHeader(Transform parent)
        {
            GameObject logoCard = CreatePanel(parent, "Scene Logo Card", new Vector2(80f, -52f), new Vector2(78f, 78f), new Color(1f, 1f, 1f, 0.96f));
            AddOutline(logoCard, new Color(0.24f, 0.58f, 1f, 0.3f));
            Texture2D logoTexture = Resources.Load<Texture2D>("Login/logo");
            if (logoTexture != null)
            {
                RawImage logo = CreateRawImage(logoCard.transform, "Logo", logoTexture, Color.white);
                RectTransform logoRect = logo.rectTransform;
                logoRect.anchorMin = new Vector2(0.5f, 0.5f);
                logoRect.anchorMax = logoRect.anchorMin;
                logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.anchoredPosition = Vector2.zero;
                logoRect.sizeDelta = new Vector2(62f, 62f);
            }

            CreateText(parent, "青雲启飞", new Vector2(180f, -54f), new Vector2(360f, 52f), 38, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            CreateText(parent, "低空飞行训练模拟器", new Vector2(182f, -104f), new Vector2(430f, 32f), 20, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.38f, 0.68f, 0.95f));
            CreateText(parent, "训练场-场景选择", new Vector2(0f, -82f), new Vector2(720f, 62f), 46, FontStyle.Bold, TextAnchor.UpperCenter, ink);
            CreateText(parent, "选择训练环境，系统将载入对应的飞行任务", new Vector2(0f, -148f), new Vector2(760f, 34f), 20, FontStyle.Normal, TextAnchor.UpperCenter, mutedBlue);
        }

        private void CreateSceneCard(Transform parent, SceneOption option, Vector2 position, float width)
        {
            GameObject card = CreatePanel(parent, option.Title + " Card", position, new Vector2(width, 554f), new Color(0.97f, 0.995f, 1f, 0.9f));
            AddOutline(card, new Color(0.18f, 0.52f, 0.96f, 0.28f));

            GameObject previewFrame = CreatePanel(card.transform, "Preview", new Vector2(18f, -18f), new Vector2(width - 36f, 250f), new Color(0.82f, 0.94f, 1f, 0.92f));
            previewFrame.AddComponent<Mask>().showMaskGraphic = false;
            Texture2D previewTexture = Resources.Load<Texture2D>(option.PreviewResource);
            if (previewTexture == null)
            {
                previewTexture = Resources.Load<Texture2D>("Login/mode-training-preview");
            }

            RawImage preview = CreateRawImage(previewFrame.transform, option.Title + " Preview", previewTexture, Color.white);
            StretchToFill(preview.rectTransform);
            AspectRatioFitter fitter = preview.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = previewTexture != null && previewTexture.height > 0
                ? (float)previewTexture.width / previewTexture.height
                : 16f / 9f;

            CreateText(card.transform, option.Title, new Vector2(22f, -292f), new Vector2(width - 44f, 42f), 28, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            CreateText(card.transform, option.English, new Vector2(24f, -338f), new Vector2(width - 48f, 26f), 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.82f));
            CreateText(card.transform, option.Description, new Vector2(22f, -378f), new Vector2(width - 44f, 74f), 17, FontStyle.Normal, TextAnchor.UpperLeft, mutedBlue);

            Button enterButton = CreateButton(card.transform, "进入场景", new Vector2(22f, -474f), new Vector2(width - 44f, 58f));
            string targetScene = option.SceneName;
            enterButton.onClick.AddListener(() => SelectScene(targetScene));

            Image cardImage = card.GetComponent<Image>();
            AddHover(
                card,
                () => cardImage.color = new Color(0.84f, 0.94f, 1f, 0.98f),
                () => cardImage.color = new Color(0.97f, 0.995f, 1f, 0.9f));
        }

        private void SelectScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("Training scene is not in Build Settings: " + sceneName);
                return;
            }

            SceneLoadRequest.Select(sceneName);
            SceneManager.LoadScene(loadingSceneName);
        }

        private Canvas EnsureCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Training Scene Selection Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static void ClearCanvas(Transform canvasTransform)
        {
            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                GameObject child = canvasTransform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private static GameObject CreatePanel(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return panel;
        }

        private Text CreateText(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(text, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            bool centered = alignment == TextAnchor.UpperCenter || alignment == TextAnchor.MiddleCenter;
            rect.anchorMin = centered ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = centered ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text label = textObject.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private Button CreateButton(Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = CreatePanel(parent, text + " Button", position, size, new Color(0.08f, 0.44f, 0.92f, 0.96f));
            AddOutline(buttonObject, new Color(0.24f, 0.58f, 1f, 0.28f));
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.87f, 1f, 1f);
            button.colors = colors;
            Text label = CreateText(buttonObject.transform, text, Vector2.zero, size, 19, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            StretchToFill(label.rectTransform);
            return button;
        }

        private static RawImage CreateRawImage(Transform parent, string objectName, Texture texture, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddOutline(GameObject target, Color color)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static void AddHover(GameObject target, UnityEngine.Events.UnityAction enter, UnityEngine.Events.UnityAction exit)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => enter());
            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => exit());
            trigger.triggers.Add(enterEntry);
            trigger.triggers.Add(exitEntry);
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Font CreateChineseFont()
        {
            Font chineseFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS" },
                22);
            return chineseFont != null ? chineseFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void EnsureSceneCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.88f, 0.96f, 1f, 1f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }
    }
}
