using UnityEngine;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class DroneHud : MonoBehaviour
    {
        [SerializeField] private SimpleFlightController target;
        [SerializeField] private DroneFleetManager fleet;
        [SerializeField] private DroneCameraRig cameraRig;
        [SerializeField] private Text titleText;
        [SerializeField] private Text altitudeBadgeText;
        [SerializeField] private Text altitudeText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text attitudeText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text throttleText;
        [SerializeField] private Text parameterText;
        [SerializeField] private Text cameraModeText;
        [SerializeField] private Text aircraftSelectText;
        [SerializeField] private RawImage noseCameraView;
        [SerializeField] private Image throttleFill;
        [SerializeField] private DroneMiniMap miniMap;

        private const float TextUpdateInterval = 0.08f;
        private float nextTextUpdateTime;

        public void Bind(SimpleFlightController newTarget)
        {
            target = newTarget;
            if (miniMap != null)
            {
                miniMap.Bind(newTarget);
            }
        }

        public void Bind(SimpleFlightController newTarget, DroneFleetManager newFleet, DroneCameraRig newCameraRig, RenderTexture noseCameraTexture)
        {
            target = newTarget;
            fleet = newFleet;
            cameraRig = newCameraRig;
            if (noseCameraView != null)
            {
                noseCameraView.texture = noseCameraTexture;
            }

            if (miniMap != null)
            {
                miniMap.Bind(newTarget);
            }
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            if (throttleFill != null)
            {
                throttleFill.fillAmount = target.CurrentThrottle;
            }

            if (Time.unscaledTime < nextTextUpdateTime)
            {
                return;
            }

            nextTextUpdateTime = Time.unscaledTime + TextUpdateInterval;

            DroneProfile profile = target.Profile;
            if (titleText != null)
            {
                titleText.text = profile != null ? profile.displayName : "Training Quad";
            }

            if (altitudeBadgeText != null)
            {
                altitudeBadgeText.text = $"ALT {target.Altitude:0.0} m";
            }

            if (altitudeText != null)
            {
                altitudeText.text = $"Altitude: {target.Altitude:0.00} m   Target: {target.TargetAltitude:0.00} m";
            }

            if (speedText != null)
            {
                speedText.text = $"Speed: {target.Speed:0.00} m/s   Horizontal: {target.HorizontalSpeed:0.00}   V/S: {target.VerticalSpeed:0.00}";
            }

            if (attitudeText != null)
            {
                attitudeText.text = $"Pitch {target.PitchDegrees:0}   Roll {target.RollDegrees:0}   Heading {target.YawDegrees:000}";
            }

            if (modeText != null)
            {
                modeText.text = $"Height Ctrl {(target.AltitudeHoldEnabled ? "ON" : "OFF")}\nBalance Ctrl {(target.StabilizeEnabled ? "ON" : "OFF")}\nControl Force {target.CurrentThrottle * 100f:0}%";
            }

            if (throttleText != null)
            {
                throttleText.text = $"THR {target.CurrentThrottle * 100f:0}%";
            }

            if (parameterText != null && profile != null)
            {
                parameterText.text =
                    $"{profile.modelCode}\n" +
                    $"Mass {profile.mass:0.00} kg\n" +
                    $"Max Lift {profile.maxLiftForce:0.0} N\n" +
                    $"Lift/Weight {target.LiftToWeightRatio:0.00}x\n" +
                    $"Torque P/R/Y {profile.pitchTorque:0.0}/{profile.rollTorque:0.0}/{profile.yawTorque:0.0}\n" +
                    $"Damping {profile.linearDamping:0.00}/{profile.angularDamping:0.00}\n" +
                    $"Tilt {profile.maxTiltAngle:0} deg  Response {profile.inputResponseSpeed:0.0}\n" +
                    $"Sense C/Y/V {profile.cyclicSensitivity:0.00}/{profile.yawSensitivity:0.00}/{profile.verticalSensitivity:0.00}\n" +
                    $"Brake {profile.hoverBrakeAcceleration:0.0} x{profile.activeInputBrakeScale:0.00}\n" +
                    profile.handlingNotes;
            }

            if (cameraModeText != null)
            {
                cameraModeText.text = cameraRig != null ? $"Camera: {cameraRig.ModeName}" : "Camera: FOLLOW";
            }

            if (aircraftSelectText != null)
            {
                aircraftSelectText.text = fleet != null
                    ? $"Aircraft {fleet.CurrentIndex + 1}/{fleet.VariantCount}: {fleet.CurrentVariantName}"
                    : "Aircraft: Training Drone";
            }
        }
    }
}
