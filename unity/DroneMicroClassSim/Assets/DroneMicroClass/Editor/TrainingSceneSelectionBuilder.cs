using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public static class TrainingSceneSelectionBuilder
    {
        private const string SceneRoot = "Assets/DroneMicroClass/Scenes";
        private const string LoginScenePath = SceneRoot + "/Login.unity";
        private const string MainMenuScenePath = SceneRoot + "/MainMenu.unity";
        private const string SelectionScenePath = SceneRoot + "/TrainingSceneSelection.unity";
        private const string LoadingScenePath = SceneRoot + "/Loading.unity";
        private const string RectangleScenePath = SceneRoot + "/矩形飞行训练.unity";
        private const string FigureEightScenePath = SceneRoot + "/8字飞行.unity";
        private const string ForestScenePath = SceneRoot + "/forest.unity";
        private const string SchoolScenePath = SceneRoot + "/学校.unity";

        [MenuItem("Drone MicroClass/Build Training Scene Selection")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play mode before building the training scene selection flow.");
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            EnsureRectangleScene();
            CreateSelectionScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SelectionScenePath, OpenSceneMode.Single);
            Debug.Log("Training scene selection flow is ready.");
        }

        private static void EnsureRectangleScene()
        {
            if (!File.Exists(FigureEightScenePath))
            {
                throw new FileNotFoundException("Figure-eight source scene was not found.", FigureEightScenePath);
            }

            if (!File.Exists(RectangleScenePath) && !AssetDatabase.CopyAsset(FigureEightScenePath, RectangleScenePath))
            {
                throw new IOException("Failed to copy the figure-eight scene to the rectangle training scene.");
            }

            AssetDatabase.Refresh();
            Scene rectangleScene = EditorSceneManager.OpenScene(RectangleScenePath, OpenSceneMode.Single);
            SingleLevelChallenge challenge = Object.FindFirstObjectByType<SingleLevelChallenge>();
            if (challenge == null)
            {
                GameObject manager = new GameObject("Single Level Challenge Manager");
                challenge = manager.AddComponent<SingleLevelChallenge>();
            }

            SerializedObject serialized = new SerializedObject(challenge);
            SerializedProperty routeLayout = serialized.FindProperty("routeLayout");
            if (routeLayout != null)
            {
                routeLayout.enumValueIndex = 1;
            }

            SerializedProperty checkpoints = serialized.FindProperty("checkpoints");
            if (checkpoints != null)
            {
                checkpoints.arraySize = 0;
            }

            SerializedProperty createDefaultCourse = serialized.FindProperty("createDefaultCourseIfEmpty");
            if (createDefaultCourse != null)
            {
                createDefaultCourse.boolValue = true;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(challenge);
            EditorSceneManager.MarkSceneDirty(rectangleScene);
            EditorSceneManager.SaveScene(rectangleScene, RectangleScenePath);
        }

        private static void CreateSelectionScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.88f, 0.96f, 1f, 1f);

            GameObject controllerObject = new GameObject("Training Scene Selection Controller");
            controllerObject.AddComponent<TrainingSceneSelectionController>();
            EditorSceneManager.SaveScene(scene, SelectionScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            string[] requiredPaths =
            {
                LoginScenePath,
                MainMenuScenePath,
                SelectionScenePath,
                LoadingScenePath,
                RectangleScenePath,
                FigureEightScenePath,
                ForestScenePath,
                SchoolScenePath
            };

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            HashSet<string> added = new HashSet<string>();
            foreach (string path in requiredPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("Build Settings skipped missing scene: " + path);
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
                added.Add(path);
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!added.Contains(existing.path))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
