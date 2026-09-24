using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public static class SingleLevelFlowBuilder
    {
        private const string SceneRoot = "Assets/DroneMicroClass/Scenes";
        private const string LoginScenePath = SceneRoot + "/Login.unity";
        private const string MainMenuScenePath = SceneRoot + "/MainMenu.unity";
        private const string LoadingScenePath = SceneRoot + "/Loading.unity";
        private const string LevelScenePath = SceneRoot + "/DroneFigureEightTrainingUnity.unity";

        [MenuItem("Drone MicroClass/Build Single Level Flow")]
        public static void BuildSingleLevelFlow()
        {
            EnsureSceneFolder();
            CreateMainMenuScene();
            CreateLoadingScene();
            ConfigureBuildSettings();
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            Debug.Log("Stage 1 flow created. Press Play in MainMenu, then click Start Training.");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DroneMicroClass"))
            {
                AssetDatabase.CreateFolder("Assets", "DroneMicroClass");
            }

            if (!AssetDatabase.IsValidFolder(SceneRoot))
            {
                AssetDatabase.CreateFolder("Assets/DroneMicroClass", "Scenes");
            }
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            Camera camera = CreateCamera(new Vector3(0f, 2f, -10f), Quaternion.identity, new Color(0.02f, 0.05f, 0.08f));
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;

            GameObject controllerObject = new GameObject("Main Menu Controller");
            MainMenuController controller = controllerObject.AddComponent<MainMenuController>();

            Canvas canvas = CreateCanvas("Main Menu Canvas");
            CreateEventSystem();
            CreateText(canvas.transform, "Drone Flight Training Simulator", new Vector2(0f, -170f), new Vector2(1200f, 90f), 48, FontStyle.Bold, TextAnchor.MiddleCenter);
            CreateText(canvas.transform, "Level 1: Figure Eight Flight Test", new Vector2(0f, -250f), new Vector2(900f, 60f), 28, FontStyle.Normal, TextAnchor.MiddleCenter);
            CreateText(canvas.transform, "Goal: take off, pass checkpoints in order, then reach the finish. Start from 100 points; overtime, collisions, missed checkpoints, and unsafe altitude subtract points.", new Vector2(0f, -322f), new Vector2(1180f, 90f), 22, FontStyle.Normal, TextAnchor.MiddleCenter);

            Button startButton = CreateButton(canvas.transform, "Start Training", new Vector2(0f, -460f), new Vector2(320f, 82f));
            UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartSingleLevel);

            Button scoreButton = CreateButton(canvas.transform, "Score Records", new Vector2(0f, -560f), new Vector2(320f, 72f));
            UnityEventTools.AddPersistentListener(scoreButton.onClick, controller.ShowScorePlaceholder);

            Button exitButton = CreateButton(canvas.transform, "Exit", new Vector2(0f, -648f), new Vector2(320f, 72f));
            UnityEventTools.AddPersistentListener(exitButton.onClick, controller.Quit);

            GameObject placeholderPanel = CreatePanel(canvas.transform, "Score Placeholder Panel", new Vector2(0f, -742f), new Vector2(780f, 104f));
            Text placeholderText = CreateText(placeholderPanel.transform, "Score history will be available in Stage 4.", Vector2.zero, new Vector2(720f, 70f), 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            placeholderPanel.SetActive(false);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("scorePlaceholderPanel").objectReferenceValue = placeholderPanel;
            serialized.FindProperty("scorePlaceholderText").objectReferenceValue = placeholderText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateLoadingScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Loading";

            Camera camera = CreateCamera(new Vector3(0f, 2f, -10f), Quaternion.identity, new Color(0.015f, 0.025f, 0.04f));
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;

            Canvas canvas = CreateCanvas("Loading Canvas");
            CreateEventSystem();
            CreateText(canvas.transform, "Entering Training Field", new Vector2(0f, -250f), new Vector2(900f, 80f), 42, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text progressText = CreateText(canvas.transform, "Loading 0%", new Vector2(0f, -430f), new Vector2(500f, 60f), 24, FontStyle.Normal, TextAnchor.MiddleCenter);
            Slider slider = CreateSlider(canvas.transform, new Vector2(0f, -360f), new Vector2(720f, 26f));

            GameObject controllerObject = new GameObject("Loading Screen Controller");
            LoadingScreenController controller = controllerObject.AddComponent<LoadingScreenController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("targetSceneName").stringValue = "DroneFigureEightTrainingUnity";
            serialized.FindProperty("progressBar").objectReferenceValue = slider;
            serialized.FindProperty("progressText").objectReferenceValue = progressText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, LoadingScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            if (File.Exists(LoginScenePath))
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(LoginScenePath, true),
                    new EditorBuildSettingsScene(MainMenuScenePath, true),
                    new EditorBuildSettingsScene(LoadingScenePath, true),
                    new EditorBuildSettingsScene(LevelScenePath, true)
                };
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LoadingScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true)
            };
        }

        private static Camera CreateCamera(Vector3 position, Quaternion rotation, Color background)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(position, rotation);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            return camera;
        }

        private static Canvas CreateCanvas(string objectName)
        {
            GameObject canvasObject = new GameObject(objectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.position = Vector3.zero;
        }

        private static Text CreateText(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.1f, 0.85f, 1f, 0.92f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.45f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.02f, 0.62f, 0.78f, 1f);
            button.colors = colors;

            Text label = CreateText(buttonObject.transform, text, Vector2.zero, size, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = new Color(0.01f, 0.04f, 0.06f, 1f);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            return button;
        }

        private static GameObject CreatePanel(Transform parent, string objectName, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.46f);
            return panel;
        }

        private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject sliderObject = new GameObject("Progress Bar", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            GameObject background = CreateSliderImage(sliderObject.transform, "Background", new Color(1f, 1f, 1f, 0.15f), Vector2.zero, Vector2.one);
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = CreateSliderImage(fillArea.transform, "Fill", new Color(0.1f, 0.85f, 1f, 1f), Vector2.zero, Vector2.one);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.targetGraphic = background.GetComponent<Image>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            return slider;
        }

        private static GameObject CreateSliderImage(Transform parent, string objectName, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            imageObject.GetComponent<Image>().color = color;
            return imageObject;
        }
    }
}
