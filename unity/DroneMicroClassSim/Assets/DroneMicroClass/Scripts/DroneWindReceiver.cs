using UnityEngine;

namespace DroneMicroClass
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DroneWindReceiver : MonoBehaviour
    {
        [SerializeField] private SimpleFlightController flightController;
        [SerializeField, Min(0.01f)] private float airDensity = 1.225f;
        [SerializeField, Min(0.01f)] private float dragCoefficient = 1.1f;
        [SerializeField, Min(0.01f)] private float horizontalReferenceArea = 0.12f;
        [SerializeField, Min(0.1f)] private float maximumWindAcceleration = 5f;

        private Rigidbody body;

        public Vector3 LastAppliedWindForce { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (flightController == null)
            {
                flightController = GetComponent<SimpleFlightController>();
            }
        }

        private void FixedUpdate()
        {
            WindSystem wind = WindSystem.Active;
            if (wind == null || body == null || body.isKinematic || flightController == null || !flightController.MotorsArmed)
            {
                LastAppliedWindForce = Vector3.zero;
                return;
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 relativeWind = wind.CurrentWindVelocity - horizontalVelocity;
            float relativeSpeed = relativeWind.magnitude;
            if (relativeSpeed < 0.01f)
            {
                LastAppliedWindForce = Vector3.zero;
                return;
            }

            float forceMagnitude = 0.5f
                * airDensity
                * dragCoefficient
                * horizontalReferenceArea
                * relativeSpeed
                * relativeSpeed;
            Vector3 windForce = relativeWind.normalized * forceMagnitude;
            LastAppliedWindForce = Vector3.ClampMagnitude(
                windForce,
                maximumWindAcceleration * body.mass);
            body.AddForce(LastAppliedWindForce, ForceMode.Force);
        }
    }
}
