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
            Briefing,
            ReadyForTakeoff,
            Running,
            Finished
        }

        private enum RouteLayout
        {
            FigureEight,
            Rectangle
        }

        private const int CheckpointsPerLoop = 9;
        private const int BriefingPageCount = 3;

        [Header("Mission")]
        [SerializeField] private SimpleFlightController drone;
        [SerializeField] private Transform[] checkpoints;
        [SerializeField] private Transform finishTarget;
        [SerializeField] private float targetTimeSeconds = 120f;
        [SerializeField] private float maxTimeSeconds = 180f;
        [SerializeField] private float minSafeAltitude = 4f;
        [SerializeField] private float maxSafeAltitude = 8f;
        [SerializeField] private float altitudePenaltyInterval = 1.5f;
        [SerializeField] private int collisionPenalty = 8;
        [SerializeField] private int unsafeAltitudePenalty = 2;

        [Header("Course Boundary")]
        [SerializeField] private float courseHalfWidth = 2f;
        [SerializeField] private float outOfCourseGraceSeconds = 1f;
        [SerializeField] private float outOfCoursePenaltyPerMeterSecond = 0.5f;

        [Header("Auto Setup")]
        [SerializeField] private bool createDefaultCourseIfEmpty = true;
        [SerializeField] private bool startWhenDroneTakesOff = true;
        [SerializeField] private Vector3 displayOrigin = new Vector3(325f, 48f, 468f);

        [Header("Training Route")]
        [SerializeField] private RouteLayout routeLayout = RouteLayout.FigureEight;
        [SerializeField] private Vector3 courseCenter = Vector3.zero;
        [SerializeField, Min(0.1f)] private float courseRadius = 6f;
        [SerializeField] private Vector2 rectangleHalfExtents = new Vector2(15f, 8f);
        [SerializeField] private Renderer rectangleBoundaryRenderer;
        [SerializeField, Min(0f)] private float rectangleBoundaryCenterInset = 0.03f;
        [SerializeField, Min(0.1f)] private float checkpointHeight = 6f;
        [SerializeField, Min(0.5f)] private float checkpointHorizontalTolerance = 1f;
        [SerializeField, Min(0.5f)] private float checkpointVerticalTolerance = 2f;
        [SerializeField, Min(0.05f)] private float sharedCheckpointDebounceSeconds = 0.35f;

        [Header("Guidance")]
        [SerializeField] private bool showGuidanceLights = true;
        [SerializeField] private float guideLineWidth = 0.18f;
        [SerializeField] private float guideLightRange = 10f;
        [SerializeField] private float guideLightIntensity = 3.2f;

        private readonly List<ChallengeCheckpoint> checkpointTriggers = new List<ChallengeCheckpoint>();
        private readonly List<Renderer> markerRenderers = new List<Renderer>();
        private readonly List<Vector3> markerBaseScales = new List<Vector3>();
        private readonly List<Light> markerLights = new List<Light>();
        private DroneController droneInputController;
        private bool finishWhenFinalCheckpointCleared;
        private bool hasCourseStartPosition;
        private Vector3 courseStartPosition;
        private ChallengeCheckpoint courseEntryTrigger;
        private Renderer courseEntryMarkerRenderer;
        private bool courseEntryReached;
        private float lastCourseEntryTime = -10f;
        private float lastCheckpointClearTime = -10f;
        private Vector3 lastCheckpointClearPosition;
        private ChallengeCheckpoint finishTrigger;
        private Renderer finishMarkerRenderer;
        private Vector3 finishMarkerBaseScale = Vector3.one;
        private Light finishLight;
        private LineRenderer activeGuideLine;
        private Material guideMaterial;
        private ChallengeState state = ChallengeState.Briefing;
        private float startTime;
        private float finishTime;
        private float nextAltitudePenaltyTime;
        private float lastCollisionPenaltyTime = -10f;
        private float feedbackUntilTime;
        private int expectedCheckpoint;
        private int penalties;
        private int collisions;
        private int unsafeAltitudeTicks;
        private int outOfCourseEvents;
        private bool isOutOfCourse;
        private float outOfCourseStartedAt;
        private float lastCourseBoundaryUpdateTime;
        private float outOfCoursePenaltyPoints;
        private int briefingPageIndex;
        private GameObject briefingPanel;
        private GameObject resultPanel;
        private Text titleText;
        private Text detailText;
        private Text feedbackText;
        private Text resultText;
        private Text resultTitleText;
        private Text resultSummaryText;
        private Text resultBreakdownText;
        private Text resultAdviceText;
        private Text briefingTitleText;
        private Text briefingSubtitleText;
        private Text briefingBodyText;
        private Text briefingPageText;
        private Text briefingNextButtonText;
        private GameObject briefingControlsDiagram;

        private int RequiredCheckpointCount => checkpointTriggers.Count;
        private float ElapsedSeconds => state == ChallengeState.Finished ? finishTime - startTime : Time.time - startTime;
        private string RouteTitle => routeLayout == RouteLayout.Rectangle ? "矩形飞行训练" : "8字飞行训练";
        private string RouteDisplayName => routeLayout == RouteLayout.Rectangle ? "矩形航线" : "8字航线";
        private string RouteEntryLabel => routeLayout == RouteLayout.Rectangle ? "矩形底边中点" : "中心切点";
        private string RouteSubtitle => routeLayout == RouteLayout.Rectangle
            ? "沿着蓝色矩形航道完成一整圈飞行"
            : "沿着蓝色8字航道完成一整圈飞行";
        private string RouteTaskDescription => routeLayout == RouteLayout.Rectangle
            ? "从起降点起飞后，先飞向矩形底边中点。\n\n" +
              "保持约 6 米高度，沿底边向右，再依次沿右边、顶边和左边绕场一周。\n\n" +
              "检查点具有水平和垂直容错，主要用于引导飞行路线。"
            : "从起降点起飞后，先飞向两个圆环的中心切点。\n\n" +
              "保持约 6 米高度，先沿右环顺时针飞行，再沿左环逆时针飞行。\n\n" +
              "检查点具有水平和垂直容错，主要用于引导飞行路线。";

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name.Contains("矩形"))
            {
                routeLayout = RouteLayout.Rectangle;
                AlignRectangleRouteToBoundary();
            }

            if (drone == null)
            {
                drone = FindFirstObjectByType<SimpleFlightController>();
            }

            if (drone != null)
            {
                droneInputController = drone.GetComponent<DroneController>();
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

            if (state == ChallengeState.Briefing)
            {
                UpdateHud();
                return;
            }

            if (state == ChallengeState.ReadyForTakeoff && startWhenDroneTakesOff && drone.HeightFromHome > 0.35f)
            {
                BeginChallenge();
            }

            if (state == ChallengeState.Running)
            {
                UpdateAltitudePenalty();
                UpdateCourseBoundaryPenalty();
                UpdateCourseVisuals();
                if (ElapsedSeconds >= maxTimeSeconds)
                {
                    SetFeedback("训练时间已到，任务结束。", 3f);
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
            UpdateHud();
        }

        public void RegisterCheckpoint(ChallengeCheckpoint checkpoint, Collider other)
        {
            if (state != ChallengeState.Running || !IsDroneCollider(other))
            {
                return;
            }

            if (checkpoint != finishTrigger && !IsWithinCheckpointTolerance(checkpoint.transform.position))
            {
                return;
            }

            if (checkpoint == courseEntryTrigger)
            {
                if (!courseEntryReached)
                {
                    courseEntryReached = true;
                    lastCourseEntryTime = Time.time;
                    SetFeedback($"已进入{RouteDisplayName}，前往检查点 1。", 2f);
                    UpdateCourseVisuals();
                }

                return;
            }

            if (!courseEntryReached)
            {
                return;
            }

            if (ShouldIgnoreSharedCheckpointEvent(checkpoint))
            {
                return;
            }

            if (checkpoint == finishTrigger)
            {
                if (expectedCheckpoint >= RequiredCheckpointCount)
                {
                    SetFeedback("已到达终点，训练完成。", 3f);
                    FinishChallenge();
                }

                return;
            }

            if (checkpoint.Index != expectedCheckpoint)
            {
                return;
            }

            lastCheckpointClearTime = Time.time;
            lastCheckpointClearPosition = checkpoint.transform.position;
            expectedCheckpoint++;
            if (finishWhenFinalCheckpointCleared && expectedCheckpoint >= RequiredCheckpointCount)
            {
                FinishChallenge();
                return;
            }

            SetFeedback($"已通过检查点 {checkpoint.Index + 1}。", 1.6f);
            UpdateCourseVisuals();
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
            SetFeedback("发生碰撞，已扣分。", 1.8f);
        }

        private void BeginChallenge()
        {
            state = ChallengeState.Running;
            startTime = Time.time;
            finishTime = 0f;
            expectedCheckpoint = 0;
            courseEntryReached = false;
            lastCourseEntryTime = -10f;
            lastCheckpointClearTime = -10f;
            penalties = 0;
            collisions = 0;
            unsafeAltitudeTicks = 0;
            outOfCourseEvents = 0;
            isOutOfCourse = false;
            outOfCourseStartedAt = 0f;
            lastCourseBoundaryUpdateTime = Time.time;
            outOfCoursePenaltyPoints = 0f;
            lastCollisionPenaltyTime = -10f;
            nextAltitudePenaltyTime = Time.time + altitudePenaltyInterval;
            SetFeedback($"训练开始，请先飞向{RouteEntryLabel}。", 2f);
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
            resultText.text = $"训练完成  等级 {score.Grade}  得分 {score.FinalScore}\nR 或 Enter 重新训练 / Esc 返回菜单";
            }

            ShowResultPanel(score);
            SaveScoreRecord(score);
        }

        private void ResetChallenge()
        {
            state = ChallengeState.Briefing;
            startTime = Time.time;
            finishTime = 0f;
            expectedCheckpoint = 0;
            courseEntryReached = false;
            lastCourseEntryTime = -10f;
            lastCheckpointClearTime = -10f;
            penalties = 0;
            collisions = 0;
            unsafeAltitudeTicks = 0;
            outOfCourseEvents = 0;
            isOutOfCourse = false;
            outOfCourseStartedAt = 0f;
            lastCourseBoundaryUpdateTime = Time.time;
            outOfCoursePenaltyPoints = 0f;
            lastCollisionPenaltyTime = -10f;
            briefingPageIndex = 0;
            SetDroneInputEnabled(false);
            if (briefingPanel != null)
            {
                briefingPanel.SetActive(true);
            }

            UpdateBriefingPage();
            SetFeedback("请阅读训练说明。", 3f);
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

        private void AdvanceBriefing()
        {
            if (briefingPageIndex < BriefingPageCount - 1)
            {
                briefingPageIndex++;
                UpdateBriefingPage();
                return;
            }

            state = ChallengeState.ReadyForTakeoff;
            if (briefingPanel != null)
            {
                briefingPanel.SetActive(false);
            }

            SetDroneInputEnabled(true);
            SetFeedback("长按 S + D + ↓ + ← 解锁起飞，开始训练任务。", 3f);
            if (resultText != null)
            {
                resultText.text = string.Empty;
            }
        }

        private void SetDroneInputEnabled(bool enabled)
        {
            if (droneInputController == null && drone != null)
            {
                droneInputController = drone.GetComponent<DroneController>();
            }

            if (droneInputController != null)
            {
                droneInputController.enabled = enabled;
            }
        }

        private void UpdateAltitudePenalty()
        {
            if (!courseEntryReached || Time.time < nextAltitudePenaltyTime)
            {
                return;
            }

            nextAltitudePenaltyTime = Time.time + altitudePenaltyInterval;
            if (drone.Altitude < minSafeAltitude || drone.Altitude > maxSafeAltitude)
            {
                AddPenalty(unsafeAltitudePenalty);
                unsafeAltitudeTicks++;
                SetFeedback("高度不在安全范围内，请尽快调整。", 1.2f);
            }
        }

        private void UpdateCourseBoundaryPenalty()
        {
            float now = Time.time;
            float overflowDistance = GetCourseOverflowDistance(drone.transform.position);
            if (overflowDistance <= 0f)
            {
                if (isOutOfCourse)
                {
                    SetFeedback("已回到训练航道内。", 1.4f);
                }

                isOutOfCourse = false;
                lastCourseBoundaryUpdateTime = now;
                return;
            }

            if (!isOutOfCourse)
            {
                isOutOfCourse = true;
                outOfCourseEvents++;
                outOfCourseStartedAt = now;
                lastCourseBoundaryUpdateTime = now;
                SetFeedback("您已偏离航道。", 2f, new Color(1f, 0.16f, 0.16f, 1f));
                return;
            }

            float penaltyStart = outOfCourseStartedAt + outOfCourseGraceSeconds;
            float sampleStart = Mathf.Max(lastCourseBoundaryUpdateTime, penaltyStart);
            if (now > sampleStart)
            {
                float duration = now - sampleStart;
                outOfCoursePenaltyPoints += overflowDistance * duration * outOfCoursePenaltyPerMeterSecond;
            }

            lastCourseBoundaryUpdateTime = now;
        }

        private float GetCourseOverflowDistance(Vector3 position)
        {
            if (drone == null)
            {
                return 0f;
            }

            float distanceToCenterline = routeLayout == RouteLayout.Rectangle
                ? GetRectangleCenterlineDistance(position)
                : GetFigureEightCenterlineDistance(position);

            if (!courseEntryReached && hasCourseStartPosition && courseEntryTrigger != null)
            {
                float entryDistance = Mathf.Sqrt(DistanceSqToSegmentXZ(position, courseStartPosition, courseEntryTrigger.transform.position));
                distanceToCenterline = Mathf.Min(distanceToCenterline, entryDistance);
            }

            return Mathf.Max(0f, distanceToCenterline - Mathf.Max(0.1f, courseHalfWidth));
        }

        private float GetFigureEightCenterlineDistance(Vector3 position)
        {
            Vector2 point = new Vector2(position.x, position.z);
            Vector2 center = new Vector2(courseCenter.x, courseCenter.z);
            Vector2 leftCenter = center + Vector2.left * courseRadius;
            Vector2 rightCenter = center + Vector2.right * courseRadius;
            float leftLoopDistance = Mathf.Abs(Vector2.Distance(point, leftCenter) - courseRadius);
            float rightLoopDistance = Mathf.Abs(Vector2.Distance(point, rightCenter) - courseRadius);
            return Mathf.Min(leftLoopDistance, rightLoopDistance);
        }

        private float GetRectangleCenterlineDistance(Vector3 position)
        {
            float halfWidth = Mathf.Max(1f, rectangleHalfExtents.x);
            float halfDepth = Mathf.Max(1f, rectangleHalfExtents.y);
            Vector3 bottomLeft = courseCenter + new Vector3(-halfWidth, 0f, -halfDepth);
            Vector3 bottomRight = courseCenter + new Vector3(halfWidth, 0f, -halfDepth);
            Vector3 topRight = courseCenter + new Vector3(halfWidth, 0f, halfDepth);
            Vector3 topLeft = courseCenter + new Vector3(-halfWidth, 0f, halfDepth);

            float distanceSq = DistanceSqToSegmentXZ(position, bottomLeft, bottomRight);
            distanceSq = Mathf.Min(distanceSq, DistanceSqToSegmentXZ(position, bottomRight, topRight));
            distanceSq = Mathf.Min(distanceSq, DistanceSqToSegmentXZ(position, topRight, topLeft));
            distanceSq = Mathf.Min(distanceSq, DistanceSqToSegmentXZ(position, topLeft, bottomLeft));
            return Mathf.Sqrt(distanceSq);
        }

        private void AlignRectangleRouteToBoundary()
        {
            if (rectangleBoundaryRenderer == null)
            {
                GameObject boundary = GameObject.Find("Flight_Area_Orange_Border");
                if (boundary != null)
                {
                    rectangleBoundaryRenderer = boundary.GetComponent<Renderer>();
                }
            }

            if (rectangleBoundaryRenderer == null)
            {
                return;
            }

            Bounds bounds = rectangleBoundaryRenderer.bounds;
            float inset = Mathf.Max(0f, rectangleBoundaryCenterInset);
            rectangleHalfExtents = new Vector2(
                Mathf.Max(1f, bounds.extents.x - inset),
                Mathf.Max(1f, bounds.extents.z - inset));
            courseCenter = new Vector3(bounds.center.x, courseCenter.y, bounds.center.z);
        }

        private static float DistanceSqToSegmentXZ(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 a = new Vector2(segmentStart.x, segmentStart.z);
            Vector2 b = new Vector2(segmentEnd.x, segmentEnd.z);
            Vector2 segment = b - a;
            float lengthSq = segment.sqrMagnitude;
            if (lengthSq <= 0.0001f)
            {
                return (p - a).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, segment) / lengthSq);
            Vector2 closest = a + segment * t;
            return (p - closest).sqrMagnitude;
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
            bool timerActive = state == ChallengeState.Running || state == ChallengeState.Finished;
            float elapsed = Mathf.Max(0f, timerActive ? ElapsedSeconds : 0f);
            int timePenalty = elapsed <= targetTimeSeconds ? 0 : Mathf.CeilToInt((elapsed - targetTimeSeconds) / 5f);
            int collisionPenaltyTotal = collisions * collisionPenalty;
            int unsafeAltitudePenaltyTotal = unsafeAltitudeTicks * unsafeAltitudePenalty;
            int outOfCoursePenaltyTotal = Mathf.CeilToInt(Mathf.Max(0f, outOfCoursePenaltyPoints - 0.0001f));
            int countedEventPenalty = collisionPenaltyTotal + unsafeAltitudePenaltyTotal;
            int otherPenalty = Mathf.Max(0, penalties - countedEventPenalty);
            int incompleteCheckpoints = state == ChallengeState.Finished
                ? Mathf.Max(0, RequiredCheckpointCount - expectedCheckpoint)
                : 0;
            int incompleteCheckpointPenalty = incompleteCheckpoints * 12;
            int totalPenalty = timePenalty + penalties + outOfCoursePenaltyTotal + incompleteCheckpointPenalty;
            int finalScore = Mathf.Clamp(100 - totalPenalty, 0, 100);

            return new ScoreBreakdown
            {
                ElapsedSeconds = elapsed,
                TimePenalty = timePenalty,
                CollisionPenalty = collisionPenaltyTotal,
                UnsafeAltitudePenalty = unsafeAltitudePenaltyTotal,
                OutOfCoursePenalty = outOfCoursePenaltyTotal,
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
                resultTitleText.text = score.FinalScore >= 60 ? "训练完成 · 通过" : "训练完成 · 未通过";
            }

            if (resultSummaryText != null)
            {
                resultSummaryText.text =
                    $"得分 {score.FinalScore}    等级 {score.Grade}\n" +
                    $"用时 {FormatTime(score.ElapsedSeconds)}    检查点 {Mathf.Min(expectedCheckpoint, RequiredCheckpointCount)}/{RequiredCheckpointCount}";
            }

            if (resultBreakdownText != null)
            {
                resultBreakdownText.text =
                    "扣分明细\n" +
                    $"超时：-{score.TimePenalty}\n" +
                    $"碰撞：{collisions} 次，-{score.CollisionPenalty}\n" +
                    $"偏离航道：{outOfCourseEvents} 次，-{score.OutOfCoursePenalty}\n" +
                    $"高度警告：{unsafeAltitudeTicks} 次，-{score.UnsafeAltitudePenalty}\n" +
                    $"其他扣分：-{score.OtherPenalty}\n" +
                    $"未完成检查点：{score.IncompleteCheckpoints} 个，-{score.IncompleteCheckpointPenalty}\n" +
                    $"总扣分：-{score.TotalPenalty}";
            }

            if (resultAdviceText != null)
            {
                resultAdviceText.text = BuildTrainingAdvice(score);
            }
        }

        private string BuildTrainingAdvice(ScoreBreakdown score)
        {
            List<string> advice = new List<string>();
            if (outOfCourseEvents > 0)
            {
                advice.Add("转弯前适当减速，尽量沿蓝色航道中心线飞行。");
            }

            if (collisions > 0)
            {
                advice.Add("接近桶桩和中心区域时减小操纵量，预留制动距离。");
            }

            if (unsafeAltitudeTicks > 0)
            {
                advice.Add("保持 4–8 米安全高度，目标高度为 6 米。");
            }

            if (score.TimePenalty > 0)
            {
                advice.Add("在保持航线准确的前提下减少悬停和重复修正。");
            }

            if (advice.Count == 0)
            {
                advice.Add("航线控制稳定，继续保持匀速和小幅度操纵。");
            }

            if (advice.Count < 2)
            {
                advice.Add("通过检查点后提前观察下一个蓝色目标，平滑衔接转弯。");
            }

            return "训练建议\n1. " + advice[0] + "\n2. " + advice[1];
        }

        private void SaveScoreRecord(ScoreBreakdown score)
        {
            ScoreRecordStore.Add(new ScoreRecordStore.ScoreRecord
            {
                score = score.FinalScore,
                grade = score.Grade,
                completionTimeSeconds = score.ElapsedSeconds,
                collisions = collisions,
                wrongCheckpointHits = 0,
                outOfCourseTicks = outOfCourseEvents,
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
            SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded("TrainingSceneSelection")
                ? "TrainingSceneSelection"
                : "MainMenu");
        }

        private bool IsDroneCollider(Collider other)
        {
            return drone != null && other.GetComponentInParent<SimpleFlightController>() == drone;
        }

        private bool IsWithinCheckpointTolerance(Vector3 checkpointPosition)
        {
            if (drone == null)
            {
                return false;
            }

            Vector3 offset = drone.transform.position - checkpointPosition;
            float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
            return horizontalDistance <= checkpointHorizontalTolerance
                && Mathf.Abs(offset.y) <= checkpointVerticalTolerance;
        }

        private bool ShouldIgnoreSharedCheckpointEvent(ChallengeCheckpoint checkpoint)
        {
            if (checkpoint == null)
            {
                return true;
            }

            Vector3 position = checkpoint.transform.position;
            float duplicateDistance = Mathf.Max(0.1f, checkpointHorizontalTolerance * 0.1f);
            if (courseEntryTrigger != null
                && Time.time - lastCourseEntryTime <= sharedCheckpointDebounceSeconds
                && Vector3.Distance(position, courseEntryTrigger.transform.position) <= duplicateDistance)
            {
                return true;
            }

            if (Time.time - lastCheckpointClearTime <= sharedCheckpointDebounceSeconds
                && Vector3.Distance(position, lastCheckpointClearPosition) <= duplicateDistance)
            {
                return true;
            }

            if (checkpoint.Index != expectedCheckpoint
                && expectedCheckpoint >= 0
                && expectedCheckpoint < checkpointTriggers.Count
                && Vector3.Distance(position, checkpointTriggers[expectedCheckpoint].transform.position) <= duplicateDistance)
            {
                return true;
            }

            return false;
        }

        private void BuildCourseTriggers()
        {
            if (checkpoints == null || checkpoints.Length == 0)
            {
                if (!createDefaultCourseIfEmpty)
                {
                    return;
                }

                checkpoints = CreateDefaultCheckpointTransforms();
            }

            checkpointTriggers.Clear();
            markerRenderers.Clear();
            markerBaseScales.Clear();
            markerLights.Clear();
            guideMaterial = CreateGuideMaterial();
            activeGuideLine = showGuidanceLights ? CreateGuideLine("Active Target Guide", guideLineWidth, 0.9f) : null;

            Vector3 entryPosition = GetCourseEntryPosition();
            courseEntryTrigger = CreateTrigger(
                "Course_Entry",
                entryPosition,
                new Vector3(checkpointHorizontalTolerance * 2f, checkpointVerticalTolerance * 2f, checkpointHorizontalTolerance * 2f));
            courseEntryTrigger.Configure(this, -1);
            Transform entryMarker = CreateCourseMarker("Course Entry Marker", entryPosition, new Color(0.15f, 0.95f, 1f, 0.72f));
            courseEntryMarkerRenderer = entryMarker.GetComponent<Renderer>();

            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] == null)
                {
                    continue;
                }

                ChallengeCheckpoint trigger = CreateTrigger(
                    "Checkpoint_" + (i + 1),
                    checkpoints[i].position,
                    new Vector3(checkpointHorizontalTolerance * 2f, checkpointVerticalTolerance * 2f, checkpointHorizontalTolerance * 2f));
                trigger.Configure(this, checkpointTriggers.Count);
                checkpointTriggers.Add(trigger);
                Renderer markerRenderer = checkpoints[i].GetComponentInChildren<Renderer>();
                markerRenderers.Add(markerRenderer);
                markerBaseScales.Add(checkpoints[i].localScale);
                markerLights.Add(showGuidanceLights ? CreateGuideLight("Checkpoint_" + (i + 1) + "_Light", checkpoints[i], new Color(0.15f, 0.95f, 1f, 1f)) : null);
            }

            finishWhenFinalCheckpointCleared = finishTarget == null;
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
                finishLight = CreateGuideLight("Finish_Light", finishMarker.transform, new Color(1f, 0.2f, 1f, 1f));
            }
        }

        private Transform[] CreateDefaultCheckpointTransforms()
        {
            courseStartPosition = drone != null ? drone.transform.position : displayOrigin;
            hasCourseStartPosition = true;

            return routeLayout == RouteLayout.Rectangle
                ? CreateRectangleCheckpointTransforms()
                : CreateFigureEightCheckpointTransforms();
        }

        private Transform[] CreateFigureEightCheckpointTransforms()
        {

            Vector3[] positions = new Vector3[CheckpointsPerLoop * 2];
            Vector3 rightCenter = courseCenter + Vector3.right * courseRadius;
            Vector3 leftCenter = courseCenter + Vector3.left * courseRadius;
            float angleStep = Mathf.PI * 2f / CheckpointsPerLoop;

            // Start at the tangent point, fly the right loop clockwise, then the left loop counter-clockwise.
            for (int i = 1; i <= CheckpointsPerLoop; i++)
            {
                float angle = Mathf.PI - angleStep * i;
                positions[i - 1] = new Vector3(
                    rightCenter.x + Mathf.Cos(angle) * courseRadius,
                    courseCenter.y + checkpointHeight,
                    rightCenter.z + Mathf.Sin(angle) * courseRadius);
            }

            for (int i = 1; i <= CheckpointsPerLoop; i++)
            {
                float angle = angleStep * i;
                positions[CheckpointsPerLoop + i - 1] = new Vector3(
                    leftCenter.x + Mathf.Cos(angle) * courseRadius,
                    courseCenter.y + checkpointHeight,
                    leftCenter.z + Mathf.Sin(angle) * courseRadius);
            }

            Transform[] generated = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Color color = i == 0
                    ? new Color(0.25f, 0.9f, 1f, 0.55f)
                    : new Color(1f, 0.78f, 0.1f, 0.55f);
                generated[i] = CreateCourseMarker("Mission Marker " + (i + 1), positions[i], color);
            }

            return generated;
        }

        private Transform[] CreateRectangleCheckpointTransforms()
        {
            float halfWidth = Mathf.Max(1f, rectangleHalfExtents.x);
            float halfDepth = Mathf.Max(1f, rectangleHalfExtents.y);
            float halfStep = halfWidth * 0.5f;
            Vector3[] positions =
            {
                courseCenter + new Vector3(halfStep, checkpointHeight, -halfDepth),
                courseCenter + new Vector3(halfWidth, checkpointHeight, -halfDepth),
                courseCenter + new Vector3(halfWidth, checkpointHeight, 0f),
                courseCenter + new Vector3(halfWidth, checkpointHeight, halfDepth),
                courseCenter + new Vector3(halfStep, checkpointHeight, halfDepth),
                courseCenter + new Vector3(0f, checkpointHeight, halfDepth),
                courseCenter + new Vector3(-halfStep, checkpointHeight, halfDepth),
                courseCenter + new Vector3(-halfWidth, checkpointHeight, halfDepth),
                courseCenter + new Vector3(-halfWidth, checkpointHeight, 0f),
                courseCenter + new Vector3(-halfWidth, checkpointHeight, -halfDepth),
                courseCenter + new Vector3(-halfStep, checkpointHeight, -halfDepth),
                courseCenter + new Vector3(0f, checkpointHeight, -halfDepth)
            };

            Transform[] generated = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Color color = i == 0
                    ? new Color(0.25f, 0.9f, 1f, 0.55f)
                    : new Color(1f, 0.78f, 0.1f, 0.55f);
                generated[i] = CreateCourseMarker("Mission Marker " + (i + 1), positions[i], color);
            }

            return generated;
        }

        private Vector3 GetCourseEntryPosition()
        {
            if (routeLayout == RouteLayout.Rectangle)
            {
                return courseCenter + new Vector3(0f, checkpointHeight, -Mathf.Max(1f, rectangleHalfExtents.y));
            }

            return courseCenter + Vector3.up * checkpointHeight;
        }

        private Transform CreateCourseMarker(string objectName, Vector3 position, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = objectName;
            marker.transform.SetParent(transform);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one;
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMarkerMaterial(color);
            }

            return marker.transform;
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
            Shader shader = Shader.Find("DroneMicroClass/CheckpointSphere");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            Material material = new Material(shader);
            material.color = color;
            material.renderQueue = 3000;
            return material;
        }

        private void CreateHud()
        {
            Canvas existingCanvas = GetComponentInChildren<Canvas>(true);
            if (existingCanvas != null)
            {
                existingCanvas.gameObject.SetActive(false);
                Destroy(existingCanvas.gameObject);
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
            GameObject missionPanel = CreatePanel(canvasObject.transform, "Mission Status Background", Vector2.zero, new Vector2(350f, 250f), new Color(0.01f, 0.02f, 0.025f, 0.72f));
            RectTransform missionPanelRect = missionPanel.GetComponent<RectTransform>();
            missionPanelRect.anchorMin = new Vector2(0f, 1f);
            missionPanelRect.anchorMax = missionPanelRect.anchorMin;
            missionPanelRect.pivot = new Vector2(0f, 1f);
            missionPanelRect.anchoredPosition = new Vector2(20f, -20f);
            titleText = CreateText(missionPanel.transform, "Mission Title", new Vector2(16f, -14f), new Vector2(318f, 44f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            detailText = CreateText(missionPanel.transform, "Mission Detail", new Vector2(16f, -60f), new Vector2(318f, 180f), 20, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject feedbackPanel = CreatePanel(canvasObject.transform, "Flight Prompt Background", Vector2.zero, new Vector2(1000f, 82f), new Color(0.01f, 0.02f, 0.025f, 0.72f));
            RectTransform feedbackPanelRect = feedbackPanel.GetComponent<RectTransform>();
            feedbackPanelRect.anchorMin = new Vector2(0.5f, 1f);
            feedbackPanelRect.anchorMax = feedbackPanelRect.anchorMin;
            feedbackPanelRect.pivot = new Vector2(0.5f, 1f);
            feedbackPanelRect.anchoredPosition = new Vector2(0f, -170f);
            feedbackText = CreateText(feedbackPanel.transform, "Mission Feedback", new Vector2(0f, -18f), new Vector2(960f, 52f), 24, FontStyle.Bold, TextAnchor.UpperCenter);
            resultText = CreateText(canvasObject.transform, "Mission Result", new Vector2(0f, -72f), new Vector2(1100f, 120f), 34, FontStyle.Bold, TextAnchor.UpperCenter);
            resultPanel = CreatePanel(canvasObject.transform, "Result Panel", Vector2.zero, new Vector2(720f, 660f), new Color(0.01f, 0.025f, 0.035f, 0.92f));
            resultTitleText = CreateText(resultPanel.transform, "Result Title", new Vector2(0f, -32f), new Vector2(620f, 54f), 34, FontStyle.Bold, TextAnchor.UpperCenter);
            resultSummaryText = CreateText(resultPanel.transform, "Result Summary", new Vector2(0f, -96f), new Vector2(640f, 92f), 24, FontStyle.Bold, TextAnchor.UpperCenter);
            resultBreakdownText = CreateText(resultPanel.transform, "Result Breakdown", new Vector2(56f, -198f), new Vector2(610f, 220f), 20, FontStyle.Normal, TextAnchor.UpperLeft);
            resultAdviceText = CreateText(resultPanel.transform, "Result Advice", new Vector2(56f, -430f), new Vector2(610f, 110f), 19, FontStyle.Normal, TextAnchor.UpperLeft);

            Button restartButton = CreateButton(resultPanel.transform, "重新训练", new Vector2(-172f, -570f), new Vector2(220f, 64f));
            restartButton.onClick.AddListener(RestartLevel);
            Button menuButton = CreateButton(resultPanel.transform, "返回菜单", new Vector2(172f, -570f), new Vector2(220f, 64f));
            menuButton.onClick.AddListener(ReturnToMenu);
            resultPanel.SetActive(false);
            briefingPanel = CreateBriefingPanel(canvasObject.transform);

            titleText.font = font;
            detailText.font = font;
            feedbackText.font = font;
            resultText.font = font;
            resultTitleText.font = font;
            resultSummaryText.font = font;
            resultBreakdownText.font = font;
            resultAdviceText.font = font;
        }

        private GameObject CreateBriefingPanel(Transform parent)
        {
            GameObject overlay = new GameObject("Training Briefing Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0.01f, 0.02f, 0.03f, 0.66f);

            GameObject panel = CreatePanel(overlay.transform, "Training Briefing Panel", Vector2.zero, new Vector2(1400f, 1000f), new Color(0.015f, 0.045f, 0.06f, 0.95f));
            briefingTitleText = CreateText(panel.transform, "Briefing Title", new Vector2(0f, -34f), new Vector2(1240f, 48f), 32, FontStyle.Bold, TextAnchor.UpperCenter);

            briefingSubtitleText = CreateText(panel.transform, "Briefing Subtitle", new Vector2(0f, -84f), new Vector2(1240f, 36f), 22, FontStyle.Bold, TextAnchor.UpperCenter);
            briefingSubtitleText.text = RouteSubtitle;

            briefingBodyText = CreateText(panel.transform, "Briefing Body", new Vector2(90f, -142f), new Vector2(1220f, 650f), 21, FontStyle.Normal, TextAnchor.UpperLeft);
            briefingControlsDiagram = CreateControlsDiagram(panel.transform);
            briefingControlsDiagram.SetActive(false);
            briefingPageText = CreateText(panel.transform, "Briefing Page", new Vector2(0f, -852f), new Vector2(220f, 32f), 18, FontStyle.Normal, TextAnchor.UpperCenter);

            Button nextButton = CreateButton(panel.transform, "下一步", new Vector2(-150f, -910f), new Vector2(240f, 62f));
            nextButton.onClick.AddListener(AdvanceBriefing);
            briefingNextButtonText = nextButton.GetComponentInChildren<Text>();

            Button menuButton = CreateButton(panel.transform, "返回菜单", new Vector2(150f, -910f), new Vector2(240f, 62f));
            menuButton.onClick.AddListener(ReturnToMenu);
            UpdateBriefingPage();
            return overlay;
        }

        private void UpdateBriefingPage()
        {
            if (briefingTitleText == null || briefingBodyText == null)
            {
                return;
            }

            briefingPageIndex = Mathf.Clamp(briefingPageIndex, 0, BriefingPageCount - 1);
            bool showControlsDiagram = briefingPageIndex == BriefingPageCount - 1;
            if (briefingControlsDiagram != null)
            {
                briefingControlsDiagram.SetActive(showControlsDiagram);
            }

            RectTransform bodyRect = briefingBodyText.rectTransform;
            if (showControlsDiagram)
            {
                bodyRect.anchoredPosition = new Vector2(120f, -810f);
                bodyRect.sizeDelta = new Vector2(1160f, 42f);
                briefingBodyText.fontSize = 17;
            }
            else
            {
                bodyRect.anchoredPosition = new Vector2(90f, -142f);
                bodyRect.sizeDelta = new Vector2(1220f, 650f);
                briefingBodyText.fontSize = 21;
            }

            switch (briefingPageIndex)
            {
                case 0:
                    briefingTitleText.text = "训练任务";
                    briefingBodyText.text = RouteTaskDescription;
                    break;

                case 1:
                    briefingTitleText.text = "扣分规则";
                    briefingBodyText.text =
                        "任务初始分为 100 分。\n\n" +
                        "碰撞障碍物：每次扣 8 分（1 秒内重复接触只计一次）\n" +
                        "飞出蓝色航道：宽限 1 秒，之后按偏离距离和持续时间扣分\n" +
                        "高度低于 4 米或高于 8 米：每 1.5 秒扣 2 分\n" +
                        "超过 120 秒：每 5 秒扣 1 分；180 秒强制结束\n\n" +
                        "请保持平稳飞行，转弯前提前减速。";
                    break;

                default:
                    briefingTitleText.text = "基础操作";
                    briefingBodyText.text = "键盘：W/S/A/D 对应左摇杆，方向键对应右摇杆。长按 S + D + ↓ + ← 解锁/起飞。";
                    break;
            }

            if (briefingPageText != null)
            {
                briefingPageText.text = $"{briefingPageIndex + 1} / {BriefingPageCount}";
            }

            if (briefingNextButtonText != null)
            {
                briefingNextButtonText.text = briefingPageIndex == BriefingPageCount - 1 ? "开始训练" : "下一步";
            }
        }

        private static GameObject CreateControlsDiagram(Transform parent)
        {
            Texture2D controlsTexture = Resources.Load<Texture2D>("UI/caac-controls");
            if (controlsTexture != null)
            {
                GameObject imageObject = new GameObject("CAAC Controls Image", typeof(RectTransform), typeof(RawImage));
                imageObject.transform.SetParent(parent, false);

                RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.anchorMin = new Vector2(0.5f, 1f);
                imageRect.anchorMax = imageRect.anchorMin;
                imageRect.pivot = new Vector2(0.5f, 1f);
                imageRect.anchoredPosition = new Vector2(0f, -104f);
                imageRect.sizeDelta = new Vector2(980f, 735f);

                RawImage image = imageObject.GetComponent<RawImage>();
                image.texture = controlsTexture;
                image.color = Color.white;
                image.raycastTarget = false;
                return imageObject;
            }

            GameObject missingPanel = CreatePanel(parent, "CAAC Controls Image Missing", new Vector2(0f, -104f), new Vector2(980f, 735f), new Color(0.98f, 0.98f, 0.96f, 1f));
            RectTransform missingRect = missingPanel.GetComponent<RectTransform>();
            missingRect.anchorMin = new Vector2(0.5f, 1f);
            missingRect.anchorMax = missingRect.anchorMin;
            missingRect.pivot = new Vector2(0.5f, 1f);
            CreateText(missingPanel.transform, "Missing Controls Image", Vector2.zero, new Vector2(860f, 80f), 24, FontStyle.Bold, TextAnchor.MiddleCenter).text =
                "缺少遥控器说明图：Resources/UI/caac-controls.png";
            return missingPanel;
        }

#if false
            GameObject root = CreatePanel(parent, "CAAC Controls Diagram", new Vector2(0f, -132f), new Vector2(804f, 292f), Color.white);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = rootRect.anchorMin;
            rootRect.pivot = new Vector2(0.5f, 1f);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.98f, 0.98f, 0.96f, 1f);

            GameObject content = new GameObject("Diagram Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = contentRect.anchorMin;
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 146f);
            contentRect.sizeDelta = Vector2.zero;

            Color ink = new Color(0.02f, 0.02f, 0.02f, 1f);
            Color paleInk = new Color(0.02f, 0.02f, 0.02f, 0.72f);

            Transform page = content.transform;

            GameObject titleFrame = CreateDiagramRect(page, "Diagram Title Frame", new Vector2(0f, -24f), new Vector2(368f, 38f), Color.clear, ink, 2f);
            Text title = CreateDiagramText(titleFrame.transform, "功能适配 CAAC 训练", Vector2.zero, new Vector2(350f, 34f), 25, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
            title.raycastTarget = false;

            GameObject controller = CreateDiagramRect(page, "Controller Outline", new Vector2(0f, -88f), new Vector2(460f, 180f), Color.clear, ink, 3f);
            CreateDiagramRect(controller.transform, "Top Handle", new Vector2(0f, -2f), new Vector2(260f, 32f), Color.clear, ink, 3f);
            CreateDiagramRect(controller.transform, "Center Screen", new Vector2(0f, -54f), new Vector2(126f, 54f), Color.clear, ink, 2f);
            CreateDiagramRect(controller.transform, "Power Switch", new Vector2(170f, -130f), new Vector2(42f, 54f), Color.clear, ink, 2f);
            CreateDiagramText(controller.transform, "I\nO", new Vector2(170f, -134f), new Vector2(36f, 48f), 17, FontStyle.Bold, TextAnchor.MiddleCenter, ink);

            CreateStick(controller.transform, "Left Stick", new Vector2(-118f, -76f), "横滚 / 俯仰", ink);
            CreateStick(controller.transform, "Right Stick", new Vector2(118f, -76f), "偏航 / 升降", ink);
            CreateDiagramRect(controller.transform, "Left Trim", new Vector2(-118f, -138f), new Vector2(86f, 22f), Color.clear, ink, 2f);
            CreateDiagramRect(controller.transform, "Right Trim", new Vector2(118f, -138f), new Vector2(86f, 22f), Color.clear, ink, 2f);
            CreateDiagramText(controller.transform, "||||", new Vector2(-118f, -140f), new Vector2(80f, 20f), 16, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
            CreateDiagramText(controller.transform, "||||", new Vector2(118f, -140f), new Vector2(80f, 20f), 16, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
            CreateDiagramText(controller.transform, "●", new Vector2(-150f, -36f), new Vector2(28f, 28f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
            CreateDiagramText(controller.transform, "●", new Vector2(150f, -36f), new Vector2(28f, 28f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, ink);

            CreateDiagramText(page, "辅助通道", new Vector2(-308f, -82f), new Vector2(150f, 32f), 20, FontStyle.Bold, TextAnchor.MiddleLeft, ink);
            CreateDiagramLine(page, "Left Aux Lead", new Vector2(-218f, -90f), new Vector2(-144f, -46f), 3f, ink);
            CreateDiagramText(page, "辅助通道", new Vector2(308f, -82f), new Vector2(150f, 32f), 20, FontStyle.Bold, TextAnchor.MiddleRight, ink);
            CreateDiagramLine(page, "Right Aux Lead", new Vector2(218f, -90f), new Vector2(144f, -46f), 3f, ink);

            CreateDiagramText(page, "左摇杆：\n横滚 / 俯仰", new Vector2(-310f, -158f), new Vector2(166f, 56f), 19, FontStyle.Bold, TextAnchor.MiddleLeft, ink);
            CreateDiagramLine(page, "Left Stick Lead", new Vector2(-212f, -166f), new Vector2(-122f, -140f), 3f, ink);
            CreateDiagramText(page, "右摇杆：\n偏航 / 升降", new Vector2(310f, -158f), new Vector2(166f, 56f), 19, FontStyle.Bold, TextAnchor.MiddleRight, ink);
            CreateDiagramLine(page, "Right Stick Lead", new Vector2(212f, -166f), new Vector2(122f, -140f), 3f, ink);

            CreateDiagramText(page, "微调按键", new Vector2(-302f, -226f), new Vector2(142f, 32f), 18, FontStyle.Bold, TextAnchor.MiddleLeft, ink);
            CreateDiagramLine(page, "Left Trim Lead", new Vector2(-220f, -224f), new Vector2(-118f, -202f), 3f, ink);
            CreateDiagramText(page, "微调按键", new Vector2(302f, -226f), new Vector2(142f, 32f), 18, FontStyle.Bold, TextAnchor.MiddleRight, ink);
            CreateDiagramLine(page, "Right Trim Lead", new Vector2(220f, -224f), new Vector2(118f, -202f), 3f, ink);
            CreateDiagramText(page, "电源开关", new Vector2(292f, -262f), new Vector2(142f, 28f), 18, FontStyle.Bold, TextAnchor.MiddleRight, ink);
            CreateDiagramLine(page, "Power Lead", new Vector2(214f, -252f), new Vector2(170f, -204f), 3f, ink);

            Text footer = CreateDiagramText(page, "F2 打开手柄检测", new Vector2(0f, -266f), new Vector2(240f, 24f), 18, FontStyle.Bold, TextAnchor.MiddleCenter, paleInk);
            footer.raycastTarget = false;
            return root;
        }

        private static void CreateStick(Transform parent, string name, Vector2 anchoredPosition, string label, Color ink)
        {
            GameObject frame = CreateDiagramRect(parent, name + " Frame", anchoredPosition, new Vector2(100f, 82f), Color.clear, ink, 2f);
            CreateDiagramText(frame.transform, "↑\n←  ●  →\n↓", new Vector2(0f, -2f), new Vector2(92f, 66f), 24, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
            CreateDiagramText(frame.transform, label, new Vector2(0f, -62f), new Vector2(118f, 24f), 13, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
        }

        private static GameObject CreateDiagramRect(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color fill, Color stroke, float strokeWidth)
        {
            GameObject rect = CreatePanel(parent, objectName, anchoredPosition, size, fill);
            Image fillImage = rect.GetComponent<Image>();
            fillImage.color = fill;

            if (strokeWidth > 0f)
            {
                CreateDiagramRectLine(rect.transform, objectName + " Top", new Vector2(0f, size.y * 0.5f), new Vector2(size.x, strokeWidth), 0f, stroke);
                CreateDiagramRectLine(rect.transform, objectName + " Bottom", new Vector2(0f, -size.y * 0.5f), new Vector2(size.x, strokeWidth), 0f, stroke);
                CreateDiagramRectLine(rect.transform, objectName + " Left", new Vector2(-size.x * 0.5f, 0f), new Vector2(strokeWidth, size.y), 0f, stroke);
                CreateDiagramRectLine(rect.transform, objectName + " Right", new Vector2(size.x * 0.5f, 0f), new Vector2(strokeWidth, size.y), 0f, stroke);
            }

            return rect;
        }

        private static Text CreateDiagramText(Transform parent, string textValue, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            Text text = CreateText(parent, "Diagram Text", anchoredPosition, size, fontSize, style, alignment);
            text.text = textValue;
            text.color = color;
            return text;
        }

        private static void CreateDiagramLine(Transform parent, string objectName, Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            Vector2 center = from + delta * 0.5f;
            GameObject line = CreatePanel(parent, objectName, center, new Vector2(length, Mathf.Max(1f, width)), color);
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.localRotation = length > 0.001f
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg)
                : Quaternion.identity;
        }

        private static void CreateDiagramRectLine(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, float rotationDegrees, Color color)
        {
            GameObject line = CreatePanel(parent, objectName, anchoredPosition, size, color);
            line.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        }

#endif

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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            text.text = label;
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
            titleText.text = RouteTitle;
            if (state == ChallengeState.Briefing)
            {
                detailText.text =
                    "状态：训练说明\n" +
                    $"检查点：0 / {RequiredCheckpointCount}\n" +
                    "计时：00:00\n" +
                    "分数：100\n" +
                    "请完成三页说明后开始训练。";
                return;
            }

            if (state == ChallengeState.ReadyForTakeoff)
            {
                detailText.text =
                    "状态：等待起飞\n" +
                    $"当前目标：{RouteEntryLabel}\n" +
                    $"检查点：0 / {RequiredCheckpointCount}\n" +
                    "计时：00:00\n" +
                    "分数：100\n" +
                    "长按 S + D + ↓ + ← 解锁起飞，离地后开始计时。";
                return;
            }

            detailText.text =
                $"状态：{GetStateLabel()}\n" +
                $"当前目标：{GetTargetLabel()}\n" +
                $"检查点：{Mathf.Min(expectedCheckpoint, RequiredCheckpointCount)} / {RequiredCheckpointCount}\n" +
                $"计时：{FormatTime(ElapsedSeconds)} / 目标 {FormatTime(targetTimeSeconds)}\n" +
                $"分数：{score.FinalScore}  扣分：{score.TotalPenalty}\n" +
                $"碰撞：{collisions}  偏离航道：{outOfCourseEvents}  高度警告：{unsafeAltitudeTicks}\n" +
                "R：重新开始    Esc：返回菜单";

            if (feedbackText != null && state == ChallengeState.Running && isOutOfCourse)
            {
                feedbackText.color = new Color(1f, 0.16f, 0.16f, 1f);
                feedbackText.text = "您已偏离航道。";
            }
            else if (feedbackText != null && Time.time > feedbackUntilTime && state == ChallengeState.Running)
            {
                feedbackText.color = Color.white;
                feedbackText.text = "飞往蓝色检查点";
            }
        }

        private string GetStateLabel()
        {
            switch (state)
            {
                case ChallengeState.Briefing:
                    return "训练说明";
                case ChallengeState.ReadyForTakeoff:
                    return "等待起飞";
                case ChallengeState.Running:
                    return "训练中";
                default:
                    return "已完成";
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
                return "任务完成";
            }

            if (!courseEntryReached)
            {
                return "航线入口（" + RouteEntryLabel + "）";
            }

            if (expectedCheckpoint < RequiredCheckpointCount)
            {
                return "检查点 " + (expectedCheckpoint + 1);
            }

            return "任务完成";
        }

        private void SetFeedback(string message, float duration)
        {
            SetFeedback(message, duration, Color.white);
        }

        private void SetFeedback(string message, float duration, Color color)
        {
            feedbackUntilTime = Time.time + Mathf.Max(0.2f, duration);
            if (feedbackText != null)
            {
                feedbackText.color = color;
                feedbackText.text = message;
            }
        }

        private void UpdateCourseVisuals()
        {
            if (courseEntryMarkerRenderer != null)
            {
                bool entryActive = state == ChallengeState.Running && !courseEntryReached;
                courseEntryMarkerRenderer.material.color = entryActive
                    ? new Color(0.15f, 0.95f, 1f, 0.95f)
                    : new Color(0.15f, 0.95f, 1f, courseEntryReached ? 0.28f : 0.62f);
            }

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
                else if (courseEntryReached && i == expectedCheckpoint && state == ChallengeState.Running)
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
            if (activeGuideLine == null || !showGuidanceLights || drone == null || state != ChallengeState.Running)
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
            float pulse = 0.7f + Mathf.Sin(Time.time * 7.5f) * 0.18f;
            Color color = expectedCheckpoint < RequiredCheckpointCount
                ? new Color(0.15f, 0.95f, 1f, pulse)
                : new Color(1f, 0.2f, 1f, pulse);

            activeGuideLine.enabled = true;
            activeGuideLine.positionCount = 2;
            activeGuideLine.SetPosition(0, dronePosition);
            activeGuideLine.SetPosition(1, targetPosition);
            activeGuideLine.widthMultiplier = guideLineWidth * (1f + Mathf.Sin(Time.time * 6f) * 0.14f);
            SetLineColor(activeGuideLine, color);
        }

        private bool TryGetActiveTargetPosition(out Vector3 targetPosition)
        {
            if (!courseEntryReached && courseEntryMarkerRenderer != null)
            {
                targetPosition = courseEntryMarkerRenderer.transform.position;
                return true;
            }

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
            public int UnsafeAltitudePenalty;
            public int OutOfCoursePenalty;
            public int OtherPenalty;
            public int IncompleteCheckpointPenalty;
            public int IncompleteCheckpoints;
            public int TotalPenalty;
            public int FinalScore;
            public string Grade;
        }
    }
}
