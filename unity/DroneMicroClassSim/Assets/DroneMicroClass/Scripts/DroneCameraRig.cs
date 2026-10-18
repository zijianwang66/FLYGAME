using UnityEngine;

namespace DroneMicroClass
{
    public sealed class DroneCameraRig : MonoBehaviour
    {
        public enum CameraMode
        {
            Chase = 0,
            WideFollow = 1,
            StartTower = 2,
            Nose = 3,
            GroundObserver = 4
        }

        [SerializeField] private Transform target;
        [SerializeField] private DroneFleetManager fleet;
        [SerializeField] private CameraMode mode = CameraMode.GroundObserver;
        [SerializeField] private bool allowInput = true;
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 3.8f, -10f);
        [SerializeField] private Vector3 wideFollowOffset = new Vector3(0f, 7f, -16f);
        [SerializeField] private Vector3 startTowerPosition = new Vector3(0f, 1.6f, -14f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField] private float positionSmoothTime = 0.18f;
        [SerializeField] private float rotationSharpness = 9f;
        [SerializeField] private float lookAheadTime = 0.2f;
        [SerializeField] private float maxLookAheadDistance = 3.5f;
        [SerializeField] private bool yawOnlyFollowOffset = true;
        [SerializeField] private bool stabilizeNoseHorizon = true;
        [SerializeField] private float noseHorizonSharpness = 24f;
        [SerializeField] private KeyCode nextCameraKey = KeyCode.C;
        [SerializeField] private KeyCode previousCameraKey = KeyCode.V;

        [Header("Development Camera Switching")]
        [SerializeField] private bool defaultToGroundObserver = true;
        [SerializeField] private bool allowViewSwitchingInRelease;

        [Header("Fixed Human-height Observer")]
        [SerializeField, Min(0.5f)] private float observerEyeHeight = 2.2f;
        [SerializeField, Min(1f)] private float observerDistanceBehindLaunch = 8f;
        [SerializeField, Min(1f)] private float observerZoomStartDistance = 18f;
        [SerializeField, Range(1f, 16f)] private float observerMaximumZoom = 8f;
        [SerializeField, Min(0.05f)] private float observerZoomSmoothTime = 0.45f;
        [SerializeField, Min(1f)] private float observerRotationSharpness = 12f;
        [SerializeField] private KeyCode groundObserverKey = KeyCode.G;

        private Vector3 velocity;
        private Rigidbody targetBody;
        private bool noseCameraSnapInitialized;
        private Camera rigCamera;
        private float originalFieldOfView;
        private float originalFarClipPlane;
        private float observerZoom = 1f;
        private float observerZoomVelocity;
        private Vector3 observerPosition;
        private bool observerPositionInitialized;
        private bool observerRotationInitialized;

        public string ModeName
        {
            get
            {
                return mode switch
                {
                    CameraMode.Chase => "FOLLOW",
                    CameraMode.WideFollow => "WIDE FOLLOW",
                    CameraMode.StartTower => "START TOWER",
                    CameraMode.Nose => "NOSE CAM",
                    CameraMode.GroundObserver => "GROUND OBSERVER",
                    _ => "CAMERA"
                };
            }
        }

        public float GimbalPitchDegrees => fleet != null ? fleet.CurrentGimbalPitchDegrees : 0f;

        private void Awake()
        {
            CacheCamera();
        }

        private void Start()
        {
            ConfigurePrimaryAudioListener();
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
            // Capture the launch area before takeoff, even when another view is active.
            InitializeObserverPosition();
            if (allowInput && defaultToGroundObserver)
            {
                SetMode(CameraMode.GroundObserver);
            }
        }

        private void ConfigurePrimaryAudioListener()
        {
            if (!allowInput)
            {
                return;
            }

            AudioListener primaryListener = GetComponent<AudioListener>();
            if (primaryListener == null)
            {
                primaryListener = gameObject.AddComponent<AudioListener>();
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = listener == primaryListener;
            }
        }

        private void OnDisable()
        {
            RestoreCameraLens();
            observerRotationInitialized = false;
        }

        public void Configure(Transform newTarget, DroneFleetManager newFleet, CameraMode initialMode, bool inputEnabled)
        {
            bool targetChanged = target != newTarget;
            target = newTarget;
            fleet = newFleet;
            allowInput = inputEnabled;
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
            if (targetChanged)
            {
                observerPositionInitialized = false;
            }
            SetMode(initialMode);
        }

        public void SetTarget(Transform newTarget)
        {
            if (target == newTarget)
            {
                return;
            }

            target = newTarget;
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
            noseCameraSnapInitialized = false;
            observerPositionInitialized = false;
            observerRotationInitialized = false;
        }

        private void Update()
        {
            bool isDevelopmentContext = Application.isEditor || Debug.isDebugBuild;
            if (!allowInput || (!isDevelopmentContext && !allowViewSwitchingInRelease))
            {
                return;
            }

            if (Input.GetKeyDown(groundObserverKey))
            {
                SetMode(CameraMode.GroundObserver);
                return;
            }

            if (Input.GetKeyDown(nextCameraKey))
            {
                StepMode(1);
            }

            if (Input.GetKeyDown(previousCameraKey))
            {
                StepMode(-1);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            switch (mode)
            {
                case CameraMode.Chase:
                    MoveCamera(GetFollowPosition(chaseOffset), GetLookTarget(), true);
                    break;
                case CameraMode.WideFollow:
                    MoveCamera(GetFollowPosition(wideFollowOffset), GetLookTarget(), true);
                    break;
                case CameraMode.StartTower:
                    MoveCamera(startTowerPosition, GetLookTarget(), false);
                    break;
                case CameraMode.Nose:
                    MoveNoseCamera();
                    break;
                case CameraMode.GroundObserver:
                    MoveGroundObserverCamera(Time.unscaledDeltaTime);
                    break;
            }
        }

        private void StepMode(int direction)
        {
            int modeCount = System.Enum.GetValues(typeof(CameraMode)).Length;
            int next = ((int)mode + direction + modeCount) % modeCount;
            SetMode((CameraMode)next);
        }

        private void SetMode(CameraMode newMode)
        {
            if (mode == newMode)
            {
                return;
            }

            RestoreCameraLens();
            mode = newMode;
            velocity = Vector3.zero;
            noseCameraSnapInitialized = false;
            observerRotationInitialized = false;
        }

        private void CacheCamera()
        {
            if (rigCamera != null)
            {
                return;
            }

            rigCamera = GetComponent<Camera>();
            if (rigCamera != null)
            {
                originalFieldOfView = rigCamera.fieldOfView;
                originalFarClipPlane = rigCamera.farClipPlane;
            }
        }

        private void RestoreCameraLens()
        {
            CacheCamera();
            if (rigCamera != null)
            {
                rigCamera.fieldOfView = originalFieldOfView;
                rigCamera.farClipPlane = originalFarClipPlane;
            }

            observerZoom = 1f;
            observerZoomVelocity = 0f;
        }

        private void InitializeObserverPosition()
        {
            if (observerPositionInitialized || target == null)
            {
                return;
            }

            observerPosition = target.position
                - GetYawOnlyRotation() * Vector3.forward * observerDistanceBehindLaunch;
            // Query once at the observer's feet; ignore aircraft and trigger volumes.
            RaycastHit[] hits = Physics.RaycastAll(
                observerPosition + Vector3.up * 2f, Vector3.down, 1000f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(target) || hit.normal.y < 0.5f || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                observerPosition.y = hit.point.y;
            }

            observerPosition.y += observerEyeHeight;
            observerPositionInitialized = true;
        }

        private void MoveGroundObserverCamera(float deltaTime)
        {
            InitializeObserverPosition();
            CacheCamera();
            transform.position = observerPosition;
            // Aim at the actual aircraft: flight look-ahead can miss it at long focal lengths.
            Vector3 lookDirection = target.position - observerPosition;
            float distance = lookDirection.magnitude;
            deltaTime = Mathf.Max(deltaTime, 0.0001f);
            if (rigCamera != null)
            {
                float desiredZoom = Mathf.Clamp(
                    distance / Mathf.Max(1f, observerZoomStartDistance), 1f, Mathf.Max(1f, observerMaximumZoom));
                observerZoom = Mathf.SmoothDamp(
                    observerZoom, desiredZoom, ref observerZoomVelocity,
                    observerZoomSmoothTime, Mathf.Infinity, deltaTime);
                // Zoom scales focal length, rather than linearly scaling the field of view.
                rigCamera.fieldOfView = 2f * Mathf.Atan(
                    Mathf.Tan(originalFieldOfView * Mathf.Deg2Rad * 0.5f) / observerZoom) * Mathf.Rad2Deg;
                rigCamera.farClipPlane = Mathf.Max(originalFarClipPlane, distance + 50f);
            }

            if (distance < 0.001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection / distance, Vector3.up);
            Quaternion rotation = observerRotationInitialized
                ? Quaternion.Slerp(transform.rotation, desiredRotation,
                    1f - Mathf.Exp(-observerRotationSharpness * deltaTime))
                : desiredRotation;
            // Bound tracking lag inside the narrower screen dimension, including portrait windows.
            float verticalFov = rigCamera != null ? rigCamera.fieldOfView : 58f;
            float horizontalFov = rigCamera != null
                ? 2f * Mathf.Atan(Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f) * rigCamera.aspect) * Mathf.Rad2Deg
                : verticalFov;
            float maxLag = Mathf.Min(verticalFov, horizontalFov) * 0.2f;
            float angle = Quaternion.Angle(rotation, desiredRotation);
            transform.rotation = angle > maxLag
                ? Quaternion.RotateTowards(rotation, desiredRotation, angle - maxLag)
                : rotation;
            observerRotationInitialized = true;
        }

        private Vector3 GetFollowPosition(Vector3 localOffset)
        {
            if (!yawOnlyFollowOffset)
            {
                return target.TransformPoint(localOffset);
            }

            return target.position + GetYawOnlyRotation() * localOffset;
        }

        private Vector3 GetLookTarget()
        {
            Vector3 lookTarget = target.position + lookAtOffset;
            if (targetBody == null)
            {
                return lookTarget;
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(targetBody.linearVelocity, Vector3.up);
            Vector3 lookAhead = Vector3.ClampMagnitude(horizontalVelocity * lookAheadTime, maxLookAheadDistance);
            return lookTarget + lookAhead;
        }

        private Quaternion GetYawOnlyRotation()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }

        private void MoveCamera(Vector3 desiredPosition, Vector3 lookTarget, bool smoothPosition)
        {
            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            transform.position = smoothPosition
                ? Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime, Mathf.Infinity, deltaTime)
                : desiredPosition;

            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                1f - Mathf.Exp(-rotationSharpness * deltaTime));
        }

        private void MoveNoseCamera()
        {
            Vector3 localPosition = fleet != null ? fleet.CurrentNoseCameraLocalPosition : new Vector3(0f, 0.2f, 1f);
            Vector3 localEuler = fleet != null ? fleet.CurrentNoseCameraLocalEuler : Vector3.zero;
            Vector3 gimbalEuler = localEuler + new Vector3(-GimbalPitchDegrees, 0f, 0f);
            bool keepHorizonLevel = fleet != null
                ? fleet.CurrentNoseCameraIsLevel
                : stabilizeNoseHorizon;
            if (keepHorizonLevel)
            {
                Quaternion yawOnly = GetYawOnlyRotation();
                Vector3 desiredPosition = target.TransformPoint(localPosition);
                Quaternion desiredRotation = yawOnly * Quaternion.Euler(gimbalEuler);
                float sharpness = fleet != null
                    ? Mathf.Max(noseHorizonSharpness, fleet.CurrentNoseCameraLevelSharpness)
                    : noseHorizonSharpness;
                float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

                transform.position = desiredPosition;
                if (!noseCameraSnapInitialized)
                {
                    transform.rotation = desiredRotation;
                    noseCameraSnapInitialized = true;
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        desiredRotation,
                        1f - Mathf.Exp(-sharpness * deltaTime));
                }

                return;
            }

            noseCameraSnapInitialized = false;
            transform.position = target.TransformPoint(localPosition);
            transform.rotation = target.rotation * Quaternion.Euler(gimbalEuler);
        }
    }
}
