using DroneMicroClass;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass.Editor
{
    public static class DroneMicroClassSceneBuilder
    {
        private const string Root = "Assets/DroneMicroClass";
        private const string FigureEightScenePath = Root + "/Scenes/DroneFigureEightTrainingUnity.unity";
        private const string PrefabPath = Root + "/Prefabs/TeachingDrone.prefab";
        private const string HudPrefabPath = Root + "/Prefabs/FlightHud.prefab";
        private const string DjiMainDronePath = Root + "/Models/Drones/DJI/DJI_Drone.fbx";
        private const string CityBackgroundPath = Root + "/Models/Environment/CityBackground.fbx";
        private const string QuadProfilePath = Root + "/Profiles/QuadRacer.asset";
        private const string InspireProfilePath = Root + "/Profiles/DJIInspire.asset";
        private const string RedProfilePath = Root + "/Profiles/RedGuard.asset";
        private const string DjiMaterialRoot = Root + "/Materials";
        private const string VariantMaterialRoot = Root + "/Materials/Drones";
        private const string WildTerrainPath = Root + "/Terrain/WildValleyTerrain.asset";
        private const string GrassTexturePath = Root + "/Terrain/WildGrassTexture.asset";
        private const string DirtTexturePath = Root + "/Terrain/WildDirtTexture.asset";
        private const string RockTexturePath = Root + "/Terrain/WildRockTexture.asset";
        private const string GrassLayerPath = Root + "/Terrain/WildGrass.terrainlayer";
        private const string DirtLayerPath = Root + "/Terrain/WildDirt.terrainlayer";
        private const string RockLayerPath = Root + "/Terrain/WildRock.terrainlayer";
        private const string NoseRenderTexturePath = Root + "/RenderTextures/NoseCameraView.renderTexture";
        private const string UrpAssetPath = Root + "/Settings/DroneMicroClassURP.asset";
        private const string UrpRendererPath = Root + "/Settings/DroneMicroClassRenderer.asset";
        private const int MiniMapTextureSize = 512;
        private const int MiniMapSafetyLeft = 48;
        private const int MiniMapSafetyBottom = 121;
        private const int MiniMapSafetyRight = 464;
        private const int MiniMapSafetyTop = 391;
        private static readonly Vector2 MiniMapWorldCenter = Vector2.zero;
        private static readonly Vector2 MiniMapWorldHalfExtents = new Vector2(20f, 13f);
        private static readonly Vector2 MiniMapContentViewportMin = new Vector2(MiniMapSafetyLeft / (float)MiniMapTextureSize, MiniMapSafetyBottom / (float)MiniMapTextureSize);
        private static readonly Vector2 MiniMapContentViewportMax = new Vector2(MiniMapSafetyRight / (float)MiniMapTextureSize, MiniMapSafetyTop / (float)MiniMapTextureSize);
        private const string MiniMapTrackSpritePath = Root + "/Textures/MiniMapFieldFigureEight.png";
        private const string MiniMapAircraftSpritePath = Root + "/Textures/MiniMapAircraftTriangle.png";
        private const string ImportedRoot = "Assets/Imported/FlyAssets";
        private const string QuadRacerPath = ImportedRoot + "/Quad_Racer/Art/Drones/01/Drone_01.fbx";
        private const string InspirePath = ImportedRoot + "/dji-inspire-2-with-zenmuse-x5s/source/TEST.fbx";
        private const string RedDronePath = ImportedRoot + "/DroneControllerModels/Drone_red/Drone_red.FBX";
        private const string KenneyNatureRoot = Root + "/ThirdParty/KenneyNatureKit";
        private const string KenneyNatureModelsRoot = KenneyNatureRoot + "/Models";
        private const string AmbientCgRoot = Root + "/ThirdParty/ambientCG";
        private const string AmbientGrassColorPath = AmbientCgRoot + "/Grass001/Grass001_1K-JPG_Color.jpg";
        private const string AmbientGrassNormalPath = AmbientCgRoot + "/Grass001/Grass001_1K-JPG_NormalGL.jpg";
        private const string AmbientGroundColorPath = AmbientCgRoot + "/Ground039/Ground039_1K-JPG_Color.jpg";
        private const string AmbientGroundNormalPath = AmbientCgRoot + "/Ground039/Ground039_1K-JPG_NormalGL.jpg";
        private const string AmbientRockColorPath = AmbientCgRoot + "/Rock003/Rock003_1K-JPG_Color.jpg";
        private const string AmbientRockNormalPath = AmbientCgRoot + "/Rock003/Rock003_1K-JPG_NormalGL.jpg";
        private const string CanyonTerrainRoot = Root + "/ThirdParty/CanyonTerrain";
        private const string CanyonModelsRoot = CanyonTerrainRoot + "/Models";
        private const string CanyonTexturesRoot = CanyonTerrainRoot + "/Textures";
        private const string CanyonGroundDiffusePath = CanyonTexturesRoot + "/CanyonGround/CanyonGround_Diffuse.png";
        private const string CanyonGroundNormalPath = CanyonTexturesRoot + "/CanyonGround/CanyonGround_Normal.png";
        private const string CanyonWallDiffusePath = CanyonTexturesRoot + "/CanyonWall_3/CanyonWall_3_Diffuse.png";
        private const string CanyonWallNormalPath = CanyonTexturesRoot + "/CanyonWall_3/CanyonWall_3_Normal.png";
        private const string CanyonRocksDiffusePath = CanyonTexturesRoot + "/Rocks/Rocks_Diffuse.png";
        private const string CanyonRocksNormalPath = CanyonTexturesRoot + "/Rocks/Rocks_Normal.png";
        private const string CanyonPlantsDiffusePath = CanyonTexturesRoot + "/plants_2/Plants_2_Diffuse.png";
        private const string CanyonPlantsNormalPath = CanyonTexturesRoot + "/plants_2/Plants_2_Normal.png";
        private const string PolyHavenRoot = Root + "/ThirdParty/PolyHaven";
        private const string PolyHavenSkyboxTexturePath = PolyHavenRoot + "/kloofendal_48d_partly_cloudy_puresky_1k.hdr";
        private const string PolyHavenSkyboxMaterialPath = Root + "/Materials/PolyHavenKloofendalSkybox.mat";
        private static readonly Vector3 WildTerrainPosition = new Vector3(-90f, -0.35f, -80f);
        private static readonly Vector3 WildTerrainSize = new Vector3(180f, 22f, 180f);
        private static readonly Vector2 LaunchCenter = new Vector2(0f, 2.25f);
        private static readonly string[] KenneyTreeModels =
        {
            "tree_pineTallA_detailed.fbx",
            "tree_pineTallB_detailed.fbx",
            "tree_pineTallC_detailed.fbx",
            "tree_pineRoundC.fbx",
            "tree_pineRoundE.fbx",
            "tree_oak.fbx",
            "tree_default.fbx"
        };
        private static readonly string[] KenneyRockModels =
        {
            "rock_largeA.fbx",
            "rock_largeC.fbx",
            "rock_largeF.fbx",
            "rock_tallB.fbx",
            "rock_tallH.fbx",
            "rock_smallFlatA.fbx",
            "rock_smallFlatC.fbx"
        };
        private static readonly string[] KenneyGroundModels =
        {
            "plant_bushDetailed.fbx",
            "plant_bushLarge.fbx",
            "grass_large.fbx",
            "grass_leafsLarge.fbx",
            "flower_yellowB.fbx",
            "flower_purpleB.fbx",
            "mushroom_redGroup.fbx"
        };
        private static readonly string[] CanyonFormationModels =
        {
            "Rock_Formations_1.fbx",
            "Rock_Formations_3.fbx",
            "Rock_Formations_5.fbx",
            "Rock_Formations_8.fbx",
            "Rock_Formations_11.fbx",
            "Rock_Formations_12.fbx",
            "Rock_Platforms_2.fbx",
            "Rock_Platforms_5.fbx",
            "Rock_Platforms_7.fbx",
            "Rock_Platforms_10.fbx"
        };
        private static readonly string[] CanyonBoulderModels =
        {
            "Rock_Boulders_1.fbx",
            "Rock_Boulders_2.fbx",
            "Rock_Boulders_3.fbx",
            "Rock_Boulders_5.fbx",
            "Rock_Boulders_6.fbx"
        };
        private static readonly string[] CanyonPlantModels =
        {
            "Cactus_Tall_1.fbx",
            "Cactus_Tall_3.fbx",
            "Cactus_Tall_5.fbx",
            "Cactus_Small_1.fbx",
            "Cactus_Small_4.fbx",
            "Grass_1.fbx",
            "Grass_4.fbx",
            "Grass_7.fbx",
            "Leafs_1.fbx",
            "Sticks_1.fbx"
        };

        [MenuItem("Drone MicroClass/Install Demo Runtime In Current Scene")]
        public static void InstallDemoRuntimeInCurrentScene()
        {
            CreateFolders();

            GameObject drone = GameObject.Find("TeachingDrone_On_TakeoffPad") ?? GameObject.Find("TeachingDrone");
            if (drone == null)
            {
                Debug.LogError("Could not install demo runtime: no TeachingDrone_On_TakeoffPad or TeachingDrone object found in the current scene.");
                return;
            }

            var flight = drone.GetComponent<SimpleFlightController>();
            var fleet = drone.GetComponent<DroneFleetManager>();
            if (flight == null || fleet == null)
            {
                Debug.LogError("Could not install demo runtime: target drone needs SimpleFlightController and DroneFleetManager components.");
                return;
            }

            if (fleet.VariantCount == 0)
            {
                DroneProfile[] profiles = CreateProfiles();
                DroneFleetManager.DroneVariant[] variants = CreateVariants(profiles);
                Transform visualRoot = drone.transform.Find("Visual Root") ?? drone.transform;
                fleet.Configure(flight, visualRoot, variants, 0);
                fleet.PreviewVariant(0);
            }

            DisableFixedSceneCameras();
            DestroySceneObject("Follow Camera");
            DestroySceneObject("Nose Feed Camera");
            DestroySceneObject("Flight HUD");

            EnsureRuntimeSettings();

            GameObject cameraObject = new GameObject("Follow Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = drone.transform.position + new Vector3(0f, 3.8f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 260f;
            camera.fieldOfView = 58f;
            ConfigureUrpPostProcessing(camera);
            var cameraRig = cameraObject.AddComponent<DroneCameraRig>();
            cameraRig.Configure(drone.transform, fleet, DroneCameraRig.CameraMode.Chase, true);

            RenderTexture noseTexture = CreateOrUpdateNoseRenderTexture();
            GameObject noseCameraObject = new GameObject("Nose Feed Camera");
            var noseCamera = noseCameraObject.AddComponent<Camera>();
            noseCamera.targetTexture = noseTexture;
            noseCamera.fieldOfView = 72f;
            noseCamera.nearClipPlane = 0.03f;
            noseCamera.farClipPlane = 260f;
            noseCamera.depth = -2f;
            ConfigureNoseFeedRendering(noseCamera);
            var noseRig = noseCameraObject.AddComponent<DroneCameraRig>();
            noseRig.Configure(drone.transform, fleet, DroneCameraRig.CameraMode.Nose, false);

            CreateReusableHud(flight, fleet, cameraRig, noseTexture);
            EnsureEventSystem();

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Installed demo camera and HUD runtime in scene: " + activeScene.path);
        }

        [MenuItem("Drone MicroClass/Apply DJI Main Drone And City Background")]
        public static void ApplyDjiMainDroneAndCityBackground()
        {
            CreateFolders();
            AssetDatabase.Refresh();

            GameObject djiVisual = AssetDatabase.LoadAssetAtPath<GameObject>(DjiMainDronePath);
            if (djiVisual == null)
            {
                Debug.LogError("Could not apply DJI visual: missing model at " + DjiMainDronePath);
                return;
            }

            GameObject cityBackground = AssetDatabase.LoadAssetAtPath<GameObject>(CityBackgroundPath);
            if (cityBackground == null)
            {
                Debug.LogWarning("City background model is not imported yet: " + CityBackgroundPath);
            }

            ApplyDjiVisualToTeachingDronePrefab(djiVisual);
            EnsureHudPrefabHasMiniMap();

            string[] scenePaths = FindDroneMicroClassScenePaths();
            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ApplyDjiVisualToLoadedSceneDrones(djiVisual);

                if (scenePath == FigureEightScenePath)
                {
                    EnsureCityBackgroundInCurrentScene(cityBackground);
                }

                EnsureHudMiniMapInCurrentScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (File.Exists(FigureEightScenePath))
            {
                EditorSceneManager.OpenScene(FigureEightScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Applied DJI main visual and city background. " + BuildDjiRotorReport(djiVisual));
        }

        private static void CreateFolders()
        {
            EnsureFolder("Assets", "DroneMicroClass");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Editor");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Profiles");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Terrain");
            EnsureFolder(Root, "RenderTextures");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Models");
            EnsureFolder(Root + "/Models", "Drones");
            EnsureFolder(Root + "/Models/Drones", "DJI");
            EnsureFolder(Root + "/Models", "Environment");
            EnsureFolder(Root, "ThirdParty");
            EnsureFolder(Root + "/ThirdParty", "KenneyNatureKit");
            EnsureFolder(KenneyNatureRoot, "Models");
            EnsureFolder(Root + "/ThirdParty", "ambientCG");
            EnsureFolder(AmbientCgRoot, "Grass001");
            EnsureFolder(AmbientCgRoot, "Ground039");
            EnsureFolder(AmbientCgRoot, "Rock003");
            EnsureFolder(Root + "/ThirdParty", "CanyonTerrain");
            EnsureFolder(CanyonTerrainRoot, "Models");
            EnsureFolder(CanyonTerrainRoot, "Textures");
            EnsureFolder(Root + "/ThirdParty", "PolyHaven");
        }

        private static void ApplyDjiVisualToTeachingDronePrefab(GameObject djiVisual)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var fleet = prefabRoot.GetComponent<DroneFleetManager>();
                if (fleet == null)
                {
                    Debug.LogWarning("TeachingDrone prefab has no DroneFleetManager: " + PrefabPath);
                    return;
                }

                UpdateMainVariantVisual(fleet, djiVisual);
                fleet.PreviewVariant(0);
                ApplyDjiPersistentMaterials(fleet);
                PersistRotorVisualBindings(fleet);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ApplyDjiVisualToLoadedSceneDrones(GameObject djiVisual)
        {
            DroneFleetManager[] fleets = Object.FindObjectsByType<DroneFleetManager>(FindObjectsSortMode.None);
            foreach (DroneFleetManager fleet in fleets)
            {
                UpdateMainVariantVisual(fleet, djiVisual);
                fleet.PreviewVariant(0);
                ApplyDjiPersistentMaterials(fleet);
                PersistRotorVisualBindings(fleet);
                EditorUtility.SetDirty(fleet);
            }
        }

        private static void UpdateMainVariantVisual(DroneFleetManager fleet, GameObject djiVisual)
        {
            var serializedFleet = new SerializedObject(fleet);
            SerializedProperty variants = serializedFleet.FindProperty("variants");
            if (variants == null)
            {
                return;
            }

            if (variants.arraySize == 0)
            {
                DroneProfile[] profiles = CreateProfiles();
                Transform visualRoot = fleet.transform.Find("Visual Root") ?? fleet.transform;
                fleet.Configure(fleet.GetComponent<SimpleFlightController>(), visualRoot, CreateVariants(profiles), 0);
                serializedFleet.Update();
                variants = serializedFleet.FindProperty("variants");
            }

            if (variants == null || variants.arraySize == 0)
            {
                return;
            }

            DroneProfile quadProfile = AssetDatabase.LoadAssetAtPath<DroneProfile>(QuadProfilePath);
            SerializedProperty mainVariant = variants.GetArrayElementAtIndex(0);
            mainVariant.FindPropertyRelative("displayName").stringValue = "DJI Training Balanced";
            if (quadProfile != null)
            {
                mainVariant.FindPropertyRelative("profile").objectReferenceValue = quadProfile;
            }

            mainVariant.FindPropertyRelative("visualPrefab").objectReferenceValue = djiVisual;
            mainVariant.FindPropertyRelative("visualTargetSize").floatValue = 1.55f;
            mainVariant.FindPropertyRelative("visualRotationEuler").vector3Value = new Vector3(0f, 180f, 0f);
            mainVariant.FindPropertyRelative("noseCameraLocalPosition").vector3Value = new Vector3(0f, 0.2f, 1f);
            mainVariant.FindPropertyRelative("noseCameraLocalEuler").vector3Value = Vector3.zero;
            mainVariant.FindPropertyRelative("levelNoseCamera").boolValue = true;
            mainVariant.FindPropertyRelative("levelNoseCameraSharpness").floatValue = 24f;
            mainVariant.FindPropertyRelative("supportsGimbalPitch").boolValue = true;
            mainVariant.FindPropertyRelative("plasticShellMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingShell.mat");
            mainVariant.FindPropertyRelative("plasticAccentMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingPanel.mat");
            mainVariant.FindPropertyRelative("plasticDarkMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingDarkParts.mat");
            mainVariant.FindPropertyRelative("plasticRotorMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingPropeller.mat");
            mainVariant.FindPropertyRelative("plasticLensMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingLens.mat");
            serializedFleet.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PersistRotorVisualBindings(DroneFleetManager fleet)
        {
            var flight = fleet.GetComponent<SimpleFlightController>();
            if (flight == null)
            {
                return;
            }

            Transform visualRoot = fleet.transform.Find("Visual Root") ?? fleet.transform;
            Transform activeVisual = visualRoot.Find("Active Visual - DJI Training Balanced");
            Transform[] rotors = FindRotorTransforms(activeVisual != null ? activeVisual : visualRoot);
            var serializedFlight = new SerializedObject(flight);
            SerializedProperty rotorVisuals = serializedFlight.FindProperty("rotorVisuals");
            if (rotorVisuals == null)
            {
                return;
            }

            rotorVisuals.arraySize = rotors.Length;
            for (int i = 0; i < rotors.Length; i++)
            {
                rotorVisuals.GetArrayElementAtIndex(i).objectReferenceValue = rotors[i];
            }

            serializedFlight.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flight);
        }

        private static void ApplyDjiPersistentMaterials(DroneFleetManager fleet)
        {
            Transform visualRoot = fleet.transform.Find("Visual Root") ?? fleet.transform;
            Transform activeVisual = visualRoot.Find("Active Visual - DJI Training Balanced");
            if (activeVisual == null)
            {
                return;
            }

            Material shell = CreateMaterial("DJITrainingShell", new Color(0.68f, 0.72f, 0.72f));
            Material panel = CreateMaterial("DJITrainingPanel", new Color(0.38f, 0.43f, 0.46f));
            Material dark = CreateMaterial("DJITrainingDarkParts", new Color(0.09f, 0.10f, 0.11f));
            Material propeller = CreateMaterial("DJITrainingPropeller", new Color(0.15f, 0.16f, 0.17f));
            Material lens = CreateMaterial("DJITrainingLens", new Color(0.02f, 0.03f, 0.035f));

            Renderer[] renderers = activeVisual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string objectName = renderer.gameObject.name.ToLowerInvariant();
                if (objectName.Contains("\u6868\u53f6"))
                {
                    renderer.sharedMaterial = propeller;
                }
                else if (objectName.Contains("glass") || objectName.Contains("black") || objectName.Contains("\u7403\u4f53"))
                {
                    renderer.sharedMaterial = lens;
                }
                else if (objectName.Contains("jt") || objectName.Contains("\u6324\u538b"))
                {
                    renderer.sharedMaterial = dark;
                }
                else if (objectName.Contains("\u673a\u8eab"))
                {
                    renderer.sharedMaterial = shell;
                }
                else
                {
                    renderer.sharedMaterial = panel;
                }

                EditorUtility.SetDirty(renderer);
            }
        }

        private static string[] FindDroneMicroClassScenePaths()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { Root + "/Scenes" });
            var paths = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(System.StringComparer.Ordinal);
            return paths.ToArray();
        }

        private static void EnsureCityBackgroundInCurrentScene(GameObject cityBackground)
        {
            if (cityBackground == null)
            {
                return;
            }

            DestroySceneObjects("City_Background_Model");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(cityBackground);
            instance.name = "City_Background_Model";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            RemoveCollidersInChildren(instance);
        }

        private static string BuildDjiRotorReport(GameObject djiVisual)
        {
            GameObject probe = null;
            try
            {
                probe = (GameObject)PrefabUtility.InstantiatePrefab(djiVisual);
                probe.hideFlags = HideFlags.HideAndDontSave;
                Transform[] rotorCandidates = FindRotorTransforms(probe.transform);
                Bounds bounds = CalculateRendererBounds(probe);
                int sampleCount = Mathf.Min(rotorCandidates.Length, 8);
                var sampleNames = new string[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    sampleNames[i] = rotorCandidates[i].name;
                }

                DroneProfile profile = AssetDatabase.LoadAssetAtPath<DroneProfile>(QuadProfilePath);
                Vector3 axis = profile != null ? profile.rotorVisualAxis : Vector3.up;
                return string.Format(
                    "DJI rotor check: {0} movable mesh transform candidates found, spin axis {1}, model bounds {2}, samples [{3}].",
                    rotorCandidates.Length,
                    axis,
                    bounds.size,
                    string.Join(", ", sampleNames));
            }
            finally
            {
                if (probe != null)
                {
                    Object.DestroyImmediate(probe);
                }
            }
        }

        private static void SetupUrp()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UrpRendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, UrpRendererPath);
            }

            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, UrpAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
            EditorUtility.SetDirty(QualitySettings.GetQualitySettings());
        }

        private static DroneProfile[] CreateProfiles()
        {
            return new[]
            {
                CreateProfile(
                    QuadProfilePath,
                    "DJI Training Balanced",
                    "DJI-BAL",
                    "Balanced DJI host aircraft: neutral classroom handling, solid altitude hold, and enough response for normal maneuver training without racing drift.",
                    mass: 1.55f,
                    maxLiftForce: 39f,
                    pitchTorque: 3.8f,
                    rollTorque: 3.8f,
                    yawTorque: 1.5f,
                    maxTiltAngle: 20f,
                    linearDamping: 0.45f,
                    angularDamping: 2f,
                    throttleResponse: 0.7f,
                    angularRateDamping: 0.38f,
                    cyclicSensitivity: 1.02f,
                    yawSensitivity: 0.96f,
                    verticalSensitivity: 1f,
                    inputResponseSpeed: 8f,
                    attitudeTorqueScale: 1.08f,
                    activeInputBrakeScale: 0.16f,
                    maxAngularVelocity: 7.2f,
                    hoverBrakeAcceleration: 3f,
                    maxHoverBrakeForce: 13f,
                    altitudeChangeSpeed: 2.35f,
                    altitudePid: new PidGains(9.2f, 0.26f, 5.3f, 24f),
                    pitchPid: new PidGains(0.205f, 0f, 0.044f, 4.3f),
                    rollPid: new PidGains(0.205f, 0f, 0.044f, 4.3f),
                    rotorVisualSpeed: 8800f,
                    rotorVisualAxis: Vector3.up,
                    rotorGroundSpinRatio: 0.32f,
                    rotorFlightSpinRatio: 0.84f,
                    rotorGroundSpinupSpeed: 2.4f,
                    rotorFlightSpinupSpeed: 12f,
                    rotorStrobeSampleCount: 6,
                    rotorStrobeStartRatio: 0.68f,
                    rotorStrobeWobbleDegrees: 4f),
                CreateProfile(
                    InspireProfilePath,
                    "DJI Inspire Payload",
                    "CARGO",
                    "DJI Normal-mode tune for the larger Inspire body: smooth stick response, useful tilt authority, firm GPS braking, and stable altitude changes without a racing feel.",
                    mass: 3.45f,
                    maxLiftForce: 78f,
                    pitchTorque: 3.1f,
                    rollTorque: 3.1f,
                    yawTorque: 1.05f,
                    maxTiltAngle: 24f,
                    linearDamping: 0.72f,
                    angularDamping: 3.2f,
                    throttleResponse: 0.68f,
                    angularRateDamping: 0.62f,
                    cyclicSensitivity: 0.92f,
                    yawSensitivity: 0.78f,
                    verticalSensitivity: 0.9f,
                    inputResponseSpeed: 7.2f,
                    attitudeTorqueScale: 1.02f,
                    activeInputBrakeScale: 0.28f,
                    maxAngularVelocity: 5.6f,
                    hoverBrakeAcceleration: 5.6f,
                    maxHoverBrakeForce: 42f,
                    altitudeChangeSpeed: 2.2f,
                    altitudePid: new PidGains(15.8f, 0.55f, 9.8f, 42f),
                    pitchPid: new PidGains(0.165f, 0f, 0.072f, 3.6f),
                    rollPid: new PidGains(0.165f, 0f, 0.072f, 3.6f),
                    rotorVisualSpeed: 7600f,
                    rotorVisualAxis: Vector3.forward,
                    rotorGroundSpinRatio: 0.34f,
                    rotorFlightSpinRatio: 0.82f,
                    rotorGroundSpinupSpeed: 2f,
                    rotorFlightSpinupSpeed: 10f,
                    rotorStrobeSampleCount: 6,
                    rotorStrobeStartRatio: 0.66f,
                    rotorStrobeWobbleDegrees: 4f),
                CreateProfile(
                    RedProfilePath,
                    "Red Guard Agile",
                    "AGILE",
                    "Agile tune for the smallest body: high tilt authority, fast input response, tamed yaw, low braking, and light damping for nimble maneuver practice.",
                    mass: 0.62f,
                    maxLiftForce: 42f,
                    pitchTorque: 7.4f,
                    rollTorque: 7.4f,
                    yawTorque: 2.2f,
                    maxTiltAngle: 38f,
                    linearDamping: 0.12f,
                    angularDamping: 0.65f,
                    throttleResponse: 1.45f,
                    angularRateDamping: 0.42f,
                    cyclicSensitivity: 1.45f,
                    yawSensitivity: 0.85f,
                    verticalSensitivity: 1.35f,
                    inputResponseSpeed: 15f,
                    attitudeTorqueScale: 1.5f,
                    activeInputBrakeScale: 0.04f,
                    maxAngularVelocity: 10.5f,
                    hoverBrakeAcceleration: 1.2f,
                    maxHoverBrakeForce: 4.8f,
                    altitudeChangeSpeed: 4.6f,
                    altitudePid: new PidGains(9.2f, 0.12f, 3.2f, 30f),
                    pitchPid: new PidGains(0.34f, 0f, 0.03f, 8.4f),
                    rollPid: new PidGains(0.34f, 0f, 0.03f, 8.4f),
                    rotorVisualSpeed: 8000f,
                    rotorVisualAxis: Vector3.forward,
                    rotorGroundSpinRatio: 0.32f,
                    rotorFlightSpinRatio: 0.83f,
                    rotorGroundSpinupSpeed: 2.2f,
                    rotorFlightSpinupSpeed: 10.5f,
                    rotorStrobeSampleCount: 6,
                    rotorStrobeStartRatio: 0.67f,
                    rotorStrobeWobbleDegrees: 4f)
            };
        }

        private static DroneProfile CreateProfile(
            string path,
            string displayName,
            string modelCode,
            string handlingNotes,
            float mass,
            float maxLiftForce,
            float pitchTorque,
            float rollTorque,
            float yawTorque,
            float maxTiltAngle,
            float linearDamping,
            float angularDamping,
            float throttleResponse,
            float angularRateDamping,
            float cyclicSensitivity,
            float yawSensitivity,
            float verticalSensitivity,
            float inputResponseSpeed,
            float attitudeTorqueScale,
            float activeInputBrakeScale,
            float maxAngularVelocity,
            float hoverBrakeAcceleration,
            float maxHoverBrakeForce,
            float altitudeChangeSpeed,
            PidGains altitudePid,
            PidGains pitchPid,
            PidGains rollPid,
            float rotorVisualSpeed,
            Vector3 rotorVisualAxis,
            float rotorGroundSpinRatio,
            float rotorFlightSpinRatio,
            float rotorGroundSpinupSpeed,
            float rotorFlightSpinupSpeed,
            int rotorStrobeSampleCount,
            float rotorStrobeStartRatio,
            float rotorStrobeWobbleDegrees)
        {
            var profile = AssetDatabase.LoadAssetAtPath<DroneProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DroneProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.displayName = displayName;
            profile.modelCode = modelCode;
            profile.handlingNotes = handlingNotes;
            profile.mass = mass;
            profile.maxLiftForce = maxLiftForce;
            profile.pitchTorque = pitchTorque;
            profile.rollTorque = rollTorque;
            profile.yawTorque = yawTorque;
            profile.maxTiltAngle = maxTiltAngle;
            profile.linearDamping = linearDamping;
            profile.angularDamping = angularDamping;
            profile.throttleResponse = throttleResponse;
            profile.angularRateDamping = angularRateDamping;
            profile.cyclicSensitivity = cyclicSensitivity;
            profile.yawSensitivity = yawSensitivity;
            profile.verticalSensitivity = verticalSensitivity;
            profile.inputResponseSpeed = inputResponseSpeed;
            profile.attitudeTorqueScale = attitudeTorqueScale;
            profile.activeInputBrakeScale = activeInputBrakeScale;
            profile.maxAngularVelocity = maxAngularVelocity;
            profile.hoverBrakeAcceleration = hoverBrakeAcceleration;
            profile.maxHoverBrakeForce = maxHoverBrakeForce;
            profile.altitudeChangeSpeed = altitudeChangeSpeed;
            profile.altitudePid = altitudePid;
            profile.pitchPid = pitchPid;
            profile.rollPid = rollPid;
            profile.rotorVisualSpeed = rotorVisualSpeed;
            profile.rotorVisualAxis = rotorVisualAxis.sqrMagnitude > 0.0001f ? rotorVisualAxis.normalized : Vector3.up;
            profile.rotorGroundSpinRatio = Mathf.Clamp01(rotorGroundSpinRatio);
            profile.rotorFlightSpinRatio = Mathf.Clamp01(rotorFlightSpinRatio);
            profile.rotorGroundSpinupSpeed = Mathf.Max(0.1f, rotorGroundSpinupSpeed);
            profile.rotorFlightSpinupSpeed = Mathf.Max(0.1f, rotorFlightSpinupSpeed);
            profile.rotorStrobeSampleCount = Mathf.Max(0, rotorStrobeSampleCount);
            profile.rotorStrobeStartRatio = Mathf.Clamp01(rotorStrobeStartRatio);
            profile.rotorStrobeWobbleDegrees = Mathf.Max(0f, rotorStrobeWobbleDegrees);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static DroneFleetManager.DroneVariant[] CreateVariants(DroneProfile[] profiles)
        {
            return new[]
            {
                new DroneFleetManager.DroneVariant
                {
                    displayName = profiles[0].displayName,
                    profile = profiles[0],
                    visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DjiMainDronePath) ?? AssetDatabase.LoadAssetAtPath<GameObject>(QuadRacerPath),
                    visualTargetSize = 1.55f,
                    visualRotationEuler = new Vector3(0f, 180f, 0f),
                    noseCameraLocalPosition = new Vector3(0f, 0.2f, 1f),
                    levelNoseCamera = true,
                    levelNoseCameraSharpness = 24f,
                    supportsGimbalPitch = true,
                    plasticShellMaterial = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingShell.mat"),
                    plasticAccentMaterial = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingPanel.mat"),
                    plasticDarkMaterial = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingDarkParts.mat"),
                    plasticRotorMaterial = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingPropeller.mat"),
                    plasticLensMaterial = AssetDatabase.LoadAssetAtPath<Material>(DjiMaterialRoot + "/DJITrainingLens.mat")
                },
                new DroneFleetManager.DroneVariant
                {
                    displayName = profiles[1].displayName,
                    profile = profiles[1],
                    visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InspirePath),
                    visualTargetSize = 1.75f,
                    visualRotationEuler = new Vector3(0f, 180f, 0f),
                    noseCameraLocalPosition = new Vector3(0f, 0.22f, 1.12f),
                    levelNoseCamera = true,
                    levelNoseCameraSharpness = 24f,
                    supportsGimbalPitch = true,
                    plasticShellMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/InspirePlasticShell.mat"),
                    plasticAccentMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/InspirePlasticAccent.mat"),
                    plasticDarkMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/InspirePlasticDark.mat"),
                    plasticRotorMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/InspirePlasticRotor.mat"),
                    plasticLensMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/InspirePlasticLens.mat")
                },
                new DroneFleetManager.DroneVariant
                {
                    displayName = profiles[2].displayName,
                    profile = profiles[2],
                    visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedDronePath),
                    visualTargetSize = 1.45f,
                    visualRotationEuler = new Vector3(0f, 180f, 0f),
                    noseCameraLocalPosition = new Vector3(0f, 0.16f, 0.92f),
                    levelNoseCamera = false,
                    levelNoseCameraSharpness = 24f,
                    supportsGimbalPitch = false,
                    plasticShellMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/RedGuardPlasticShell.mat"),
                    plasticAccentMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/RedGuardPlasticAccent.mat"),
                    plasticDarkMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/RedGuardPlasticDark.mat"),
                    plasticRotorMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/RedGuardPlasticRotor.mat"),
                    plasticLensMaterial = AssetDatabase.LoadAssetAtPath<Material>(VariantMaterialRoot + "/RedGuardPlasticLens.mat")
                }
            };
        }

        private static GameObject CreateDronePrefab(DroneProfile profile, DroneFleetManager.DroneVariant[] variants)
        {
            Material bodyMaterial = CreateMaterial("DroneBody", new Color(0.1f, 0.35f, 0.8f));
            Material armMaterial = CreateMaterial("DroneArm", new Color(0.12f, 0.12f, 0.14f));
            Material rotorMaterial = CreateMaterial("Rotor", new Color(0.02f, 0.02f, 0.025f));

            GameObject root = new GameObject("TeachingDrone");
            root.transform.position = new Vector3(0f, 1.2f, 0f);
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = profile.mass;
            rb.linearDamping = profile.linearDamping;
            rb.angularDamping = profile.angularDamping;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var flight = root.AddComponent<SimpleFlightController>();
            flight.SetProfile(profile);
            var controller = root.AddComponent<DroneController>();

            GameObject body = CreatePrimitiveChild(root.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.75f, 0.16f, 0.48f), bodyMaterial);
            GameObject armX = CreatePrimitiveChild(root.transform, "Arm_X", PrimitiveType.Cube, Vector3.zero, new Vector3(1.5f, 0.06f, 0.08f), armMaterial);
            GameObject armZ = CreatePrimitiveChild(root.transform, "Arm_Z", PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.06f, 1.5f), armMaterial);
            GameObject visualRoot = new GameObject("Visual Root");
            visualRoot.transform.SetParent(root.transform, false);

            Transform[] rotors =
            {
                CreateRotor(root.transform, "Rotor_FL", new Vector3(-0.75f, 0.07f, 0.75f), rotorMaterial),
                CreateRotor(root.transform, "Rotor_FR", new Vector3(0.75f, 0.07f, 0.75f), rotorMaterial),
                CreateRotor(root.transform, "Rotor_BL", new Vector3(-0.75f, 0.07f, -0.75f), rotorMaterial),
                CreateRotor(root.transform, "Rotor_BR", new Vector3(0.75f, 0.07f, -0.75f), rotorMaterial),
            };

            SetRendererVisible(body, false);
            SetRendererVisible(armX, false);
            SetRendererVisible(armZ, false);
            foreach (Transform rotor in rotors)
            {
                SetRendererVisible(rotor.gameObject, false);
            }

            SetPrivateField(flight, "rotorVisuals", rotors);
            SetPrivateField(controller, "flightController", flight);
            var fleet = root.AddComponent<DroneFleetManager>();
            fleet.Configure(flight, visualRoot.transform, variants, 0);
            fleet.PreviewVariant(0);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureWildLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.68f, 0.78f, 0.86f);
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.ambientIntensity = 0.8f;
        }

        private static void CreateRuntimeSettings()
        {
            GameObject settings = new GameObject("Flight Runtime Settings");
            settings.AddComponent<FlightRuntimeSettings>();
        }

        private static void EnsureRuntimeSettings()
        {
            if (Object.FindFirstObjectByType<FlightRuntimeSettings>() != null)
            {
                return;
            }

            CreateRuntimeSettings();
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            CreateEventSystem();
        }

        private static void ConfigureUrpPostProcessing(Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = LayerMask.GetMask("Default");
        }

        private static void ConfigureNoseFeedRendering(Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            camera.allowHDR = false;
            cameraData.renderPostProcessing = false;
            cameraData.volumeLayerMask = 0;
        }

        private static void DisableFixedSceneCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera sceneCamera in cameras)
            {
                if (sceneCamera == null || sceneCamera.gameObject.name == "Follow Camera" || sceneCamera.gameObject.name == "Nose Feed Camera")
                {
                    continue;
                }

                sceneCamera.enabled = false;
                if (sceneCamera.CompareTag("MainCamera"))
                {
                    sceneCamera.gameObject.tag = "Untagged";
                }
            }
        }

        private static void DestroySceneObject(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void DestroySceneObjects(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            while (existing != null)
            {
                Object.DestroyImmediate(existing);
                existing = GameObject.Find(objectName);
            }
        }

        private static void ApplyImportedSkybox()
        {
            Material skybox = CreatePolyHavenSkybox();
            if (skybox == null)
            {
                skybox = AssetDatabase.LoadAssetAtPath<Material>(ImportedRoot + "/Skybox/Materials/Skybox_Daytime.mat");
            }

            if (skybox == null)
            {
                skybox = AssetDatabase.LoadAssetAtPath<Material>(ImportedRoot + "/Skybox/Materials/Skybox_Sunset.mat");
            }

            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                DynamicGI.UpdateEnvironment();
            }
        }

        private static Material CreatePolyHavenSkybox()
        {
            Texture skyTexture = AssetDatabase.LoadAssetAtPath<Texture>(PolyHavenSkyboxTexturePath);
            Shader skyShader = Shader.Find("Skybox/Panoramic");
            if (skyTexture == null || skyShader == null)
            {
                return null;
            }

            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(PolyHavenSkyboxMaterialPath);
            if (skybox == null)
            {
                skybox = new Material(skyShader);
                AssetDatabase.CreateAsset(skybox, PolyHavenSkyboxMaterialPath);
            }

            skybox.shader = skyShader;
            if (skybox.HasProperty("_MainTex"))
            {
                skybox.SetTexture("_MainTex", skyTexture);
            }

            if (skybox.HasProperty("_Exposure"))
            {
                skybox.SetFloat("_Exposure", 1.05f);
            }

            if (skybox.HasProperty("_Rotation"))
            {
                skybox.SetFloat("_Rotation", 32f);
            }

            EditorUtility.SetDirty(skybox);
            return skybox;
        }

        private static void CreateWildEnvironment(
            Material clearingMaterial,
            Material waterMaterial,
            Material rockMaterial,
            Material trunkMaterial,
            Material foliageMaterial,
            Material grassPatchMaterial)
        {
            GameObject root = new GameObject("Wild Valley Environment");
            TerrainData terrainData = CreateOrUpdateWildTerrainData();
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Wild Valley Terrain";
            terrainObject.transform.SetParent(root.transform, true);
            terrainObject.transform.position = WildTerrainPosition;

            var terrain = terrainObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.drawInstanced = true;
                terrain.heightmapPixelError = 4f;
                terrain.basemapDistance = 120f;
                terrain.detailObjectDistance = 70f;
            }

            GameObject clearing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clearing.name = "Launch Clearing";
            clearing.transform.SetParent(root.transform, true);
            float launchGroundY = GetWildTerrainWorldY(LaunchCenter.x, LaunchCenter.y);
            clearing.transform.position = new Vector3(LaunchCenter.x, launchGroundY + 0.014f, LaunchCenter.y);
            clearing.transform.localScale = new Vector3(5.2f, 0.012f, 4.2f);
            AssignMaterial(clearing, clearingMaterial);
            RemoveCollider(clearing);

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Valley Pond";
            water.transform.SetParent(root.transform, true);
            water.transform.position = new Vector3(-28f, 0.02f, 22f);
            water.transform.localScale = new Vector3(15f, 0.02f, 8f);
            AssignMaterial(water, waterMaterial);
            RemoveCollider(water);

            CreateTreeScatter(root.transform, trunkMaterial, foliageMaterial);
            CreateRockScatter(root.transform, rockMaterial);
            CreateGrassScatter(root.transform, grassPatchMaterial);
            CreateKenneyNatureScatter(root.transform);
            CreateCanyonTerrainScatter(root.transform);
        }

        private static TerrainData CreateOrUpdateWildTerrainData()
        {
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(WildTerrainPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, WildTerrainPath);
            }

            const int resolution = 257;
            terrainData.heightmapResolution = resolution;
            terrainData.alphamapResolution = 128;
            terrainData.size = WildTerrainSize;
            terrainData.terrainLayers = CreateWildTerrainLayers();

            float[,] heights = new float[resolution, resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = y / (float)(resolution - 1);
                    heights[y, x] = GetWildTerrainHeight01(nx, nz);
                }
            }

            terrainData.SetHeights(0, 0, heights);
            PaintWildTerrain(terrainData);
            EditorUtility.SetDirty(terrainData);
            return terrainData;
        }

        private static TerrainLayer[] CreateWildTerrainLayers()
        {
            return new[]
            {
                CreateTerrainLayer(GrassLayerPath, GrassTexturePath, new Color(0.29f, 0.48f, 0.19f), new Vector2(9f, 9f), AmbientGrassColorPath, AmbientGrassNormalPath),
                CreateTerrainLayer(DirtLayerPath, DirtTexturePath, new Color(0.42f, 0.35f, 0.22f), new Vector2(7f, 7f), AmbientGroundColorPath, AmbientGroundNormalPath),
                CreateTerrainLayer(RockLayerPath, RockTexturePath, new Color(0.38f, 0.38f, 0.35f), new Vector2(13f, 13f), AmbientRockColorPath, AmbientRockNormalPath)
            };
        }

        private static TerrainLayer CreateTerrainLayer(string layerPath, string fallbackTexturePath, Color fallbackColor, Vector2 tileSize, string diffusePath, string normalPath)
        {
            Texture2D diffuse = LoadRepeatingTexture(diffusePath, false) ?? CreateFlatColorTexture(fallbackTexturePath, fallbackColor);
            Texture2D normal = LoadRepeatingTexture(normalPath, true);
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            layer.diffuseTexture = diffuse;
            layer.normalMapTexture = normal;
            layer.normalScale = normal != null ? 1f : 0f;
            layer.tileSize = tileSize;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Texture2D LoadRepeatingTexture(string assetPath, bool normalMap)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return null;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.wrapMode != TextureWrapMode.Repeat)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    changed = true;
                }

                if (normalMap && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    changed = true;
                }

                if (!normalMap && !importer.sRGBTexture)
                {
                    importer.sRGBTexture = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            return texture;
        }

        private static Texture2D CreateFlatColorTexture(string path, Color color)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                AssetDatabase.CreateAsset(texture, path);
            }

            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                float tint = i % 2 == 0 ? 1f : 0.88f;
                pixels[i] = new Color(color.r * tint, color.g * tint, color.b * tint, color.a);
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static void PaintWildTerrain(TerrainData terrainData)
        {
            int resolution = terrainData.alphamapResolution;
            float[,,] alphas = new float[resolution, resolution, 3];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = y / (float)(resolution - 1);
                    float height = GetWildTerrainHeight01(nx, nz);
                    Vector2 world = NormalizedToWildWorld(nx, nz);
                    float clearing = DistanceFalloff01(10f, Vector2.Distance(world, LaunchCenter));
                    float pond = DistanceFalloff01(14f, Vector2.Distance(world, new Vector2(-28f, 22f)));
                    float rock = SmoothRange01(0.13f, 0.38f, height);
                    float dirt = Mathf.Clamp01(clearing + pond * 0.35f);
                    float grass = Mathf.Clamp01(1f - Mathf.Max(rock, dirt));
                    float total = grass + dirt + rock;
                    alphas[y, x, 0] = grass / total;
                    alphas[y, x, 1] = dirt / total;
                    alphas[y, x, 2] = rock / total;
                }
            }

            terrainData.SetAlphamaps(0, 0, alphas);
        }

        private static float GetWildTerrainHeight01(float nx, float nz)
        {
            Vector2 world = NormalizedToWildWorld(nx, nz);
            float rolling = Mathf.PerlinNoise(nx * 4.2f + 1.3f, nz * 4.2f + 7.1f) * 0.055f;
            float fine = Mathf.PerlinNoise(nx * 13.5f + 2.4f, nz * 13.5f + 9.6f) * 0.018f;
            float farRidge = SmoothRange01(0.58f, 1f, nz) * 0.36f;
            float sideRidge = SmoothRange01(0.42f, 0.92f, Mathf.Abs(nx - 0.5f)) * 0.2f;
            float pondBasin = DistanceFalloff01(18f, Vector2.Distance(world, new Vector2(-28f, 22f))) * 0.035f;
            float height = 0.018f + rolling + fine + farRidge + sideRidge - pondBasin;
            float clearing = DistanceFalloff01(13f, Vector2.Distance(world, LaunchCenter));
            height = Mathf.Lerp(height, 0.018f, clearing);
            return Mathf.Clamp01(height);
        }

        private static float SmoothRange01(float lower, float upper, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lower, upper, value));
        }

        private static float DistanceFalloff01(float radius, float distance)
        {
            return Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(distance / Mathf.Max(0.001f, radius)));
        }

        private static Vector2 NormalizedToWildWorld(float nx, float nz)
        {
            return new Vector2(
                WildTerrainPosition.x + nx * WildTerrainSize.x,
                WildTerrainPosition.z + nz * WildTerrainSize.z);
        }

        private static float GetWildTerrainWorldY(float worldX, float worldZ)
        {
            float nx = Mathf.InverseLerp(WildTerrainPosition.x, WildTerrainPosition.x + WildTerrainSize.x, worldX);
            float nz = Mathf.InverseLerp(WildTerrainPosition.z, WildTerrainPosition.z + WildTerrainSize.z, worldZ);
            return WildTerrainPosition.y + GetWildTerrainHeight01(nx, nz) * WildTerrainSize.y;
        }

        private static void CreateTreeScatter(Transform parent, Material trunkMaterial, Material foliageMaterial)
        {
            System.Random random = new System.Random(1427);
            for (int i = 0; i < 34; i++)
            {
                float x = RandomRange(random, -76f, 76f);
                float z = RandomRange(random, -56f, 88f);
                if (Vector2.Distance(new Vector2(x, z), LaunchCenter) < 15f || IsInClearFlightCorridor(x, z, 11f, -62f, 30f))
                {
                    i--;
                    continue;
                }

                CreatePineTree(parent, new Vector3(x, GetWildTerrainWorldY(x, z), z), RandomRange(random, 0.75f, 1.45f), trunkMaterial, foliageMaterial);
            }
        }

        private static void CreatePineTree(Transform parent, Vector3 position, float scale, Material trunkMaterial, Material foliageMaterial)
        {
            GameObject root = new GameObject("Pine Tree");
            root.transform.SetParent(parent, true);
            root.transform.position = position;

            GameObject trunk = CreatePrimitiveChild(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.55f * scale, 0f), new Vector3(0.12f * scale, 0.55f * scale, 0.12f * scale), trunkMaterial);
            RemoveCollider(trunk);
            GameObject canopyLow = CreatePrimitiveChild(root.transform, "Canopy Low", PrimitiveType.Sphere, new Vector3(0f, 1.05f * scale, 0f), new Vector3(0.78f * scale, 0.34f * scale, 0.78f * scale), foliageMaterial);
            RemoveCollider(canopyLow);
            GameObject canopyHigh = CreatePrimitiveChild(root.transform, "Canopy High", PrimitiveType.Sphere, new Vector3(0f, 1.45f * scale, 0f), new Vector3(0.56f * scale, 0.32f * scale, 0.56f * scale), foliageMaterial);
            RemoveCollider(canopyHigh);
        }

        private static void CreateRockScatter(Transform parent, Material rockMaterial)
        {
            System.Random random = new System.Random(3811);
            for (int i = 0; i < 24; i++)
            {
                float x = RandomRange(random, -70f, 70f);
                float z = RandomRange(random, -45f, 82f);
                if (Vector2.Distance(new Vector2(x, z), LaunchCenter) < 10f || IsInClearFlightCorridor(x, z, 8f, -42f, 24f))
                {
                    i--;
                    continue;
                }

                float size = RandomRange(random, 0.35f, 1.2f);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Field Rock";
                rock.transform.SetParent(parent, true);
                rock.transform.position = new Vector3(x, GetWildTerrainWorldY(x, z) + size * 0.18f, z);
                rock.transform.localScale = new Vector3(size * RandomRange(random, 0.9f, 1.7f), size * 0.34f, size * RandomRange(random, 0.8f, 1.35f));
                rock.transform.rotation = Quaternion.Euler(0f, RandomRange(random, 0f, 360f), RandomRange(random, -8f, 8f));
                AssignMaterial(rock, rockMaterial);
                RemoveCollider(rock);
            }
        }

        private static void CreateGrassScatter(Transform parent, Material grassPatchMaterial)
        {
            System.Random random = new System.Random(9133);
            for (int i = 0; i < 48; i++)
            {
                float x = RandomRange(random, -82f, 82f);
                float z = RandomRange(random, -65f, 92f);
                if (Vector2.Distance(new Vector2(x, z), LaunchCenter) < 7f || IsInClearFlightCorridor(x, z, 5f, -26f, 18f))
                {
                    i--;
                    continue;
                }

                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                patch.name = "Wild Grass Patch";
                patch.transform.SetParent(parent, true);
                patch.transform.position = new Vector3(x, GetWildTerrainWorldY(x, z) + 0.035f, z);
                patch.transform.localScale = new Vector3(RandomRange(random, 0.45f, 1.25f), 0.018f, RandomRange(random, 0.45f, 1.25f));
                patch.transform.rotation = Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f);
                AssignMaterial(patch, grassPatchMaterial);
                RemoveCollider(patch);
            }
        }

        private static void CreateKenneyNatureScatter(Transform parent)
        {
            GameObject root = new GameObject("CC0 Kenney Nature Props");
            root.transform.SetParent(parent, true);

            CreateKenneyModelScatter(root.transform, "Kenney Tree", KenneyTreeModels, 42, 2468, -82f, 82f, -62f, 94f, 17f, 12f, -64f, 34f, 1.2f, 2.6f, 0f);
            CreateKenneyModelScatter(root.transform, "Kenney Rock", KenneyRockModels, 30, 4192, -78f, 78f, -54f, 88f, 10f, 8f, -42f, 26f, 0.9f, 2.5f, 0.02f);
            CreateKenneyModelScatter(root.transform, "Kenney Ground Detail", KenneyGroundModels, 60, 7319, -84f, 84f, -66f, 94f, 7f, 5f, -30f, 20f, 0.7f, 1.6f, 0.01f);

            CreateKenneyModel("Kenney Bridge", "bridge_wood.fbx", root.transform, new Vector3(-28f, GetWildTerrainWorldY(-28f, 13f) + 0.08f, 13f), new Vector3(2.2f, 2.2f, 2.2f), Quaternion.Euler(0f, 4f, 0f));
            CreateKenneyModel("Kenney Canoe", "canoe.fbx", root.transform, new Vector3(-34.5f, 0.08f, 24f), new Vector3(1.8f, 1.8f, 1.8f), Quaternion.Euler(0f, 28f, 0f));
            CreateKenneyModel("Kenney Campfire", "campfire_logs.fbx", root.transform, new Vector3(7.4f, GetWildTerrainWorldY(7.4f, 9f) + 0.02f, 9f), new Vector3(1.3f, 1.3f, 1.3f), Quaternion.Euler(0f, 38f, 0f));
            CreateKenneyModel("Kenney Log Stack", "log_stackLarge.fbx", root.transform, new Vector3(10.5f, GetWildTerrainWorldY(10.5f, 7.5f) + 0.02f, 7.5f), new Vector3(1.2f, 1.2f, 1.2f), Quaternion.Euler(0f, -22f, 0f));
        }

        private static void CreateKenneyModelScatter(
            Transform parent,
            string namePrefix,
            string[] modelNames,
            int count,
            int seed,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float launchClearRadius,
            float corridorHalfWidth,
            float corridorMinZ,
            float corridorMaxZ,
            float minScale,
            float maxScale,
            float yOffset)
        {
            System.Random random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float x = RandomRange(random, minX, maxX);
                float z = RandomRange(random, minZ, maxZ);
                if (Vector2.Distance(new Vector2(x, z), LaunchCenter) < launchClearRadius || IsInClearFlightCorridor(x, z, corridorHalfWidth, corridorMinZ, corridorMaxZ))
                {
                    i--;
                    continue;
                }

                string modelName = modelNames[random.Next(modelNames.Length)];
                float scale = RandomRange(random, minScale, maxScale);
                Vector3 position = new Vector3(x, GetWildTerrainWorldY(x, z) + yOffset, z);
                Quaternion rotation = Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f);
                CreateKenneyModel(namePrefix, modelName, parent, position, Vector3.one * scale, rotation);
            }
        }

        private static GameObject CreateKenneyModel(string objectName, string modelName, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyNatureModelsRoot + "/" + modelName);
            if (asset == null)
            {
                Debug.LogWarning("Kenney Nature model missing: " + modelName);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = objectName + " - " + modelName.Replace(".fbx", string.Empty);
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            RemoveCollidersInChildren(instance);
            return instance;
        }

        private static void CreateCanyonTerrainScatter(Transform parent)
        {
            if (!HasAnyCanyonModel(CanyonFormationModels) && !HasAnyCanyonModel(CanyonBoulderModels))
            {
                return;
            }

            GameObject root = new GameObject("CC0 Canyon Terrain Props");
            root.transform.SetParent(parent, true);

            Material rockMaterial = CreateTexturedMaterial("CanyonRockPBR", CanyonRocksDiffusePath, CanyonRocksNormalPath, new Color(0.43f, 0.39f, 0.34f), 0.28f);
            Material wallMaterial = CreateTexturedMaterial("CanyonWallPBR", CanyonWallDiffusePath, CanyonWallNormalPath, new Color(0.47f, 0.39f, 0.31f), 0.22f);
            Material plantMaterial = CreateTexturedMaterial("CanyonPlantPBR", CanyonPlantsDiffusePath, CanyonPlantsNormalPath, new Color(0.26f, 0.45f, 0.24f), 0.38f);

            CreateCanyonBoundaryCliffs(root.transform, wallMaterial);
            CreateCanyonModelScatter(root.transform, "Canyon Boulder", CanyonBoulderModels, rockMaterial, 22, 1881, -82f, 82f, -58f, 92f, 13f, 8f, -38f, 26f, 0.8f, 2.2f, -0.02f, 0.16f);
            CreateCanyonModelScatter(root.transform, "Canyon Rock Formation", CanyonFormationModels, rockMaterial, 12, 5487, -84f, 84f, -64f, 96f, 18f, 11f, -48f, 36f, 2.4f, 5.8f, -0.08f, 0.24f);
            CreateCanyonModelScatter(root.transform, "Canyon Dry Plant", CanyonPlantModels, plantMaterial, 42, 9274, -86f, 86f, -66f, 96f, 10f, 6f, -32f, 22f, 0.65f, 1.8f, 0.02f, 0.03f);
        }

        private static bool HasAnyCanyonModel(string[] modelNames)
        {
            foreach (string modelName in modelNames)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(CanyonModelsRoot + "/" + modelName) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateCanyonBoundaryCliffs(Transform parent, Material material)
        {
            System.Random random = new System.Random(7781);
            for (int i = 0; i < 18; i++)
            {
                float x;
                float z;
                if (i < 12)
                {
                    x = RandomRange(random, -84f, 84f);
                    z = RandomRange(random, 72f, 98f);
                }
                else if (i < 21)
                {
                    x = RandomRange(random, -88f, -72f);
                    z = RandomRange(random, -60f, 92f);
                }
                else
                {
                    x = RandomRange(random, 72f, 88f);
                    z = RandomRange(random, -60f, 92f);
                }

                string modelName = CanyonFormationModels[random.Next(CanyonFormationModels.Length)];
                float targetSize = RandomRange(random, 4.5f, 10.5f);
                Quaternion rotation = Quaternion.Euler(RandomRange(random, -5f, 7f), RandomRange(random, 0f, 360f), RandomRange(random, -8f, 8f));
                CreateCanyonModel("Canyon Vista Ridge", modelName, parent, new Vector3(x, GetWildTerrainWorldY(x, z) - targetSize * 0.3f, z), targetSize, rotation, material);
            }
        }

        private static void CreateCanyonModelScatter(
            Transform parent,
            string namePrefix,
            string[] modelNames,
            Material material,
            int count,
            int seed,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float launchClearRadius,
            float corridorHalfWidth,
            float corridorMinZ,
            float corridorMaxZ,
            float minTargetSize,
            float maxTargetSize,
            float yOffset,
            float sinkRatio)
        {
            if (modelNames == null || modelNames.Length == 0)
            {
                return;
            }

            System.Random random = new System.Random(seed);
            int created = 0;
            int attempts = 0;
            while (created < count && attempts < count * 8)
            {
                attempts++;
                float x = RandomRange(random, minX, maxX);
                float z = RandomRange(random, minZ, maxZ);
                if (Vector2.Distance(new Vector2(x, z), LaunchCenter) < launchClearRadius || IsInClearFlightCorridor(x, z, corridorHalfWidth, corridorMinZ, corridorMaxZ))
                {
                    continue;
                }

                string modelName = modelNames[random.Next(modelNames.Length)];
                float targetSize = RandomRange(random, minTargetSize, maxTargetSize);
                Quaternion rotation = Quaternion.Euler(RandomRange(random, -4f, 5f), RandomRange(random, 0f, 360f), RandomRange(random, -5f, 5f));
                Vector3 position = new Vector3(x, GetWildTerrainWorldY(x, z) + yOffset - targetSize * sinkRatio, z);
                if (CreateCanyonModel(namePrefix, modelName, parent, position, targetSize, rotation, material) != null)
                {
                    created++;
                }
            }
        }

        private static GameObject CreateCanyonModel(string objectName, string modelName, Transform parent, Vector3 position, float targetSize, Quaternion rotation, Material material)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(CanyonModelsRoot + "/" + modelName);
            if (asset == null)
            {
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = objectName + " - " + modelName.Replace(".fbx", string.Empty);
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            ApplyMaterialToRenderers(instance, material);
            FitRendererToSize(instance, targetSize, 0f);
            RemoveCollidersInChildren(instance);
            return instance;
        }

        private static Material CreateTexturedMaterial(string name, string diffusePath, string normalPath, Color fallbackColor, float smoothness)
        {
            string materialPath = Root + "/Materials/" + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            Texture2D diffuse = LoadRepeatingTexture(diffusePath, false);
            if (diffuse != null)
            {
                SetMaterialTexture(material, "_BaseMap", "_MainTex", diffuse);
            }

            Texture2D normal = LoadRepeatingTexture(normalPath, true);
            if (normal != null)
            {
                SetMaterialTexture(material, "_BumpMap", "_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", fallbackColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", fallbackColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetMaterialTexture(Material material, string urpProperty, string standardProperty, Texture texture)
        {
            if (material.HasProperty(urpProperty))
            {
                material.SetTexture(urpProperty, texture);
            }
            else if (material.HasProperty(standardProperty))
            {
                material.SetTexture(standardProperty, texture);
            }
        }

        private static void ApplyMaterialToRenderers(GameObject target, Material material)
        {
            if (material == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
            }
        }
        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private static bool IsInClearFlightCorridor(float x, float z, float halfWidth, float minZ, float maxZ)
        {
            return Mathf.Abs(x) < halfWidth && z > minZ && z < maxZ;
        }

        private static RenderTexture CreateOrUpdateNoseRenderTexture()
        {
            var renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(NoseRenderTexturePath);
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(640, 360, 16, RenderTextureFormat.ARGB32);
                AssetDatabase.CreateAsset(renderTexture, NoseRenderTexturePath);
            }

            renderTexture.width = 640;
            renderTexture.height = 360;
            renderTexture.depth = 16;
            renderTexture.antiAliasing = 2;
            renderTexture.filterMode = FilterMode.Bilinear;

            var serializedTexture = new SerializedObject(renderTexture);
            SerializedProperty sRgbProperty = serializedTexture.FindProperty("m_SRGB");
            if (sRgbProperty != null && !sRgbProperty.boolValue)
            {
                sRgbProperty.boolValue = true;
                serializedTexture.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(renderTexture);
            return renderTexture;
        }

        private static GameObject CreateReusableHud(SimpleFlightController flight, DroneFleetManager fleet, DroneCameraRig cameraRig, RenderTexture noseTexture)
        {
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (hudPrefab != null)
            {
                EnsureHudPrefabHasMiniMap();
                hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
                GameObject hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                hudInstance.name = "Flight HUD";
                var hud = hudInstance.GetComponent<DroneHud>();
                DroneMiniMap miniMap = EnsureMiniMap(hudInstance, flight);
                if (hud != null)
                {
                    SetPrivateField(hud, "miniMap", miniMap);
                    hud.Bind(flight, fleet, cameraRig, noseTexture);
                }

                return hudInstance;
            }

            GameObject hudRoot = CreateHud(flight, fleet, cameraRig, noseTexture);
            PrefabUtility.SaveAsPrefabAssetAndConnect(hudRoot, HudPrefabPath, InteractionMode.AutomatedAction);
            return hudRoot;
        }

        private static GameObject CreateHud(SimpleFlightController flight, DroneFleetManager fleet, DroneCameraRig cameraRig, RenderTexture noseTexture)
        {
            GameObject canvasObject = new GameObject("Flight HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            var hud = canvasObject.AddComponent<DroneHud>();
            hud.Bind(flight, fleet, cameraRig, noseTexture);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color glass = new Color(0f, 0f, 0f, 0.36f);
            Color green = new Color(0.25f, 1f, 0.16f, 1f);
            Color cyan = new Color(0.1f, 0.95f, 1f, 1f);

            Text altitudeBadge = CreateHudText(canvasObject.transform, "Altitude Badge", font, new Vector2(28f, -28f), 36, FontStyle.Bold, green, TextAnchor.MiddleLeft);

            GameObject throttlePanel = CreateHudPanel(canvasObject.transform, "Throttle Panel", new Vector2(24f, 24f), new Vector2(220f, 260f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), glass);
            Image throttleFill = CreateHudImage(throttlePanel.transform, "Throttle Fill", new Vector2(112f, 16f), new Vector2(54f, 210f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Color(0.1f, 1f, 0.18f, 0.82f));
            throttleFill.type = Image.Type.Filled;
            throttleFill.fillMethod = Image.FillMethod.Vertical;
            throttleFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            throttleFill.fillAmount = 0.5f;
            Text throttle = CreateHudText(throttlePanel.transform, "Throttle Text", font, new Vector2(0f, -14f), 18, FontStyle.Bold, Color.white, TextAnchor.UpperCenter);
            SetStretch(throttle.rectTransform, 0f, 0f, 1f, 1f, 8f, 8f, -8f, -8f);

            GameObject cameraPanel = CreateHudPanel(canvasObject.transform, "Nose Camera Panel", new Vector2(0f, 32f), new Vector2(520f, 345f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0f, 0f, 0f, 0.42f));
            RawImage noseView = CreateHudRawImage(cameraPanel.transform, "Nose Camera View", noseTexture, new Vector2(0f, 22f), new Vector2(480f, 270f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Text title = CreateHudText(cameraPanel.transform, "Title", font, new Vector2(0f, -12f), 24, FontStyle.Bold, green, TextAnchor.UpperCenter);
            Text speed = CreateHudText(cameraPanel.transform, "Speed", font, new Vector2(-250f, 12f), 20, FontStyle.Bold, green, TextAnchor.LowerLeft);
            Text altitude = CreateHudText(cameraPanel.transform, "Altitude", font, new Vector2(-250f, 38f), 16, FontStyle.Bold, green, TextAnchor.LowerLeft);
            Text attitude = CreateHudText(cameraPanel.transform, "Attitude", font, new Vector2(250f, 12f), 16, FontStyle.Bold, green, TextAnchor.LowerRight);

            GameObject rightPanel = CreateHudPanel(canvasObject.transform, "Aircraft Parameters Panel", new Vector2(-24f, 0f), new Vector2(390f, 330f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), glass);
            Text mode = CreateHudText(rightPanel.transform, "Mode", font, new Vector2(0f, -18f), 24, FontStyle.Bold, Color.white, TextAnchor.UpperCenter);
            Text parameters = CreateHudText(rightPanel.transform, "Parameters", font, new Vector2(18f, -92f), 14, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            parameters.rectTransform.sizeDelta = new Vector2(360f, 230f);

            GameObject commandPanel = CreateHudPanel(canvasObject.transform, "Command Panel", new Vector2(-24f, 28f), new Vector2(430f, 118f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Color(0f, 0f, 0f, 0.28f));
            Text cameraMode = CreateHudText(commandPanel.transform, "Camera Mode", font, new Vector2(0f, -14f), 18, FontStyle.Bold, cyan, TextAnchor.UpperCenter);
            Text aircraftSelect = CreateHudText(commandPanel.transform, "Aircraft Select", font, new Vector2(0f, -48f), 18, FontStyle.Bold, Color.white, TextAnchor.UpperCenter);
            DroneMiniMap miniMap = CreateMiniMap(canvasObject.transform, flight);

            SetPrivateField(hud, "titleText", title);
            SetPrivateField(hud, "altitudeBadgeText", altitudeBadge);
            SetPrivateField(hud, "altitudeText", altitude);
            SetPrivateField(hud, "speedText", speed);
            SetPrivateField(hud, "attitudeText", attitude);
            SetPrivateField(hud, "modeText", mode);
            SetPrivateField(hud, "throttleText", throttle);
            SetPrivateField(hud, "parameterText", parameters);
            SetPrivateField(hud, "cameraModeText", cameraMode);
            SetPrivateField(hud, "aircraftSelectText", aircraftSelect);
            SetPrivateField(hud, "noseCameraView", noseView);
            SetPrivateField(hud, "throttleFill", throttleFill);
            SetPrivateField(hud, "miniMap", miniMap);
            return canvasObject;
        }

        private static void EnsureHudPrefabHasMiniMap()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath) == null)
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                var hud = prefabRoot.GetComponent<DroneHud>();
                DroneMiniMap miniMap = EnsureMiniMap(prefabRoot, null);
                if (hud != null)
                {
                    SetPrivateField(hud, "miniMap", miniMap);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureHudMiniMapInCurrentScene()
        {
            GameObject hudRoot = GameObject.Find("Flight HUD");
            if (hudRoot == null)
            {
                return;
            }

            SimpleFlightController flight = FindPrimaryFlightController();
            DroneMiniMap miniMap = EnsureMiniMap(hudRoot, flight);
            var hud = hudRoot.GetComponent<DroneHud>();
            if (hud != null)
            {
                SetPrivateField(hud, "miniMap", miniMap);
                hud.Bind(flight);
            }
        }

        private static SimpleFlightController FindPrimaryFlightController()
        {
            GameObject primaryDrone = GameObject.Find("TeachingDrone_On_TakeoffPad") ?? GameObject.Find("TeachingDrone");
            if (primaryDrone != null && primaryDrone.TryGetComponent(out SimpleFlightController primaryFlight))
            {
                return primaryFlight;
            }

            SimpleFlightController[] flights = Object.FindObjectsByType<SimpleFlightController>(FindObjectsSortMode.None);
            return flights.Length > 0 ? flights[0] : null;
        }

        private static DroneMiniMap CreateMiniMap(Transform parent, SimpleFlightController flight)
        {
            GameObject panel = CreateHudPanel(
                parent,
                "Mini Map",
                new Vector2(-24f, -24f),
                new Vector2(250f, 250f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.36f));

            return ConfigureMiniMap(panel, flight);
        }

        private static DroneMiniMap EnsureMiniMap(GameObject hudRoot, SimpleFlightController flight)
        {
            DestroyMiniMapChild(hudRoot.transform, "Mini Map");
            DestroyMiniMapChild(hudRoot.transform, "Mini Map Track");
            DestroyMiniMapChild(hudRoot.transform, "Aircraft Marker");
            GameObject panel = CreateHudPanel(
                hudRoot.transform,
                "Mini Map",
                new Vector2(-24f, -24f),
                new Vector2(250f, 250f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.36f));

            return ConfigureMiniMap(panel, flight);
        }

        private static DroneMiniMap ConfigureMiniMap(GameObject panel, SimpleFlightController flight)
        {
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -24f);
            panelRect.sizeDelta = new Vector2(250f, 250f);

            var panelImage = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.36f);
            panelImage.raycastTarget = false;

            EnsureMiniMapSprites();

            RectMask2D mask = panel.GetComponent<RectMask2D>();
            if (mask == null)
            {
                panel.AddComponent<RectMask2D>();
            }

            DestroyMiniMapChild(panel.transform, "Mini Map Track");
            GameObject trackObject = new GameObject("Mini Map Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObject.transform.SetParent(panel.transform, false);
            var trackRect = trackObject.GetComponent<RectTransform>();
            SetStretch(trackRect, 0f, 0f, 1f, 1f, 10f, 10f, -10f, -10f);
            var trackImage = trackObject.GetComponent<Image>();
            trackImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MiniMapTrackSpritePath);
            trackImage.color = Color.white;
            trackImage.raycastTarget = false;

            DestroyMiniMapChild(panel.transform, "Aircraft Marker");
            DestroyMiniMapChild(trackObject.transform, "Aircraft Marker");
            GameObject markerObject = new GameObject("Aircraft Marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerObject.transform.SetParent(trackObject.transform, false);
            var markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(28f, 36f);
            var markerImage = markerObject.GetComponent<Image>();
            markerImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MiniMapAircraftSpritePath);
            markerImage.color = Color.white;
            markerImage.raycastTarget = false;

            var miniMap = panel.GetComponent<DroneMiniMap>() ?? panel.AddComponent<DroneMiniMap>();
            miniMap.Configure(
                flight,
                trackRect,
                markerRect,
                MiniMapWorldHalfExtents,
                MiniMapWorldCenter,
                MiniMapContentViewportMin,
                MiniMapContentViewportMax);
            return miniMap;
        }

        private static void EnsureMiniMapSprites()
        {
            EnsureFolder(Root, "Textures");
            WriteMiniMapTrackTextureIfNeeded();
            WriteMiniMapAircraftTextureIfNeeded();
        }

        private static void WriteMiniMapTrackTextureIfNeeded()
        {
            var texture = new Texture2D(MiniMapTextureSize, MiniMapTextureSize, TextureFormat.RGBA32, false);
            ClearTexture(texture);

            Color32 gridColor = new Color32(255, 255, 255, 38);
            Color32 fieldColor = new Color32(75, 230, 115, 210);
            Color32 flightAreaColor = new Color32(255, 176, 24, 220);
            Color32 routeColor = new Color32(30, 170, 255, 235);
            Color32 padFillColor = new Color32(30, 220, 255, 70);
            Color32 padOutlineColor = new Color32(30, 220, 255, 220);
            DrawMiniMapGridWorld(texture, 5f, gridColor);
            DrawRectOutlineWorld(texture, Vector2.zero, new Vector2(40f, 26f), 6, fieldColor);
            DrawRectOutlineWorld(texture, Vector2.zero, new Vector2(30f, 16f), 4, flightAreaColor);
            DrawDashedCircleWorld(texture, new Vector2(-6f, 0f), 6f, 5, routeColor, 1.15f, 0.7f);
            DrawDashedCircleWorld(texture, new Vector2(6f, 0f), 6f, 5, routeColor, 1.15f, 0.7f);
            DrawRectFillWorld(texture, new Vector2(0f, -11.6f), new Vector2(3f, 2f), padFillColor);
            DrawRectOutlineWorld(texture, new Vector2(0f, -11.6f), new Vector2(3f, 2f), 3, padOutlineColor);

            texture.Apply();
            File.WriteAllBytes(MiniMapTrackSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            ImportMiniMapSprite(MiniMapTrackSpritePath, MiniMapTextureSize);
        }

        private static void WriteMiniMapAircraftTextureIfNeeded()
        {
            const int width = 128;
            const int height = 160;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            ClearTexture(texture);

            Color32 aircraftColor = new Color32(235, 40, 35, 255);
            FillTriangle(texture, new Vector2Int(64, 158), new Vector2Int(20, 8), new Vector2Int(108, 8), aircraftColor);

            texture.Apply();
            File.WriteAllBytes(MiniMapAircraftSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            ImportMiniMapSprite(MiniMapAircraftSpritePath, 160);
        }

        private static void ImportMiniMapSprite(string assetPath, int pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void DrawMiniMapGridWorld(Texture2D texture, float spacingMeters, Color32 color)
        {
            for (float x = -MiniMapWorldHalfExtents.x; x <= MiniMapWorldHalfExtents.x + 0.01f; x += spacingMeters)
            {
                DrawLine(
                    texture,
                    WorldToMiniMapPixel(new Vector2(x, -MiniMapWorldHalfExtents.y)),
                    WorldToMiniMapPixel(new Vector2(x, MiniMapWorldHalfExtents.y)),
                    1,
                    color);
            }

            float firstHorizontal = Mathf.Ceil(-MiniMapWorldHalfExtents.y / spacingMeters) * spacingMeters;
            for (float z = firstHorizontal; z <= MiniMapWorldHalfExtents.y + 0.01f; z += spacingMeters)
            {
                DrawLine(
                    texture,
                    WorldToMiniMapPixel(new Vector2(-MiniMapWorldHalfExtents.x, z)),
                    WorldToMiniMapPixel(new Vector2(MiniMapWorldHalfExtents.x, z)),
                    1,
                    color);
            }
        }

        private static void DrawRectFillWorld(Texture2D texture, Vector2 center, Vector2 sizeMeters, Color32 color)
        {
            Vector2Int min = WorldToMiniMapPixel(center - sizeMeters * 0.5f);
            Vector2Int max = WorldToMiniMapPixel(center + sizeMeters * 0.5f);
            DrawRectFill(texture, Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y), color);
        }

        private static void DrawRectOutlineWorld(Texture2D texture, Vector2 center, Vector2 sizeMeters, int thickness, Color32 color)
        {
            Vector2 halfSize = sizeMeters * 0.5f;
            Vector2Int bottomLeft = WorldToMiniMapPixel(center + new Vector2(-halfSize.x, -halfSize.y));
            Vector2Int bottomRight = WorldToMiniMapPixel(center + new Vector2(halfSize.x, -halfSize.y));
            Vector2Int topRight = WorldToMiniMapPixel(center + new Vector2(halfSize.x, halfSize.y));
            Vector2Int topLeft = WorldToMiniMapPixel(center + new Vector2(-halfSize.x, halfSize.y));

            DrawLine(texture, bottomLeft, bottomRight, thickness, color);
            DrawLine(texture, bottomRight, topRight, thickness, color);
            DrawLine(texture, topRight, topLeft, thickness, color);
            DrawLine(texture, topLeft, bottomLeft, thickness, color);
        }

        private static void DrawDashedCircleWorld(Texture2D texture, Vector2 center, float radiusMeters, int thickness, Color32 color, float dashMeters, float gapMeters)
        {
            float totalMeters = Mathf.Max(0.01f, dashMeters + gapMeters);
            for (int i = 0; i < 360; i += 2)
            {
                float radians = i * Mathf.Deg2Rad;
                float arcMeters = radians * radiusMeters;
                if (arcMeters % totalMeters >= dashMeters)
                {
                    continue;
                }

                Vector2 world = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radiusMeters;
                Vector2Int pixel = WorldToMiniMapPixel(world);
                DrawDisc(texture, pixel.x, pixel.y, thickness, color);
            }
        }

        private static Vector2Int WorldToMiniMapPixel(Vector2 world)
        {
            float normalizedX = Mathf.InverseLerp(-MiniMapWorldHalfExtents.x, MiniMapWorldHalfExtents.x, world.x - MiniMapWorldCenter.x);
            float normalizedY = Mathf.InverseLerp(-MiniMapWorldHalfExtents.y, MiniMapWorldHalfExtents.y, world.y - MiniMapWorldCenter.y);
            int x = Mathf.RoundToInt(Mathf.Lerp(MiniMapSafetyLeft, MiniMapSafetyRight, normalizedX));
            int y = Mathf.RoundToInt(Mathf.Lerp(MiniMapSafetyBottom, MiniMapSafetyTop, normalizedY));
            return new Vector2Int(x, y);
        }

        private static void DrawMiniMapGrid(Texture2D texture, int left, int bottom, int right, int top, int spacing, Color32 color)
        {
            for (int x = left; x <= right; x += spacing)
            {
                DrawLine(texture, new Vector2Int(x, bottom), new Vector2Int(x, top), 1, color);
            }

            for (int y = bottom; y <= top; y += spacing)
            {
                DrawLine(texture, new Vector2Int(left, y), new Vector2Int(right, y), 1, color);
            }
        }

        private static void DrawRectFill(Texture2D texture, int left, int bottom, int right, int top, Color32 color)
        {
            for (int y = bottom; y <= top; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    SetPixelSafe(texture, x, y, color);
                }
            }
        }

        private static void ClearTexture(Texture2D texture)
        {
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            texture.SetPixels32(pixels);
        }

        private static void DrawRectOutline(Texture2D texture, int left, int bottom, int right, int top, int thickness, Color32 color)
        {
            DrawLine(texture, new Vector2Int(left, bottom), new Vector2Int(right, bottom), thickness, color);
            DrawLine(texture, new Vector2Int(right, bottom), new Vector2Int(right, top), thickness, color);
            DrawLine(texture, new Vector2Int(right, top), new Vector2Int(left, top), thickness, color);
            DrawLine(texture, new Vector2Int(left, top), new Vector2Int(left, bottom), thickness, color);
        }

        private static void DrawDashedCircle(Texture2D texture, Vector2Int center, int radius, int thickness, Color32 color, int dashPixels, int gapPixels)
        {
            int total = Mathf.Max(1, dashPixels + gapPixels);
            for (int i = 0; i < 360; i += 2)
            {
                int segment = Mathf.RoundToInt(i * Mathf.Deg2Rad * radius);
                if (segment % total >= dashPixels)
                {
                    continue;
                }

                float radians = i * Mathf.Deg2Rad;
                int x = center.x + Mathf.RoundToInt(Mathf.Cos(radians) * radius);
                int y = center.y + Mathf.RoundToInt(Mathf.Sin(radians) * radius);
                DrawDisc(texture, x, y, thickness, color);
            }
        }

        private static void DrawLine(Texture2D texture, Vector2Int start, Vector2Int end, int thickness, Color32 color)
        {
            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            int steps = Mathf.Max(dx, dy);
            if (steps <= 0)
            {
                DrawDisc(texture, start.x, start.y, thickness, color);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
                DrawDisc(texture, x, y, thickness, color);
            }
        }

        private static void DrawDisc(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
        {
            int radiusSquared = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSquared)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void FillTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.Min(texture.width - 1, Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.Max(0, Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.Min(texture.height - 1, Mathf.Max(a.y, Mathf.Max(b.y, c.y)));

            float area = Edge(a, b, c);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var point = new Vector2Int(x, y);
                    float w0 = Edge(b, c, point);
                    float w1 = Edge(c, a, point);
                    float w2 = Edge(a, b, point);
                    if (area >= 0f ? w0 >= 0f && w1 >= 0f && w2 >= 0f : w0 <= 0f && w1 <= 0f && w2 <= 0f)
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static float Edge(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static void DestroyMiniMapChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            while (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
                child = parent.Find(childName);
            }
        }

        private static GameObject CreateHudPanel(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static Image CreateHudImage(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateHudRawImage(
            Transform parent,
            string name,
            Texture texture,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = imageObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            var aspectFitter = imageObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspectFitter.aspectRatio = texture != null && texture.height > 0
                ? (float)texture.width / texture.height
                : 16f / 9f;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateHudText(Transform parent, string name, Font font, Vector2 anchoredPosition, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(520f, 24f);

            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void SetStretch(RectTransform rect, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static void CreateInitialDroneLineup(Material displayPadMaterial)
        {
            GameObject lineup = new GameObject("Initial Drone Lineup");
            CreateModelShowcase(lineup.transform, "Quad Racer Model", QuadRacerPath, GetTerrainAnchoredPosition(9.5f, -2.5f), 1.7f, Quaternion.Euler(0f, 205f, 0f), displayPadMaterial, Vector3.up, 3600f);
            CreateModelShowcase(lineup.transform, "DJI Inspire Model", InspirePath, GetTerrainAnchoredPosition(13.6f, -2.5f), 2.05f, Quaternion.Euler(0f, 205f, 0f), displayPadMaterial, Vector3.forward, 3400f);
            CreateModelShowcase(lineup.transform, "Red Training Drone", RedDronePath, GetTerrainAnchoredPosition(17.7f, -2.5f), 1.65f, Quaternion.Euler(0f, 205f, 0f), displayPadMaterial, Vector3.forward, 3500f);
        }

        private static Vector3 GetTerrainAnchoredPosition(float x, float z, float yOffset = 0f)
        {
            return new Vector3(x, GetWildTerrainWorldY(x, z) + yOffset, z);
        }

        private static void CreateModelShowcase(Transform parent, string displayName, string assetPath, Vector3 position, float targetSize, Quaternion rotation, Material padMaterial, Vector3 rotorAxis, float rotorSpeed)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                return;
            }

            GameObject root = new GameObject(displayName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Display Pad";
            pad.transform.SetParent(root.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            pad.transform.localScale = new Vector3(1.7f, 0.05f, 1.7f);
            AssignMaterial(pad, padMaterial);
            ReplaceColliderWithMeshCollider(pad);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = "Visual";
            instance.transform.SetParent(root.transform, false);
            instance.transform.localRotation = rotation;
            HideNonDroneShowcaseMeshes(instance);
            FitRendererToSize(instance, targetSize, 0.16f);
            AddIdleRotorSpinners(instance, rotorAxis, rotorSpeed);

            GameObject label = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.12f, -1.65f);
            label.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            var text = label.AddComponent<TextMesh>();
            text.text = displayName;
            text.fontSize = 42;
            text.characterSize = 0.045f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private static GameObject TryAttachImportedVisual(Transform parent, string assetPath, string name, float targetSize, Quaternion rotation)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localRotation = rotation;
            FitRendererToSize(instance, targetSize, 0f);
            return instance;
        }

        private static void AddIdleRotorSpinners(GameObject root, Vector3 axis, float speed)
        {
            Transform[] rotors = FindRotorTransforms(root.transform);
            for (int i = 0; i < rotors.Length; i++)
            {
                var spinner = rotors[i].gameObject.GetComponent<RotorSpinner>();
                if (spinner == null)
                {
                    spinner = rotors[i].gameObject.AddComponent<RotorSpinner>();
                }

                spinner.Configure(axis, speed, i % 2 == 0 ? 1 : -1, false, 6, 1800f, 4f);
            }
        }

        private static Transform[] FindRotorTransforms(Transform root)
        {
            var pivotGroups = new List<Transform>();
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name.StartsWith("Rotor Pivot - ", System.StringComparison.Ordinal))
                {
                    pivotGroups.Add(child);
                }
            }

            if (pivotGroups.Count > 0)
            {
                pivotGroups.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
                return pivotGroups.ToArray();
            }

            var rotors = new List<Transform>();
            foreach (Transform child in allChildren)
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

        private static bool IsRotorName(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("propeller") || lower.StartsWith("rotor") || lower.Contains("blade") || lower.Contains("\u6868\u53f6");
        }

        private static void FitRendererToSize(GameObject target, float targetMaxSize, float bottomOffset)
        {
            Bounds bounds = CalculateRendererBounds(target);
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max <= 0.0001f)
            {
                return;
            }

            float scale = targetMaxSize / max;
            target.transform.localScale *= scale;

            bounds = CalculateRendererBounds(target);
            Vector3 anchor = target.transform.position;
            Vector3 offset = new Vector3(
                anchor.x - bounds.center.x,
                anchor.y + bottomOffset - bounds.min.y,
                anchor.z - bounds.center.z);
            target.transform.position += offset;
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

        private static void HideNonDroneShowcaseMeshes(GameObject target)
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

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            AssignMaterial(child, material);
            return child;
        }

        private static Transform CreateRotor(Transform parent, string name, Vector3 localPosition, Material material)
        {
            GameObject rotor = CreatePrimitiveChild(parent, name, PrimitiveType.Cylinder, localPosition, new Vector3(0.28f, 0.025f, 0.28f), material);
            return rotor.transform;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void ReplaceColliderWithMeshCollider(GameObject target)
        {
            RemoveCollider(target);
            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            var meshCollider = target.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        private static void RemoveCollidersInChildren(GameObject target)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void SetRendererVisible(GameObject target, bool visible)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
