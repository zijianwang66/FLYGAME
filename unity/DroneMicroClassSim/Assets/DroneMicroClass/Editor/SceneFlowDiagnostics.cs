using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public static class SceneFlowDiagnostics
    {
        private const string SceneRoot = "Assets/DroneMicroClass/Scenes";

        private static readonly SceneExpectation[] Expectations =
        {
            new SceneExpectation("登录页", "Login", SceneRoot + "/Login.unity", typeof(LoginSceneController), false),
            new SceneExpectation("主菜单", "MainMenu", SceneRoot + "/MainMenu.unity", typeof(MainMenuController), false),
            new SceneExpectation("训练场选择", "TrainingSceneSelection", SceneRoot + "/TrainingSceneSelection.unity", typeof(TrainingSceneSelectionController), false),
            new SceneExpectation("Loading", "Loading", SceneRoot + "/Loading.unity", typeof(LoadingScreenController), false),
            new SceneExpectation("矩形训练", "矩形飞行训练", SceneRoot + "/矩形飞行训练.unity", typeof(SingleLevelChallenge), true),
            new SceneExpectation("8字飞行", "8字飞行", SceneRoot + "/8字飞行.unity", typeof(SingleLevelChallenge), true),
            new SceneExpectation("学校自由飞", "学校", SceneRoot + "/学校.unity", typeof(SimpleFlightController), false),
            new SceneExpectation("森林自由飞", "forest", SceneRoot + "/forest.unity", typeof(SimpleFlightController), false)
        };

        [MenuItem("Drone MicroClass/Diagnostics/Write Scene Flow Report")]
        public static void WriteSceneFlowReport()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Drone MicroClass scene flow diagnostics");
            builder.AppendLine("Generated at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine();

            HashSet<string> buildScenePaths = GetEnabledBuildScenePaths();
            bool allPassed = true;
            foreach (SceneExpectation expectation in Expectations)
            {
                bool passed = CheckScene(expectation, buildScenePaths, builder);
                allPassed &= passed;
                builder.AppendLine();
            }

            builder.AppendLine("Runtime hooks");
            builder.AppendLine("FlightExitConfirmation type: " + (typeof(FlightExitConfirmation) != null ? "OK" : "Missing"));
            builder.AppendLine("SceneLoadRequest type: " + (typeof(SceneLoadRequest) != null ? "OK" : "Missing"));
            builder.AppendLine("ScoreRecordStore type: " + (typeof(ScoreRecordStore) != null ? "OK" : "Missing"));
            builder.AppendLine();
            builder.AppendLine("Overall: " + (allPassed ? "PASS" : "CHECK REQUIRED"));

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/scene_flow_diagnostics.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            Debug.Log("Wrote scene flow diagnostics to " + path);
        }

        private static bool CheckScene(SceneExpectation expectation, HashSet<string> buildScenePaths, StringBuilder builder)
        {
            builder.AppendLine(expectation.Label + " / " + expectation.SceneName);
            builder.AppendLine("Path: " + expectation.Path);

            bool exists = File.Exists(expectation.Path);
            bool inBuild = buildScenePaths.Contains(expectation.Path);
            bool canStream = Application.CanStreamedLevelBeLoaded(expectation.SceneName);
            builder.AppendLine("Scene file exists: " + exists);
            builder.AppendLine("In Build Settings: " + inBuild);
            builder.AppendLine("Can streamed level be loaded: " + canStream);

            if (!exists)
            {
                builder.AppendLine("Result: FAIL - scene file missing.");
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(expectation.Path, OpenSceneMode.Single);
            Component required = UnityEngine.Object.FindFirstObjectByType(expectation.RequiredType) as Component;
            builder.AppendLine("Required component " + expectation.RequiredType.Name + ": " + (required != null ? "OK" : "Missing"));

            bool flightControllerOk = true;
            SimpleFlightController flight = UnityEngine.Object.FindFirstObjectByType<SimpleFlightController>();
            if (expectation.RequiresChallengeSettlement)
            {
                flightControllerOk = flight != null;
                builder.AppendLine("SimpleFlightController: " + (flightControllerOk ? "OK" : "Missing"));
                builder.AppendLine("Training settlement: " + (required != null ? "SingleLevelChallenge present" : "Missing SingleLevelChallenge"));
            }

            bool scenePassed = exists && inBuild && canStream && required != null && flightControllerOk;
            builder.AppendLine("Opened scene: " + scene.path);
            builder.AppendLine("Result: " + (scenePassed ? "PASS" : "CHECK REQUIRED"));
            return scenePassed;
        }

        private static HashSet<string> GetEnabledBuildScenePaths()
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    paths.Add(scene.path);
                }
            }

            return paths;
        }

        private readonly struct SceneExpectation
        {
            public SceneExpectation(string label, string sceneName, string path, Type requiredType, bool requiresChallengeSettlement)
            {
                Label = label;
                SceneName = sceneName;
                Path = path;
                RequiredType = requiredType;
                RequiresChallengeSettlement = requiresChallengeSettlement;
            }

            public string Label { get; }
            public string SceneName { get; }
            public string Path { get; }
            public Type RequiredType { get; }
            public bool RequiresChallengeSettlement { get; }
        }
    }
}
