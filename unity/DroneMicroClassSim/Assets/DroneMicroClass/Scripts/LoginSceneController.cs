using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class LoginSceneController : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private string demoAccount = "pilot";
        [SerializeField] private string demoPassword = "123456";

        private readonly System.Collections.Generic.List<ShardParticle> shards = new System.Collections.Generic.List<ShardParticle>();
        private InputField accountInput;
        private InputField passwordInput;
        private Text statusText;
        private RectTransform aircraftRoot;
        private Vector2 aircraftBasePosition;
        private float loginStartTime;
        private bool isLoggingIn;

        private void Awake()
        {
            EnsureSceneCamera();
            EnsureEventSystem();
            BuildLoginUi();
        }

        private void Update()
        {
            AnimateAircraftShowcase();
            AnimateBrokenParticles();

            if (isLoggingIn && Time.time - loginStartTime > 0.85f)
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        private void BuildLoginUi()
        {
            Canvas canvas = CreateCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateBackground(canvas.transform);
            CreateAircraftShowcase(canvas.transform, font);
            CreateBrandLockup(canvas.transform, font);
            CreateLoginPanel(canvas.transform, font);
        }

        private void CreateBackground(Transform parent)
        {
            Texture2D backgroundTexture = Resources.Load<Texture2D>("Login/drone-bg");
            if (backgroundTexture != null)
            {
                RawImage background = CreateRawImage(parent, "Drone Background", backgroundTexture, Color.white);
                StretchToFill(background.GetComponent<RectTransform>());
            }
            else
            {
                GameObject fallback = CreatePanel(parent, "Fallback Background", Vector2.zero, Vector2.zero, new Color(0.88f, 0.96f, 1f, 1f));
                StretchToFill(fallback.GetComponent<RectTransform>());
            }

            GameObject tint = CreatePanel(parent, "White Blue Wash", Vector2.zero, Vector2.zero, new Color(0.86f, 0.95f, 1f, 0.72f));
            StretchToFill(tint.GetComponent<RectTransform>());

            GameObject leftVignette = CreatePanel(parent, "Left Soft Blue Field", Vector2.zero, Vector2.zero, new Color(0.96f, 0.99f, 1f, 0.72f));
            RectTransform vignetteRect = leftVignette.GetComponent<RectTransform>();
            vignetteRect.anchorMin = new Vector2(0f, 0f);
            vignetteRect.anchorMax = new Vector2(0.52f, 1f);
            vignetteRect.pivot = new Vector2(0f, 0.5f);
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;
        }

        private void CreateBrandLockup(Transform parent, Font font)
        {
            GameObject logoCard = CreatePanel(parent, "Logo Card", Vector2.zero, new Vector2(102f, 102f), new Color(1f, 1f, 1f, 0.96f));
            RectTransform logoCardRect = logoCard.GetComponent<RectTransform>();
            logoCardRect.anchorMin = new Vector2(0f, 0.5f);
            logoCardRect.anchorMax = logoCardRect.anchorMin;
            logoCardRect.pivot = new Vector2(0f, 0.5f);
            logoCardRect.anchoredPosition = new Vector2(126f, 244f);
            AddOutline(logoCard, new Color(0.24f, 0.58f, 1f, 0.34f));

            Texture2D logoTexture = Resources.Load<Texture2D>("Login/logo");
            if (logoTexture != null)
            {
                RawImage logo = CreateRawImage(logoCard.transform, "Logo Image", logoTexture, Color.white);
                RectTransform logoRect = logo.GetComponent<RectTransform>();
                logoRect.anchorMin = new Vector2(0.5f, 0.5f);
                logoRect.anchorMax = logoRect.anchorMin;
                logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.anchoredPosition = Vector2.zero;
                logoRect.sizeDelta = new Vector2(82f, 82f);
            }
            else
            {
                CreateText(logoCard.transform, "Q+", Vector2.zero, new Vector2(104f, 104f), 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.05f, 0.12f, 0.13f, 1f), font);
            }

            Text brandTitle = CreateText(parent, "\u9752\u9706\u542f\u98de", Vector2.zero, new Vector2(520f, 62f), 52, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.04f, 0.22f, 0.42f, 1f), font);
            RectTransform brandTitleRect = brandTitle.GetComponent<RectTransform>();
            brandTitleRect.anchorMin = new Vector2(0f, 0.5f);
            brandTitleRect.anchorMax = brandTitleRect.anchorMin;
            brandTitleRect.pivot = new Vector2(0f, 0.5f);
            brandTitleRect.anchoredPosition = new Vector2(250f, 268f);

            Text brandSubtitle = CreateText(parent, "\u4f4e\u7a7a\u98de\u884c\u8bad\u7ec3\u6a21\u62df\u5668", Vector2.zero, new Vector2(560f, 40f), 26, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.38f, 0.68f, 0.96f), font);
            RectTransform subtitleRect = brandSubtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0f, 0.5f);
            subtitleRect.anchorMax = subtitleRect.anchorMin;
            subtitleRect.pivot = new Vector2(0f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(252f, 210f);

            Text brandCode = CreateText(parent, "LOW ALTITUDE FLIGHT TRAINING SIMULATOR", Vector2.zero, new Vector2(560f, 28f), 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.82f), font);
            RectTransform codeRect = brandCode.GetComponent<RectTransform>();
            codeRect.anchorMin = new Vector2(0f, 0.5f);
            codeRect.anchorMax = codeRect.anchorMin;
            codeRect.pivot = new Vector2(0f, 0.5f);
            codeRect.anchoredPosition = new Vector2(254f, 170f);
        }

        private void CreateLoginPanel(Transform parent, Font font)
        {
            CreateLoginShadow(parent);

            GameObject loginPanel = CreatePanel(parent, "Login Glass Panel", Vector2.zero, new Vector2(650f, 390f), new Color(1f, 1f, 1f, 0.9f));
            RectTransform loginRect = loginPanel.GetComponent<RectTransform>();
            loginRect.anchorMin = new Vector2(0f, 0.5f);
            loginRect.anchorMax = loginRect.anchorMin;
            loginRect.pivot = new Vector2(0f, 0.5f);
            loginRect.anchoredPosition = new Vector2(126f, -110f);
            AddOutline(loginPanel, new Color(0.18f, 0.52f, 0.96f, 0.42f));
            CreateBrokenParticles(parent);

            CreateText(loginPanel.transform, "\u98de\u624b\u767b\u5f55", new Vector2(38f, -28f), new Vector2(220f, 42f), 28, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.04f, 0.22f, 0.42f, 1f), font);
            CreateText(loginPanel.transform, "\u8fde\u63a5\u8bad\u7ec3\u8231\uff0c\u542f\u52a8\u4f4e\u7a7a\u98de\u884c\u8bad\u7ec3\u3002", new Vector2(38f, -70f), new Vector2(560f, 32f), 17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.16f, 0.42f, 0.68f, 0.86f), font);

            accountInput = CreateInput(loginPanel.transform, "\u98de\u624b\u8d26\u53f7", "\u8bf7\u8f93\u5165\u8d26\u53f7", new Vector2(38f, -126f), new Vector2(270f, 52f), font, false);
            passwordInput = CreateInput(loginPanel.transform, "\u8bbf\u95ee\u5bc6\u7801", "\u8bf7\u8f93\u5165\u5bc6\u7801", new Vector2(342f, -126f), new Vector2(270f, 52f), font, true);

            Toggle rememberToggle = CreateToggle(loginPanel.transform, "\u8bb0\u4f4f\u98de\u624b", new Vector2(38f, -214f), font);
            rememberToggle.isOn = true;
            CreateText(loginPanel.transform, "\u672c\u5730\u6f14\u793a\u8d26\u53f7: pilot / 123456", new Vector2(-38f, -212f), new Vector2(280f, 28f), 16, FontStyle.Normal, TextAnchor.UpperRight, new Color(0.08f, 0.45f, 0.9f, 0.76f), font);

            Button loginButton = CreateButton(loginPanel.transform, "\u542f\u52a8\u8bad\u7ec3\u8231", new Vector2(38f, -256f), new Vector2(270f, 56f), font);
            loginButton.onClick.AddListener(SubmitLogin);

            Button exitButton = CreateButton(loginPanel.transform, "\u9000\u51fa\u7cfb\u7edf", new Vector2(342f, -256f), new Vector2(270f, 56f), font);
            exitButton.onClick.AddListener(Application.Quit);

            statusText = CreateText(loginPanel.transform, "\u7cfb\u7edf\u72b6\u6001: \u8bad\u7ec3\u7f51\u7edc\u5f85\u547d    \u98de\u63a7\u534f\u8bae: \u672c\u5730\u6f14\u793a\u6a21\u5f0f", new Vector2(38f, -336f), new Vector2(574f, 28f), 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f), font);
        }

        private void CreateLoginShadow(Transform parent)
        {
            GameObject deepShadow = CreatePanel(parent, "Login Deep Shadow", Vector2.zero, new Vector2(690f, 430f), new Color(0.16f, 0.48f, 0.92f, 0.12f));
            RectTransform deepRect = deepShadow.GetComponent<RectTransform>();
            deepRect.anchorMin = new Vector2(0f, 0.5f);
            deepRect.anchorMax = deepRect.anchorMin;
            deepRect.pivot = new Vector2(0f, 0.5f);
            deepRect.anchoredPosition = new Vector2(106f, -126f);

            GameObject blueShadow = CreatePanel(parent, "Login Blue Back Glow", Vector2.zero, new Vector2(670f, 410f), new Color(0.2f, 0.58f, 1f, 0.1f));
            RectTransform blueRect = blueShadow.GetComponent<RectTransform>();
            blueRect.anchorMin = new Vector2(0f, 0.5f);
            blueRect.anchorMax = blueRect.anchorMin;
            blueRect.pivot = new Vector2(0f, 0.5f);
            blueRect.anchoredPosition = new Vector2(116f, -118f);
            AddOutline(blueShadow, new Color(0.18f, 0.56f, 1f, 0.18f));
        }

        private void CreateAircraftShowcase(Transform parent, Font font)
        {
            GameObject showcase = CreatePanel(parent, "Right Aircraft Showcase", Vector2.zero, new Vector2(760f, 620f), new Color(0.96f, 0.99f, 1f, 0.48f));
            RectTransform showcaseRect = showcase.GetComponent<RectTransform>();
            showcaseRect.anchorMin = new Vector2(1f, 0.5f);
            showcaseRect.anchorMax = showcaseRect.anchorMin;
            showcaseRect.pivot = new Vector2(1f, 0.5f);
            showcaseRect.anchoredPosition = new Vector2(-118f, 8f);
            AddOutline(showcase, new Color(0.18f, 0.52f, 0.96f, 0.2f));

            GameObject pad = CreatePanel(showcase.transform, "Aircraft Landing Glow", Vector2.zero, new Vector2(520f, 118f), new Color(0.18f, 0.58f, 1f, 0.12f));
            RectTransform padRect = pad.GetComponent<RectTransform>();
            padRect.anchorMin = new Vector2(0.5f, 0.5f);
            padRect.anchorMax = padRect.anchorMin;
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.anchoredPosition = new Vector2(0f, -142f);
            AddOutline(pad, new Color(0.12f, 0.52f, 1f, 0.24f));

            GameObject root = new GameObject("Right Shaking Aircraft", typeof(RectTransform));
            root.transform.SetParent(showcase.transform, false);
            aircraftRoot = root.GetComponent<RectTransform>();
            aircraftRoot.anchorMin = new Vector2(0.5f, 0.5f);
            aircraftRoot.anchorMax = aircraftRoot.anchorMin;
            aircraftRoot.pivot = new Vector2(0.5f, 0.5f);
            aircraftRoot.sizeDelta = new Vector2(650f, 433f);
            aircraftBasePosition = new Vector2(34f, 16f);
            aircraftRoot.anchoredPosition = aircraftBasePosition;

            Texture2D aircraftTexture = Resources.Load<Texture2D>("Login/aircraft-showcase");
            if (aircraftTexture != null)
            {
                RawImage aircraftImage = CreateRawImage(aircraftRoot, "Aircraft Showcase Image", aircraftTexture, Color.white);
                RectTransform imageRect = aircraftImage.GetComponent<RectTransform>();
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;
            }
            else
            {
                Color blue = new Color(0.05f, 0.36f, 0.78f, 0.92f);
                Color softBlue = new Color(0.3f, 0.7f, 1f, 0.72f);
                Color pale = new Color(0.88f, 0.97f, 1f, 0.92f);

                CreateAircraftPart(aircraftRoot, "Body", Vector2.zero, new Vector2(150f, 66f), blue, 0f);
                CreateAircraftPart(aircraftRoot, "Nose", new Vector2(92f, 0f), new Vector2(52f, 38f), softBlue, 0f);
                CreateAircraftPart(aircraftRoot, "Left Arm", new Vector2(-116f, 52f), new Vector2(190f, 16f), blue, 28f);
                CreateAircraftPart(aircraftRoot, "Right Arm", new Vector2(116f, 52f), new Vector2(190f, 16f), blue, -28f);
                CreateAircraftPart(aircraftRoot, "Left Rear Arm", new Vector2(-116f, -52f), new Vector2(190f, 16f), blue, -28f);
                CreateAircraftPart(aircraftRoot, "Right Rear Arm", new Vector2(116f, -52f), new Vector2(190f, 16f), blue, 28f);

                CreateAircraftRotor(aircraftRoot, new Vector2(-178f, 94f), pale, softBlue);
                CreateAircraftRotor(aircraftRoot, new Vector2(178f, 94f), pale, softBlue);
                CreateAircraftRotor(aircraftRoot, new Vector2(-178f, -94f), pale, softBlue);
                CreateAircraftRotor(aircraftRoot, new Vector2(178f, -94f), pale, softBlue);
            }

            CreateText(showcase.transform, "AIRCRAFT READY", new Vector2(-36f, -36f), new Vector2(330f, 30f), 17, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.08f, 0.38f, 0.7f, 0.82f), font);
            CreateText(showcase.transform, "\u53f3\u4fa7\u98de\u884c\u5668\u5f85\u673a\u6296\u52a8", new Vector2(-36f, -70f), new Vector2(330f, 32f), 20, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.04f, 0.22f, 0.42f, 0.92f), font);
        }

        private static void CreateAircraftPart(Transform parent, string name, Vector2 position, Vector2 size, Color color, float rotation)
        {
            GameObject part = CreatePanel(parent, name, Vector2.zero, size, color);
            RectTransform rect = part.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            AddOutline(part, new Color(0.72f, 0.92f, 1f, 0.36f));
        }

        private static void CreateAircraftRotor(Transform parent, Vector2 position, Color outerColor, Color innerColor)
        {
            CreateAircraftPart(parent, "Rotor Disc", position, new Vector2(88f, 88f), outerColor, 45f);
            CreateAircraftPart(parent, "Rotor Hub", position, new Vector2(38f, 38f), innerColor, 45f);
        }

        private void CreateBrokenParticles(Transform parent)
        {
            shards.Clear();
            for (int i = 0; i < 30; i++)
            {
                float sideBand = Frac(Mathf.Sin(i * 19.37f) * 997.31f);
                float x = sideBand > 0.35f ? Mathf.Lerp(610f, 770f, Frac(Mathf.Sin(i * 3.91f) * 431.7f)) : Mathf.Lerp(92f, 680f, Frac(Mathf.Sin(i * 5.33f) * 719.2f));
                float y = sideBand > 0.35f ? Mathf.Lerp(88f, 430f, Frac(Mathf.Sin(i * 7.17f) * 211.9f)) : Mathf.Lerp(404f, 488f, Frac(Mathf.Sin(i * 11.13f) * 553.2f));
                float size = Mathf.Lerp(4f, 13f, Frac(Mathf.Sin(i * 13.57f) * 337.8f));
                Color color = i % 3 == 0
                    ? new Color(0.9f, 0.97f, 1f, 0.46f)
                    : new Color(0.18f, 0.56f, 1f, 0.32f);

                GameObject shard = CreatePanel(parent, "Broken Light Particle", new Vector2(x, y), new Vector2(size, size * Mathf.Lerp(0.28f, 0.9f, sideBand)), color);
                RectTransform rect = shard.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-38f, 38f, Frac(Mathf.Sin(i * 17.41f) * 899.4f)));

                shards.Add(new ShardParticle
                {
                    Rect = rect,
                    Image = shard.GetComponent<Image>(),
                    BasePosition = rect.anchoredPosition,
                    BaseColor = color,
                    Phase = i * 0.73f,
                    Drift = Mathf.Lerp(2.5f, 9.5f, Frac(Mathf.Sin(i * 23.1f) * 661.6f))
                });
            }
        }

        private void AnimateBrokenParticles()
        {
            for (int i = 0; i < shards.Count; i++)
            {
                ShardParticle shard = shards[i];
                if (shard.Rect == null || shard.Image == null)
                {
                    continue;
                }

                float pulse = 0.62f + Mathf.Sin(Time.time * 1.8f + shard.Phase) * 0.28f;
                Vector2 drift = new Vector2(
                    Mathf.Sin(Time.time * 0.9f + shard.Phase) * shard.Drift,
                    Mathf.Cos(Time.time * 0.7f + shard.Phase * 1.3f) * shard.Drift * 0.55f);
                shard.Rect.anchoredPosition = shard.BasePosition + drift;
                shard.Rect.localRotation = Quaternion.Euler(0f, 0f, shard.Rect.localEulerAngles.z + Time.deltaTime * (8f + shard.Drift));
                shard.Image.color = new Color(shard.BaseColor.r, shard.BaseColor.g, shard.BaseColor.b, shard.BaseColor.a * pulse);
            }
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        private void AnimateAircraftShowcase()
        {
            if (aircraftRoot == null)
            {
                return;
            }

            float time = Time.time;
            Vector2 jitter = new Vector2(
                Mathf.Sin(time * 8.2f) * 7f + Mathf.Sin(time * 17.1f) * 2.2f,
                Mathf.Cos(time * 6.6f) * 5.2f + Mathf.Sin(time * 13.4f) * 1.8f);
            aircraftRoot.anchoredPosition = aircraftBasePosition + jitter;
            aircraftRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 9.4f) * 1.2f);
            float scale = 1f + Mathf.Sin(time * 10.8f) * 0.012f;
            aircraftRoot.localScale = new Vector3(scale, scale, 1f);
        }

        private void SubmitLogin()
        {
            string account = accountInput != null ? accountInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;
            bool emptyInput = string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password);
            bool demoMismatch = !emptyInput && (account != demoAccount || password != demoPassword);

            if (emptyInput)
            {
                SetStatus("\u8eab\u4efd\u9a8c\u8bc1\u5931\u8d25: \u8bf7\u8f93\u5165\u98de\u624b\u8d26\u53f7\u4e0e\u8bbf\u95ee\u5bc6\u7801\u3002");
                return;
            }

            if (demoMismatch)
            {
                SetStatus("\u672c\u5730\u6f14\u793a\u8d26\u53f7: pilot / 123456");
                return;
            }

            SetStatus("\u9a8c\u8bc1\u901a\u8fc7\uff0c\u8bad\u7ec3\u8231\u5df2\u5c31\u7eea\u3002\u6b63\u5728\u52a0\u8f7d\u822a\u7ebf...");
            isLoggingIn = true;
            loginStartTime = Time.time;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Login Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static InputField CreateInput(Transform parent, string label, string placeholder, Vector2 position, Vector2 size, Font font, bool password)
        {
            CreateText(parent, label, position, new Vector2(size.x, 24f), 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.1f, 0.36f, 0.64f, 0.92f), font);
            GameObject inputObject = CreatePanel(parent, label + " Input", position + new Vector2(0f, -28f), size, new Color(0.94f, 0.98f, 1f, 0.96f));
            AddOutline(inputObject, new Color(0.2f, 0.56f, 1f, 0.3f));

            InputField input = inputObject.AddComponent<InputField>();
            Text text = CreateInputText(inputObject.transform, string.Empty, new Color(0.04f, 0.18f, 0.34f, 1f), font);
            Text placeholderText = CreateInputText(inputObject.transform, placeholder, new Color(0.18f, 0.42f, 0.62f, 0.48f), font);

            input.textComponent = text;
            input.placeholder = placeholderText;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            return input;
        }

        private static Text CreateInputText(Transform parent, string value, Color color, Font font)
        {
            Text text = CreateText(parent, value, Vector2.zero, Vector2.zero, 17, FontStyle.Normal, TextAnchor.MiddleLeft, color, font);
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(14f, 0f);
            rect.offsetMax = new Vector2(-14f, 0f);
            return text;
        }

        private static Toggle CreateToggle(Transform parent, string label, Vector2 position, Font font)
        {
            GameObject toggleObject = new GameObject(label + " Toggle", typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);
            RectTransform rect = toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(180f, 28f);

            GameObject box = CreatePanel(toggleObject.transform, "Box", new Vector2(0f, 0f), new Vector2(22f, 22f), new Color(0.94f, 0.98f, 1f, 0.96f));
            AddOutline(box, new Color(0.2f, 0.56f, 1f, 0.3f));

            GameObject check = CreatePanel(box.transform, "Checkmark", Vector2.zero, new Vector2(12f, 12f), new Color(0.08f, 0.44f, 0.92f, 1f));
            RectTransform checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = checkRect.anchorMin;
            checkRect.pivot = new Vector2(0.5f, 0.5f);

            CreateText(toggleObject.transform, label, new Vector2(32f, 1f), new Vector2(140f, 24f), 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.38f, 0.62f, 0.86f), font);

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }

        private static Button CreateButton(Transform parent, string text, Vector2 position, Vector2 size, Font font)
        {
            GameObject buttonObject = CreatePanel(parent, text + " Button", position, size, new Color(0.08f, 0.44f, 0.92f, 0.9f));
            AddOutline(buttonObject, new Color(0.72f, 0.92f, 1f, 0.58f));
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.44f, 0.92f, 0.9f);
            colors.highlightedColor = new Color(0.22f, 0.64f, 1f, 0.98f);
            colors.pressedColor = new Color(0.04f, 0.28f, 0.68f, 1f);
            button.colors = colors;
            CreateText(buttonObject.transform, text, Vector2.zero, size, 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, font);
            return button;
        }

        private static RawImage CreateRawImage(Transform parent, string objectName, Texture texture, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment, Color color, Font font)
        {
            GameObject textObject = new GameObject(string.IsNullOrEmpty(text) ? "Text" : text, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            bool left = alignment == TextAnchor.UpperLeft || alignment == TextAnchor.MiddleLeft;
            bool right = alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight;
            rect.anchorMin = right ? new Vector2(1f, 1f) : left ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = right ? new Vector2(1f, 1f) : left ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text label = textObject.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
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
            return panel;
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
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.88f, 0.96f, 1f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private struct ShardParticle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 BasePosition;
            public Color BaseColor;
            public float Phase;
            public float Drift;
        }
    }
}
