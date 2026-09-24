using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public sealed class WindSystem : MonoBehaviour
    {
        public enum WindPreset
        {
            NoWind,
            Light,
            Moderate,
            Gusty
        }

        [SerializeField] private WindPreset preset = WindPreset.Light;
        [SerializeField, Range(0f, 359f)] private float windDirectionDegrees = 45f;
        [SerializeField, Min(0f)] private float averageWindSpeed = 2f;
        [SerializeField, Min(0f)] private float gustStrength = 0.5f;
        [SerializeField, Min(0f)] private float turbulenceFrequency = 0.08f;
        [SerializeField] private bool autoConfigureFromScene = true;
        [SerializeField] private bool fixedWindForAssessment;
        [SerializeField] private float noiseSeed = 137.5f;

        private float currentDirectionDegrees;
        private float currentWindSpeed;
        private Vector3 currentWindVelocity;

        public static WindSystem Active { get; private set; }

        public WindPreset Preset => preset;
        public string PresetDisplayName => GetPresetDisplayName(preset);
        public float WindDirectionDegrees => windDirectionDegrees;
        public float AverageWindSpeed => averageWindSpeed;
        public float GustStrength => fixedWindForAssessment ? 0f : gustStrength;
        public float TurbulenceFrequency => fixedWindForAssessment ? 0f : turbulenceFrequency;
        public float CurrentDirectionDegrees => currentDirectionDegrees;
        public float CurrentWindSpeed => currentWindSpeed;
        public Vector3 CurrentWindVelocity => currentWindVelocity;
        public bool IsFixedWind => fixedWindForAssessment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureWindSystemForActiveScene()
        {
            WindSystem system = Object.FindFirstObjectByType<WindSystem>();
            if (system == null)
            {
                GameObject systemObject = new GameObject("Wind System");
                system = systemObject.AddComponent<WindSystem>();
            }

            if (system.autoConfigureFromScene)
            {
                system.ConfigureForScene(SceneManager.GetActiveScene().name);
            }

            system.UpdateWind(0f);
        }

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            UpdateWind(Time.unscaledTime);
        }

        public void ApplyPreset(WindPreset newPreset)
        {
            preset = newPreset;
            switch (newPreset)
            {
                case WindPreset.NoWind:
                    windDirectionDegrees = 0f;
                    averageWindSpeed = 0f;
                    gustStrength = 0f;
                    turbulenceFrequency = 0f;
                    break;
                case WindPreset.Light:
                    windDirectionDegrees = 45f;
                    averageWindSpeed = 2f;
                    gustStrength = 0.5f;
                    turbulenceFrequency = 0.08f;
                    break;
                case WindPreset.Moderate:
                    windDirectionDegrees = 270f;
                    averageWindSpeed = 5f;
                    gustStrength = 1.5f;
                    turbulenceFrequency = 0.12f;
                    break;
                case WindPreset.Gusty:
                    windDirectionDegrees = 315f;
                    averageWindSpeed = 8f;
                    gustStrength = 3f;
                    turbulenceFrequency = 0.25f;
                    break;
            }
        }

        public void ConfigureForScene(string sceneName)
        {
            string normalizedName = string.IsNullOrEmpty(sceneName) ? string.Empty : sceneName.ToLowerInvariant();
            fixedWindForAssessment = normalizedName.Contains("exam")
                || normalizedName.Contains("assessment")
                || normalizedName.Contains("考核");

            bool isTrainingAssessment = normalizedName.Contains("figureeight")
                || normalizedName.Contains("trainingunity")
                || normalizedName.Contains("training");
            ApplyPreset(isTrainingAssessment ? WindPreset.NoWind : normalizedName.Contains("forest") ? WindPreset.Moderate : WindPreset.Light);
            noiseSeed = 100f + Mathf.Abs(sceneName != null ? sceneName.GetHashCode() % 10000 : 0) * 0.01f;
        }

        private void UpdateWind(float time)
        {
            if (fixedWindForAssessment || averageWindSpeed <= 0f)
            {
                currentDirectionDegrees = Mathf.Repeat(windDirectionDegrees, 360f);
                currentWindSpeed = Mathf.Max(0f, averageWindSpeed);
            }
            else
            {
                float frequency = Mathf.Max(0.01f, turbulenceFrequency);
                float speedNoise = SampleSignedNoise(noiseSeed, time * frequency);
                float directionNoise = SampleSignedNoise(noiseSeed + 43.7f, time * frequency * 0.67f);
                float directionVariation = Mathf.Clamp(2f + gustStrength * 4f, 2f, 18f);

                currentWindSpeed = Mathf.Max(0f, averageWindSpeed + speedNoise * gustStrength);
                currentDirectionDegrees = Mathf.Repeat(windDirectionDegrees + directionNoise * directionVariation, 360f);
            }

            currentWindVelocity = DirectionFromToVelocity(currentDirectionDegrees, currentWindSpeed);
        }

        private static float SampleSignedNoise(float seed, float time)
        {
            return Mathf.PerlinNoise(seed, time) * 2f - 1f;
        }

        private static Vector3 DirectionFromToVelocity(float directionFromDegrees, float speed)
        {
            float radians = directionFromDegrees * Mathf.Deg2Rad;
            Vector3 directionFrom = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            return -directionFrom * speed;
        }

        private static string GetPresetDisplayName(WindPreset value)
        {
            return value switch
            {
                WindPreset.NoWind => "无风",
                WindPreset.Light => "微风",
                WindPreset.Moderate => "中风",
                WindPreset.Gusty => "阵风",
                _ => "风况"
            };
        }
    }
}
