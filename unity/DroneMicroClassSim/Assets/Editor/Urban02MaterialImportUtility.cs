using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DroneMicroClass.Editor
{
    /// <summary>
    /// Builds persistent URP materials for the corrected Urban 02 FBX and
    /// remaps the model importer's embedded material slots to those assets.
    /// </summary>
    public static class Urban02MaterialImportUtility
    {
        private const string ModelPath =
            "Assets/DroneMicroClass/Models/UrbanBuildings/02_Corrected.fbx";

        private const string TextureFolder =
            "Assets/DroneMicroClass/Textures/Urban02";

        private const string MaterialFolder =
            "Assets/DroneMicroClass/Materials/Urban02";

        private static readonly IReadOnlyDictionary<string, string> BaseMapByMaterial =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["branch-01"] = "branch-2-01.png",
                ["branch-02"] = "branch-2-02.png",
                ["branch-1-01"] = "branch-01.png",
                ["branch-1-02"] = "branch-02.png",
                ["branch-2-01"] = "branch-1-01.png",
                ["branch-2-02"] = "branch-1-02.png",
                ["trunk-01"] = "bark.jpg",
                ["Mat3d66_1014492_6_4698"] = "Mat3d66_1014492_6_4698.jpg",
                ["Mat3d66_1014492_7_4547"] = "Mat3d66_1014492_7_4547.jpg",
                ["Mat3d66_1014492_8_5461"] = "Mat3d66_1014492_8_5461.jpg",
                ["Mat3d66_1014492_10_4725"] = "Mat3d66_1014492_10_4725.jpg",
                ["Mat3d66_1014492_15_617"] = "Mat3d66_1014492_15_617.jpg",
                ["Mat3d66_1014492_16_3068"] = "Mat3d66_1014492_16_3068.jpg",
                ["Mat3d66_1014492_16_3068_1"] = "Mat3d66_1014492_16_3068_1.jpg",
                ["Mat3d66_1014492_17_4052"] = "Mat3d66_1014492_17_4052.jpg",
                ["Mat3d66_1014492_18_4928"] = "Mat3d66_1014492_18_4928.jpg",
            };

        [MenuItem("Tools/DroneMicroClass/Import Urban 02 Materials")]
        public static void ImportMaterials()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"ModelImporter not found: {ModelPath}");
            }

            EnsureAssetFolder(MaterialFolder);

            var textures = LoadAndValidateTextures();
            var sourceMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .OrderBy(material => material.name, StringComparer.Ordinal)
                .ToArray();

            if (sourceMaterials.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No embedded material slots were found in {ModelPath}.");
            }

            foreach (var remap in importer.GetExternalObjectMap()
                         .Where(pair => pair.Key.type == typeof(Material))
                         .ToArray())
            {
                importer.RemoveRemap(remap.Key);
            }

            var created = 0;
            var updated = 0;
            var textured = 0;
            var alphaClipped = 0;

            foreach (var source in sourceMaterials)
            {
                var materialPath =
                    $"{MaterialFolder}/{SanitizeFileName(source.name)}.mat";
                var target = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (target == null)
                {
                    target = new Material(source)
                    {
                        name = source.name
                    };
                    AssetDatabase.CreateAsset(target, materialPath);
                    created++;
                }
                else
                {
                    target.shader = source.shader;
                    target.CopyPropertiesFromMaterial(source);
                    target.name = source.name;
                    updated++;
                }

                if (BaseMapByMaterial.TryGetValue(source.name, out var textureFile))
                {
                    var texture = textures[textureFile];
                    AssignBaseMap(target, texture);
                    textured++;
                }

                if (IsBranchMaterial(source.name))
                {
                    ConfigureLeafCutout(target);
                    alphaClipped++;
                }
                else
                {
                    ConfigureOpaque(target);
                }

                EditorUtility.SetDirty(target);
                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name),
                    target);
            }

            EditorUtility.SetDirty(importer);
            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Urban02Import] Remapped {sourceMaterials.Length} materials " +
                $"({created} created, {updated} updated, {textured} textured, " +
                $"{alphaClipped} alpha-clipped) for {ModelPath}.");
        }

        private static Dictionary<string, Texture2D> LoadAndValidateTextures()
        {
            var textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            foreach (var fileName in BaseMapByMaterial.Values.Distinct())
            {
                var path = $"{TextureFolder}/{fileName}";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    throw new FileNotFoundException(
                        $"Required Urban 02 texture is missing or not imported: {path}");
                }

                textures.Add(fileName, texture);
            }

            return textures;
        }

        private static void AssignBaseMap(Material material, Texture2D texture)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
        }

        private static void ConfigureLeafCutout(Material material)
        {
            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 1f);
            SetFloatIfPresent(material, "_Cutoff", 0.35f);
            SetFloatIfPresent(material, "_Cull", 0f);
            SetFloatIfPresent(material, "_ZWrite", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.doubleSidedGI = true;
        }

        private static void ConfigureOpaque(Material material)
        {
            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_ZWrite", 1f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
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

        private static bool IsBranchMaterial(string materialName)
        {
            return materialName.StartsWith("branch-", StringComparison.Ordinal);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            var segments = folderPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
