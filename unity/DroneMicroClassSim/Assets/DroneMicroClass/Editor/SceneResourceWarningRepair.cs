using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public static class SceneResourceWarningRepair
    {
        private const string SchoolScenePath = "Assets/DroneMicroClass/Scenes/学校.unity";
        private const string ForestScenePath = "Assets/DroneMicroClass/Scenes/forest.unity";
        private const string BadSchoolLightingPath = "Assets/DroneMicroClass/Scenes/sch/LightingData.asset";
        private const string BadForestLightingPath = "Assets/DroneMicroClass/Scenes/forest/LightingData.asset";
        private const string BadSchoolTerrainPath = "Assets/New Terrain 2.asset";
        private const string GeneratedFolder = "Assets/DroneMicroClass/Generated";
        private const string ReplacementTerrainPath = GeneratedFolder + "/SchoolFallbackTerrain.asset";

        [MenuItem("Drone MicroClass/Diagnostics/Repair Scene Resource Warnings")]
        public static void RepairSceneResourceWarnings()
        {
            EnsureGeneratedFolder();
            TerrainData replacementTerrain = EnsureReplacementTerrain();

            RepairSchoolScene(replacementTerrain);
            RepairForestScene();
            BackupCorruptAsset(BadSchoolLightingPath);
            BackupCorruptAsset(BadForestLightingPath);
            BackupCorruptAsset(BadSchoolTerrainPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Scene resource warning repair completed.");
        }

        private static void RepairSchoolScene(TerrainData replacementTerrain)
        {
            Scene scene = EditorSceneManager.OpenScene(SchoolScenePath, OpenSceneMode.Single);
            Lightmapping.lightingDataAsset = null;

            foreach (Terrain terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                if (terrain == null)
                {
                    continue;
                }

                string terrainPath = terrain.terrainData != null ? AssetDatabase.GetAssetPath(terrain.terrainData) : string.Empty;
                if (terrain.terrainData == null || terrainPath == BadSchoolTerrainPath)
                {
                    terrain.terrainData = replacementTerrain;
                    EditorUtility.SetDirty(terrain);
                }
            }

            foreach (TerrainCollider collider in Object.FindObjectsByType<TerrainCollider>(FindObjectsSortMode.None))
            {
                if (collider == null)
                {
                    continue;
                }

                string terrainPath = collider.terrainData != null ? AssetDatabase.GetAssetPath(collider.terrainData) : string.Empty;
                if (collider.terrainData == null || terrainPath == BadSchoolTerrainPath)
                {
                    collider.terrainData = replacementTerrain;
                    EditorUtility.SetDirty(collider);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RepairForestScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ForestScenePath, OpenSceneMode.Single);
            Lightmapping.lightingDataAsset = null;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static TerrainData EnsureReplacementTerrain()
        {
            TerrainData terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(ReplacementTerrainPath);
            if (terrain != null)
            {
                return terrain;
            }

            terrain = new TerrainData
            {
                heightmapResolution = 129,
                alphamapResolution = 128,
                baseMapResolution = 128,
                size = new Vector3(420f, 22f, 420f)
            };

            AssetDatabase.CreateAsset(terrain, ReplacementTerrainPath);
            AssetDatabase.ImportAsset(ReplacementTerrainPath);
            return terrain;
        }

        private static void EnsureGeneratedFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DroneMicroClass"))
            {
                AssetDatabase.CreateFolder("Assets", "DroneMicroClass");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets/DroneMicroClass", "Generated");
            }
        }

        private static void BackupCorruptAsset(string assetPath)
        {
            string absolutePath = Path.GetFullPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            string backupRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/CorruptResourceBackup"));
            Directory.CreateDirectory(backupRoot);
            string backupPath = Path.Combine(backupRoot, assetPath.Replace('/', '_'));
            string backupMetaPath = backupPath + ".meta";

            FileUtil.ReplaceFile(absolutePath, backupPath);
            File.Delete(absolutePath);

            string metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath))
            {
                FileUtil.ReplaceFile(metaPath, backupMetaPath);
                File.Delete(metaPath);
            }
        }
    }
}
