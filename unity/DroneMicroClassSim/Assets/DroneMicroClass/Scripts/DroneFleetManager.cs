using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneMicroClass
{
    public sealed class DroneFleetManager : MonoBehaviour
    {
        [Serializable]
        public sealed class DroneVariant
        {
            public string displayName;
            public DroneProfile profile;
            public GameObject visualPrefab;
            public float visualTargetSize = 1.4f;
            public Vector3 visualRotationEuler = new Vector3(0f, 180f, 0f);
            public Vector3 noseCameraLocalPosition = new Vector3(0f, 0.2f, 1f);
            public Vector3 noseCameraLocalEuler = Vector3.zero;
            public bool levelNoseCamera = true;
            public float levelNoseCameraSharpness = 24f;
            public bool supportsGimbalPitch = true;
            public Material plasticShellMaterial;
            public Material plasticAccentMaterial;
            public Material plasticDarkMaterial;
            public Material plasticRotorMaterial;
            public Material plasticLensMaterial;
        }

        [SerializeField] private SimpleFlightController flightController;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private DroneVariant[] variants;
        [SerializeField] private int startingIndex;
        [SerializeField] private KeyCode nextAircraftKey = KeyCode.Tab;

        [Header("Gimbal Pitch")]
        [SerializeField] private KeyCode gimbalPitchUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode gimbalPitchDownKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode gimbalCenterKey = KeyCode.B;
        [SerializeField, Min(1f)] private float gimbalPitchSpeed = 35f;
        [SerializeField, Range(-90f, 0f)] private float minimumGimbalPitch = -90f;
        [SerializeField, Range(0f, 30f)] private float maximumGimbalPitch = 30f;
        [SerializeField, Range(-90f, 30f)] private float gimbalPitchDegrees;

        private int currentIndex = -1;
        private GameObject activeVisual;

        public event Action<DroneVariant> VariantChanged;

        public DroneVariant CurrentVariant =>
            variants != null && currentIndex >= 0 && currentIndex < variants.Length ? variants[currentIndex] : null;

        public int CurrentIndex => currentIndex;
        public int VariantCount => variants != null ? variants.Length : 0;
        public string CurrentVariantName => CurrentVariant != null ? CurrentVariant.displayName : "Training Drone";
        public Vector3 CurrentNoseCameraLocalPosition => CurrentVariant != null ? CurrentVariant.noseCameraLocalPosition : new Vector3(0f, 0.2f, 1f);
        public Vector3 CurrentNoseCameraLocalEuler => CurrentVariant != null ? CurrentVariant.noseCameraLocalEuler : Vector3.zero;
        public bool CurrentNoseCameraIsLevel => CurrentVariant == null || CurrentVariant.levelNoseCamera;
        public float CurrentNoseCameraLevelSharpness => CurrentVariant != null ? Mathf.Max(0.1f, CurrentVariant.levelNoseCameraSharpness) : 24f;
        public bool CurrentVariantSupportsGimbalPitch => CurrentVariant == null || CurrentVariant.supportsGimbalPitch;
        public float CurrentGimbalPitchDegrees => CurrentVariantSupportsGimbalPitch ? gimbalPitchDegrees : 0f;

        public void Configure(SimpleFlightController flight, Transform visuals, DroneVariant[] newVariants, int defaultIndex = 0)
        {
            flightController = flight;
            visualRoot = visuals;
            variants = newVariants;
            startingIndex = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, VariantCount - 1));
        }

        private void Awake()
        {
            NormalizeGimbalLimits();
            gimbalPitchDegrees = Mathf.Clamp(gimbalPitchDegrees, minimumGimbalPitch, maximumGimbalPitch);

            if (flightController == null)
            {
                flightController = GetComponent<SimpleFlightController>();
            }

            if (visualRoot == null)
            {
                Transform existingVisualRoot = transform.Find("Visual Root");
                visualRoot = existingVisualRoot != null ? existingVisualRoot : transform;
            }
        }

        private void Start()
        {
            if (VariantCount > 0)
            {
                SwitchTo(Mathf.Clamp(startingIndex, 0, VariantCount - 1), false);
            }
        }

        private void Update()
        {
            UpdateGimbalPitch();

            if (VariantCount == 0)
            {
                return;
            }

            if (Input.GetKeyDown(nextAircraftKey))
            {
                SwitchTo((currentIndex + 1 + VariantCount) % VariantCount, true);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchTo(0, true);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchTo(1, true);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwitchTo(2, true);
            }
        }

        public void SetGimbalPitch(float pitchDegrees)
        {
            NormalizeGimbalLimits();
            gimbalPitchDegrees = Mathf.Clamp(pitchDegrees, minimumGimbalPitch, maximumGimbalPitch);
        }

        private void UpdateGimbalPitch()
        {
            if (!CurrentVariantSupportsGimbalPitch)
            {
                gimbalPitchDegrees = 0f;
                return;
            }

            if (Input.GetKeyDown(gimbalCenterKey))
            {
                SetGimbalPitch(0f);
                return;
            }

            float input = 0f;
            if (Input.GetKey(gimbalPitchUpKey))
            {
                input += 1f;
            }

            if (Input.GetKey(gimbalPitchDownKey))
            {
                input -= 1f;
            }

            if (Mathf.Approximately(input, 0f))
            {
                return;
            }

            SetGimbalPitch(gimbalPitchDegrees + input * gimbalPitchSpeed * Time.unscaledDeltaTime);
        }

        private void NormalizeGimbalLimits()
        {
            if (gimbalPitchSpeed < 1f)
            {
                gimbalPitchSpeed = 35f;
            }

            if (Mathf.Approximately(minimumGimbalPitch, maximumGimbalPitch))
            {
                minimumGimbalPitch = -90f;
                maximumGimbalPitch = 30f;
            }

            minimumGimbalPitch = Mathf.Min(minimumGimbalPitch, 0f);
            maximumGimbalPitch = Mathf.Max(maximumGimbalPitch, 0f);
            if (minimumGimbalPitch > maximumGimbalPitch)
            {
                (minimumGimbalPitch, maximumGimbalPitch) = (maximumGimbalPitch, minimumGimbalPitch);
            }
        }

        public void SwitchTo(int index, bool resetMotion)
        {
            if (variants == null || variants.Length == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, variants.Length - 1);
            DroneVariant variant = variants[index];
            if (variant == null || variant.profile == null)
            {
                return;
            }

            currentIndex = index;
            if (!variant.supportsGimbalPitch)
            {
                gimbalPitchDegrees = 0f;
            }

            flightController.SetProfile(variant.profile);
            RebuildVisual(variant);
            if (resetMotion)
            {
                flightController.ResetMotion(false);
            }

            VariantChanged?.Invoke(variant);
        }

        public void PreviewVariant(int index)
        {
            SwitchTo(index, false);
        }

        private void RebuildVisual(DroneVariant variant)
        {
            if (visualRoot == null)
            {
                return;
            }

            ClearVisualRoot();
            if (variant.visualPrefab == null)
            {
                flightController.SetRotorVisuals(Array.Empty<Transform>());
                return;
            }

            activeVisual = Instantiate(variant.visualPrefab);
            activeVisual.name = "Active Visual - " + variant.displayName;
            activeVisual.transform.SetParent(visualRoot, false);
            activeVisual.transform.localPosition = Vector3.zero;
            activeVisual.transform.localRotation = Quaternion.Euler(variant.visualRotationEuler);
            RemoveImportedSceneHelpers(activeVisual);
            HideNonDroneMeshes(activeVisual);
            ApplyPlasticDroneMaterials(activeVisual, variant);
            FitRendererToSize(activeVisual, variant.visualTargetSize, 0f);
            flightController.SetRotorVisuals(BuildRotorVisuals(activeVisual.transform));
        }

        private static void RemoveImportedSceneHelpers(GameObject target)
        {
            Light[] lights = target.GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                light.enabled = false;
                light.intensity = 0f;
            }

            Camera[] cameras = target.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                DestroyImportedComponent(camera);
            }

            AudioListener[] listeners = target.GetComponentsInChildren<AudioListener>(true);
            foreach (AudioListener listener in listeners)
            {
                DestroyImportedComponent(listener);
            }
        }

        private static void DestroyImportedComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(component);
            }
            else
            {
                DestroyImmediate(component);
            }
        }

        private void ClearVisualRoot()
        {
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = visualRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Transform[] FindRotorTransforms(Transform root)
        {
            Transform[] pivotGroups = FindRotorPivotGroups(root);
            if (pivotGroups.Length > 0)
            {
                return pivotGroups;
            }

            var rotors = new List<Transform>();
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == root || !IsRotorName(child.name))
                {
                    continue;
                }

                if (child.GetComponent<Renderer>() != null)
                {
                    rotors.Add(child);
                }
            }

            rotors.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return rotors.ToArray();
        }

        private static Transform[] BuildRotorVisuals(Transform root)
        {
            Transform[] djiPivots = BuildDjiBladePivots(root);
            return djiPivots.Length > 0 ? djiPivots : FindRotorTransforms(root);
        }

        private static Transform[] BuildDjiBladePivots(Transform root)
        {
            var groups = new Dictionary<string, List<Transform>>();
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == root || child.GetComponent<Renderer>() == null)
                {
                    continue;
                }

                if (!TryGetDjiBladeGroup(child.name, out string groupName))
                {
                    continue;
                }

                if (!groups.TryGetValue(groupName, out List<Transform> group))
                {
                    group = new List<Transform>();
                    groups.Add(groupName, group);
                }

                group.Add(child);
            }

            var pivots = new List<Transform>();
            foreach (KeyValuePair<string, List<Transform>> pair in groups)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }

                List<Transform> spinningParts = SelectDjiBladeParts(pair.Value);
                if (spinningParts.Count < 2)
                {
                    continue;
                }

                Bounds bounds = CalculateRendererBounds(spinningParts);
                Vector3 axis = EstimateRotorAxis(spinningParts, bounds);
                GameObject pivotObject = new GameObject("Rotor Pivot - " + pair.Key);
                Transform pivot = pivotObject.transform;
                pivot.SetParent(root, false);
                pivot.position = bounds.center;
                pivot.rotation = Quaternion.FromToRotation(root.up, axis) * root.rotation;
                pivot.localScale = Vector3.one;

                foreach (Transform bladePart in spinningParts)
                {
                    bladePart.SetParent(pivot, true);
                }

                pivots.Add(pivot);
            }

            pivots.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return pivots.ToArray();
        }

        private static List<Transform> SelectDjiBladeParts(List<Transform> group)
        {
            Bounds groupBounds = CalculateRendererBounds(group);
            Vector3 center = groupBounds.center;
            float maxDistance = 0f;
            foreach (Transform part in group)
            {
                Renderer renderer = part.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(renderer.bounds.center, center));
            }

            var selected = new List<Transform>();
            float distanceThreshold = Mathf.Max(0.01f, maxDistance * 0.38f);
            foreach (Transform part in group)
            {
                Renderer renderer = part.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                float longestSide = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                float distance = Vector3.Distance(renderer.bounds.center, center);
                if (distance >= distanceThreshold || longestSide >= groupBounds.size.magnitude * 0.18f)
                {
                    selected.Add(part);
                }
            }

            return selected.Count >= 2 ? selected : new List<Transform>(group);
        }

        private static Vector3 EstimateRotorAxis(List<Transform> parts, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Transform first = null;
            Transform second = null;
            float farthest = 0f;
            foreach (Transform left in parts)
            {
                foreach (Transform right in parts)
                {
                    if (left == right)
                    {
                        continue;
                    }

                    Renderer leftRenderer = left.GetComponent<Renderer>();
                    Renderer rightRenderer = right.GetComponent<Renderer>();
                    if (leftRenderer == null || rightRenderer == null)
                    {
                        continue;
                    }

                    float distance = Vector3.SqrMagnitude(leftRenderer.bounds.center - rightRenderer.bounds.center);
                    if (distance > farthest)
                    {
                        farthest = distance;
                        first = left;
                        second = right;
                    }
                }
            }

            Vector3 span = Vector3.forward;
            if (first != null && second != null)
            {
                span = (first.GetComponent<Renderer>().bounds.center - second.GetComponent<Renderer>().bounds.center).normalized;
            }

            Vector3 reference = Vector3.zero;
            foreach (Transform part in parts)
            {
                Renderer renderer = part.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                Vector3 fromCenter = renderer.bounds.center - center;
                if (fromCenter.sqrMagnitude > reference.sqrMagnitude && Mathf.Abs(Vector3.Dot(fromCenter.normalized, span)) < 0.92f)
                {
                    reference = fromCenter;
                }
            }

            if (reference.sqrMagnitude < 0.0001f)
            {
                reference = Vector3.Cross(span, Vector3.up).sqrMagnitude > 0.0001f ? Vector3.Cross(span, Vector3.up) : Vector3.Cross(span, Vector3.right);
            }

            Vector3 axis = Vector3.Cross(span, reference).normalized;
            if (axis.sqrMagnitude < 0.0001f)
            {
                axis = Vector3.up;
            }

            return Vector3.Dot(axis, Vector3.up) < 0f ? -axis : axis;
        }

        private static bool TryGetDjiBladeGroup(string objectName, out string groupName)
        {
            const string bladePrefix = "\u6868\u53f6";
            groupName = null;
            if (!objectName.StartsWith(bladePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int separatorIndex = objectName.IndexOf('_');
            groupName = separatorIndex > 0 ? objectName.Substring(0, separatorIndex) : objectName;
            return groupName.Length > bladePrefix.Length;
        }

        private static Transform[] FindRotorPivotGroups(Transform root)
        {
            var pivots = new List<Transform>();
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.StartsWith("Rotor Pivot - ", StringComparison.Ordinal))
                {
                    pivots.Add(child);
                }
            }

            pivots.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return pivots.ToArray();
        }

        private static bool IsRotorName(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("propeller") || lower.StartsWith("rotor") || lower.Contains("blade") || lower.Contains("\u6868\u53f6");
        }

        private static void HideNonDroneMeshes(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string objectName = renderer.gameObject.name.ToLowerInvariant();
                if (objectName.Contains("piste") || objectName.Contains("runway"))
                {
                    renderer.enabled = false;
                }
            }
        }

        private static void ApplyPlasticDroneMaterials(GameObject target, DroneVariant variant)
        {
            Material shell = variant.plasticShellMaterial ?? CreateVisualMaterial("Fallback Plastic Shell", new Color(0.68f, 0.72f, 0.72f), 0.46f, 0f);
            Material accent = variant.plasticAccentMaterial ?? CreateVisualMaterial("Fallback Plastic Accent", new Color(0.38f, 0.43f, 0.46f), 0.38f, 0f);
            Material dark = variant.plasticDarkMaterial ?? CreateVisualMaterial("Fallback Plastic Dark", new Color(0.09f, 0.10f, 0.11f), 0.32f, 0f);
            Material propeller = variant.plasticRotorMaterial ?? CreateVisualMaterial("Fallback Plastic Rotor", new Color(0.15f, 0.16f, 0.17f), 0.26f, 0f);
            Material lens = variant.plasticLensMaterial ?? CreateVisualMaterial("Fallback Camera Lens", new Color(0.02f, 0.03f, 0.035f), 0.72f, 0f);

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string searchText = BuildMaterialSearchText(renderer);
                Material selectedMaterial;
                if (ContainsAny(searchText, "propeller", "rotor", "blade", "\u6868\u53f6"))
                {
                    selectedMaterial = propeller;
                }
                else if (ContainsAny(searchText, "camera", "lens", "glass", "optic", "\u955c\u5934", "\u7403\u4f53"))
                {
                    selectedMaterial = lens;
                }
                else if (ContainsAny(searchText, "motor", "rubber", "black", "dark", "jt", "\u6324\u538b"))
                {
                    selectedMaterial = dark;
                }
                else if (ContainsAny(searchText, "panel", "trim", "arm", "leg", "landing", "skid", "accent"))
                {
                    selectedMaterial = accent;
                }
                else
                {
                    selectedMaterial = shell;
                }

                Material[] slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderer.sharedMaterial = selectedMaterial;
                    continue;
                }

                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = selectedMaterial;
                }

                renderer.sharedMaterials = slots;
            }
        }

        private static string BuildMaterialSearchText(Renderer renderer)
        {
            string searchText = renderer.gameObject.name.ToLowerInvariant();
            Material[] materials = renderer.sharedMaterials;
            foreach (Material material in materials)
            {
                if (material != null)
                {
                    searchText += " " + material.name.ToLowerInvariant();
                }
            }

            return searchText;
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            foreach (string term in terms)
            {
                if (value.Contains(term))
                {
                    return true;
                }
            }

            return false;
        }

        private static Material CreateVisualMaterial(string name, Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.DontSave
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            return material;
        }

        private static void FitRendererToSize(GameObject target, float targetMaxSize, float bottomOffset)
        {
            Bounds bounds = CalculateRendererBounds(target);
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max <= 0.0001f)
            {
                return;
            }

            target.transform.localScale *= targetMaxSize / max;
            bounds = CalculateRendererBounds(target);
            Vector3 anchor = target.transform.position;
            target.transform.position += new Vector3(
                anchor.x - bounds.center.x,
                anchor.y + bottomOffset - bounds.min.y,
                anchor.z - bounds.center.z);
        }

        private static Bounds CalculateRendererBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            Renderer firstEnabled = null;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled)
                {
                    firstEnabled = renderer;
                    break;
                }
            }

            if (firstEnabled == null)
            {
                return new Bounds(target.transform.position, Vector3.one);
            }

            Bounds bounds = firstEnabled.bounds;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled && renderer != firstEnabled)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static Bounds CalculateRendererBounds(List<Transform> targets)
        {
            Renderer firstEnabled = null;
            foreach (Transform target in targets)
            {
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    firstEnabled = renderer;
                    break;
                }
            }

            if (firstEnabled == null)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Bounds bounds = firstEnabled.bounds;
            foreach (Transform target in targets)
            {
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled && renderer != firstEnabled)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }
    }
}
