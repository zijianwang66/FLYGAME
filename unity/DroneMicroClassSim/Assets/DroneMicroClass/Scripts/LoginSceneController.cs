using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class LoginSceneController : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "MainMenu";

        private readonly System.Collections.Generic.List<ShardParticle> shards = new System.Collections.Generic.List<ShardParticle>();
        private GameObject loginPanel;
        private GameObject registrationPanel;
        private GameObject activationPanel;
        private InputField accountInput;
        private InputField passwordInput;
        private InputField activationCodeInput;
        private InputField registerAccountInput;
        private InputField registerPasswordInput;
        private InputField registerDisplayNameInput;
        private InputField registerClassInput;
        private Text statusText;
        private Text activationStatusText;
        private Text registerStatusText;
        private RectTransform aircraftRoot;
        private Transform lightningFxRoot;
        private Vector2 aircraftBasePosition;
        private float loginStartTime;
        private float registrationFxStartedAt = -10f;
        private float loginPanelFxStartedAt = -10f;
        private float activationSuccessAt = -10f;
        private bool isLoggingIn;
        private bool activationCompleted;
        private bool pendingRegistrationPanel;
        private bool pendingLoginPanelReturn;

        private void Awake()
        {
            LocalUserDatabase.EnsureInitialized();
            CurrentUserSession.Clear();
            EnsureSceneCamera();
            EnsureEventSystem();
            BuildLoginUi();
        }

        private void Update()
        {
            AnimateAircraftShowcase();
            AnimateBrokenParticles();

            if (pendingRegistrationPanel && Time.time - registrationFxStartedAt > 0.28f)
            {
                pendingRegistrationPanel = false;
                ShowRegistrationPanel();
            }

            if (pendingLoginPanelReturn && Time.time - loginPanelFxStartedAt > 0.22f)
            {
                pendingLoginPanelReturn = false;
                ShowLoginPanel();
            }

            if (isLoggingIn && Time.time - loginStartTime > 0.85f)
            {
                SceneManager.LoadScene(nextSceneName);
            }

            if (activationCompleted && Time.time - activationSuccessAt > 0.65f)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void BuildLoginUi()
        {
            Canvas canvas = CreateCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateBackground(canvas.transform);
            CreateAircraftShowcase(canvas.transform, font);
            CreateBrandLockup(canvas.transform, font);
            if (ActivationManager.IsActivated())
            {
                CreateLoginPanel(canvas.transform, font);
            }
            else
            {
                CreateActivationPanel(canvas.transform, font);
            }
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

            loginPanel = CreatePanel(parent, "Login Glass Panel", Vector2.zero, new Vector2(650f, 390f), new Color(1f, 1f, 1f, 0.9f));
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

            CreateText(loginPanel.transform, "\u9ed8\u8ba4\u8d26\u53f7: pilot / 123456", new Vector2(38f, -212f), new Vector2(280f, 28f), 16, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.08f, 0.45f, 0.9f, 0.76f), font);

            Button loginButton = CreateButton(loginPanel.transform, "\u542f\u52a8\u8bad\u7ec3\u8231", new Vector2(38f, -256f), new Vector2(270f, 56f), font);
            RectTransform loginButtonRect = loginButton.GetComponent<RectTransform>();
            loginButton.onClick.AddListener(() =>
            {
                PlayButtonLightning(loginButtonRect);
                SubmitLogin();
            });

            Button registerButton = CreateButton(loginPanel.transform, "\u6ce8\u518c\u8d26\u53f7", new Vector2(342f, -256f), new Vector2(270f, 56f), font);
            RectTransform registerButtonRect = registerButton.GetComponent<RectTransform>();
            registerButton.onClick.AddListener(() => PlayRegistrationLightning(registerButtonRect));

            statusText = CreateText(loginPanel.transform, "\u7cfb\u7edf\u72b6\u6001: \u672c\u5730\u7528\u6237\u6570\u636e\u5e93\u5df2\u5c31\u7eea    \u98de\u63a7\u534f\u8bae: \u5355\u673a\u8bad\u7ec3\u6a21\u5f0f", new Vector2(38f, -336f), new Vector2(574f, 28f), 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f), font);
            CreateRegistrationPanel(parent, font);
        }

        private void CreateActivationPanel(Transform parent, Font font)
        {
            CreateLoginShadow(parent);

            activationPanel = CreatePanel(parent, "Activation Panel", Vector2.zero, new Vector2(650f, 430f), new Color(1f, 1f, 1f, 0.94f));
            RectTransform activationRect = activationPanel.GetComponent<RectTransform>();
            activationRect.anchorMin = new Vector2(0f, 0.5f);
            activationRect.anchorMax = activationRect.anchorMin;
            activationRect.pivot = new Vector2(0f, 0.5f);
            activationRect.anchoredPosition = new Vector2(126f, -110f);
            AddOutline(activationPanel, new Color(0.18f, 0.52f, 0.96f, 0.42f));
            CreateBrokenParticles(parent);

            CreateText(activationPanel.transform, "\u8bbe\u5907\u6fc0\u6d3b", new Vector2(38f, -28f), new Vector2(220f, 42f), 28, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.04f, 0.22f, 0.42f, 1f), font);
            CreateText(activationPanel.transform, "\u8bf7\u5c06\u672c\u673a\u673a\u5668\u7801\u53d1\u7ed9\u7ba1\u7406\u5458\uff0c\u83b7\u53d6\u5bf9\u5e94\u6fc0\u6d3b\u7801\u540e\u624d\u80fd\u8fdb\u5165\u7cfb\u7edf\u3002", new Vector2(38f, -70f), new Vector2(574f, 48f), 17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.16f, 0.42f, 0.68f, 0.86f), font);

            CreateText(activationPanel.transform, "\u672c\u673a\u673a\u5668\u7801", new Vector2(38f, -132f), new Vector2(220f, 24f), 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.1f, 0.36f, 0.64f, 0.92f), font);
            Text machineCodeText = CreateText(activationPanel.transform, ActivationManager.MachineCode, new Vector2(38f, -162f), new Vector2(400f, 40f), 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.04f, 0.18f, 0.34f, 1f), font);
            AddOutline(machineCodeText.gameObject, new Color(0.2f, 0.56f, 1f, 0.18f));

            Button copyButton = CreateButton(activationPanel.transform, "\u590d\u5236\u673a\u5668\u7801", new Vector2(462f, -154f), new Vector2(150f, 48f), font);
            copyButton.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = ActivationManager.MachineCode;
                SetActivationStatus("\u673a\u5668\u7801\u5df2\u590d\u5236\uff0c\u8bf7\u53d1\u7ed9\u7ba1\u7406\u5458\u751f\u6210\u6fc0\u6d3b\u7801\u3002");
            });

            activationCodeInput = CreateInput(activationPanel.transform, "\u6fc0\u6d3b\u7801", "\u8bf7\u8f93\u5165\u7ba1\u7406\u5458\u63d0\u4f9b\u7684\u6fc0\u6d3b\u7801", new Vector2(38f, -244f), new Vector2(574f, 52f), font, false);

            Button activateButton = CreateButton(activationPanel.transform, "\u6fc0\u6d3b\u5e76\u8fdb\u5165", new Vector2(38f, -334f), new Vector2(574f, 56f), font);
            activateButton.onClick.AddListener(SubmitActivation);

            activationStatusText = CreateText(activationPanel.transform, "\u672a\u6fc0\u6d3b\uff1a\u672c\u6b21\u5b89\u88c5\u9700\u8981\u4e00\u673a\u4e00\u7801\u3002", new Vector2(38f, -398f), new Vector2(574f, 28f), 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f), font);
        }

        private void CreateRegistrationPanel(Transform parent, Font font)
        {
            registrationPanel = CreatePanel(parent, "Registration Glass Panel", Vector2.zero, new Vector2(650f, 470f), new Color(1f, 1f, 1f, 0.94f));
            RectTransform registerRect = registrationPanel.GetComponent<RectTransform>();
            registerRect.anchorMin = new Vector2(0f, 0.5f);
            registerRect.anchorMax = registerRect.anchorMin;
            registerRect.pivot = new Vector2(0f, 0.5f);
            registerRect.anchoredPosition = new Vector2(126f, -110f);
            AddOutline(registrationPanel, new Color(0.18f, 0.52f, 0.96f, 0.42f));

            CreateText(registrationPanel.transform, "\u6ce8\u518c\u5b66\u5458\u8d26\u53f7", new Vector2(38f, -28f), new Vector2(260f, 42f), 28, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.04f, 0.22f, 0.42f, 1f), font);
            CreateText(registrationPanel.transform, "\u8d26\u53f7\u4fdd\u5b58\u5728\u672c\u673a SQLite \u6570\u636e\u5e93\uff0c\u6bcf\u53f0\u8bbe\u5907\u72ec\u7acb\u4f7f\u7528\u3002", new Vector2(38f, -70f), new Vector2(560f, 32f), 17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.16f, 0.42f, 0.68f, 0.86f), font);

            registerAccountInput = CreateInput(registrationPanel.transform, "\u767b\u5f55\u8d26\u53f7", "\u81f3\u5c11 3 \u4f4d\uff0c\u4e0d\u8981\u7a7a\u683c", new Vector2(38f, -126f), new Vector2(270f, 52f), font, false);
            registerDisplayNameInput = CreateInput(registrationPanel.transform, "\u5b66\u5458\u59d3\u540d", "\u8bf7\u8f93\u5165\u59d3\u540d", new Vector2(342f, -126f), new Vector2(270f, 52f), font, false);
            registerClassInput = CreateInput(registrationPanel.transform, "\u73ed\u7ea7 / \u673a\u53f7", "\u4f8b\u5982\uff1a\u4e00\u73ed", new Vector2(38f, -222f), new Vector2(270f, 52f), font, false);
            registerPasswordInput = CreateInput(registrationPanel.transform, "\u8bbf\u95ee\u5bc6\u7801", "\u81f3\u5c11 6 \u4f4d", new Vector2(342f, -222f), new Vector2(270f, 52f), font, true);

            Button createButton = CreateButton(registrationPanel.transform, "\u521b\u5efa\u5e76\u8fdb\u5165", new Vector2(38f, -328f), new Vector2(270f, 56f), font);
            RectTransform createButtonRect = createButton.GetComponent<RectTransform>();
            createButton.onClick.AddListener(() =>
            {
                PlayButtonLightning(createButtonRect);
                SubmitRegistration();
            });

            Button backButton = CreateButton(registrationPanel.transform, "\u8fd4\u56de\u767b\u5f55", new Vector2(342f, -328f), new Vector2(270f, 56f), font);
            RectTransform backButtonRect = backButton.GetComponent<RectTransform>();
            backButton.onClick.AddListener(() => PlayReturnLightning(backButtonRect));

            registerStatusText = CreateText(registrationPanel.transform, "\u6ce8\u518c\u540e\u5c06\u4ee5\u666e\u901a\u5b66\u5458\u8eab\u4efd\u8fdb\u5165\u7cfb\u7edf\u3002", new Vector2(38f, -408f), new Vector2(574f, 28f), 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f), font);
            registrationPanel.SetActive(false);
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

        private void PlayRegistrationLightning(RectTransform sourceRect)
        {
            if (pendingRegistrationPanel || isLoggingIn)
            {
                return;
            }

            pendingRegistrationPanel = true;
            registrationFxStartedAt = Time.time;
            SetStatus("\u6ce8\u518c\u901a\u9053\u6fc0\u6d3b\uff1a\u6b63\u5728\u6253\u5f00\u672c\u673a\u5b66\u5458\u6ce8\u518c...");
            CreateLightningBurst(loginPanel != null ? loginPanel.transform : transform, GetRectCenter(sourceRect));
        }

        private void PlayReturnLightning(RectTransform sourceRect)
        {
            if (pendingLoginPanelReturn || isLoggingIn)
            {
                return;
            }

            pendingLoginPanelReturn = true;
            loginPanelFxStartedAt = Time.time;
            PlayButtonLightning(sourceRect);
        }

        private void PlayButtonLightning(RectTransform sourceRect)
        {
            Transform parent = sourceRect != null && sourceRect.parent != null ? sourceRect.parent : transform;
            CreateLightningBurst(parent, GetRectCenter(sourceRect));
        }

        private static Vector2 GetRectCenter(RectTransform rect)
        {
            if (rect == null)
            {
                return new Vector2(476f, -284f);
            }

            return rect.anchoredPosition + new Vector2(rect.sizeDelta.x * 0.5f, -rect.sizeDelta.y * 0.5f);
        }

        private void CreateLightningBurst(Transform parent, Vector2 origin)
        {
            if (parent == null)
            {
                return;
            }

            if (lightningFxRoot != null)
            {
                Destroy(lightningFxRoot.gameObject);
            }

            GameObject root = new GameObject("Transparent Lightning FX", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            lightningFxRoot = root.transform;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0.74f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            root.transform.SetAsLastSibling();

            Vector2[] points =
            {
                origin,
                origin + new Vector2(-26f, -16f),
                origin + new Vector2(16f, -30f),
                origin + new Vector2(-8f, -54f),
                origin + new Vector2(44f, -36f)
            };

            for (int i = 0; i < points.Length - 1; i++)
            {
                CreateLightningSegment(root.transform, points[i], points[i + 1], 6f - i, new Color(0.15f, 0.76f, 1f, 0.58f));
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                float radius = 20f + (i % 4) * 7f;
                Vector2 position = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                GameObject spark = CreateLightningVisual(root.transform, "Lightning Spark", position, new Vector2(8f + i % 3 * 3f, 8f + i % 2 * 4f), new Color(0.1f, 0.68f, 1f, 0.42f));
                spark.AddComponent<LightningFade>().Configure(0.42f + i * 0.015f, 28f + i * 2f);
            }

            root.AddComponent<LightningFade>().Configure(0.55f, 0f);
        }

        private static void CreateLightningSegment(Transform parent, Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            GameObject line = CreateLightningVisual(parent, "Lightning Segment", from + delta * 0.5f, new Vector2(length, Mathf.Max(2f, width)), color);
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            line.AddComponent<LightningFade>().Configure(0.5f, 18f);
        }

        private static GameObject CreateLightningVisual(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject visual = CreatePanel(parent, objectName, position, size, color);
            Image image = visual.GetComponent<Image>();
            image.raycastTarget = false;
            return visual;
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

        private void SubmitActivation()
        {
            string activationCode = activationCodeInput != null ? activationCodeInput.text : string.Empty;
            if (!ActivationManager.TryActivate(activationCode, out string message))
            {
                SetActivationStatus(message);
                return;
            }

            SetActivationStatus(message);
            activationCompleted = true;
            activationSuccessAt = Time.time;
        }

        private void SubmitLogin()
        {
            string account = accountInput != null ? accountInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;
            bool emptyInput = string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password);

            if (emptyInput)
            {
                SetStatus("\u8eab\u4efd\u9a8c\u8bc1\u5931\u8d25: \u8bf7\u8f93\u5165\u98de\u624b\u8d26\u53f7\u4e0e\u8bbf\u95ee\u5bc6\u7801\u3002");
                return;
            }

            if (!LocalUserDatabase.Authenticate(account, password, out LocalUserDatabase.UserRecord user))
            {
                SetStatus("\u8eab\u4efd\u9a8c\u8bc1\u5931\u8d25: \u8d26\u53f7\u6216\u5bc6\u7801\u4e0d\u6b63\u786e\u3002");
                return;
            }

            CurrentUserSession.SetUser(user);
            string displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
            SetStatus("\u9a8c\u8bc1\u901a\u8fc7: " + displayName + "\uff0c\u8bad\u7ec3\u8231\u5df2\u5c31\u7eea\u3002\u6b63\u5728\u52a0\u8f7d\u822a\u7ebf...");
            isLoggingIn = true;
            loginStartTime = Time.time;
        }

        private void SubmitRegistration()
        {
            string account = registerAccountInput != null ? registerAccountInput.text.Trim() : string.Empty;
            string password = registerPasswordInput != null ? registerPasswordInput.text : string.Empty;
            string displayName = registerDisplayNameInput != null ? registerDisplayNameInput.text.Trim() : string.Empty;
            string className = registerClassInput != null ? registerClassInput.text.Trim() : string.Empty;

            if (!LocalUserDatabase.RegisterStudent(account, password, displayName, className, out LocalUserDatabase.UserRecord user, out string message))
            {
                SetRegisterStatus("\u6ce8\u518c\u5931\u8d25: " + message);
                return;
            }

            CurrentUserSession.SetUser(user);
            string name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
            SetRegisterStatus("\u6ce8\u518c\u6210\u529f: " + name + "\uff0c\u6b63\u5728\u8fdb\u5165\u7cfb\u7edf...");
            isLoggingIn = true;
            loginStartTime = Time.time;
        }

        private void ShowRegistrationPanel()
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(false);
            }

            if (registrationPanel != null)
            {
                registrationPanel.SetActive(true);
            }

            SetRegisterStatus("\u6ce8\u518c\u540e\u5c06\u4ee5\u666e\u901a\u5b66\u5458\u8eab\u4efd\u8fdb\u5165\u7cfb\u7edf\u3002");
        }

        private void ShowLoginPanel()
        {
            if (registrationPanel != null)
            {
                registrationPanel.SetActive(false);
            }

            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
            }

            SetStatus("\u7cfb\u7edf\u72b6\u6001: \u672c\u5730\u7528\u6237\u6570\u636e\u5e93\u5df2\u5c31\u7eea    \u98de\u63a7\u534f\u8bae: \u5355\u673a\u8bad\u7ec3\u6a21\u5f0f");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void SetActivationStatus(string message)
        {
            if (activationStatusText != null)
            {
                activationStatusText.text = message;
            }
        }

        private void SetRegisterStatus(string message)
        {
            if (registerStatusText != null)
            {
                registerStatusText.text = message;
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
            ResponsiveCanvasScaler.ApplyTo(canvas);
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

        private sealed class LightningFade : MonoBehaviour
        {
            private RectTransform rect;
            private Graphic graphic;
            private Color baseColor;
            private float duration = 0.5f;
            private float driftSpeed;
            private float startedAt;

            public void Configure(float effectDuration, float upwardDrift)
            {
                rect = GetComponent<RectTransform>();
                graphic = GetComponent<Graphic>();
                if (graphic != null)
                {
                    baseColor = graphic.color;
                }

                duration = Mathf.Max(0.05f, effectDuration);
                driftSpeed = upwardDrift;
                startedAt = Time.time;
            }

            private void Update()
            {
                float progress = Mathf.Clamp01((Time.time - startedAt) / duration);
                if (graphic != null)
                {
                    graphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - progress));
                }

                if (rect != null && driftSpeed > 0f)
                {
                    rect.anchoredPosition += Vector2.up * driftSpeed * Time.deltaTime;
                    float scale = 1f + progress * 0.28f;
                    rect.localScale = new Vector3(scale, scale, 1f);
                }

                if (progress >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
