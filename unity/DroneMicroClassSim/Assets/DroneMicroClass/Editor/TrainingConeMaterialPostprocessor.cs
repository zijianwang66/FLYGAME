using UnityEditor;
using UnityEngine;

namespace DroneMicroClass.Editor
{
    public sealed class TrainingConeMaterialPostprocessor : AssetPostprocessor
    {
        private const string TrainingFieldPath = "Assets/DroneMicroClass/Models/Training/DroneFigureEightTraining.fbx";
        private const string MaterialFolder = "Assets/DroneMicroClass/Materials/Training";

        private static readonly Color ConeOrange = new Color(0.95f, 0.40f, 0.04f, 1f);
        private static readonly Color ConeWhite = new Color(0.93f, 0.90f, 0.82f, 1f);
        private static readonly Color ConeDark = new Color(0.055f, 0.06f, 0.055f, 1f);

        private void OnPostprocessModel(GameObject importedRoot)
        {
            if (assetPath != TrainingFieldPath)
            {
                return;
            }

            Material orange = EnsureOpaqueMaterial("Traffic_Cone_Orange", ConeOrange, 0.48f);
            Material white = EnsureOpaqueMaterial("Traffic_Cone_White_Band", ConeWhite, 0.42f);
            Material dark = EnsureOpaqueMaterial("Traffic_Cone_Dark_Base", ConeDark, 0.78f);

            foreach (Renderer renderer in importedRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        continue;
                    }

                    string materialName = materials[i].name;
                    if (materialName == "Traffic_Cone_Orange")
                    {
                        materials[i] = orange;
                        changed = true;
                    }
                    else if (materialName == "Traffic_Cone_White_Band")
                    {
                        materials[i] = white;
                        changed = true;
                    }
                    else if (materialName == "Traffic_Cone_Dark_Base")
                    {
                        materials[i] = dark;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        [MenuItem("Drone MicroClass/Fix Training Cone Materials")]
        public static void FixTrainingConeMaterials()
        {
            EnsureOpaqueMaterial("Traffic_Cone_Orange", ConeOrange, 0.48f);
            EnsureOpaqueMaterial("Traffic_Cone_White_Band", ConeWhite, 0.42f);
            EnsureOpaqueMaterial("Traffic_Cone_Dark_Base", ConeDark, 0.78f);
            AssetDatabase.ImportAsset(TrainingFieldPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material EnsureOpaqueMaterial(string name, Color color, float smoothness)
        {
            EnsureFolder("Assets", "DroneMicroClass");
            EnsureFolder("Assets/DroneMicroClass", "Materials");
            EnsureFolder("Assets/DroneMicroClass/Materials", "Training");

            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindDefaultShader());
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.shader = FindDefaultShader();
            material.color = color;
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            SetColorIfPresent(material, "_BaseColor", color);
            SetColorIfPresent(material, "_Color", color);
            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_Metallic", 0f);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_Glossiness", smoothness);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindDefaultShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
