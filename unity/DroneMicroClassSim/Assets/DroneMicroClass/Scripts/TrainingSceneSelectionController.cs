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
        private GameObject userDataDropdown;
        private GameObject reportListPanel;
        private GameObject reportTableRoot;
        private Text userDataButtonText;
        private Text[] userDataValueTexts;
        private Text reportEmptyText;
        private Text sunAltitudeStatusText;
        private Image morningSunButtonImage;
        private Image afternoonSunButtonImage;
        private SunAltitudePreset selectedSunPreset = SunAltitudePreset.Morning;
        private bool userDataExpanded;

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
                new SceneOption("矩形飞行训练", "RECTANGLE COURSE", "沿矩形航线依次通过检查点，完成一整圈规范飞行。", "矩形飞行训练", "SceneThumbnails/rectangle-course"),
                new SceneOption("8字飞行训练", "FIGURE EIGHT COURSE", "沿8字航线保持高度与速度，完成连续转向训练。", "8字飞行", "SceneThumbnails/figure-eight-flight"),
                new SceneOption("校园自由飞行", "CAMPUS FREE FLIGHT", "在校园建筑环境中自由练习起飞、巡航和降落。", "学校", "SceneThumbnails/campus-free-flight"),
                new SceneOption("森林自由飞行", "FOREST FREE FLIGHT", "在森林与丘陵环境中练习低空飞行和抗风控制。", "forest", "SceneThumbnails/forest-free-flight")
            };

            GameObject content = CreatePanel(
                canvas.transform,
                "Scene Selection Field",
                new Vector2(80f, -252f),
                new Vector2(1760f, 548f),
                new Color(0.94f, 0.985f, 1f, 0.68f));
            AddOutline(content, new Color(0.18f, 0.52f, 0.96f, 0.28f));

            const float cardWidth = 402f;
            const float cardGap = 28f;
            for (int i = 0; i < options.Length; i++)
            {
                CreateSceneCard(content.transform, options[i], new Vector2(28f + i * (cardWidth + cardGap), -28f), cardWidth);
            }

            Button backButton = CreateButton(canvas.transform, "返回主页", new Vector2(-80f, -58f), new Vector2(170f, 52f));
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.one;
            backRect.anchorMax = Vector2.one;
            backRect.pivot = Vector2.one;
            backButton.onClick.AddListener(ReturnToMainMenu);

            CreateUserDataControls(canvas.transform);
            CreateReportListPanel(canvas.transform);
            CreateSunAltitudeControls(canvas.transform);

            Text status = CreateText(
                canvas.transform,
                "当前状态：训练场景已就绪 ｜ 请选择训练项目",
                new Vector2(80f, 28f),
                new Vector2(720f, 28f),
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

            CreateText(parent, "青霆启飞", new Vector2(180f, -54f), new Vector2(360f, 52f), 38, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            CreateText(parent, "低空飞行训练模拟器", new Vector2(182f, -104f), new Vector2(430f, 32f), 20, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.08f, 0.38f, 0.68f, 0.95f));
            CreateText(parent, "训练场-场景选择", new Vector2(0f, -82f), new Vector2(720f, 62f), 46, FontStyle.Bold, TextAnchor.UpperCenter, ink);
            CreateText(parent, "选择训练环境，系统将载入对应的飞行任务", new Vector2(0f, -148f), new Vector2(760f, 34f), 20, FontStyle.Normal, TextAnchor.UpperCenter, mutedBlue);
        }

        private void CreateUserDataControls(Transform parent)
        {
            Button userDataButton = CreateButton(parent, "用户数据 ▾", new Vector2(-280f, -58f), new Vector2(170f, 52f));
            RectTransform buttonRect = userDataButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.one;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = Vector2.one;
            userDataButtonText = userDataButton.GetComponentInChildren<Text>();
            userDataButton.onClick.AddListener(ToggleUserDataDropdown);

            userDataDropdown = CreatePanel(parent, "User Data Dropdown", new Vector2(-80f, -126f), new Vector2(392f, 472f), new Color(0.98f, 0.995f, 1f, 0.98f));
            RectTransform dropdownRect = userDataDropdown.GetComponent<RectTransform>();
            dropdownRect.anchorMin = Vector2.one;
            dropdownRect.anchorMax = Vector2.one;
            dropdownRect.pivot = Vector2.one;
            AddOutline(userDataDropdown, new Color(0.18f, 0.52f, 0.96f, 0.34f));

            CreateText(userDataDropdown.transform, "用户数据", new Vector2(24f, -20f), new Vector2(190f, 32f), 24, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            CreateText(userDataDropdown.transform, "纵向训练对比", new Vector2(24f, -54f), new Vector2(190f, 24f), 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.78f));
            CreateUserDataMetricRows(userDataDropdown.transform);

            Button reportButton = CreateButton(userDataDropdown.transform, "进入用户报告列表", new Vector2(24f, -402f), new Vector2(344f, 46f));
            reportButton.onClick.AddListener(ShowReportList);
            userDataDropdown.SetActive(false);
        }

        private void CreateUserDataMetricRows(Transform parent)
        {
            string[] labels =
            {
                "当前用户",
                "记录范围",
                "训练次数",
                "完成时间对比",
                "得分变化",
                "优势分析",
                "劣势分析"
            };

            Color[] rowColors =
            {
                new Color(0.08f, 0.44f, 0.92f, 0.13f),
                new Color(0.05f, 0.62f, 0.72f, 0.13f),
                new Color(0.20f, 0.62f, 0.28f, 0.13f),
                new Color(0.96f, 0.62f, 0.08f, 0.16f),
                new Color(0.70f, 0.32f, 0.90f, 0.14f),
                new Color(0.10f, 0.58f, 0.36f, 0.14f),
                new Color(0.92f, 0.24f, 0.24f, 0.12f)
            };

            userDataValueTexts = new Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                GameObject row = CreatePanel(parent, labels[i] + " Row", new Vector2(24f, -88f - i * 43f), new Vector2(344f, 36f), rowColors[i]);
                AddOutline(row, new Color(rowColors[i].r, rowColors[i].g, rowColors[i].b, 0.26f));
                HoverScaleExtend hover = row.AddComponent<HoverScaleExtend>();
                hover.Configure(new Vector2(344f, 36f), new Vector2(372f, 42f), rowColors[i], new Color(rowColors[i].r, rowColors[i].g, rowColors[i].b, 0.28f));

                CreateText(row.transform, labels[i], new Vector2(12f, -9f), new Vector2(104f, 20f), 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.10f, 0.36f, 0.58f, 0.92f));
                userDataValueTexts[i] = CreateText(row.transform, "-", new Vector2(120f, -7f), new Vector2(206f, 24f), 15, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            }
        }

        private void ToggleUserDataDropdown()
        {
            userDataExpanded = !userDataExpanded;
            if (userDataDropdown != null)
            {
                userDataDropdown.SetActive(userDataExpanded);
                if (userDataExpanded)
                {
                    userDataDropdown.transform.SetAsLastSibling();
                }
            }

            if (userDataButtonText != null)
            {
                userDataButtonText.text = userDataExpanded ? "用户数据 ▴" : "用户数据 ▾";
            }

            if (userDataExpanded)
            {
                RefreshUserDataSummary();
            }
        }

        private void RefreshUserDataSummary()
        {
            if (userDataValueTexts == null || userDataValueTexts.Length < 7)
            {
                return;
            }

            ScoreRecordStore.ScoreRecord[] records = ScoreRecordStore.LoadRecent();
            LocalUserDatabase.UserRecord user = CurrentUserSession.GetUserOrDefault();
            string displayName = user != null && !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user != null ? user.Username : "未登录";
            if (records.Length == 0)
            {
                userDataValueTexts[0].text = displayName;
                userDataValueTexts[1].text = user != null && user.IsAdmin ? "全部用户" : "本人";
                userDataValueTexts[2].text = "0";
                userDataValueTexts[3].text = "暂无训练";
                userDataValueTexts[4].text = "暂无变化";
                userDataValueTexts[5].text = "完成训练后生成";
                userDataValueTexts[6].text = "完成训练后生成";
                return;
            }

            ScoreRecordStore.ScoreRecord latest = records[0];
            ScoreRecordStore.ScoreRecord baseline = records[records.Length - 1];
            int scoreDelta = latest.score - baseline.score;
            float timeDelta = baseline.completionTimeSeconds - latest.completionTimeSeconds;

            userDataValueTexts[0].text = displayName;
            userDataValueTexts[1].text = user != null && user.IsAdmin ? "全部用户" : "本人";
            userDataValueTexts[2].text = records.Length.ToString();
            userDataValueTexts[3].text = FormatTime(latest.completionTimeSeconds) + "  " + FormatTimeDelta(timeDelta);
            userDataValueTexts[4].text = latest.score.ToString("000") + "  " + FormatScoreDelta(scoreDelta);
            userDataValueTexts[5].text = BuildStrength(records);
            userDataValueTexts[6].text = BuildWeakness(records);
        }

        private void CreateReportListPanel(Transform parent)
        {
            reportListPanel = CreatePanel(parent, "User Report List Panel", Vector2.zero, new Vector2(1040f, 700f), new Color(0.95f, 0.985f, 1f, 0.985f));
            RectTransform panelRect = reportListPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = panelRect.anchorMin;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            AddOutline(reportListPanel, new Color(0.18f, 0.52f, 0.96f, 0.34f));

            CreateText(reportListPanel.transform, "用户报告列表", new Vector2(0f, -34f), new Vector2(900f, 52f), 34, FontStyle.Bold, TextAnchor.UpperCenter, ink);
            CreateText(reportListPanel.transform, "按训练完成时间倒序展示，包含每次训练的结果与改进方向。", new Vector2(0f, -88f), new Vector2(900f, 28f), 17, FontStyle.Normal, TextAnchor.UpperCenter, mutedBlue);
            reportTableRoot = CreatePanel(reportListPanel.transform, "Report Table Root", new Vector2(42f, -136f), new Vector2(956f, 442f), new Color(0f, 0f, 0f, 0f));
            reportEmptyText = CreateText(reportListPanel.transform, string.Empty, new Vector2(58f, -156f), new Vector2(924f, 140f), 20, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.08f, 0.28f, 0.46f, 0.96f));
            reportEmptyText.gameObject.SetActive(false);

            Button closeButton = CreateButton(reportListPanel.transform, "关闭", new Vector2(410f, -624f), new Vector2(210f, 54f));
            closeButton.onClick.AddListener(HideReportList);
            reportListPanel.SetActive(false);
        }

        private void CreateSunAltitudeControls(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "Sun Altitude Selector", new Vector2(-80f, -126f), new Vector2(600f, 58f), new Color(0.98f, 0.995f, 1f, 0.92f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.one;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = Vector2.one;
            AddOutline(panel, new Color(0.18f, 0.52f, 0.96f, 0.25f));

            CreateText(panel.transform, "太阳高度", new Vector2(18f, -14f), new Vector2(126f, 30f), 20, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            sunAltitudeStatusText = CreateText(panel.transform, string.Empty, new Vector2(142f, -16f), new Vector2(176f, 28f), 16, FontStyle.Bold, TextAnchor.UpperLeft, mutedBlue);

            Button morningButton = CreateButton(panel.transform, "上午 35°", new Vector2(326f, -8f), new Vector2(118f, 42f));
            morningSunButtonImage = morningButton.GetComponent<Image>();
            morningButton.onClick.AddListener(() => SetSunAltitudePreset(SunAltitudePreset.Morning));

            Button afternoonButton = CreateButton(panel.transform, "下午 58°", new Vector2(462f, -8f), new Vector2(118f, 42f));
            afternoonSunButtonImage = afternoonButton.GetComponent<Image>();
            afternoonButton.onClick.AddListener(() => SetSunAltitudePreset(SunAltitudePreset.Afternoon));

            SetSunAltitudePreset(selectedSunPreset);
        }

        private void SetSunAltitudePreset(SunAltitudePreset preset)
        {
            selectedSunPreset = preset;
            if (sunAltitudeStatusText != null)
            {
                sunAltitudeStatusText.text = preset == SunAltitudePreset.Afternoon
                    ? "当前：下午高太阳"
                    : "当前：上午低太阳";
            }

            SetSunButtonState(morningSunButtonImage, preset == SunAltitudePreset.Morning);
            SetSunButtonState(afternoonSunButtonImage, preset == SunAltitudePreset.Afternoon);
        }

        private void SetSunButtonState(Image image, bool selected)
        {
            if (image == null)
            {
                return;
            }

            Color normal = selected
                ? new Color(0.08f, 0.44f, 0.92f, 0.96f)
                : new Color(0.48f, 0.72f, 1f, 0.62f);
            image.color = normal;

            Button button = image.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = selected
                ? new Color(0.16f, 0.56f, 1f, 1f)
                : new Color(0.62f, 0.82f, 1f, 0.82f);
            colors.pressedColor = new Color(0.04f, 0.28f, 0.68f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        private void ShowReportList()
        {
            if (reportListPanel == null)
            {
                return;
            }

            userDataExpanded = false;
            if (userDataDropdown != null)
            {
                userDataDropdown.SetActive(false);
            }

            if (userDataButtonText != null)
            {
                userDataButtonText.text = "用户数据 ▾";
            }

            RefreshReportList();
            reportListPanel.SetActive(true);
            reportListPanel.transform.SetAsLastSibling();
        }

        private void HideReportList()
        {
            if (reportListPanel != null)
            {
                reportListPanel.SetActive(false);
            }
        }

        private void RefreshReportList()
        {
            if (reportTableRoot == null || reportEmptyText == null)
            {
                return;
            }

            ClearCanvas(reportTableRoot.transform);
            ScoreRecordStore.ScoreRecord[] records = ScoreRecordStore.LoadRecent();
            if (records.Length == 0)
            {
                reportEmptyText.gameObject.SetActive(true);
                reportEmptyText.text = "暂无训练报告。\n完成一次训练后，系统会在这里生成用户报告。";
                return;
            }

            reportEmptyText.gameObject.SetActive(false);
            string[] headers = { "序号", "用户", "场景", "分数", "等级", "用时", "碰撞", "越界", "高度", "训练结论" };
            float[] widths = { 54f, 94f, 178f, 70f, 58f, 78f, 58f, 58f, 58f, 250f };
            CreateReportTableRow(reportTableRoot.transform, 0, headers, widths, true);
            for (int i = 0; i < records.Length; i++)
            {
                ScoreRecordStore.ScoreRecord record = records[i];
                string[] cells =
                {
                    (i + 1).ToString(),
                    string.IsNullOrWhiteSpace(record.displayName) ? record.username : record.displayName,
                    string.IsNullOrWhiteSpace(record.sceneName) ? "-" : record.sceneName,
                    record.score.ToString("000"),
                    record.grade,
                    FormatTime(record.completionTimeSeconds),
                    record.collisions.ToString(),
                    record.outOfCourseTicks.ToString(),
                    record.unsafeAltitudeTicks.ToString(),
                    BuildRecordConclusion(record)
                };
                CreateReportTableRow(reportTableRoot.transform, i + 1, cells, widths, false);
            }
        }

        private void CreateReportTableRow(Transform parent, int rowIndex, string[] cells, float[] widths, bool header)
        {
            float x = 0f;
            float height = header ? 40f : 42f;
            float y = -rowIndex * 46f;
            for (int i = 0; i < cells.Length && i < widths.Length; i++)
            {
                Color cellColor = header
                    ? new Color(0.08f, 0.44f, 0.92f, 0.90f)
                    : GetReportCellColor(i, rowIndex);
                GameObject cell = CreatePanel(parent, "Report Cell", new Vector2(x, y), new Vector2(widths[i] - 4f, height), cellColor);
                AddOutline(cell, header ? new Color(0.72f, 0.90f, 1f, 0.26f) : new Color(0.18f, 0.52f, 0.96f, 0.13f));
                Text label = CreateText(
                    cell.transform,
                    cells[i],
                    Vector2.zero,
                    new Vector2(widths[i] - 10f, height),
                    header ? 15 : 14,
                    header ? FontStyle.Bold : FontStyle.Normal,
                    TextAnchor.MiddleCenter,
                    header ? Color.white : ink);
                StretchToFill(label.rectTransform);
                x += widths[i];
            }
        }

        private static Color GetReportCellColor(int columnIndex, int rowIndex)
        {
            float alpha = rowIndex % 2 == 0 ? 0.22f : 0.15f;
            switch (columnIndex)
            {
                case 3:
                    return new Color(0.18f, 0.58f, 0.95f, alpha + 0.04f);
                case 4:
                    return new Color(0.46f, 0.42f, 0.92f, alpha + 0.02f);
                case 5:
                    return new Color(0.08f, 0.62f, 0.72f, alpha);
                case 6:
                case 7:
                case 8:
                    return new Color(0.92f, 0.38f, 0.18f, alpha);
                case 9:
                    return new Color(0.12f, 0.62f, 0.36f, alpha);
                default:
                    return new Color(0.88f, 0.96f, 1f, alpha + 0.08f);
            }
        }

        private static string BuildStrength(ScoreRecordStore.ScoreRecord[] records)
        {
            ScoreRecordStore.ScoreRecord latest = records[0];
            if (latest.collisions == 0 && latest.outOfCourseTicks == 0 && latest.unsafeAltitudeTicks == 0)
            {
                return "航线稳定，安全控制表现好";
            }

            if (GetAverageScore(records) >= 85f)
            {
                return "平均分较高，动作完成质量稳定";
            }

            if (latest.collisions == 0)
            {
                return "避障和落点控制较稳";
            }

            if (latest.unsafeAltitudeTicks == 0)
            {
                return "高度保持较好";
            }

            return "已形成连续训练记录，便于追踪进步";
        }

        private static string BuildWeakness(ScoreRecordStore.ScoreRecord[] records)
        {
            ScoreRecordStore.ScoreRecord latest = records[0];
            if (latest.outOfCourseTicks > 0)
            {
                return "航线偏离偏多，建议提前减速入弯";
            }

            if (latest.unsafeAltitudeTicks > 0)
            {
                return "高度波动较大，建议稳定在目标高度附近";
            }

            if (latest.collisions > 0)
            {
                return "碰撞控制需加强，接近障碍物时减小操作量";
            }

            if (records.Length >= 2 && latest.score < records[records.Length - 1].score)
            {
                return "近期得分回落，建议复盘最近一次训练";
            }

            return "主要提升点在速度效率和动作衔接";
        }

        private static string BuildRecordConclusion(ScoreRecordStore.ScoreRecord record)
        {
            if (record.score >= 90 && record.collisions == 0)
            {
                return "表现优秀，保持节奏";
            }

            if (record.outOfCourseTicks > 0)
            {
                return "重点修正航线偏离";
            }

            if (record.unsafeAltitudeTicks > 0)
            {
                return "重点控制高度";
            }

            if (record.collisions > 0)
            {
                return "重点减少碰撞";
            }

            return "继续提升完成效率";
        }

        private static float GetAverageScore(ScoreRecordStore.ScoreRecord[] records)
        {
            if (records == null || records.Length == 0)
            {
                return 0f;
            }

            int total = 0;
            for (int i = 0; i < records.Length; i++)
            {
                total += records[i].score;
            }

            return (float)total / records.Length;
        }

        private static string FormatScoreDelta(int delta)
        {
            if (delta > 0)
            {
                return "较早期 +" + delta;
            }

            if (delta < 0)
            {
                return "较早期 " + delta;
            }

            return "较早期 持平";
        }

        private static string FormatTimeDelta(float deltaSeconds)
        {
            if (Mathf.Abs(deltaSeconds) < 0.5f)
            {
                return "较早期 持平";
            }

            return deltaSeconds > 0f
                ? "较早期 缩短 " + FormatTime(deltaSeconds)
                : "较早期 增加 " + FormatTime(-deltaSeconds);
        }

        private static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60:00}:{whole % 60:00}";
        }

        private void CreateSceneCard(Transform parent, SceneOption option, Vector2 position, float width)
        {
            const float cardHeight = 492f;
            GameObject card = CreatePanel(parent, option.Title + " Card", position, new Vector2(width, cardHeight), new Color(0.97f, 0.995f, 1f, 0.9f));
            AddOutline(card, new Color(0.18f, 0.52f, 0.96f, 0.28f));

            Texture2D previewTexture = Resources.Load<Texture2D>(option.PreviewResource);
            if (previewTexture == null)
            {
                previewTexture = Resources.Load<Texture2D>("Login/mode-training-preview");
            }

            float previewWidth = width - 36f;
            float previewHeight = previewTexture != null && previewTexture.width > 0
                ? previewWidth * previewTexture.height / previewTexture.width
                : 226f;
            previewHeight = Mathf.Min(previewHeight, 246f);

            GameObject previewFrame = CreatePanel(card.transform, "Preview", new Vector2(18f, -18f), new Vector2(previewWidth, previewHeight), new Color(0.82f, 0.94f, 1f, 0.92f));
            previewFrame.AddComponent<Mask>().showMaskGraphic = false;
            RawImage preview = CreateRawImage(previewFrame.transform, option.Title + " Preview", previewTexture, Color.white);
            StretchToFill(preview.rectTransform);
            AspectRatioFitter fitter = preview.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = previewTexture != null && previewTexture.height > 0
                ? (float)previewTexture.width / previewTexture.height
                : 16f / 9f;

            float textTop = -18f - previewHeight - 20f;
            CreateText(card.transform, option.Title, new Vector2(22f, textTop), new Vector2(width - 44f, 36f), 25, FontStyle.Bold, TextAnchor.UpperLeft, ink);
            CreateText(card.transform, option.English, new Vector2(24f, textTop - 38f), new Vector2(width - 48f, 24f), 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.48f, 0.86f, 0.82f));
            CreateText(card.transform, option.Description, new Vector2(22f, textTop - 72f), new Vector2(width - 44f, 58f), 15, FontStyle.Normal, TextAnchor.UpperLeft, mutedBlue);

            Button enterButton = CreateButton(card.transform, "进入场景", new Vector2(22f, -cardHeight + 72f), new Vector2(width - 44f, 50f));
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

            SceneLoadRequest.Select(sceneName, selectedSunPreset);
            SceneManager.LoadScene(loadingSceneName);
        }

        private Canvas EnsureCanvas()
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Training Scene Selection Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            ResponsiveCanvasScaler.ApplyTo(canvas);
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

        private sealed class HoverScaleExtend : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private RectTransform rect;
            private Image image;
            private Vector2 normalSize;
            private Vector2 hoverSize;
            private Color normalColor;
            private Color hoverColor;
            private bool hovering;

            public void Configure(Vector2 normal, Vector2 hover, Color normalTint, Color hoverTint)
            {
                rect = GetComponent<RectTransform>();
                image = GetComponent<Image>();
                normalSize = normal;
                hoverSize = hover;
                normalColor = normalTint;
                hoverColor = hoverTint;
                if (rect != null)
                {
                    rect.sizeDelta = normalSize;
                }

                if (image != null)
                {
                    image.color = normalColor;
                }
            }

            private void Awake()
            {
                rect = GetComponent<RectTransform>();
                image = GetComponent<Image>();
                if (normalSize == Vector2.zero && rect != null)
                {
                    normalSize = rect.sizeDelta;
                    hoverSize = normalSize + new Vector2(28f, 6f);
                }

                if (normalColor.a <= 0f && image != null)
                {
                    normalColor = image.color;
                    hoverColor = new Color(normalColor.r, normalColor.g, normalColor.b, Mathf.Min(0.34f, normalColor.a + 0.16f));
                }
            }

            private void Update()
            {
                if (rect == null)
                {
                    return;
                }

                Vector2 targetSize = hovering ? hoverSize : normalSize;
                rect.sizeDelta = Vector2.Lerp(rect.sizeDelta, targetSize, Time.unscaledDeltaTime * 14f);
                rect.localScale = Vector3.Lerp(rect.localScale, hovering ? new Vector3(1.025f, 1.025f, 1f) : Vector3.one, Time.unscaledDeltaTime * 14f);
                if (image != null)
                {
                    image.color = Color.Lerp(image.color, hovering ? hoverColor : normalColor, Time.unscaledDeltaTime * 14f);
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                hovering = true;
                transform.SetAsLastSibling();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                hovering = false;
            }
        }
    }
}
