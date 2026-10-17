using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public static class TrainingSunLighting
    {
        private const float DefaultSunIntensity = 1.15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneLoadedHandler()
        {
            SceneManager.sceneLoaded -= ApplySelectedSun;
            SceneManager.sceneLoaded += ApplySelectedSun;
            ApplySelectedSun(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void ApplySelectedSun(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || scene.name == "Login" || scene.name == "MainMenu" || scene.name == "TrainingSceneSelection" || scene.name == "Loading")
            {
                return;
            }

            Light sun = FindDirectionalSun();
            if (sun == null)
            {
                GameObject sunObject = new GameObject("Training Sun");
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            float altitude = SceneLoadRequest.SelectedSunAltitudeDegrees;
            float azimuth = SceneLoadRequest.SelectedSunAzimuthDegrees;
            sun.transform.rotation = Quaternion.Euler(altitude, azimuth, 0f);
            sun.intensity = SceneLoadRequest.SelectedSunAltitudePreset == SunAltitudePreset.Afternoon
                ? DefaultSunIntensity * 1.08f
                : DefaultSunIntensity;
            sun.color = SceneLoadRequest.SelectedSunAltitudePreset == SunAltitudePreset.Afternoon
                ? new Color(1f, 0.95f, 0.86f, 1f)
                : new Color(1f, 0.98f, 0.9f, 1f);

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = SceneLoadRequest.SelectedSunAltitudePreset == SunAltitudePreset.Afternoon
                ? new Color(0.72f, 0.82f, 0.94f, 1f)
                : new Color(0.78f, 0.88f, 1f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.62f, 0.68f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.34f, 0.32f, 1f);
        }

        private static Light FindDirectionalSun()
        {
            if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional)
            {
                return RenderSettings.sun;
            }

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            return null;
        }
    }
}
