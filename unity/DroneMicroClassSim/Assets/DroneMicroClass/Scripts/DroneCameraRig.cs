using UnityEngine;

namespace DroneMicroClass
{
    public sealed class DroneCameraRig : MonoBehaviour
    {
        public enum CameraMode
        {
            Chase,
            WideFollow,
            StartTower,
            Nose
        }

        [SerializeField] private Transform target;
        [SerializeField] private DroneFleetManager fleet;
        [SerializeField] private CameraMode mode = CameraMode.Chase;
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

        private Vector3 velocity;
        private Rigidbody targetBody;
        private bool noseCameraSnapInitialized;

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
                    _ => "CAMERA"
                };
            }
        }

        public float GimbalPitchDegrees => fleet != null ? fleet.CurrentGimbalPitchDegrees : 0f;

        public void Configure(Transform newTarget, DroneFleetManager newFleet, CameraMode initialMode, bool inputEnabled)
        {
            target = newTarget;
            fleet = newFleet;
            mode = initialMode;
            allowInput = inputEnabled;
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
            noseCameraSnapInitialized = false;
        }

        private void Update()
        {
            if (!allowInput)
            {
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
            }
        }

        private void StepMode(int direction)
        {
            int modeCount = System.Enum.GetValues(typeof(CameraMode)).Length;
            int next = ((int)mode + direction + modeCount) % modeCount;
            mode = (CameraMode)next;
            velocity = Vector3.zero;
            noseCameraSnapInitialized = false;
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
