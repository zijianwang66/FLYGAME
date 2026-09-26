namespace DroneMicroClass
{
    public static class SceneLoadRequest
    {
        public const string DefaultTrainingScene = "8字飞行";

        private static string targetSceneName = DefaultTrainingScene;

        public static string TargetSceneName => string.IsNullOrWhiteSpace(targetSceneName)
            ? DefaultTrainingScene
            : targetSceneName;

        public static void Select(string sceneName)
        {
            targetSceneName = string.IsNullOrWhiteSpace(sceneName)
                ? DefaultTrainingScene
                : sceneName;
        }
    }
}
