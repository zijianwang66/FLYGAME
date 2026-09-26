using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class FlightExitConfirmation : MonoBehaviour
    {
        private const string SceneSelectionName = "TrainingSceneSelection";

        private GameObject confirmationOverlay;
        private float previousTimeScale = 1f;
        private bool isOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsFlightScene(scene.name) || FindFirstObjectByType<FlightExitConfirmation>() != null)
            {
                return;
            }

            GameObject controller = new GameObject("Flight Exit Confirmation");
            controller.AddComponent<FlightExitConfirmation>();
        }

        private static bool IsFlightScene(string sceneName)
        {
            return sceneName == "8字飞行" ||
                   sceneName == "矩形飞行训练" ||
                   sceneName == "forest" ||
                   sceneName == "学校";
        }

        private void Awake()
        {
            BuildConfirmationUi();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (isOpen)
            {
                CloseConfirmation();
            }
            else
            {
                OpenConfirmation();
            }
        }

        private void OnDestroy()
        {
            RestoreTimeScale();
        }

        private void OpenConfirmation()
        {
            if (confirmationOverlay == null)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
            confirmationOverlay.SetActive(true);
        }

        public void CloseConfirmation()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            confirmationOverlay.SetActive(false);
            RestoreTimeScale();
        }

        public void ReturnToSceneSelection()
        {
            isOpen = false;
            RestoreTimeScale();
            SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(SceneSelectionName)
                ? SceneSelectionName
                : "MainMenu");
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = Mathf.Max(0.0001f, previousTimeScale);
            }
        }

        private void BuildConfirmationUi()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject(
                "Flight Exit Confirmation Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            confirmationOverlay = CreateImage(
                "Confirmation Overlay",
                canvasObject.transform,
                new Color(0f, 0f, 0f, 0.58f));
            Stretch(confirmationOverlay.GetComponent<RectTransform>());

            GameObject panel = CreateImage(
                "Confirmation Panel",
                confirmationOverlay.transform,
                new Color(0.035f, 0.07f, 0.095f, 0.98f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = panelRect.anchorMin;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(560f, 310f);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.78f, 1f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text title = CreateText("Title", panel.transform, "退出当前飞行？", 32, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(30f, -32f), new Vector2(500f, 48f));
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.72f, 0.96f, 1f, 1f);

            Text body = CreateText(
                "Body",
                panel.transform,
                "返回场景选择后，当前飞行进度将不会保留。",
                21,
                FontStyle.Normal);
            SetRect(body.rectTransform, new Vector2(30f, -96f), new Vector2(500f, 54f));
            body.alignment = TextAnchor.MiddleCenter;
            body.color = new Color(0.9f, 0.94f, 0.97f, 1f);

            Button continueButton = CreateButton(
                "Continue Button",
                panel.transform,
                "继续飞行",
                new Color(0.12f, 0.28f, 0.38f, 1f));
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(46f, -205f), new Vector2(215f, 64f));
            continueButton.onClick.AddListener(CloseConfirmation);

            Button exitButton = CreateButton(
                "Exit Button",
                panel.transform,
                "返回场景选择",
                new Color(0.05f, 0.63f, 0.85f, 1f));
            SetRect(exitButton.GetComponent<RectTransform>(), new Vector2(299f, -205f), new Vector2(215f, 64f));
            exitButton.onClick.AddListener(ReturnToSceneSelection);

            confirmationOverlay.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return imageObject;
        }

        private static Text CreateText(string objectName, Transform parent, string content, int size, FontStyle style)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = GetChineseFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string objectName, Transform parent, string label, Color color)
        {
            GameObject buttonObject = CreateImage(objectName, parent, color);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.9f, 0.96f, 1f);
            button.colors = colors;

            Text text = CreateText("Label", buttonObject.transform, label, 22, FontStyle.Bold);
            Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font GetChineseFont()
        {
            return Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS" },
                24);
        }
    }
}
