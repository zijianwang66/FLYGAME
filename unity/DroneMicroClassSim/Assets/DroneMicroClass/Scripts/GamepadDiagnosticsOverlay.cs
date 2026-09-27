using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class GamepadDiagnosticsOverlay : MonoBehaviour
    {
        private static readonly string[] AxisNames =
        {
            "Drone Left Stick X",
            "Drone Left Stick Y",
            "Drone Right Stick X",
            "Drone Right Stick Y",
            "Drone Raw Axis 1",
            "Drone Raw Axis 2",
            "Drone Raw Axis 3",
            "Drone Raw Axis 4",
            "Drone Raw Axis 5",
            "Drone Raw Axis 6",
            "Drone Raw Axis 7",
            "Drone Raw Axis 8",
            "Drone Raw Axis 9",
            "Drone Raw Axis 10",
        };

        private static readonly KeyCode[] Buttons =
        {
            KeyCode.JoystickButton0,
            KeyCode.JoystickButton1,
            KeyCode.JoystickButton2,
            KeyCode.JoystickButton3,
            KeyCode.JoystickButton4,
            KeyCode.JoystickButton5,
            KeyCode.JoystickButton6,
            KeyCode.JoystickButton7,
            KeyCode.JoystickButton8,
            KeyCode.JoystickButton9,
            KeyCode.JoystickButton10,
            KeyCode.JoystickButton11,
            KeyCode.JoystickButton12,
            KeyCode.JoystickButton13,
            KeyCode.JoystickButton14,
            KeyCode.JoystickButton15,
        };

        private const float UpdateInterval = 0.08f;
        private static GamepadDiagnosticsOverlay instance;
        private readonly StringBuilder builder = new StringBuilder(1200);
        private GameObject panel;
        private Text readout;
        private float nextUpdateTime;
        private bool visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOverlay()
        {
            if (instance != null)
            {
                return;
            }

            GameObject overlayObject = new GameObject("Gamepad Diagnostics Overlay");
            instance = overlayObject.AddComponent<GamepadDiagnosticsOverlay>();
            DontDestroyOnLoad(overlayObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildUi();
            SetVisible(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SetVisible(!visible);
            }

            if (!visible || Time.unscaledTime < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = Time.unscaledTime + UpdateInterval;
            RefreshReadout();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "Gamepad Diagnostics Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panel = new GameObject("Diagnostics Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(560f, 620f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.035f, 0.04f, 0.88f);
            panelImage.raycastTarget = false;

            readout = new GameObject("Readout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
                .GetComponent<Text>();
            readout.transform.SetParent(panel.transform, false);
            readout.font = GetReadableFont();
            readout.fontSize = 20;
            readout.lineSpacing = 1.08f;
            readout.color = new Color(0.92f, 0.97f, 1f, 1f);
            readout.alignment = TextAnchor.UpperLeft;
            readout.raycastTarget = false;

            RectTransform readoutRect = readout.rectTransform;
            readoutRect.anchorMin = new Vector2(0f, 0f);
            readoutRect.anchorMax = new Vector2(1f, 1f);
            readoutRect.offsetMin = new Vector2(22f, 18f);
            readoutRect.offsetMax = new Vector2(-22f, -18f);
        }

        private void SetVisible(bool isVisible)
        {
            visible = isVisible;
            if (panel != null)
            {
                panel.SetActive(isVisible);
            }

            if (visible)
            {
                RefreshReadout();
            }
        }

        private void RefreshReadout()
        {
            builder.Clear();
            builder.AppendLine("手柄检测 / 校准  F2关闭");
            builder.AppendLine();

            string[] joystickNames = Input.GetJoystickNames();
            if (joystickNames.Length == 0)
            {
                builder.AppendLine("未检测到手柄。");
            }
            else
            {
                builder.AppendLine("已检测设备:");
                for (int i = 0; i < joystickNames.Length; i++)
                {
                    string joystickName = string.IsNullOrWhiteSpace(joystickNames[i]) ? "(未命名设备)" : joystickNames[i];
                    builder.AppendLine($"{i + 1}. {joystickName}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("常用映射:");
            AppendAxis("左摇杆 横滚", "Drone Left Stick X");
            AppendAxis("左摇杆 俯仰", "Drone Left Stick Y");
            AppendAxis("右摇杆 偏航", "Drone Right Stick X");
            AppendAxis("右摇杆 升降", "Drone Right Stick Y");

            builder.AppendLine();
            builder.AppendLine("原始轴检测:");
            for (int i = 1; i <= 10; i++)
            {
                AppendAxis($"轴{i:00}", $"Drone Raw Axis {i}");
            }

            builder.AppendLine();
            builder.Append("当前按下按钮: ");
            bool anyButton = false;
            for (int i = 0; i < Buttons.Length; i++)
            {
                if (!Input.GetKey(Buttons[i]))
                {
                    continue;
                }

                if (anyButton)
                {
                    builder.Append(", ");
                }

                builder.Append(i);
                anyButton = true;
            }

            builder.AppendLine(anyButton ? string.Empty : "无");
            builder.AppendLine();
            builder.AppendLine("解锁: 左摇杆右下 + 右摇杆左下，长按 2 秒");
            builder.AppendLine("锁定: 左摇杆左下 + 右摇杆右下，长按 2 秒");
            builder.AppendLine("键盘解锁: S + D + ↓ + ←，长按 2 秒");
            builder.AppendLine("键盘锁定: S + A + ↓ + →，长按 2 秒");
            builder.AppendLine("降落: B / Circle 或 L");

            if (readout != null)
            {
                readout.text = builder.ToString();
            }
        }

        private void AppendAxis(string label, string axisName)
        {
            float value = TryReadAxis(axisName);
            builder.Append(label);
            builder.Append(": ");
            builder.Append(value.ToString("+0.00;-0.00; 0.00"));
            builder.Append(" ");
            builder.AppendLine(BuildMeter(value));
        }

        private static float TryReadAxis(string axisName)
        {
            try
            {
                return Input.GetAxisRaw(axisName);
            }
            catch (ArgumentException)
            {
                return 0f;
            }
        }

        private static string BuildMeter(float value)
        {
            const int slots = 13;
            int center = slots / 2;
            int filled = Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * center);
            char[] chars = new char[slots];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = '-';
            }

            chars[center] = '|';
            if (filled != 0)
            {
                int target = Mathf.Clamp(center + filled, 0, slots - 1);
                int start = Mathf.Min(center, target);
                int end = Mathf.Max(center, target);
                for (int i = start; i <= end; i++)
                {
                    chars[i] = i == center ? '|' : '#';
                }
            }

            return new string(chars);
        }

        private static Font GetReadableFont()
        {
            return Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS" },
                20);
        }
    }
}
