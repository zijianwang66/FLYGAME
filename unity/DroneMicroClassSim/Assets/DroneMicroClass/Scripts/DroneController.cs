using System.Collections.Generic;
using UnityEngine;

namespace DroneMicroClass
{
    public sealed class DroneController : MonoBehaviour
    {
        [SerializeField] private SimpleFlightController flightController;
        [SerializeField] private bool altitudeHold = true;
        [SerializeField] private bool stabilize = true;

        [Header("Keyboard")]
        [SerializeField] private KeyCode pitchForwardKey = KeyCode.W;
        [SerializeField] private KeyCode pitchBackwardKey = KeyCode.S;
        [SerializeField] private KeyCode rollRightKey = KeyCode.D;
        [SerializeField] private KeyCode rollLeftKey = KeyCode.A;
        [SerializeField] private KeyCode ascendKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode descendKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode landingKey = KeyCode.L;
        [SerializeField] private KeyCode yawLeftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode yawRightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode toggleAltitudeHoldKey = KeyCode.H;
        [SerializeField] private KeyCode toggleStabilizeKey = KeyCode.T;
        [SerializeField] private KeyCode resetKey = KeyCode.Backspace;
        [SerializeField, Min(0.1f)] private float keyboardArmTakeoffHoldSeconds = 2f;
        [SerializeField, Min(0.1f)] private float keyboardMotorLockHoldSeconds = 2f;

        [Header("Gamepad")]
        [SerializeField] private bool gamepadEnabled = true;
        [SerializeField] private string gamepadRollAxis = "Drone Left Stick X";
        [SerializeField] private string gamepadPitchAxis = "Drone Left Stick Y";
        [SerializeField] private string gamepadYawAxis = "Drone Right Stick X";
        [SerializeField] private string gamepadVerticalAxis = "Drone Right Stick Y";
        [SerializeField, Range(0f, 0.6f)] private float gamepadDeadZone = 0.18f;
        [SerializeField, Range(1f, 3f)] private float gamepadResponseCurve = 1.35f;
        [SerializeField, Range(0.1f, 1f)] private float stickComboThreshold = 0.72f;
        [SerializeField, Min(0.1f)] private float gamepadArmTakeoffHoldSeconds = 2f;
        [SerializeField, Min(0.1f)] private float gamepadMotorLockHoldSeconds = 2f;
        [SerializeField] private KeyCode gamepadLandingButton = KeyCode.JoystickButton1;
        [SerializeField] private KeyCode gamepadToggleAltitudeHoldButton = KeyCode.JoystickButton8;
        [SerializeField] private KeyCode gamepadToggleStabilizeButton = KeyCode.JoystickButton9;
        [SerializeField] private KeyCode gamepadResetModifierLeft = KeyCode.JoystickButton6;
        [SerializeField] private KeyCode gamepadResetModifierRight = KeyCode.JoystickButton7;

        private static readonly HashSet<string> UnavailableAxes = new HashSet<string>();
        private float keyboardArmTakeoffTimer;
        private bool keyboardArmTakeoffFired;
        private float keyboardMotorLockTimer;
        private bool keyboardMotorLockFired;
        private float gamepadArmTakeoffTimer;
        private bool gamepadArmTakeoffFired;
        private float gamepadMotorLockTimer;
        private bool gamepadMotorLockFired;

        private void Awake()
        {
            if (flightController == null)
            {
                flightController = GetComponent<SimpleFlightController>();
            }
        }

        private void Update()
        {
            if (flightController == null)
            {
                return;
            }

            if (Input.GetKeyDown(toggleAltitudeHoldKey) || GetGamepadButtonDown(gamepadToggleAltitudeHoldButton))
            {
                altitudeHold = !altitudeHold;
            }

            if (Input.GetKeyDown(toggleStabilizeKey) || GetGamepadButtonDown(gamepadToggleStabilizeButton))
            {
                stabilize = !stabilize;
            }

            if (Input.GetKeyDown(landingKey) || GetGamepadButtonDown(gamepadLandingButton))
            {
                flightController.RequestLanding();
            }

            float keyboardRoll = BoolAxis(rollRightKey, rollLeftKey);
            float keyboardPitch = BoolAxis(pitchForwardKey, pitchBackwardKey);
            float keyboardYaw = BoolAxis(yawRightKey, yawLeftKey);
            float keyboardVertical = BoolAxis(ascendKey, descendKey);

            float gamepadRoll = ReadGamepadAxis(gamepadRollAxis);
            float gamepadPitch = ReadGamepadAxis(gamepadPitchAxis);
            float gamepadYaw = ReadGamepadAxis(gamepadYawAxis);
            float gamepadVertical = ReadGamepadAxis(gamepadVerticalAxis);

            UpdateKeyboardStickCombos(keyboardRoll, keyboardPitch, keyboardYaw, keyboardVertical);
            UpdateGamepadStickCombos(gamepadRoll, gamepadPitch, gamepadYaw, gamepadVertical);

            float roll = SelectDominantAxis(keyboardRoll, gamepadRoll);
            float pitch = SelectDominantAxis(keyboardPitch, gamepadPitch);
            float yaw = SelectDominantAxis(keyboardYaw, gamepadYaw);
            float vertical = flightController.MotorsArmed && !flightController.LandingActive
                ? SelectDominantAxis(keyboardVertical, gamepadVertical)
                : 0f;

            flightController.SetCommand(new DroneCommand
            {
                pitch = pitch,
                roll = roll,
                yaw = yaw,
                vertical = vertical,
                altitudeHoldEnabled = altitudeHold,
                stabilizeEnabled = stabilize,
                resetRequested = Input.GetKeyDown(resetKey) || GetGamepadResetRequested(),
            });
        }

        private static float BoolAxis(KeyCode positive, KeyCode negative)
        {
            float value = 0f;
            if (Input.GetKey(positive))
            {
                value += 1f;
            }

            if (Input.GetKey(negative))
            {
                value -= 1f;
            }

            return value;
        }

        private void UpdateKeyboardStickCombos(float roll, float pitch, float yaw, float vertical)
        {
            bool innerDownHeld = IsStickComboHeld(roll, pitch, yaw, vertical, 1f, -1f, -1f, -1f);
            if (UpdateHoldTimer(innerDownHeld, keyboardArmTakeoffHoldSeconds, ref keyboardArmTakeoffTimer, ref keyboardArmTakeoffFired))
            {
                flightController.RequestTakeoff();
            }

            bool outerDownHeld = IsStickComboHeld(roll, pitch, yaw, vertical, -1f, -1f, 1f, -1f);
            if (UpdateHoldTimer(outerDownHeld, keyboardMotorLockHoldSeconds, ref keyboardMotorLockTimer, ref keyboardMotorLockFired))
            {
                flightController.RequestMotorLock();
            }
        }

        private void UpdateGamepadStickCombos(float roll, float pitch, float yaw, float vertical)
        {
            if (!gamepadEnabled)
            {
                gamepadArmTakeoffTimer = 0f;
                gamepadArmTakeoffFired = false;
                gamepadMotorLockTimer = 0f;
                gamepadMotorLockFired = false;
                return;
            }

            bool innerDownHeld = IsStickComboHeld(roll, pitch, yaw, vertical, 1f, -1f, -1f, -1f);
            if (UpdateHoldTimer(innerDownHeld, gamepadArmTakeoffHoldSeconds, ref gamepadArmTakeoffTimer, ref gamepadArmTakeoffFired))
            {
                flightController.RequestTakeoff();
            }

            bool outerDownHeld = IsStickComboHeld(roll, pitch, yaw, vertical, -1f, -1f, 1f, -1f);
            if (UpdateHoldTimer(outerDownHeld, gamepadMotorLockHoldSeconds, ref gamepadMotorLockTimer, ref gamepadMotorLockFired))
            {
                flightController.RequestMotorLock();
            }
        }

        private bool IsStickComboHeld(float roll, float pitch, float yaw, float vertical, float targetRoll, float targetPitch, float targetYaw, float targetVertical)
        {
            return AxisMatches(roll, targetRoll)
                && AxisMatches(pitch, targetPitch)
                && AxisMatches(yaw, targetYaw)
                && AxisMatches(vertical, targetVertical);
        }

        private bool AxisMatches(float value, float targetDirection)
        {
            return targetDirection >= 0f
                ? value >= stickComboThreshold
                : value <= -stickComboThreshold;
        }

        private static bool UpdateHoldTimer(bool comboHeld, float holdSeconds, ref float timer, ref bool fired)
        {
            if (!comboHeld)
            {
                timer = 0f;
                fired = false;
                return false;
            }

            if (fired)
            {
                return false;
            }

            timer += Time.unscaledDeltaTime;
            if (timer < holdSeconds)
            {
                return false;
            }

            fired = true;
            return true;
        }

        private bool GetGamepadResetRequested()
        {
            if (!gamepadEnabled)
            {
                return false;
            }

            bool leftHeld = Input.GetKey(gamepadResetModifierLeft);
            bool rightHeld = Input.GetKey(gamepadResetModifierRight);
            return leftHeld && Input.GetKeyDown(gamepadResetModifierRight)
                || rightHeld && Input.GetKeyDown(gamepadResetModifierLeft);
        }

        private bool GetGamepadButtonDown(KeyCode button)
        {
            return gamepadEnabled && Input.GetKeyDown(button);
        }

        private float ReadGamepadAxis(string axisName)
        {
            if (!gamepadEnabled || string.IsNullOrWhiteSpace(axisName) || UnavailableAxes.Contains(axisName))
            {
                return 0f;
            }

            float value;
            try
            {
                value = Input.GetAxisRaw(axisName);
            }
            catch (System.ArgumentException)
            {
                UnavailableAxes.Add(axisName);
                return 0f;
            }

            return ApplyGamepadResponseCurve(value);
        }

        private float ApplyGamepadResponseCurve(float value)
        {
            float magnitude = Mathf.Abs(value);
            if (magnitude <= gamepadDeadZone)
            {
                return 0f;
            }

            float normalized = Mathf.InverseLerp(gamepadDeadZone, 1f, Mathf.Min(1f, magnitude));
            float curved = Mathf.Pow(normalized, gamepadResponseCurve);
            return Mathf.Sign(value) * curved;
        }

        private static float SelectDominantAxis(float keyboardValue, float gamepadValue)
        {
            return Mathf.Abs(gamepadValue) > Mathf.Abs(keyboardValue)
                ? gamepadValue
                : keyboardValue;
        }
    }
}
