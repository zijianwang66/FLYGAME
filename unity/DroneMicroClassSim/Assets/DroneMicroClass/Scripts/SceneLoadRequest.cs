namespace DroneMicroClass
{
    public enum SunAltitudePreset
    {
        Morning,
        Afternoon
    }

    public static class SceneLoadRequest
    {
        public const string DefaultTrainingScene = "8字飞行";
        public const float MorningSunAltitudeDegrees = 35f;
        public const float AfternoonSunAltitudeDegrees = 58f;

        private static string targetSceneName = DefaultTrainingScene;
        private static SunAltitudePreset sunAltitudePreset = SunAltitudePreset.Morning;

        public static string TargetSceneName => string.IsNullOrWhiteSpace(targetSceneName)
            ? DefaultTrainingScene
            : targetSceneName;

        public static SunAltitudePreset SelectedSunAltitudePreset => sunAltitudePreset;

        public static float SelectedSunAltitudeDegrees => sunAltitudePreset == SunAltitudePreset.Afternoon
            ? AfternoonSunAltitudeDegrees
            : MorningSunAltitudeDegrees;

        public static float SelectedSunAzimuthDegrees => sunAltitudePreset == SunAltitudePreset.Afternoon
            ? 128f
            : -42f;

        public static string SelectedSunAltitudeLabel => sunAltitudePreset == SunAltitudePreset.Afternoon
            ? "下午"
            : "上午";

        public static void Select(string sceneName)
        {
            Select(sceneName, sunAltitudePreset);
        }

        public static void Select(string sceneName, SunAltitudePreset preset)
        {
            targetSceneName = string.IsNullOrWhiteSpace(sceneName)
                ? DefaultTrainingScene
                : sceneName;
            sunAltitudePreset = preset;
        }
    }
}
