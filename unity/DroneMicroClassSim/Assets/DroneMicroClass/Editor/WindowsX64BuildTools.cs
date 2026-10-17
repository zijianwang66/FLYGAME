using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DroneMicroClass.Editor
{
    public static class WindowsX64BuildTools
    {
        private const string BuildDirectory = "Builds/WindowsX64";
        private const string ExecutableName = "DroneMicroClassSim.exe";
        private const string SqlitePluginPath = "Assets/Plugins/x86_64/drone_sqlite3.dll";
        private const string DisabledSqlitePluginPath = "Assets/Plugins/x86_64/sqlite3.dll";

        [MenuItem("Drone MicroClass/Build/Configure Windows x64")]
        public static void ConfigureWindowsX64()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;

            PlayerSettings.productName = "DroneMicroClassSim";
            PlayerSettings.companyName = "DroneMicroClass";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            ConfigureSqlitePluginImporters();
            WriteWindowsX64ReadinessReport();
        }

        [MenuItem("Drone MicroClass/Build/Build Windows x64")]
        public static void BuildWindowsX64()
        {
            ConfigureWindowsX64();

            string absoluteBuildDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildDirectory));
            Directory.CreateDirectory(absoluteBuildDirectory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = Path.Combine(absoluteBuildDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            string summaryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/windows_x64_build_summary.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(summaryPath));
            File.WriteAllText(summaryPath, BuildSummaryText(report));

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Windows x64 build failed: " + report.summary.result);
            }
        }

        [MenuItem("Drone MicroClass/Build/Write Windows x64 Readiness Report")]
        public static void WriteWindowsX64ReadinessReport()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/windows_x64_readiness.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            PluginImporter sqliteImporter = AssetImporter.GetAtPath(SqlitePluginPath) as PluginImporter;
            PluginImporter disabledImporter = AssetImporter.GetAtPath(DisabledSqlitePluginPath) as PluginImporter;
            string absoluteSqlitePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SqlitePluginPath));

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Drone MicroClass Windows x64 readiness");
            builder.AppendLine("Active build target: " + EditorUserBuildSettings.activeBuildTarget);
            builder.AppendLine("Selected standalone target: " + EditorUserBuildSettings.selectedStandaloneTarget);
            builder.AppendLine("Scripting backend: " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
            builder.AppendLine("Enabled scenes:");
            string[] scenes = GetEnabledScenePaths();
            for (int i = 0; i < scenes.Length; i++)
            {
                builder.AppendLine("  " + (i + 1) + ". " + scenes[i]);
            }

            builder.AppendLine("SQLite native plugin:");
            builder.AppendLine("  Path: " + absoluteSqlitePath);
            builder.AppendLine("  Exists: " + File.Exists(absoluteSqlitePath));
            builder.AppendLine("  Importer exists: " + (sqliteImporter != null));
            if (sqliteImporter != null)
            {
                builder.AppendLine("  Any platform: " + sqliteImporter.GetCompatibleWithAnyPlatform());
                builder.AppendLine("  Editor compatible: " + sqliteImporter.GetCompatibleWithEditor());
                builder.AppendLine("  Windows x64 compatible: " + sqliteImporter.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64));
                builder.AppendLine("  Windows CPU: " + sqliteImporter.GetPlatformData(BuildTarget.StandaloneWindows64, "CPU"));
            }

            builder.AppendLine("Fallback sqlite3.dll:");
            builder.AppendLine("  Importer exists: " + (disabledImporter != null));
            if (disabledImporter != null)
            {
                builder.AppendLine("  Any platform: " + disabledImporter.GetCompatibleWithAnyPlatform());
                builder.AppendLine("  Windows x64 compatible: " + disabledImporter.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64));
            }

            File.WriteAllText(path, builder.ToString());
            Debug.Log("Wrote Windows x64 readiness report to " + path);
        }

        private static void ConfigureSqlitePluginImporters()
        {
            ConfigureSqliteRuntimePlugin(SqlitePluginPath, true);
            ConfigureSqliteRuntimePlugin(DisabledSqlitePluginPath, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureSqliteRuntimePlugin(string path, bool enabled)
        {
            PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null)
            {
                Debug.LogWarning("SQLite plugin importer missing: " + path);
                return;
            }

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(enabled);
            importer.SetEditorData("CPU", "x86_64");
            importer.SetEditorData("OS", "Windows");
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, enabled);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            importer.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "x86_64");
            importer.SetPlatformData(BuildTarget.StandaloneWindows64, "OS", "Windows");
            importer.SaveAndReimport();
        }

        private static string[] GetEnabledScenePaths()
        {
            System.Collections.Generic.List<string> scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }

        private static string BuildSummaryText(BuildReport report)
        {
            BuildSummary summary = report.summary;
            return "Drone MicroClass Windows x64 build summary\n" +
                "Result: " + summary.result + "\n" +
                "Output: " + summary.outputPath + "\n" +
                "Size bytes: " + summary.totalSize + "\n" +
                "Time: " + summary.totalTime + "\n" +
                "Warnings: " + summary.totalWarnings + "\n" +
                "Errors: " + summary.totalErrors + "\n";
        }
    }
}
