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
        [SerializeField] private KeyCode ascendKey = KeyCode.Space;
        [SerializeField] private KeyCode descendKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode landingKey = KeyCode.L;
        [SerializeField] private KeyCode yawLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode yawRightKey = KeyCode.E;
        [SerializeField] private KeyCode toggleAltitudeHoldKey = KeyCode.H;
        [SerializeField] private KeyCode toggleStabilizeKey = KeyCode.T;
        [SerializeField] private KeyCode resetKey = KeyCode.Backspace;

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

            if (Input.GetKeyDown(toggleAltitudeHoldKey))
            {
                altitudeHold = !altitudeHold;
            }

            if (Input.GetKeyDown(toggleStabilizeKey))
            {
                stabilize = !stabilize;
            }

            if (Input.GetKeyDown(ascendKey))
            {
                flightController.RequestTakeoff();
            }

            if (Input.GetKeyDown(landingKey))
            {
                flightController.RequestLanding();
            }

            float roll = BoolAxis(rollRightKey, rollLeftKey);
            float pitch = BoolAxis(pitchForwardKey, pitchBackwardKey);
            float yaw = BoolAxis(yawRightKey, yawLeftKey);
            float vertical = flightController.MotorsArmed && !flightController.LandingActive
                ? BoolAxis(ascendKey, descendKey)
                : 0f;

            flightController.SetCommand(new DroneCommand
            {
                pitch = pitch,
                roll = roll,
                yaw = yaw,
                vertical = vertical,
                altitudeHoldEnabled = altitudeHold,
                stabilizeEnabled = stabilize,
                resetRequested = Input.GetKeyDown(resetKey),
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
    }
}
