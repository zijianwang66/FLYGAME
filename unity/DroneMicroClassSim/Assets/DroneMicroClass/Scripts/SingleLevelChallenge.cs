using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class SingleLevelChallenge : MonoBehaviour
    {
        private enum ChallengeState
        {
            Waiting,
            Running,
            Finished
        }

        [Header("Mission")]
        [SerializeField] private SimpleFlightController drone;
        [SerializeField] private Transform[] checkpoints;
        [SerializeField] private Transform finishTarget;
        [SerializeField] private float targetTimeSeconds = 120f;
        [SerializeField] private float maxTimeSeconds = 180f;
        [SerializeField] private float minSafeAltitude = 1.2f;
        [SerializeField] private float maxSafeAltitude = 18f;
        [SerializeField] private float altitudePenaltyInterval = 1.5f;
        [SerializeField] private int wrongCheckpointPenalty = 8;
        [SerializeField] private int collisionPenalty = 10;
        [SerializeField] private int unsafeAltitudePenalty = 2;

        [Header("Auto Setup")]
        [SerializeField] private bool createDefaultCourseIfEmpty = true;
        [SerializeField] private bool startWhenDroneTakesOff = true;
        [SerializeField] private Vector3 displayOrigin = new Vector3(325f, 48f, 468f);

        [Header("Guidance")]
        [SerializeField] private bool showGuidanceLights = true;
        [SerializeField] private float guideBeamHeight = 8f;
        [SerializeField] private float guideLineWidth = 0.18f;
        [SerializeField] private float guideLightRange = 10f;
        [SerializeField] private float guideLightIntensity = 3.2f;

        private readonly List<ChallengeCheckpoint> checkpointTriggers = new List<ChallengeCheckpoint>();
        private readonly List<Renderer> markerRenderers = new List<Renderer>();
        private readonly List<Vector3> markerBaseScales = new List<Vector3>();
        private readonly List<LineRenderer> markerBeams = new List<LineRenderer>();
        private readonly List<Light> markerLights = new List<Light>();
        private bool finishWhenFinalCheckpointCleared;
        private ChallengeCheckpoint finishTrigger;
        private Renderer finishMarkerRenderer;
        private Vector3 finishMarkerBaseScale = Vector3.one;
        private LineRenderer finishBeam;
        private Light finishLight;
        private LineRenderer activeGuideLine;
        private Material guideMaterial;
        private ChallengeState state = ChallengeState.Waiting;
        private float startTime;
        private float finishTime;
        private float nextAltitudePenaltyTime;
        private float lastCollisionPenaltyTime = -10f;
        private float feedbackUntilTime;
        private int expectedCheckpoint;
        private int penalties;
        private int collisions;
        private int wrongCheckpointHits;
        private int unsafeAltitudeTicks;
        private GameObject resultPanel;
        private Text titleText;
        private Text detailText;
        private Text feedbackText;
        private Text resultText;
        private Text resultTitleText;
        private Text resultSummaryText;
        private Text resultBreakdownText;

        private int RequiredCheckpointCount => checkpointTriggers.Count;
        private float ElapsedSeconds => state == ChallengeState.Finished ? finishTime - startTime : Time.time - startTime;

        private void Awake()
        {
            if (drone == null)
            {
                drone = FindFirstObjectByType<SimpleFlightController>();
            }

            if (drone != null)
            {
                ChallengeCollisionRelay relay = drone.GetComponent<ChallengeCollisionRelay>();
                if (relay == null)
                {
                    relay = drone.gameObject.AddComponent<ChallengeCollisionRelay>();
                }

                relay.Bind(this);
            }

            CreateHud();
            BuildCourseTriggers();
            ResetChallenge();
        }

        private void Update()
        {
            if (drone == null)
            {
                return;
            }

            if (state == ChallengeState.Waiting && startWhenDroneTakesOff && drone.Altitude > 0.35f)
            {
                BeginChallenge();
            }

            if (state == ChallengeState.Running)
            {
                UpdateAltitudePenalty();
                UpdateCourseVisuals();
                if (ElapsedSeconds >= maxTimeSeconds)
                {
                    SetFeedback("Time limit reached. Mission ending.", 3f);
                    FinishChallenge();
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartLevel();
            }
            else if (Input.GetKeyDown(KeyCode.Return) && state == ChallengeState.Finished)
            {
                RestartLevel();
            }
            else if (Input.GetKeyDown(KeyCode.Return) && state == ChallengeState.Waiting)
            {
                BeginChallenge();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToMenu();
            }

            UpdateHud();
        }

        public void RegisterCheckpoint(ChallengeCheckpoint checkpoint, Collider other)
        {
            if (state != ChallengeState.Running || !IsDroneCollider(other))
            {
                return;
            }

            if (checkpoint == finishTrigger)
            {
                if (expectedCheckpoint >= RequiredCheckpointCount)
                {
                    SetFeedback("Finish reached. Mission complete.", 3f);
                    FinishChallenge();
                }
                else
                {
                    AddPenalty(wrongCheckpointPenalty);
                    wrongCheckpointHits++;
                    SetFeedback("Finish locked. Complete all checkpoints first.", 2.5f);
                }

                return;
            }

            if (checkpoint.Index == expectedCheckpoint)
            {
                expectedCheckpoint++;
                if (finishWhenFinalCheckpointCleared && expectedCheckpoint >= RequiredCheckpointCount)
                {
                    FinishChallenge();
                    return;
                }

                SetFeedback($"Checkpoint {checkpoint.Index + 1} cleared.", 1.6f);
                UpdateCourseVisuals();
            }
            else if (checkpoint.Index > expectedCheckpoint)
            {
                AddPenalty(wrongCheckpointPenalty);
                wrongCheckpointHits++;
                SetFeedback($"Wrong checkpoint. Next target is {expectedCheckpoint + 1}.", 2.2f);
            }
        }

        public void RegisterCollision(Collision collision)
        {
            if (state != ChallengeState.Running || collision.collider.isTrigger)
            {
                return;
            }

            if (Time.time - lastCollisionPenaltyTime < 1f)
            {
                return;
            }

            lastCollisionPenaltyTime = Time.time;
            AddPenalty(collisionPenalty);
            collisions++;
            SetFeedback("Collision penalty.", 1.8f);
        }

        private void BeginChallenge()
        {
            state = ChallengeState.Running;
            startTime = Time.time;
            finishTime = 0f;
            expectedCheckpoint = 0;
            penalties = 0;
            collisions = 0;
            wrongCheckpointHits = 0;
            unsafeAltitudeTicks = 0;
            lastCollisionPenaltyTime = -10f;
            nextAltitudePenaltyTime = Time.time + altitudePenaltyInterval;
            SetFeedback("Mission started. Follow the highlighted checkpoint.", 2f);
            UpdateCourseVisuals();
            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void FinishChallenge()
        {
            if (state == ChallengeState.Finished)
            {
                return;
            }

            state = ChallengeState.Finished;
            finishTime = Time.time;
            UpdateCourseVisuals();
            ScoreBreakdown score = BuildScoreBreakdown();
            if (resultText != null)
            {
                resultText.text = $"Mission complete  Grade {score.Grade}  Score {score.FinalScore}\nR or Enter restart / Esc menu";
            }

            ShowResultPanel(score);
            SaveScoreRecord(score);
        }

        private void ResetChallenge()
        {
            state = ChallengeState.Waiting;
            startTime = Time.time;
            finishTime = 0f;
            expectedCheckpoint = 0;
            penalties = 0;
            collisions = 0;
            wrongCheckpointHits = 0;
            unsafeAltitudeTicks = 0;
            lastCollisionPenaltyTime = -10f;
            SetFeedback("Take off or press Enter to start.", 3f);
            UpdateCourseVisuals();
            if (resultText != null)
            {
                resultText.text = "Take off to start / Enter manual start";
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void UpdateAltitudePenalty()
        {
            if (Time.time < nextAltitudePenaltyTime)
            {
                return;
            }

            nextAltitudePenaltyTime = Time.time + altitudePenaltyInterval;
            if (drone.Altitude < minSafeAltitude || drone.Altitude > maxSafeAltitude)
            {
                AddPenalty(unsafeAltitudePenalty);
                unsafeAltitudeTicks++;
                SetFeedback("Unsafe altitude warning.", 1.2f);
            }
        }

        private void AddPenalty(int value)
        {
            penalties += Mathf.Max(0, value);
        }

        private int CalculateScore()
        {
            return BuildScoreBreakdown().FinalScore;
        }

        private ScoreBreakdown BuildScoreBreakdown()
        {
            float elapsed = Mathf.Max(0f, state == ChallengeState.Waiting ? 0f : ElapsedSeconds);
            int timePenalty = elapsed <= targetTimeSeconds ? 0 : Mathf.CeilToInt(elapsed - targetTimeSeconds);
            int collisionPenaltyTotal = collisions * collisionPenalty;
            int wrongCheckpointPenaltyTotal = wrongCheckpointHits * wrongCheckpointPenalty;
            int unsafeAltitudePenaltyTotal = unsafeAltitudeTicks * unsafeAltitudePenalty;
            int countedEventPenalty = collisionPenaltyTotal + wrongCheckpointPenaltyTotal + unsafeAltitudePenaltyTotal;
            int otherPenalty = Mathf.Max(0, penalties - countedEventPenalty);
            int incompleteCheckpoints = Mathf.Max(0, RequiredCheckpointCount - expectedCheckpoint);
            int incompleteCheckpointPenalty = incompleteCheckpoints * 12;
            int totalPenalty = timePenalty + penalties + incompleteCheckpointPenalty;
            int finalScore = Mathf.Clamp(100 - totalPenalty, 0, 100);

            return new ScoreBreakdown
            {
                ElapsedSeconds = elapsed,
                TimePenalty = timePenalty,
                CollisionPenalty = collisionPenaltyTotal,
                WrongCheckpointPenalty = wrongCheckpointPenaltyTotal,
                UnsafeAltitudePenalty = unsafeAltitudePenaltyTotal,
                OtherPenalty = otherPenalty,
                IncompleteCheckpointPenalty = incompleteCheckpointPenalty,
                IncompleteCheckpoints = incompleteCheckpoints,
                TotalPenalty = totalPenalty,
                FinalScore = finalScore,
                Grade = GetGrade(finalScore)
            };
        }

        private static string GetGrade(int score)
        {
            if (score >= 90)
            {
                return "S";
            }

            if (score >= 80)
            {
                return "A";
            }

            if (score >= 70)
            {
                return "B";
            }

            if (score >= 60)
            {
                return "C";
            }

            return "D";
        }

        private void ShowResultPanel(ScoreBreakdown score)
        {
            if (resultPanel == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            if (resultTitleText != null)
            {
                resultTitleText.text = "Level Complete";
            }

            if (resultSummaryText != null)
            {
                resultSummaryText.text =
                    $"Score {score.FinalScore}  Grade {score.Grade}\n" +
                    $"Time {FormatTime(score.ElapsedSeconds)}  Checkpoints {Mathf.Min(expectedCheckpoint, RequiredCheckpointCount)}/{RequiredCheckpointCount}";
            }

            if (resultBreakdownText != null)
            {
                resultBreakdownText.text =
                    $"Penalty breakdown\n" +
                    $"Overtime: -{score.TimePenalty}\n" +
                    $"Collisions: {collisions} x {collisionPenalty} = -{score.CollisionPenalty}\n" +
                    $"Wrong gates: {wrongCheckpointHits} x {wrongCheckpointPenalty} = -{score.WrongCheckpointPenalty}\n" +
                    $"Unsafe altitude: {unsafeAltitudeTicks} x {unsafeAltitudePenalty} = -{score.UnsafeAltitudePenalty}\n" +
                    $"Other penalties: -{score.OtherPenalty}\n" +
                    $"Incomplete checkpoints: {score.IncompleteCheckpoints} x 12 = -{score.IncompleteCheckpointPenalty}\n" +
                    $"Total penalty: -{score.TotalPenalty}";
            }
        }

        private void SaveScoreRecord(ScoreBreakdown score)
        {
            ScoreRecordStore.Add(new ScoreRecordStore.ScoreRecord
            {
                score = score.FinalScore,
                grade = score.Grade,
                completionTimeSeconds = score.ElapsedSeconds,
                collisions = collisions,
                wrongCheckpointHits = wrongCheckpointHits,
                unsafeAltitudeTicks = unsafeAltitudeTicks,
                droneName = GetDroneName(),
                recordedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        private string GetDroneName()
        {
            if (drone == null)
            {
                return "Training Drone";
            }

            DroneFleetManager fleet = drone.GetComponent<DroneFleetManager>();
            return fleet != null ? fleet.CurrentVariantName : drone.name;
        }

        private void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ReturnToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private bool IsDroneCollider(Collider other)
        {
            return drone != null && other.GetComponentInParent<SimpleFlightController>() == drone;
        }

        private void BuildCourseTriggers()
        {
            bool generatedDefaultCourse = false;
            if (checkpoints == null || checkpoints.Length == 0)
            {
                if (!createDefaultCourseIfEmpty)
                {
                    return;
                }

                checkpoints = CreateDefaultCheckpointTransforms();
                generatedDefaultCourse = true;
            }

            checkpointTriggers.Clear();
            markerRenderers.Clear();
            markerBaseScales.Clear();
            markerBeams.Clear();
            markerLights.Clear();
            guideMaterial = CreateGuideMaterial();
            activeGuideLine = showGuidanceLights ? CreateGuideLine("Active Target Guide", guideLineWidth, 0.9f) : null;
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] == null)
                {
                    continue;
                }

                ChallengeCheckpoint trigger = CreateTrigger("Checkpoint_" + (i + 1), checkpoints[i].position, new Vector3(7f, 5f, 7f));
                trigger.Configure(this, checkpointTriggers.Count);
                checkpointTriggers.Add(trigger);
                Renderer markerRenderer = checkpoints[i].GetComponentInChildren<Renderer>();
                markerRenderers.Add(markerRenderer);
                markerBaseScales.Add(checkpoints[i].localScale);
                markerBeams.Add(showGuidanceLights ? CreateVerticalGuideBeam("Checkpoint_" + (i + 1) + "_Beam", checkpoints[i], guideBeamHeight) : null);
                markerLights.Add(showGuidanceLights ? CreateGuideLight("Checkpoint_" + (i + 1) + "_Light", checkpoints[i], new Color(0.15f, 0.95f, 1f, 1f)) : null);
            }

            finishWhenFinalCheckpointCleared = generatedDefaultCourse && finishTarget == null;
            if (finishWhenFinalCheckpointCleared)
            {
                return;
            }

            Vector3 finishPosition = finishTarget != null
                ? finishTarget.position
                : (checkpoints.Length > 0 && checkpoints[checkpoints.Length - 1] != null ? checkpoints[checkpoints.Length - 1].position + Vector3.forward * 12f : displayOrigin);
            finishPosition.y = Mathf.Max(finishPosition.y + 2.5f, 3f);

            finishTrigger = CreateTrigger("Finish_Gate", finishPosition, new Vector3(9f, 5f, 9f));
            finishTrigger.Configure(this, checkpointTriggers.Count);

            GameObject finishMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            finishMarker.name = "Mission Finish Marker";
            finishMarker.transform.SetParent(transform);
            finishMarker.transform.position = new Vector3(finishPosition.x, finishPosition.y - 2.45f, finishPosition.z);
            finishMarker.transform.localScale = new Vector3(5.8f, 0.05f, 5.8f);
            Collider finishMarkerCollider = finishMarker.GetComponent<Collider>();
            if (finishMarkerCollider != null)
            {
                Destroy(finishMarkerCollider);
            }

            finishMarkerRenderer = finishMarker.GetComponent<Renderer>();
            finishMarkerBaseScale = finishMarker.transform.localScale;
            if (finishMarkerRenderer != null)
            {
                finishMarkerRenderer.sharedMaterial = CreateMarkerMaterial(new Color(0.95f, 0.2f, 1f, 0.7f));
            }

            if (showGuidanceLights)
            {
                finishBeam = CreateVerticalGuideBeam("Finish_Beam", finishMarker.transform, guideBeamHeight + 2f);
                finishLight = CreateGuideLight("Finish_Light", finishMarker.transform, new Color(1f, 0.2f, 1f, 1f));
            }
        }

        private Transform[] CreateDefaultCheckpointTransforms()
        {
            Vector3 origin = drone != null ? drone.transform.position : displayOrigin;
            Vector3[] positions =
            {
                origin + new Vector3(-18f, 3.2f, 18f),
                origin + new Vector3(0f, 4.2f, 30f),
                origin + new Vector3(18f, 4.2f, 18f),
                origin + new Vector3(22f, 4.2f, 42f),
                origin + new Vector3(0f, 4.2f, 56f),
                origin + new Vector3(-22f, 4.2f, 42f),
                origin + new Vector3(-18f, 3.2f, 26f)
            };

            Transform[] generated = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Mission Marker " + (i + 1);
                marker.transform.SetParent(transform);
                marker.transform.position = positions[i];
                marker.transform.localScale = new Vector3(4.8f, 0.04f, 4.8f);
                Collider markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    Destroy(markerCollider);
                }

                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = i == 0 ? new Color(0.25f, 0.9f, 1f, 0.55f) : new Color(1f, 0.78f, 0.1f, 0.55f);
                    renderer.sharedMaterial = CreateMarkerMaterial(color);
                }

                generated[i] = marker.transform;
            }

            return generated;
        }

        private ChallengeCheckpoint CreateTrigger(string objectName, Vector3 position, Vector3 size)
        {
            GameObject triggerObject = new GameObject(objectName);
            triggerObject.transform.SetParent(transform);
            triggerObject.transform.position = position;
            BoxCollider box = triggerObject.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            return triggerObject.AddComponent<ChallengeCheckpoint>();
        }

        private LineRenderer CreateVerticalGuideBeam(string objectName, Transform target, float height)
        {
            if (target == null)
            {
                return null;
            }

            LineRenderer line = CreateGuideLine(objectName, guideLineWidth * 1.8f, 0.7f);
            line.positionCount = 2;
            line.SetPosition(0, target.position + Vector3.up * 0.15f);
            line.SetPosition(1, target.position + Vector3.up * Mathf.Max(1f, height));
            return line;
        }

        private LineRenderer CreateGuideLine(string objectName, float width, float alpha)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = guideMaterial != null ? guideMaterial : CreateGuideMaterial();
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.widthMultiplier = Mathf.Max(0.01f, width);
            line.numCapVertices = 8;
            line.numCornerVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Color color = new Color(0.15f, 0.95f, 1f, Mathf.Clamp01(alpha));
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        private Light CreateGuideLight(string objectName, Transform target, Color color)
        {
            if (target == null)
            {
                return null;
            }

            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(transform);
            lightObject.transform.position = target.position + Vector3.up * 2.4f;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = guideLightRange;
            light.intensity = guideLightIntensity;
            light.shadows = LightShadows.None;
            return light;
        }

        private Material CreateGuideMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = Color.white;
            material.renderQueue = 3000;
            return material;
        }

        private static Material CreateMarkerMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = color;
            material.renderQueue = 3000;
            return material;
        }

        private void CreateHud()
        {
            Canvas existingCanvas = GetComponentInChildren<Canvas>();
            if (existingCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("Single Level HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText = CreateText(canvasObject.transform, "Mission Title", new Vector2(36f, -32f), new Vector2(700f, 44f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            detailText = CreateText(canvasObject.transform, "Mission Detail", new Vector2(36f, -78f), new Vector2(720f, 210f), 20, FontStyle.Normal, TextAnchor.UpperLeft);
            feedbackText = CreateText(canvasObject.transform, "Mission Feedback", new Vector2(0f, -205f), new Vector2(960f, 52f), 24, FontStyle.Bold, TextAnchor.UpperCenter);
            resultText = CreateText(canvasObject.transform, "Mission Result", new Vector2(0f, -72f), new Vector2(1100f, 120f), 34, FontStyle.Bold, TextAnchor.UpperCenter);
            resultPanel = CreatePanel(canvasObject.transform, "Result Panel", Vector2.zero, new Vector2(720f, 560f), new Color(0.01f, 0.025f, 0.035f, 0.92f));
            resultTitleText = CreateText(resultPanel.transform, "Result Title", new Vector2(0f, -32f), new Vector2(620f, 54f), 34, FontStyle.Bold, TextAnchor.UpperCenter);
            resultSummaryText = CreateText(resultPanel.transform, "Result Summary", new Vector2(0f, -96f), new Vector2(640f, 92f), 24, FontStyle.Bold, TextAnchor.UpperCenter);
            resultBreakdownText = CreateText(resultPanel.transform, "Result Breakdown", new Vector2(56f, -206f), new Vector2(610f, 210f), 20, FontStyle.Normal, TextAnchor.UpperLeft);

            Button restartButton = CreateButton(resultPanel.transform, "Restart", new Vector2(-172f, -474f), new Vector2(220f, 64f));
            restartButton.onClick.AddListener(RestartLevel);
            Button menuButton = CreateButton(resultPanel.transform, "Menu", new Vector2(172f, -474f), new Vector2(220f, 64f));
            menuButton.onClick.AddListener(ReturnToMenu);
            resultPanel.SetActive(false);

            titleText.font = font;
            detailText.font = font;
            feedbackText.font = font;
            resultText.font = font;
            resultTitleText.font = font;
            resultSummaryText.font = font;
            resultBreakdownText.font = font;
        }

        private static Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = alignment == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = alignment == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreatePanel(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.82f, 0.95f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.45f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.05f, 0.56f, 0.68f, 1f);
            button.colors = colors;

            Text text = CreateText(buttonObject.transform, label, Vector2.zero, size, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color(0.01f, 0.035f, 0.045f, 1f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            return button;
        }

        private void UpdateHud()
        {
            if (titleText == null || detailText == null)
            {
                return;
            }

            ScoreBreakdown score = BuildScoreBreakdown();
            titleText.text = "Level 1: Figure Eight Flight Test";
            detailText.text =
                $"State: {GetStateLabel()}\n" +
                $"Next target: {GetTargetLabel()}\n" +
                $"Checkpoints: {Mathf.Min(expectedCheckpoint, RequiredCheckpointCount)}/{RequiredCheckpointCount}\n" +
                $"Time: {FormatTime(state == ChallengeState.Waiting ? 0f : ElapsedSeconds)} / Target {FormatTime(targetTimeSeconds)}\n" +
                $"Score: {score.FinalScore}  Grade: {score.Grade}  Penalty: {score.TotalPenalty}\n" +
                $"Collisions: {collisions}  Wrong gates: {wrongCheckpointHits}  Altitude warnings: {unsafeAltitudeTicks}\n" +
                "Enter start  R restart  Esc menu";

            if (feedbackText != null && Time.time > feedbackUntilTime && state == ChallengeState.Running)
            {
                feedbackText.text = $"Fly to {GetTargetLabel()}";
            }
        }

        private string GetStateLabel()
        {
            switch (state)
            {
                case ChallengeState.Waiting:
                    return "Waiting";
                case ChallengeState.Running:
                    return "Running";
                default:
                    return "Finished";
            }
        }

        private static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60:00}:{whole % 60:00}";
        }

        private string GetTargetLabel()
        {
            if (state == ChallengeState.Finished)
            {
                return "Complete";
            }

            if (expectedCheckpoint < RequiredCheckpointCount)
            {
                return "Checkpoint " + (expectedCheckpoint + 1);
            }

            return "Finish";
        }

        private void SetFeedback(string message, float duration)
        {
            feedbackUntilTime = Time.time + Mathf.Max(0.2f, duration);
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }

        private void UpdateCourseVisuals()
        {
            for (int i = 0; i < markerRenderers.Count; i++)
            {
                Renderer renderer = markerRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Transform marker = renderer.transform;
                if (i < expectedCheckpoint)
                {
                    renderer.material.color = new Color(0.25f, 1f, 0.35f, 0.7f);
                    marker.localScale = markerBaseScales[i] * 0.86f;
                    SetGuideVisual(i, new Color(0.25f, 1f, 0.35f, 0.38f), 0.45f, false);
                }
                else if (i == expectedCheckpoint && state != ChallengeState.Finished)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 5.5f) * 0.12f;
                    renderer.material.color = new Color(0.15f, 0.95f, 1f, 0.9f);
                    marker.localScale = markerBaseScales[i] * pulse;
                    float lightPulse = 1f + Mathf.Sin(Time.time * 6.5f) * 0.22f;
                    SetGuideVisual(i, new Color(0.15f, 0.95f, 1f, 0.95f), lightPulse, true);
                }
                else
                {
                    renderer.material.color = new Color(1f, 0.78f, 0.1f, 0.55f);
                    marker.localScale = markerBaseScales[i];
                    SetGuideVisual(i, new Color(1f, 0.78f, 0.1f, 0.32f), 0.35f, false);
                }
            }

            if (finishMarkerRenderer != null)
            {
                bool finishActive = expectedCheckpoint >= RequiredCheckpointCount && state != ChallengeState.Finished;
                float pulse = finishActive ? 1f + Mathf.Sin(Time.time * 5.5f) * 0.12f : 1f;
                finishMarkerRenderer.material.color = finishActive
                    ? new Color(1f, 0.2f, 1f, 0.95f)
                    : new Color(0.95f, 0.2f, 1f, 0.42f);
                finishMarkerRenderer.transform.localScale = finishMarkerBaseScale * pulse;
                Color finishColor = finishActive ? new Color(1f, 0.2f, 1f, 0.95f) : new Color(0.95f, 0.2f, 1f, 0.25f);
                SetLineColor(finishBeam, finishColor);
                if (finishBeam != null)
                {
                    finishBeam.enabled = showGuidanceLights;
                }

                if (finishLight != null)
                {
                    finishLight.enabled = showGuidanceLights && finishActive;
                    finishLight.color = finishColor;
                    finishLight.intensity = guideLightIntensity * (finishActive ? pulse : 0.35f);
                }
            }

            UpdateActiveGuideLine();
        }

        private void SetGuideVisual(int index, Color color, float intensityScale, bool lightEnabled)
        {
            if (index < markerBeams.Count)
            {
                LineRenderer beam = markerBeams[index];
                SetLineColor(beam, color);
                if (beam != null)
                {
                    beam.enabled = showGuidanceLights;
                }
            }

            if (index < markerLights.Count)
            {
                Light light = markerLights[index];
                if (light != null)
                {
                    light.enabled = showGuidanceLights && lightEnabled;
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.intensity = guideLightIntensity * Mathf.Max(0.1f, intensityScale);
                    light.range = guideLightRange * (lightEnabled ? 1.15f : 0.75f);
                }
            }
        }

        private void UpdateActiveGuideLine()
        {
            if (activeGuideLine == null || !showGuidanceLights || drone == null || state == ChallengeState.Finished)
            {
                if (activeGuideLine != null)
                {
                    activeGuideLine.enabled = false;
                }

                return;
            }

            if (!TryGetActiveTargetPosition(out Vector3 targetPosition))
            {
                activeGuideLine.enabled = false;
                return;
            }

            Vector3 dronePosition = drone.transform.position + Vector3.up * 0.22f;
            Vector3 targetAnchor = targetPosition + Vector3.up * 1.6f;
            float pulse = 0.7f + Mathf.Sin(Time.time * 7.5f) * 0.18f;
            Color color = expectedCheckpoint < RequiredCheckpointCount
                ? new Color(0.15f, 0.95f, 1f, pulse)
                : new Color(1f, 0.2f, 1f, pulse);

            activeGuideLine.enabled = true;
            activeGuideLine.positionCount = 2;
            activeGuideLine.SetPosition(0, dronePosition);
            activeGuideLine.SetPosition(1, targetAnchor);
            activeGuideLine.widthMultiplier = guideLineWidth * (1f + Mathf.Sin(Time.time * 6f) * 0.14f);
            SetLineColor(activeGuideLine, color);
        }

        private bool TryGetActiveTargetPosition(out Vector3 targetPosition)
        {
            if (expectedCheckpoint < markerRenderers.Count && markerRenderers[expectedCheckpoint] != null)
            {
                targetPosition = markerRenderers[expectedCheckpoint].transform.position;
                return true;
            }

            if (expectedCheckpoint >= RequiredCheckpointCount && finishMarkerRenderer != null)
            {
                targetPosition = finishMarkerRenderer.transform.position;
                return true;
            }

            targetPosition = Vector3.zero;
            return false;
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            if (line == null)
            {
                return;
            }

            line.startColor = color;
            line.endColor = color;
        }

        private struct ScoreBreakdown
        {
            public float ElapsedSeconds;
            public int TimePenalty;
            public int CollisionPenalty;
            public int WrongCheckpointPenalty;
            public int UnsafeAltitudePenalty;
            public int OtherPenalty;
            public int IncompleteCheckpointPenalty;
            public int IncompleteCheckpoints;
            public int TotalPenalty;
            public int FinalScore;
            public string Grade;
        }
    }
}
