using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string loadingSceneName = "Loading";
        [SerializeField] private GameObject scorePlaceholderPanel;
        [SerializeField] private Text scorePlaceholderText;

        private GameObject scoreRecordsPanel;
        private Text scoreRecordsText;
        private Image trainingCardImage;
        private Image comingCardImage;
        private Image trainingButtonImage;
        private Text trainingButtonText;
        private Image comingButtonImage;
        private Text comingButtonText;
        private RawImage previewImage;
        private AspectRatioFitter previewImageFitter;
        private Text previewTitleText;
        private Text previewSubtitleText;
        private Text previewKickerText;
        private Text previewQuestionText;
        private Text footerStatusText;

        private readonly Color ink = new Color(0.04f, 0.2f, 0.38f, 1f);
        private readonly Color blue = new Color(0.08f, 0.44f, 0.92f, 1f);
        private readonly Color softBlue = new Color(0.88f, 0.96f, 1f, 0.86f);
        private readonly Color cardWhite = new Color(1f, 1f, 1f, 0.86f);

        private void Awake()
        {
            EnsureSceneCamera();
            EnsureEventSystem();
            BuildModeSelectionUi();
            CreateScoreRecordsPanel();
        }

        public void StartSingleLevel()
        {
            SceneManager.LoadScene(loadingSceneName);
        }

        public void ReturnToLogin()
        {
            SceneManager.LoadScene("Login");
        }

        public void ShowScorePlaceholder()
        {
            ShowScoreRecords();
        }

        public void ShowScoreRecords()
        {
            if (scorePlaceholderPanel != null)
            {
                scorePlaceholderPanel.SetActive(false);
            }

            if (scoreRecordsPanel == null)
            {
                CreateScoreRecordsPanel();
            }

            RefreshScoreRecordsText();
            scoreRecordsPanel?.SetActive(true);
        }

        public void HideScorePlaceholder()
        {
            if (scorePlaceholderPanel != null)
            {
                scorePlaceholderPanel.SetActive(false);
            }

            if (scoreRecordsPanel != null)
            {
                scoreRecordsPanel.SetActive(false);
            }
        }

        public void ClearScoreRecords()
        {
            ScoreRecordStore.Clear();
            RefreshScoreRecordsText();
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void BuildModeSelectionUi()
        {
            Canvas canvas = EnsureCanvas();
            ClearCanvas(canvas.transform);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateBackground(canvas.transform);
            CreateBrandBlock(canvas.transform, font);
            CreateModePanel(canvas.transform, font);
            CreatePreviewPanel(canvas.transform, font);
            CreateFooter(canvas.transform, font);
            SetPreview(false);
        }

        private Canvas EnsureCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

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

        private void CreateBackground(Transform parent)
        {
            Texture2D backgroundTexture = Resources.Load<Texture2D>("Login/drone-bg");
            if (backgroundTexture != null)
            {
                RawImage background = CreateRawImage(parent, "Mode Selection Background", backgroundTexture, new Color(1f, 1f, 1f, 0.48f));
                StretchToFill(background.GetComponent<RectTransform>());
            }

            StretchToFill(CreatePanel(parent, "White Blue Page Wash", Vector2.zero, Vector2.zero, new Color(0.88f, 0.96f, 1f, 0.84f)).GetComponent<RectTransform>());
            GameObject leftWash = CreatePanel(parent, "Left White Field", Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.74f));
            RectTransform leftRect = leftWash.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.48f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
        }

        private void CreateBrandBlock(Transform parent, Font font)
        {
            GameObject logoCard = CreatePanel(parent, "Mode Logo Card", new Vector2(126f, -72f), new Vector2(86f, 86f), new Color(1f, 1f, 1f, 0.96f));
            AddOutline(logoCard, new Color(0.24f, 0.58f, 1f, 0.3f));

            Texture2D logoTexture = Resources.Load<Texture2D>("Login/logo");
            if (logoTexture != null)
            {
                RawImage logo = CreateRawImage(logoCard.transform, "Logo Image", logoTexture, Color.white);
                RectTransform logoRect = logo.GetComponent<RectTransform>();
                logoRect.anchorMin = new Vector2(0.5f, 0.5f);
                logoRect.anchorMax = logoRect.anchorMin;
                logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.anchoredPosition = Vector2.zero;
                logoRect.sizeDelta = new Vector2(68f, 68f);
            }

            CreateText(parent, "青雲启飞", new Vector2(232f, -76f), new Vector2(440f, 54f), 42, FontStyle.Bold, TextAnchor.UpperLeft, ink, font);
            CreateText(parent, "低空飞行训练模拟器", new Vector2(234f, -128f), new Vector2(500f, 36f), 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.38f, 0.68f, 0.95f), font);
            CreateText(parent, "LOW ALTITUDE FLIGHT TRAINING SIMULATOR", new Vector2(236f, -166f), new Vector2(560f, 24f), 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.76f), font);
            CreateText(parent, "模式选择", new Vector2(126f, -228f), new Vector2(360f, 62f), 44, FontStyle.Bold, TextAnchor.UpperLeft, ink, font);
            CreateText(parent, "请选择任务模式，系统将为您加载对应飞行场景", new Vector2(128f, -286f), new Vector2(620f, 34f), 20, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.14f, 0.4f, 0.62f, 0.86f), font);
        }

        private void CreateModePanel(Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Mode Selection Panel", new Vector2(126f, -350f), new Vector2(700f, 560f), cardWhite);
            AddOutline(panel, new Color(0.18f, 0.52f, 0.96f, 0.35f));

            CreateText(panel.transform, "飞行模式选择", new Vector2(34f, -28f), new Vector2(320f, 36f), 26, FontStyle.Bold, TextAnchor.UpperLeft, ink, font);
            CreateText(panel.transform, "请选择任务模式，系统将为您加载对应飞行场景", new Vector2(34f, -70f), new Vector2(610f, 30f), 17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.16f, 0.42f, 0.68f, 0.84f), font);

            GameObject trainingCard = CreateModeCard(panel.transform, font, new Vector2(34f, -126f), "训练场", "TRAINING MODE", "自由练习飞行基础动作，熟悉起飞、悬停、转向与降落操作。", "进入训练场", true);
            trainingCardImage = trainingCard.GetComponent<Image>();
            AddHover(trainingCard, () => SetPreview(false), () => SetPreview(false));

            Button trainingButton = trainingCard.GetComponentInChildren<Button>();
            if (trainingButton != null)
            {
                trainingButton.onClick.AddListener(StartSingleLevel);
            }

            GameObject comingCard = CreateModeCard(panel.transform, font, new Vector2(34f, -320f), "待拓展", "COMING SOON", "新的飞行任务模块正在规划中，后续将开放更多训练与考核内容。", "暂未开放", false);
            comingCardImage = comingCard.GetComponent<Image>();
            AddHover(comingCard, () => SetPreview(true), () => SetPreview(false));
        }

        private GameObject CreateModeCard(Transform parent, Font font, Vector2 position, string title, string english, string body, string buttonText, bool enabled)
        {
            GameObject card = CreatePanel(parent, title + " Card", position, new Vector2(632f, 162f), new Color(0.95f, 0.99f, 1f, 0.84f));
            AddOutline(card, new Color(0.18f, 0.52f, 0.96f, 0.24f));

            CreateText(card.transform, title, new Vector2(24f, -20f), new Vector2(220f, 34f), 28, FontStyle.Bold, TextAnchor.UpperLeft, ink, font);
            CreateText(card.transform, english, new Vector2(26f, -58f), new Vector2(240f, 24f), 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f), font);
            CreateText(card.transform, body, new Vector2(24f, -90f), new Vector2(388f, 58f), 17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.34f, 0.54f, 0.86f), font);

            Button button = CreateButton(card.transform, buttonText, new Vector2(438f, -54f), new Vector2(156f, 58f), enabled);
            Image buttonImage = button.GetComponent<Image>();
            Text buttonLabel = button.GetComponentInChildren<Text>();
            if (enabled)
            {
                trainingButtonImage = buttonImage;
                trainingButtonText = buttonLabel;
            }
            else
            {
                comingButtonImage = buttonImage;
                comingButtonText = buttonLabel;
            }

            return card;
        }

        private void CreatePreviewPanel(Transform parent, Font font)
        {
            GameObject panel = CreatePanel(parent, "Scene Preview Panel", new Vector2(-116f, -116f), new Vector2(890f, 786f), new Color(0.94f, 0.985f, 1f, 0.58f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(1f, 1f);
            AddOutline(panel, new Color(0.18f, 0.52f, 0.96f, 0.2f));

            previewKickerText = CreateText(panel.transform, "SCENE PREVIEW", new Vector2(42f, -38f), new Vector2(300f, 30f), 16, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.75f), font);
            previewTitleText = CreateText(panel.transform, "TRAINING AREA READY", new Vector2(42f, -78f), new Vector2(560f, 42f), 29, FontStyle.Bold, TextAnchor.UpperLeft, blue, font);
            previewSubtitleText = CreateText(panel.transform, "基础飞行训练环境已就绪", new Vector2(42f, -124f), new Vector2(520f, 34f), 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.38f, 0.68f, 0.92f), font);

            GameObject frame = CreatePanel(panel.transform, "Preview Image Frame", new Vector2(42f, -180f), new Vector2(806f, 492f), new Color(1f, 1f, 1f, 0.58f));
            AddOutline(frame, new Color(0.35f, 0.7f, 1f, 0.28f));
            frame.AddComponent<Mask>().showMaskGraphic = false;

            previewImage = CreateRawImage(frame.transform, "Preview Image", null, Color.white);
            RectTransform imageRect = previewImage.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            previewImageFitter = previewImage.gameObject.AddComponent<AspectRatioFitter>();
            previewImageFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            previewImageFitter.aspectRatio = 1.5f;

            previewQuestionText = CreateText(frame.transform, "?", Vector2.zero, new Vector2(260f, 260f), 190, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0f), font);
            RectTransform questionRect = previewQuestionText.GetComponent<RectTransform>();
            questionRect.anchorMin = new Vector2(0.5f, 0.5f);
            questionRect.anchorMax = questionRect.anchorMin;
            questionRect.pivot = new Vector2(0.5f, 0.5f);
            questionRect.anchoredPosition = new Vector2(0f, 0f);
            previewQuestionText.gameObject.SetActive(false);

            CreateHudLine(panel.transform, new Vector2(42f, -710f), "FLIGHT MODULE", "CONNECTED");
            CreateHudLine(panel.transform, new Vector2(324f, -710f), "CONTROL LINK", "READY");
            CreateHudLine(panel.transform, new Vector2(606f, -710f), "ENVIRONMENT", "STANDBY");
        }

        private void CreateFooter(Transform parent, Font font)
        {
            Button backButton = CreateButton(parent, "返回登录", new Vector2(126f, 68f), new Vector2(150f, 48f), true);
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = backRect.anchorMin;
            backRect.pivot = new Vector2(0f, 0f);
            backButton.onClick.AddListener(ReturnToLogin);

            Button exitButton = CreateButton(parent, "退出系统", new Vector2(294f, 68f), new Vector2(150f, 48f), true);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0f, 0f);
            exitRect.anchorMax = exitRect.anchorMin;
            exitRect.pivot = new Vector2(0f, 0f);
            exitButton.onClick.AddListener(Quit);

            footerStatusText = CreateText(parent, "当前状态：系统运行正常 ｜ 飞控模块已连接", new Vector2(126f, 34f), new Vector2(560f, 26f), 16, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f), font);
            RectTransform statusRect = footerStatusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = statusRect.anchorMin;
            statusRect.pivot = new Vector2(0f, 0f);
        }

        private void SetPreview(bool comingSoon)
        {
            if (trainingCardImage != null)
            {
                trainingCardImage.color = comingSoon ? new Color(0.95f, 0.99f, 1f, 0.76f) : new Color(0.84f, 0.94f, 1f, 0.95f);
            }

            if (comingCardImage != null)
            {
                comingCardImage.color = comingSoon ? new Color(0.84f, 0.94f, 1f, 0.95f) : new Color(0.95f, 0.99f, 1f, 0.76f);
            }

            if (trainingButtonImage != null)
            {
                trainingButtonImage.color = comingSoon ? new Color(0.92f, 0.97f, 1f, 0.9f) : new Color(0.08f, 0.44f, 0.92f, 0.94f);
            }

            if (trainingButtonText != null)
            {
                trainingButtonText.color = comingSoon ? blue : Color.white;
            }

            if (comingButtonImage != null)
            {
                comingButtonImage.color = comingSoon ? new Color(0.08f, 0.44f, 0.92f, 0.82f) : new Color(0.92f, 0.97f, 1f, 0.9f);
            }

            if (comingButtonText != null)
            {
                comingButtonText.color = comingSoon ? Color.white : blue;
            }

            Texture2D texture = Resources.Load<Texture2D>(comingSoon ? "Login/mode-coming-soon-preview" : "Login/mode-training-preview");
            if (texture == null)
            {
                texture = Resources.Load<Texture2D>(comingSoon ? "Login/drone-bg" : "Login/aircraft-showcase");
            }

            if (texture == null)
            {
                texture = Resources.Load<Texture2D>("Login/drone-bg");
            }

            if (previewImage != null)
            {
                previewImage.texture = texture;
                previewImage.color = Color.white;
            }

            if (previewImageFitter != null && texture != null && texture.height > 0)
            {
                previewImageFitter.aspectRatio = (float)texture.width / texture.height;
            }

            if (previewQuestionText != null)
            {
                previewQuestionText.gameObject.SetActive(false);
            }

            if (previewTitleText != null)
            {
                previewTitleText.text = comingSoon ? "COMING SOON" : "TRAINING AREA READY";
            }

            if (previewSubtitleText != null)
            {
                previewSubtitleText.text = comingSoon ? "待拓展" : "基础飞行训练环境已就绪";
            }

            if (previewKickerText != null)
            {
                previewKickerText.text = comingSoon ? "FUTURE MODULE" : "SCENE PREVIEW";
            }

            if (footerStatusText != null)
            {
                footerStatusText.text = comingSoon ? "当前状态：拓展模块规划中 ｜ 训练场可进入" : "当前状态：系统运行正常 ｜ 飞控模块已连接";
            }
        }

        private void CreateScoreRecordsPanel()
        {
            if (scoreRecordsPanel != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = EnsureCanvas();
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreRecordsPanel = CreatePanel(canvas.transform, "Score Records Panel", Vector2.zero, new Vector2(980f, 680f), new Color(0.94f, 0.985f, 1f, 0.96f));
            RectTransform panelRect = scoreRecordsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = panelRect.anchorMin;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            AddOutline(scoreRecordsPanel, new Color(0.18f, 0.52f, 0.96f, 0.32f));

            CreateText(scoreRecordsPanel.transform, "训练记录", new Vector2(0f, -34f), new Vector2(860f, 56f), 36, FontStyle.Bold, TextAnchor.UpperCenter, ink, font);
            scoreRecordsText = CreateText(scoreRecordsPanel.transform, "暂无训练记录。", new Vector2(56f, -114f), new Vector2(870f, 430f), 20, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.08f, 0.28f, 0.46f, 0.95f), font);

            Button clearButton = CreateButton(scoreRecordsPanel.transform, "清空", new Vector2(-160f, -592f), new Vector2(220f, 62f), true);
            clearButton.onClick.AddListener(ClearScoreRecords);
            Button closeButton = CreateButton(scoreRecordsPanel.transform, "关闭", new Vector2(160f, -592f), new Vector2(220f, 62f), true);
            closeButton.onClick.AddListener(HideScorePlaceholder);

            scoreRecordsPanel.SetActive(false);
        }

        private void RefreshScoreRecordsText()
        {
            if (scoreRecordsText == null)
            {
                return;
            }

            ScoreRecordStore.ScoreRecord[] records = ScoreRecordStore.LoadRecent();
            if (records.Length == 0)
            {
                scoreRecordsText.text = "暂无训练记录。\n完成一次训练后，记录会显示在这里。";
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine("最近 10 条训练记录");
            builder.AppendLine("排名  分数  等级  用时   碰撞  错误  高度  飞行器  时间");
            for (int i = 0; i < records.Length; i++)
            {
                ScoreRecordStore.ScoreRecord record = records[i];
                builder.Append(i + 1);
                builder.Append(".    ");
                builder.Append(record.score.ToString("000"));
                builder.Append("    ");
                builder.Append(record.grade);
                builder.Append("     ");
                builder.Append(FormatTime(record.completionTimeSeconds));
                builder.Append("   ");
                builder.Append(record.collisions);
                builder.Append("    ");
                builder.Append(record.wrongCheckpointHits);
                builder.Append("      ");
                builder.Append(record.unsafeAltitudeTicks);
                builder.Append("    ");
                builder.Append(string.IsNullOrWhiteSpace(record.droneName) ? "Training Drone" : record.droneName);
                builder.Append("  ");
                builder.AppendLine(record.recordedAt);
            }

            scoreRecordsText.text = builder.ToString();
        }

        private static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60:00}:{whole % 60:00}";
        }

        private static void AddHover(GameObject target, UnityEngine.Events.UnityAction enter, UnityEngine.Events.UnityAction exit)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            AddTrigger(trigger, EventTriggerType.PointerEnter, enter);
            AddTrigger(trigger, EventTriggerType.PointerExit, exit);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void CreateHudLine(Transform parent, Vector2 position, string label, string value)
        {
            GameObject block = CreatePanel(parent, label + " HUD", position, new Vector2(236f, 48f), new Color(1f, 1f, 1f, 0.38f));
            AddOutline(block, new Color(0.18f, 0.52f, 0.96f, 0.18f));
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateText(block.transform, label, new Vector2(14f, -8f), new Vector2(170f, 18f), 12, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.72f), font);
            CreateText(block.transform, value, new Vector2(14f, -26f), new Vector2(190f, 20f), 14, FontStyle.Bold, TextAnchor.UpperLeft, ink, font);
        }

        private static Button CreateButton(Transform parent, string text, Vector2 position, Vector2 size, bool enabled)
        {
            GameObject buttonObject = CreatePanel(parent, text + " Button", position, size, enabled ? new Color(0.08f, 0.44f, 0.92f, 0.94f) : new Color(0.92f, 0.97f, 1f, 0.9f));
            AddOutline(buttonObject, new Color(0.24f, 0.58f, 1f, 0.26f));

            Button button = buttonObject.AddComponent<Button>();
            button.interactable = enabled;
            buttonObject.GetComponent<Image>().raycastTarget = enabled;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.91f, 1f, 1f);
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text label = CreateText(buttonObject.transform, text, Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter, enabled ? Color.white : new Color(0.08f, 0.44f, 0.92f, 0.72f), font);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            return button;
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

        private static Text CreateText(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment, Color color, Font font)
        {
            GameObject textObject = new GameObject(string.IsNullOrEmpty(text) ? "Text" : text, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            bool left = alignment == TextAnchor.UpperLeft || alignment == TextAnchor.MiddleLeft;
            bool right = alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight;
            bool center = alignment == TextAnchor.UpperCenter || alignment == TextAnchor.MiddleCenter;
            rect.anchorMin = right ? new Vector2(1f, 1f) : center ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = right ? new Vector2(1f, 1f) : center ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
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

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureSceneCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5.4f;
                cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.88f, 0.96f, 1f, 1f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
