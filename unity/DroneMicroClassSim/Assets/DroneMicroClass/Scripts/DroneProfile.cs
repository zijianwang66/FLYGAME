using UnityEngine;

namespace DroneMicroClass
{
    [CreateAssetMenu(menuName = "Drone MicroClass/Drone Profile", fileName = "DroneProfile")]
    public sealed class DroneProfile : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Training Quad";
        public string modelCode = "QUAD";
        [TextArea(2, 4)] public string handlingNotes = "Balanced trainer with forgiving assisted controls.";

        [Header("Body")]
        [Min(0.1f)] public float mass = 1.2f;
        [Min(0f)] public float linearDamping = 0.35f;
        [Min(0f)] public float angularDamping = 1.8f;

        [Header("Lift and Torque")]
        [Min(1f)] public float maxLiftForce = 34f;
        [Min(0f)] public float pitchTorque = 3.4f;
        [Min(0f)] public float rollTorque = 3.4f;
        [Min(0f)] public float yawTorque = 1.4f;
        [Min(1f)] public float maxTiltAngle = 18f;
        [Min(0.1f)] public float throttleResponse = 0.65f;
        [Min(0f)] public float angularRateDamping = 0.35f;
        [Min(0f)] public float hoverBrakeAcceleration = 2.8f;
        [Min(0f)] public float maxHoverBrakeForce = 9f;

        [Header("Control Feel")]
        [Tooltip("Pitch/roll stick multiplier before converting input into target tilt.")]
        [Range(0.25f, 2f)] public float cyclicSensitivity = 1f;
        [Tooltip("Yaw stick multiplier before applying yaw torque.")]
        [Range(0.25f, 2f)] public float yawSensitivity = 1f;
        [Tooltip("Vertical stick multiplier before changing throttle or altitude target.")]
        [Range(0.25f, 2f)] public float verticalSensitivity = 1f;
        [Tooltip("How quickly keyboard/gamepad input reaches the flight controller. Low values feel heavy.")]
        [Min(0.1f)] public float inputResponseSpeed = 8f;
        [Tooltip("Multiplier applied to assisted pitch/roll torque after PID calculation.")]
        [Range(0.25f, 2.5f)] public float attitudeTorqueScale = 1f;
        [Tooltip("How much horizontal braking remains while pitch/roll input is held.")]
        [Range(0f, 1f)] public float activeInputBrakeScale = 0.15f;
        [Tooltip("Caps angular speed so large camera drones can feel deliberately sluggish.")]
        [Min(0.1f)] public float maxAngularVelocity = 7f;

        [Header("Assist")]
        [Min(0.1f)] public float altitudeChangeSpeed = 2.2f;
        public PidGains altitudePid = new PidGains(8f, 0.25f, 5f, 22f);
        public PidGains pitchPid = new PidGains(0.18f, 0f, 0.035f, 3.5f);
        public PidGains rollPid = new PidGains(0.18f, 0f, 0.035f, 3.5f);

        [Header("Visuals")]
        [Min(0f)] public float rotorVisualSpeed = 1800f;
        public Vector3 rotorVisualAxis = Vector3.up;
        [Range(0f, 1f)] public float rotorGroundSpinRatio = 0.45f;
        [Range(0f, 1f)] public float rotorFlightSpinRatio = 0.9f;
        [Min(0.1f)] public float rotorGroundSpinupSpeed = 1.8f;
        [Min(0.1f)] public float rotorFlightSpinupSpeed = 9f;
        [Min(0)] public int rotorStrobeSampleCount = 8;
        [Range(0f, 1f)] public float rotorStrobeStartRatio = 0.7f;
        [Range(0f, 12f)] public float rotorStrobeWobbleDegrees = 3f;
    }
}
