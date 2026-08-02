using UnityEngine;

namespace DroneMicroClass
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SimpleFlightController : MonoBehaviour
    {
        [SerializeField] private DroneProfile profile;
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private Transform[] rotorVisuals;
        [SerializeField, Min(0.2f)] private float assistedTakeoffHeight = 1.8f;
        [SerializeField, Min(0.1f)] private float landingDescentSpeed = 0.85f;
        [SerializeField, Min(0.02f)] private float landingMotorCutoffHeight = 0.16f;
        [SerializeField, Min(0.01f)] private float landingMotorCutoffVerticalSpeed = 0.8f;
        [SerializeField, Min(0.05f)] private float rotorStartupSmoothTime = 1.35f;
        [SerializeField, Min(0.05f)] private float rotorRuntimeSmoothTime = 0.32f;
        [SerializeField, Min(0.05f)] private float rotorShutdownSmoothTime = 3.2f;
        [SerializeField, Range(0.1f, 1f)] private float takeoffLiftStartRotorSpin = 0.58f;
        [SerializeField, Min(0.1f)] private float takeoffFullRotorHeight = 0.75f;

        [Header("GPS Position Hold")]
        [SerializeField] private bool gpsPositionHoldEnabled = true;
        [SerializeField, Min(0f)] private float gpsPositionGain = 1.4f;
        [SerializeField, Min(0f)] private float gpsVelocityGain = 2.2f;
        [SerializeField, Min(0.1f)] private float gpsMaximumAcceleration = 4.5f;

        private Rigidbody body;
        private DroneCommand command;
        private PidController altitudePid;
        private PidController pitchPid;
        private PidController rollPid;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private float throttle01;
        private float targetAltitude;
        private float smoothedPitch;
        private float smoothedRoll;
        private float smoothedYaw;
        private float smoothedVertical;
        private float rotorSpin01;
        private float rotorSpinVelocity;
        private Quaternion[] rotorBaseRotations;
        private float[] rotorAngles;
        private bool motorsArmed;
        private bool landingActive;
        private bool takeoffSequenceActive;
        private float takeoffStartHeight;
        private Vector3 gpsHoldPosition;

        public DroneProfile Profile => profile;
        public float CurrentThrottle => throttle01;
        public float RotorSpin => rotorSpin01;
        public float TargetAltitude => targetAltitude;
        public float Altitude => transform.position.y;
        public float Speed => body != null ? body.linearVelocity.magnitude : 0f;
        public float HorizontalSpeed => body != null ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude : 0f;
        public float VerticalSpeed => body != null ? body.linearVelocity.y : 0f;
        public Vector3 HomePosition => startPosition;
        public float HorizontalDistanceFromHome => Vector3.ProjectOnPlane(transform.position - startPosition, Vector3.up).magnitude;
        public float HeightFromHome => transform.position.y - startPosition.y;
        public bool GpsPositionHoldEnabled => gpsPositionHoldEnabled;
        public Vector3 GpsHoldPosition => gpsHoldPosition;
        public float LiftToWeightRatio => profile != null && body != null
            ? profile.maxLiftForce / Mathf.Max(0.01f, Mathf.Abs(Physics.gravity.y) * body.mass)
            : 0f;
        public bool AltitudeHoldEnabled => command.altitudeHoldEnabled;
        public bool StabilizeEnabled => command.stabilizeEnabled;
        public bool MotorsArmed => motorsArmed;
        public bool LandingActive => landingActive;
        public bool TakeoffSequenceActive => takeoffSequenceActive;
        public bool AssistedTakeoffControlLockActive => takeoffSequenceActive;
        public float PitchDegrees => NormalizeAngle(transform.eulerAngles.x);
        public float RollDegrees => NormalizeAngle(transform.eulerAngles.z);
        public float YawDegrees => transform.eulerAngles.y;
        public string FlightMode
        {
            get
            {
                if (!motorsArmed)
                {
                    return "IDLE";
                }

                if (landingActive)
                {
                    return "LANDING";
                }

                return command.altitudeHoldEnabled ? "ALT HOLD" : "MANUAL";
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (GetComponent<DroneWindReceiver>() == null)
            {
                gameObject.AddComponent<DroneWindReceiver>();
            }

            startPosition = transform.position;
            startRotation = transform.rotation;
            CaptureGpsHoldPosition();
            CaptureRotorBaseRotations();
            EnsurePidControllers();
            ApplyProfileToBody();
            ResetControlState();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && body != null)
            {
                ApplyProfileToBody();
                EnsurePidControllers();
            }
        }

        public void SetProfile(DroneProfile newProfile)
        {
            profile = newProfile;
            ApplyProfileToBody();
            EnsurePidControllers();
            ResetControlState();
        }

        public void SetRotorVisuals(Transform[] newRotorVisuals)
        {
            rotorVisuals = newRotorVisuals;
            CaptureRotorBaseRotations();
        }

        public void SetCommand(DroneCommand newCommand)
        {
            if (takeoffSequenceActive)
            {
                newCommand.pitch = 0f;
                newCommand.roll = 0f;
                newCommand.yaw = 0f;
                newCommand.vertical = 0f;
                newCommand.resetRequested = false;
            }

            command = newCommand;
        }

        public void RequestTakeoff()
        {
            if (profile == null)
            {
                return;
            }

            if (motorsArmed && !landingActive)
            {
                return;
            }

            if (motorsArmed && landingActive)
            {
                landingActive = false;
                takeoffSequenceActive = false;
                targetAltitude = Mathf.Max(targetAltitude, transform.position.y + assistedTakeoffHeight);
                altitudePid?.Reset();
                SetMotorPhysicsActive(true);
                return;
            }

            motorsArmed = true;
            landingActive = false;
            takeoffSequenceActive = true;
            takeoffStartHeight = transform.position.y;
            CaptureGpsHoldPosition();
            throttle01 = 0f;
            ClearPilotAxes();
            smoothedPitch = 0f;
            smoothedRoll = 0f;
            smoothedYaw = 0f;
            smoothedVertical = 0f;
            targetAltitude = Mathf.Max(targetAltitude, transform.position.y + assistedTakeoffHeight);
            altitudePid?.Reset();
            SetMotorPhysicsActive(false);
        }

        public void RequestLanding()
        {
            if (!motorsArmed)
            {
                return;
            }

            landingActive = true;
            takeoffSequenceActive = false;
            targetAltitude = Mathf.Min(targetAltitude, transform.position.y);
            altitudePid?.Reset();
            if (body != null && body.isKinematic)
            {
                SetMotorPhysicsActive(true);
            }
        }

        public void ResetMotion(bool resetPose)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body.isKinematic)
            {
                body.isKinematic = false;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            if (resetPose)
            {
                transform.SetPositionAndRotation(startPosition, startRotation);
            }

            ResetControlState();
        }

        private void Update()
        {
            if (profile == null)
            {
                return;
            }

            SpinRotors(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (profile == null || body == null)
            {
                return;
            }

            if (command.resetRequested)
            {
                ResetDrone();
            }

            ApplyProfileToBody();
            if (!motorsArmed)
            {
                throttle01 = 0f;
                targetAltitude = transform.position.y;
                return;
            }

            if (takeoffSequenceActive && body.isKinematic)
            {
                throttle01 = 0f;
                targetAltitude = takeoffStartHeight + assistedTakeoffHeight;
                if (rotorSpin01 >= takeoffLiftStartRotorSpin * 0.96f)
                {
                    SetMotorPhysicsActive(true);
                    altitudePid?.Reset();
                }

                return;
            }

            UpdateSmoothedCommand(Time.fixedDeltaTime);
            ApplyLift(Time.fixedDeltaTime);
            ApplyAttitudeControl(Time.fixedDeltaTime);
            ApplyHoverBrake();
            ApplyGpsPositionHold();
            UpdateTakeoffSequence();
            TryCompleteLanding();
        }

        private void ApplyLift(float deltaTime)
        {
            float hoverForce = Mathf.Abs(Physics.gravity.y) * body.mass;
            float liftForce;
            float verticalCommand = smoothedVertical * profile.verticalSensitivity;

            if (landingActive)
            {
                targetAltitude = Mathf.MoveTowards(targetAltitude, startPosition.y, landingDescentSpeed * deltaTime);
                float altitudeCorrection = altitudePid.Update(targetAltitude - Altitude, deltaTime);
                liftForce = Mathf.Clamp(hoverForce + altitudeCorrection, 0f, hoverForce);
                throttle01 = Mathf.Clamp01(liftForce / profile.maxLiftForce);
            }
            else if (command.altitudeHoldEnabled)
            {
                if (Mathf.Abs(verticalCommand) > 0.05f)
                {
                    targetAltitude += verticalCommand * profile.altitudeChangeSpeed * deltaTime;
                    targetAltitude = Mathf.Max(0.2f, targetAltitude);
                }

                float altitudeCorrection = altitudePid.Update(targetAltitude - Altitude, deltaTime);
                liftForce = hoverForce + altitudeCorrection;
                throttle01 = Mathf.Clamp01(liftForce / profile.maxLiftForce);
            }
            else
            {
                throttle01 += verticalCommand * profile.throttleResponse * deltaTime;
                throttle01 = Mathf.Clamp01(throttle01);
                liftForce = throttle01 * profile.maxLiftForce;
                targetAltitude = Altitude;
            }

            liftForce = Mathf.Clamp(liftForce, 0f, profile.maxLiftForce);
            body.AddForce(transform.up * liftForce, ForceMode.Force);
        }

        private void ApplyAttitudeControl(float deltaTime)
        {
            Vector3 localAngularVelocity = transform.InverseTransformDirection(body.angularVelocity);
            float pitchTorque;
            float rollTorque;

            if (command.stabilizeEnabled)
            {
                float targetPitch = smoothedPitch * profile.cyclicSensitivity * profile.maxTiltAngle;
                float targetRoll = -smoothedRoll * profile.cyclicSensitivity * profile.maxTiltAngle;
                pitchTorque = pitchPid.Update(targetPitch - PitchDegrees, deltaTime) * profile.attitudeTorqueScale
                    - localAngularVelocity.x * profile.angularRateDamping;
                rollTorque = rollPid.Update(targetRoll - RollDegrees, deltaTime) * profile.attitudeTorqueScale
                    - localAngularVelocity.z * profile.angularRateDamping;
            }
            else
            {
                pitchTorque = smoothedPitch * profile.cyclicSensitivity * profile.pitchTorque;
                rollTorque = -smoothedRoll * profile.cyclicSensitivity * profile.rollTorque;
            }

            float yawTorque = smoothedYaw * profile.yawSensitivity * profile.yawTorque
                - localAngularVelocity.y * profile.angularRateDamping * 0.35f;

            body.AddRelativeTorque(new Vector3(pitchTorque, yawTorque, rollTorque), ForceMode.Force);
        }

        private void ApplyHoverBrake()
        {
            if (!command.stabilizeEnabled)
            {
                return;
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            if (horizontalVelocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            bool hasTranslationalInput = Mathf.Abs(smoothedPitch) > 0.05f || Mathf.Abs(smoothedRoll) > 0.05f;
            if (gpsPositionHoldEnabled && !hasTranslationalInput)
            {
                return;
            }

            float brakeAcceleration = profile.hoverBrakeAcceleration > 0f ? profile.hoverBrakeAcceleration : 2.8f;
            float maxBrakeForce = profile.maxHoverBrakeForce > 0f ? profile.maxHoverBrakeForce : 9f;
            float brakeScale = hasTranslationalInput ? Mathf.Clamp01(profile.activeInputBrakeScale) : 1f;
            Vector3 desiredAcceleration = -horizontalVelocity * brakeAcceleration * brakeScale;
            Vector3 brakeForce = Vector3.ClampMagnitude(
                desiredAcceleration * body.mass,
                maxBrakeForce);

            body.AddForce(brakeForce, ForceMode.Force);
        }

        private void ApplyGpsPositionHold()
        {
            if (!gpsPositionHoldEnabled || !command.stabilizeEnabled)
            {
                CaptureGpsHoldPosition();
                return;
            }

            bool hasTranslationalInput = Mathf.Abs(smoothedPitch) > 0.05f || Mathf.Abs(smoothedRoll) > 0.05f;
            if (hasTranslationalInput)
            {
                CaptureGpsHoldPosition();
                return;
            }

            Vector3 positionError = Vector3.ProjectOnPlane(gpsHoldPosition - transform.position, Vector3.up);
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 desiredAcceleration = positionError * gpsPositionGain - horizontalVelocity * gpsVelocityGain;
            desiredAcceleration = Vector3.ClampMagnitude(desiredAcceleration, gpsMaximumAcceleration);
            body.AddForce(desiredAcceleration * body.mass, ForceMode.Force);
        }

        public void SetGpsPositionHold(bool enabled)
        {
            gpsPositionHoldEnabled = enabled;
            CaptureGpsHoldPosition();
        }

        private void CaptureGpsHoldPosition()
        {
            gpsHoldPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        }

        private void UpdateSmoothedCommand(float deltaTime)
        {
            float responseSpeed = profile != null ? Mathf.Max(0.1f, profile.inputResponseSpeed) : 8f;
            float maxDelta = responseSpeed * deltaTime;
            bool lockPilotAxes = takeoffSequenceActive;
            float targetPitch = lockPilotAxes ? 0f : command.pitch;
            float targetRoll = lockPilotAxes ? 0f : command.roll;
            float targetYaw = lockPilotAxes ? 0f : command.yaw;
            float targetVertical = lockPilotAxes ? 0f : command.vertical;

            smoothedPitch = Mathf.MoveTowards(smoothedPitch, targetPitch, maxDelta);
            smoothedRoll = Mathf.MoveTowards(smoothedRoll, targetRoll, maxDelta);
            smoothedYaw = Mathf.MoveTowards(smoothedYaw, targetYaw, maxDelta);
            smoothedVertical = Mathf.MoveTowards(smoothedVertical, targetVertical, maxDelta);
        }

        private void SpinRotors(float deltaTime)
        {
            if (rotorVisuals == null)
            {
                return;
            }

            EnsureRotorAnimationState();
            float desiredSpin01 = GetDesiredRotorSpin01();
            float smoothTime = GetRotorSmoothTime(desiredSpin01);
            float maxSpinChangeSpeed = desiredSpin01 > rotorSpin01
                ? GetCurrentRotorSpinupSpeed()
                : Mathf.Max(0.1f, profile.rotorGroundSpinupSpeed);
            rotorSpin01 = Mathf.SmoothDamp(rotorSpin01, desiredSpin01, ref rotorSpinVelocity, smoothTime, maxSpinChangeSpeed, deltaTime);
            rotorSpin01 = Mathf.Clamp01(rotorSpin01);

            float spinSpeed = profile.rotorVisualSpeed * rotorSpin01;
            float degrees = spinSpeed * deltaTime;
            Vector3 axis = profile.rotorVisualAxis.sqrMagnitude > 0.0001f
                ? profile.rotorVisualAxis.normalized
                : Vector3.up;
            for (int i = 0; i < rotorVisuals.Length; i++)
            {
                if (rotorVisuals[i] == null)
                {
                    continue;
                }

                float direction = i % 2 == 0 ? 1f : -1f;
                rotorAngles[i] = Mathf.Repeat(rotorAngles[i] + degrees * direction, 360f);
                float displayAngle = GetDisplayRotorAngle(rotorAngles[i], i);
                rotorVisuals[i].localRotation = rotorBaseRotations[i] * Quaternion.AngleAxis(displayAngle, axis);
            }
        }

        private float GetDisplayRotorAngle(float angle, int rotorIndex)
        {
            if (profile.rotorStrobeSampleCount <= 1 || rotorSpin01 < profile.rotorStrobeStartRatio)
            {
                return angle;
            }

            float step = 360f / Mathf.Max(2, profile.rotorStrobeSampleCount);
            float snapped = Mathf.Round(angle / step) * step;
            float wobble = Mathf.Sin(Time.time * 37f + rotorIndex * 1.37f) * profile.rotorStrobeWobbleDegrees;
            return snapped + wobble;
        }

        private void CaptureRotorBaseRotations()
        {
            if (rotorVisuals == null)
            {
                rotorBaseRotations = null;
                rotorAngles = null;
                return;
            }

            rotorBaseRotations = new Quaternion[rotorVisuals.Length];
            rotorAngles = new float[rotorVisuals.Length];
            for (int i = 0; i < rotorVisuals.Length; i++)
            {
                rotorBaseRotations[i] = rotorVisuals[i] != null ? rotorVisuals[i].localRotation : Quaternion.identity;
            }
        }

        private void EnsureRotorAnimationState()
        {
            if (rotorBaseRotations == null || rotorAngles == null || rotorBaseRotations.Length != rotorVisuals.Length || rotorAngles.Length != rotorVisuals.Length)
            {
                CaptureRotorBaseRotations();
            }
        }

        private float GetDesiredRotorSpin01()
        {
            if (!motorsArmed)
            {
                return 0f;
            }

            if (takeoffSequenceActive)
            {
                if (body != null && body.isKinematic)
                {
                    return Mathf.Clamp01(takeoffLiftStartRotorSpin);
                }

                float climbProgress = Mathf.InverseLerp(
                    takeoffStartHeight,
                    takeoffStartHeight + takeoffFullRotorHeight,
                    Altitude);
                return Mathf.Lerp(takeoffLiftStartRotorSpin, 1f, Mathf.SmoothStep(0f, 1f, climbProgress));
            }

            bool takingOffOrFlying =
                smoothedVertical > 0.05f ||
                Altitude > startPosition.y + 0.12f ||
                VerticalSpeed > 0.1f;

            if (takingOffOrFlying)
            {
                return Mathf.Clamp01(Mathf.Lerp(profile.rotorFlightSpinRatio, 1f, throttle01));
            }

            return Mathf.Clamp01(profile.rotorGroundSpinRatio);
        }

        private float GetCurrentRotorSpinupSpeed()
        {
            bool takingOffOrFlying =
                smoothedVertical > 0.05f ||
                Altitude > startPosition.y + 0.12f ||
                VerticalSpeed > 0.1f;

            return Mathf.Max(0.1f, takingOffOrFlying ? profile.rotorFlightSpinupSpeed : profile.rotorGroundSpinupSpeed);
        }

        private float GetRotorSmoothTime(float desiredSpin01)
        {
            if (!motorsArmed && desiredSpin01 <= 0.001f)
            {
                return rotorShutdownSmoothTime;
            }

            if (desiredSpin01 > rotorSpin01 && rotorSpin01 < 0.35f)
            {
                return rotorStartupSmoothTime;
            }

            return rotorRuntimeSmoothTime;
        }

        private void UpdateTakeoffSequence()
        {
            if (!takeoffSequenceActive)
            {
                return;
            }

            if (Altitude >= takeoffStartHeight + takeoffFullRotorHeight)
            {
                takeoffSequenceActive = false;
            }
        }

        private void ResetDrone()
        {
            ResetMotion(true);
        }

        private void TryCompleteLanding()
        {
            if (!landingActive)
            {
                return;
            }

            bool nearStartHeight = Altitude <= startPosition.y + landingMotorCutoffHeight;
            bool slowEnough = Mathf.Abs(VerticalSpeed) <= landingMotorCutoffVerticalSpeed;
            if (!nearStartHeight || !slowEnough)
            {
                return;
            }

            landingActive = false;
            takeoffSequenceActive = false;
            motorsArmed = false;
            throttle01 = 0f;
            targetAltitude = transform.position.y;
            smoothedVertical = 0f;
            altitudePid?.Reset();
            SetMotorPhysicsActive(false);
        }

        private void ResetControlState()
        {
            motorsArmed = false;
            landingActive = false;
            takeoffSequenceActive = false;
            takeoffStartHeight = transform.position.y;
            throttle01 = 0f;
            targetAltitude = transform.position.y;
            ClearPilotAxes();
            smoothedPitch = 0f;
            smoothedRoll = 0f;
            smoothedYaw = 0f;
            smoothedVertical = 0f;
            CaptureGpsHoldPosition();
            rotorSpin01 = 0f;
            rotorSpinVelocity = 0f;
            if (rotorAngles != null)
            {
                for (int i = 0; i < rotorAngles.Length; i++)
                {
                    rotorAngles[i] = 0f;
                    if (rotorVisuals != null && i < rotorVisuals.Length && rotorVisuals[i] != null && rotorBaseRotations != null && i < rotorBaseRotations.Length)
                    {
                        rotorVisuals[i].localRotation = rotorBaseRotations[i];
                    }
                }
            }
            altitudePid?.Reset();
            pitchPid?.Reset();
            rollPid?.Reset();
            SetMotorPhysicsActive(false);
        }

        private void ClearPilotAxes()
        {
            command.pitch = 0f;
            command.roll = 0f;
            command.yaw = 0f;
            command.vertical = 0f;
            command.resetRequested = false;
        }

        private void SetMotorPhysicsActive(bool active)
        {
            if (body == null)
            {
                return;
            }

            if (!active)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.useGravity = false;
                body.isKinematic = true;
                return;
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();
        }

        private void ApplyProfileToBody()
        {
            if (profile == null || body == null)
            {
                return;
            }

            body.mass = profile.mass;
            body.linearDamping = profile.linearDamping;
            body.angularDamping = profile.angularDamping;
            body.maxAngularVelocity = profile.maxAngularVelocity;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (centerOfMass != null)
            {
                body.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
            }
        }

        private void EnsurePidControllers()
        {
            if (profile == null)
            {
                return;
            }

            altitudePid ??= new PidController(profile.altitudePid);
            pitchPid ??= new PidController(profile.pitchPid);
            rollPid ??= new PidController(profile.rollPid);
            altitudePid.Gains = profile.altitudePid;
            pitchPid.Gains = profile.pitchPid;
            rollPid.Gains = profile.rollPid;
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }
            return angle;
        }
    }
}
