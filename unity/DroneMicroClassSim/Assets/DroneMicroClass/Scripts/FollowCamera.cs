using UnityEngine;

namespace DroneMicroClass
{
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.8f, -10f);
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float rotationSharpness = 10f;

        private Vector3 velocity;

        public void Bind(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                positionSmoothTime);

            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
            }
        }
    }
}
