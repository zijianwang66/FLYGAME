using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public static class Stage2ChallengeInstaller
    {
        private const string SceneRoot = "Assets/DroneMicroClass/Scenes";
        private const string LoginScenePath = SceneRoot + "/Login.unity";
        private const string MainMenuScenePath = SceneRoot + "/MainMenu.unity";
        private const string LoadingScenePath = SceneRoot + "/Loading.unity";
        private const string LevelScenePath = SceneRoot + "/DroneFigureEightTrainingUnity.unity";

        [MenuItem("Drone MicroClass/Install Stage 2 Challenge")]
        public static void InstallStage2Challenge()
        {
            Scene levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
            SingleLevelChallenge challenge = Object.FindFirstObjectByType<SingleLevelChallenge>();
            if (challenge == null)
            {
                GameObject challengeObject = new GameObject("Single Level Challenge Manager");
                challenge = challengeObject.AddComponent<SingleLevelChallenge>();
            }

            SimpleFlightController drone = Object.FindFirstObjectByType<SimpleFlightController>();
            if (drone == null)
            {
                Debug.LogError("Stage 2 install failed: no SimpleFlightController found in " + LevelScenePath);
                return;
            }

            SerializedObject serialized = new SerializedObject(challenge);
            serialized.FindProperty("drone").objectReferenceValue = drone;
            serialized.FindProperty("checkpoints").arraySize = 0;
            serialized.FindProperty("finishTarget").objectReferenceValue = null;
            serialized.FindProperty("createDefaultCourseIfEmpty").boolValue = true;
            serialized.FindProperty("startWhenDroneTakesOff").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(challenge);
            EditorSceneManager.MarkSceneDirty(levelScene);
            EditorSceneManager.SaveScene(levelScene);
            ConfigureBuildSettings();

            if (System.IO.File.Exists(MainMenuScenePath))
            {
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Stage 2 challenge installed. Press Play from MainMenu and start training to test checkpoints.");
        }

        private static void ConfigureBuildSettings()
        {
            if (System.IO.File.Exists(LoginScenePath))
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(LoginScenePath, true),
                    new EditorBuildSettingsScene(MainMenuScenePath, true),
                    new EditorBuildSettingsScene(LoadingScenePath, true),
                    new EditorBuildSettingsScene(LevelScenePath, true)
                };
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LoadingScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true)
            };
        }
    }
}
