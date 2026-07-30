using UnityEngine;

namespace DroneMicroClass
{
    public sealed class RotorSpinner : MonoBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 960f;
        [SerializeField] private int direction = 1;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private int strobeSampleCount = 8;
        [SerializeField] private float strobeStartSpeed = 1800f;
        [SerializeField] private float strobeWobbleDegrees = 3f;

        private Quaternion baseLocalRotation;
        private float angle;

        public void Configure(Vector3 axis, float speed, int spinDirection, bool unscaledTime = false, int sampleCount = 8, float startSpeed = 1800f, float wobbleDegrees = 3f)
        {
            localAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
            degreesPerSecond = Mathf.Max(0f, speed);
            direction = spinDirection < 0 ? -1 : 1;
            useUnscaledTime = unscaledTime;
            strobeSampleCount = Mathf.Max(0, sampleCount);
            strobeStartSpeed = Mathf.Max(0f, startSpeed);
            strobeWobbleDegrees = Mathf.Max(0f, wobbleDegrees);
            CaptureBaseRotation();
        }

        private void OnEnable()
        {
            CaptureBaseRotation();
        }

        private void CaptureBaseRotation()
        {
            baseLocalRotation = transform.localRotation;
            angle = 0f;
        }

        private void Update()
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            angle = Mathf.Repeat(angle + degreesPerSecond * direction * deltaTime, 360f);
            transform.localRotation = baseLocalRotation * Quaternion.AngleAxis(GetDisplayAngle(), localAxis);
        }

        private float GetDisplayAngle()
        {
            if (strobeSampleCount <= 1 || degreesPerSecond < strobeStartSpeed)
            {
                return angle;
            }

            float step = 360f / Mathf.Max(2, strobeSampleCount);
            float snapped = Mathf.Round(angle / step) * step;
            float wobble = Mathf.Sin(Time.time * 37f + transform.GetSiblingIndex() * 1.37f) * strobeWobbleDegrees;
            return snapped + wobble;
        }
    }
}
