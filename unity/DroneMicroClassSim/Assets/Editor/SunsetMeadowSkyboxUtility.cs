using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DroneMicroClass.Editor
{
    /// <summary>
    /// Creates the shared sunset meadow panoramic skybox and applies it to
    /// every SceneAsset stored under Assets.
    /// </summary>
    public static class SunsetMeadowSkyboxUtility
    {
        private const string TexturePath =
            "Assets/Environment/Skyboxes/sunset_meadow_path_4k.exr";

        private const string MaterialPath =
            "Assets/Environment/Skyboxes/SunsetMeadowPath4K_Skybox.mat";

        private const string ForestMaterialPath =
            "Assets/Environment/Skyboxes/ForestSunsetMeadowPath4K_Skybox.mat";

        private const string MenuPath =
            "Tools/DroneMicroClass/Apply Sunset Meadow Skybox To Project Scenes";

        private const string ActiveSceneMenuPath =
            "Tools/DroneMicroClass/Apply Sunset Meadow Skybox To Active Scene";

        [MenuItem(ActiveSceneMenuPath)]
        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying skybox changes to the active scene.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                throw new InvalidOperationException(
                    "The active scene must be loaded and saved before applying the skybox.");
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                throw new InvalidOperationException(
                    $"Skybox texture is missing or has not finished importing: {TexturePath}");
            }

            var isForestScene = scene.name.Equals(
                "forest",
                StringComparison.OrdinalIgnoreCase);
            var materialPath = isForestScene ? ForestMaterialPath : MaterialPath;
            var exposure = isForestScene ? 0.4f : 1f;
            var skybox = CreateOrUpdateSkyboxMaterial(
                texture,
                materialPath,
                exposure);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save skybox changes to scene: {scene.path}");
            }

            DynamicGI.UpdateEnvironment();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[SunsetMeadowSkybox] Applied {materialPath} " +
                $"(exposure {exposure:0.##}) to active scene: {scene.path}");
        }

        [MenuItem(MenuPath)]
        public static void ApplyToAllProjectScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying skybox changes to project scenes.");
            }

            EnsureLoadedScenesAreSaved();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
            {
                throw new InvalidOperationException(
                    $"Skybox texture is missing or has not finished importing: {TexturePath}");
            }

            var skybox = CreateOrUpdateSkyboxMaterial(
                texture,
                MaterialPath,
                1f);
            var scenePaths = AssetDatabase.FindAssets(
                    "t:Scene",
                    new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (scenePaths.Length == 0)
            {
                throw new InvalidOperationException(
                    "No SceneAsset files were found under Assets.");
            }

            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var scenePath in scenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Single);

                    RenderSettings.skybox = skybox;
                    RenderSettings.ambientMode = AmbientMode.Skybox;
                    EditorSceneManager.MarkSceneDirty(scene);

                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException(
                            $"Failed to save skybox changes to scene: {scenePath}");
                    }
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }

            DynamicGI.UpdateEnvironment();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[SunsetMeadowSkybox] Applied {MaterialPath} to " +
                $"{scenePaths.Length} project scenes: " +
                $"{string.Join(", ", scenePaths)}");
        }

        private static Material CreateOrUpdateSkyboxMaterial(
            Texture2D texture,
            string materialPath,
            float exposure)
        {
            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required shader was not found: Skybox/Panoramic");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", texture);
            SetFloatIfPresent(material, "_Exposure", exposure);
            SetFloatIfPresent(material, "_Rotation", 0f);
            SetFloatIfPresent(material, "_Mapping", 1f);
            SetFloatIfPresent(material, "_ImageType", 0f);
            SetFloatIfPresent(material, "_MirrorOnBack", 0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return material;
        }

        private static void EnsureLoadedScenesAreSaved()
        {
            var dirtyScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(scene => scene.isDirty)
                .Select(scene => string.IsNullOrEmpty(scene.path) ? scene.name : scene.path)
                .ToArray();

            if (dirtyScenes.Length > 0)
            {
                throw new InvalidOperationException(
                    "Save the currently loaded scenes before applying the shared skybox: " +
                    string.Join(", ", dirtyScenes));
            }
        }

        private static void SetFloatIfPresent(
            Material material,
            string propertyName,
            float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
