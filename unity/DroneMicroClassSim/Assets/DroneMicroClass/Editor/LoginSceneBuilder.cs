using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass.Editor
{
    public static class LoginSceneBuilder
    {
        private const string SceneRoot = "Assets/DroneMicroClass/Scenes";
        private const string LoginScenePath = SceneRoot + "/Login.unity";
        private const string MainMenuScenePath = SceneRoot + "/MainMenu.unity";
        private const string LoadingScenePath = SceneRoot + "/Loading.unity";
        private const string LevelScenePath = SceneRoot + "/DroneFigureEightTrainingUnity.unity";

        [MenuItem("Drone MicroClass/Build Cyber Login Scene")]
        public static void BuildCyberLoginScene()
        {
            EnsureSceneFolder();
            CreateLoginScene();
            ConfigureBuildSettings();
            EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
            Debug.Log("Cyber login scene created. Demo account: pilot / 123456.");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DroneMicroClass"))
            {
                AssetDatabase.CreateFolder("Assets", "DroneMicroClass");
            }

            if (!AssetDatabase.IsValidFolder(SceneRoot))
            {
                AssetDatabase.CreateFolder("Assets/DroneMicroClass", "Scenes");
            }
        }

        private static void CreateLoginScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Login";

            GameObject controllerObject = new GameObject("Login Scene Controller");
            controllerObject.AddComponent<DroneMicroClass.LoginSceneController>();

            EditorSceneManager.SaveScene(scene, LoginScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LoginScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LoadingScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true)
            };
        }
    }
}
