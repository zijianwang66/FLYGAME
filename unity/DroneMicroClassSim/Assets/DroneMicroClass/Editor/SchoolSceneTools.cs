using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneMicroClass.Editor
{
    public static class SchoolSceneTools
    {
        private const string SchoolScenePath = "Assets/DroneMicroClass/Scenes/sch.unity";
        private const string DronePrefabPath = "Assets/DroneMicroClass/Prefabs/TeachingDrone.prefab";
        private const float DroneScale = 0.65f;
        private const float DroneRootHeight = 0.72f;
        private const float HumanEyeHeight = 1.6f;
        private const float StartViewDistance = 2f;
        private static readonly Vector3 ChaseOffset = new Vector3(0f, 3.8f, -10f);
        private static readonly Vector3 LookAtOffset = new Vector3(0f, 0.35f, 0f);

        [MenuItem("Drone MicroClass/Configure Current School Scene")]
        public static void ConfigureCurrentSchoolScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != SchoolScenePath)
            {
                Debug.LogError("Open the sch scene before running the school scene setup.");
                return;
            }

            GameObject launchSurface = GameObject.Find("Cube");
            Collider launchCollider = launchSurface != null ? launchSurface.GetComponent<Collider>() : null;
            if (launchCollider == null)
            {
                Debug.LogError("School scene setup requires a Cube with a Collider as the launch surface.");
                return;
            }

            GameObject staticDrone = GameObject.Find("DJI_Drone");
            if (staticDrone != null)
            {
                staticDrone.SetActive(false);
            }

            GameObject drone = GameObject.Find("TeachingDrone_On_TakeoffPad");
            if (drone == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DronePrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("Missing teaching drone prefab: " + DronePrefabPath);
                    return;
                }

                drone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                drone.name = "TeachingDrone_On_TakeoffPad";
            }

            Bounds launchBounds = launchCollider.bounds;
            drone.transform.SetPositionAndRotation(
                new Vector3(launchBounds.center.x, launchBounds.max.y + DroneRootHeight, launchBounds.center.z),
                Quaternion.identity);
            drone.transform.localScale = Vector3.one * DroneScale;

            DroneMicroClassSceneBuilder.InstallDemoRuntimeInCurrentScene();

            float cameraRange = Mathf.Max(1000f, launchBounds.size.magnitude * 2f);
            ConfigureSchoolCamera(drone, launchBounds, cameraRange);
            SetCameraRange("Nose Feed Camera", cameraRange);
            DisableImportedCameraAudioListener();

            GameObject miniMap = GameObject.Find("Flight HUD/Mini Map");
            if (miniMap != null)
            {
                miniMap.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ReportSchoolScale();
        }

        [MenuItem("Drone MicroClass/Report School Scene Scale")]
        public static void ReportSchoolScale()
        {
            GameObject school = GameObject.Find("sch1 1");
            GameObject launchSurface = GameObject.Find("Cube");
            if (school == null || launchSurface == null)
            {
                Debug.LogWarning("School scale report requires sch1 1 and Cube in the active scene.");
                return;
            }

            Bounds schoolBounds = CalculateRendererBounds(school);
            Collider launchCollider = launchSurface.GetComponent<Collider>();
            Bounds launchBounds = launchCollider != null ? launchCollider.bounds : CalculateRendererBounds(launchSurface);
            Debug.Log(
                $"SCH_SCALE schoolBounds={schoolBounds.size:F2} schoolCenter={schoolBounds.center:F2} " +
                $"cubeBounds={launchBounds.size:F2} cubeCenter={launchBounds.center:F2} " +
                $"schoolRootScale={school.transform.lossyScale:F3}");
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void SetCameraRange(string cameraName, float farClipPlane)
        {
            GameObject cameraObject = GameObject.Find(cameraName);
            Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            if (camera != null)
            {
                camera.farClipPlane = farClipPlane;
            }
        }

        private static void ConfigureSchoolCamera(GameObject drone, Bounds launchBounds, float farClipPlane)
        {
            GameObject cameraObject = GameObject.Find("Follow Camera");
            if (cameraObject == null)
            {
                return;
            }

            Vector3 chasePosition = drone.transform.position + drone.transform.rotation * ChaseOffset;
            Vector3 lookTarget = drone.transform.position + LookAtOffset;
            cameraObject.transform.SetPositionAndRotation(
                chasePosition,
                Quaternion.LookRotation((lookTarget - chasePosition).normalized, Vector3.up));

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera != null)
            {
                camera.farClipPlane = farClipPlane;
                EditorUtility.SetDirty(camera);
            }

            DroneCameraRig rig = cameraObject.GetComponent<DroneCameraRig>();
            if (rig != null)
            {
                Vector3 launchTopCenter = new Vector3(
                    launchBounds.center.x,
                    launchBounds.max.y,
                    launchBounds.center.z);
                Vector3 startTowerPosition = launchTopCenter
                    + Vector3.up * HumanEyeHeight
                    - drone.transform.forward * StartViewDistance;
                SerializedObject serializedRig = new SerializedObject(rig);
                serializedRig.FindProperty("startTowerPosition").vector3Value = startTowerPosition;
                serializedRig.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rig);
            }

            EditorUtility.SetDirty(cameraObject.transform);
            Debug.Log(
                $"SCH_CAMERA chase={chasePosition:F2} startView=" +
                $"({launchBounds.center.x:F2}, {launchBounds.max.y + HumanEyeHeight:F2}, " +
                $"{launchBounds.center.z - StartViewDistance:F2}) target={lookTarget:F2}");
        }

        private static void DisableImportedCameraAudioListener()
        {
            GameObject importedCamera = GameObject.Find("Main Camera");
            AudioListener listener = importedCamera != null ? importedCamera.GetComponent<AudioListener>() : null;
            if (listener != null)
            {
                listener.enabled = false;
                EditorUtility.SetDirty(listener);
            }
        }
    }
}
