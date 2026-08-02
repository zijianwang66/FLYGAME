using UnityEngine;

namespace DroneMicroClass
{
    public sealed class FlightRuntimeSettings : MonoBehaviour
    {
        [SerializeField, Min(30)] private int targetFrameRate = 60;
        [SerializeField, Min(0.005f)] private float fixedTimestep = 1f / 60f;
        [SerializeField, Min(0.02f)] private float maximumAllowedTimestep = 0.08f;

        private void Awake()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = targetFrameRate;
            Time.fixedDeltaTime = fixedTimestep;
            Time.maximumDeltaTime = maximumAllowedTimestep;
        }
    }
}
