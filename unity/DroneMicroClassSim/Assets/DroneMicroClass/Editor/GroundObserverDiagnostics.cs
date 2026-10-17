using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DroneMicroClass.Editor
{
    // Batch entry point: exercises the real camera and physics without changing saved scenes.
    public static class GroundObserverDiagnostics
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Verify()
        {
            StringBuilder report = new StringBuilder("Ground observer camera verification\n");
            Scene originalScene = SceneManager.GetActiveScene();
            // Batch startup opens an untitled scene, which Unity cannot preserve additively.
            if (Application.isBatchMode && string.IsNullOrEmpty(originalScene.path))
            {
                originalScene = EditorSceneManager.OpenScene("Assets/DroneMicroClass/Scenes/Login.unity");
            }
            Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);
            try
            {
                VerifyCameraBehavior(report);
            }
            finally
            {
                SceneManager.SetActiveScene(originalScene);
                EditorSceneManager.CloseScene(testScene, true);
            }

            foreach (string sceneName in new[] { "矩形飞行训练", "8字飞行", "forest", "学校" })
            {
                VerifyTrainingScene("Assets/DroneMicroClass/Scenes/" + sceneName + ".unity", report);
            }

            report.AppendLine("PASS: all ground observer checks passed.");
            string path = Path.GetFullPath("Logs/ground_observer_diagnostics.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToString());
            Debug.Log(report.ToString());
        }

        private static void VerifyCameraBehavior(StringBuilder report)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(30f, 39.5f, -70f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.transform.position = new Vector3(22f, 41f, -70f);
            trigger.GetComponent<Collider>().isTrigger = true;
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.SetPositionAndRotation(new Vector3(30f, 40.7f, -70f), Quaternion.Euler(0f, 90f, 0f));
            Camera camera = new GameObject("Observer diagnostic camera").AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.farClipPlane = 260f;
            camera.aspect = 16f / 9f;
            DroneCameraRig rig = camera.gameObject.AddComponent<DroneCameraRig>();
            rig.Configure(target.transform, null, DroneCameraRig.CameraMode.Chase, true);
            Physics.SyncTransforms();
            Invoke(rig, "Start");

            // Enter after takeoff: the observer must remain anchored to the original launch area.
            target.transform.position += Vector3.up * 10f;
            Invoke(rig, "SetMode", DroneCameraRig.CameraMode.GroundObserver);
            Tick(rig);
            Vector3 anchor = camera.transform.position;
            Require(Vector3.Distance(anchor, new Vector3(22f, 41.6f, -70f)) < 0.01f,
                "Observer must stand 1.6 m above elevated ground and ignore triggers.");
            Require(Mathf.Abs(camera.fieldOfView - 58f) < 0.01f, "Nearby aircraft should retain the wide view.");

            target.transform.position = anchor + new Vector3(0f, 0f, 36f);
            float previousFov = camera.fieldOfView;
            Tick(rig);
            Require(camera.fieldOfView < previousFov && camera.fieldOfView > 40f, "Zoom must begin gradually.");
            Tick(rig, 240);
            float middleFov = camera.fieldOfView;
            float middleSize = ProjectedHeight(camera, target.transform.position);
            target.transform.position = anchor + new Vector3(0f, 0f, 72f);
            Tick(rig, 240);
            float farSize = ProjectedHeight(camera, target.transform.position);
            Require(camera.fieldOfView < middleFov, "Greater distance must increase focal length.");
            Require(Mathf.Abs(middleSize - farSize) < middleSize * 0.01f,
                "Automatic zoom must preserve aircraft screen size within its working range.");
            Require(Vector3.Distance(camera.transform.position, anchor) < 0.001f, "Observer must never chase the aircraft.");

            target.transform.position = anchor + new Vector3(0f, 0f, 500f);
            Tick(rig, 240);
            float maxZoomFov = camera.fieldOfView;
            Require(maxZoomFov > 7f && maxZoomFov < 9f, "Maximum zoom must stay bounded at eight times.");
            Require(camera.farClipPlane > 500f, "Distant aircraft must remain inside the far clip plane.");
            target.transform.position = anchor + new Vector3(0f, 0f, 1000f);
            Tick(rig, 240);
            Require(Mathf.Abs(camera.fieldOfView - maxZoomFov) < 0.01f, "Zoom must stop increasing at its limit.");

            // Abrupt direction changes under maximum zoom must keep the aircraft in view.
            foreach (float aspect in new[] { 16f / 9f, 9f / 16f })
            {
                camera.aspect = aspect;
                foreach (Vector3 direction in new[] { Vector3.right, Vector3.back, Vector3.up, Vector3.left })
                {
                    target.transform.position = anchor + direction * 300f;
                    Tick(rig);
                    Vector3 viewport = camera.WorldToViewportPoint(target.transform.position);
                    Require(viewport.z > 0f && viewport.x > 0.1f && viewport.x < 0.9f
                        && viewport.y > 0.1f && viewport.y < 0.9f, "Tracking lag must not lose the zoomed aircraft.");
                }
            }

            target.transform.position = anchor + Vector3.forward * 8f;
            Tick(rig, 240);
            Require(Mathf.Abs(camera.fieldOfView - 58f) < 0.01f, "Returning aircraft must restore the wide view.");
            target.transform.position = anchor + Vector3.forward * 500f;
            Tick(rig, 240);
            Invoke(rig, "StepMode", 1);
            Require(rig.ModeName == "FOLLOW", "Forward cycling must return from observer to follow.");
            Require(Mathf.Abs(camera.fieldOfView - 58f) < 0.01f && Mathf.Abs(camera.farClipPlane - 260f) < 0.01f,
                "Leaving observer must restore the original lens and clipping range.");
            Invoke(rig, "StepMode", -1);
            Require(rig.ModeName == "GROUND OBSERVER", "Reverse cycling must reach the observer.");
            Tick(rig, 240);
            Invoke(rig, "OnDisable");
            Require(Mathf.Abs(camera.fieldOfView - 58f) < 0.01f, "Disabling the rig must restore its lens.");
            Require((int)DroneCameraRig.CameraMode.Nose == 3 && (int)DroneCameraRig.CameraMode.StartTower == 2,
                "Existing serialized camera modes must retain their identifiers.");
            report.AppendLine($"PASS: elevated ground, trigger filtering, fixed launch anchor, smooth zoom, screen size " +
                $"({middleSize:F4}/{farSize:F4}), zoom cap ({maxZoomFov:F2} deg), far clipping, " +
                "rapid turns, overhead tracking, portrait aspect, return zoom, mode cycling and lens reset.");
        }

        private static void VerifyTrainingScene(string path, StringBuilder report)
        {
            Scene originalScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Physics.SyncTransforms();
                DroneCameraRig mainRig = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (DroneCameraRig rig in root.GetComponentsInChildren<DroneCameraRig>(true))
                    {
                        Camera candidate = rig.GetComponent<Camera>();
                        if (candidate != null && candidate.CompareTag("MainCamera"))
                        {
                            mainRig = rig;
                        }
                    }
                }

                Require(mainRig != null, path + " must contain a main flight camera rig.");
                Invoke(mainRig, "Start");
                Invoke(mainRig, "SetMode", DroneCameraRig.CameraMode.GroundObserver);
                Tick(mainRig, 120);
                Camera camera = mainRig.GetComponent<Camera>();
                Transform target = (Transform)typeof(DroneCameraRig).GetField("target", PrivateInstance).GetValue(mainRig);
                Require(target != null, path + " must bind the camera to an aircraft.");
                Vector3 viewport = camera.WorldToViewportPoint(target.position);
                Require(viewport.z > 0f && Mathf.Abs(viewport.x - 0.5f) < 0.01f && Mathf.Abs(viewport.y - 0.5f) < 0.01f,
                    path + " must center the aircraft in the observer view.");
                bool grounded = Physics.Raycast(camera.transform.position, Vector3.down, out RaycastHit hit,
                    10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                Require(grounded && Mathf.Abs(hit.distance - 1.6f) < 0.02f,
                    path + " must place the observer 1.6 m above actual terrain.");
                report.AppendLine($"PASS: {path}, observer={camera.transform.position:F2}, " +
                    $"ground={hit.collider.name}, eyeHeight={hit.distance:F2}, FOV={camera.fieldOfView:F2}");
            }
            finally
            {
                SceneManager.SetActiveScene(originalScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static float ProjectedHeight(Camera camera, Vector3 center)
        {
            return Mathf.Abs(camera.WorldToViewportPoint(center + Vector3.up * 0.5f).y
                - camera.WorldToViewportPoint(center - Vector3.up * 0.5f).y);
        }

        private static void Tick(DroneCameraRig rig, int frames = 1)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                Invoke(rig, "MoveGroundObserverCamera", 1f / 60f);
            }
        }

        private static void Invoke(DroneCameraRig rig, string method, params object[] args)
        {
            typeof(DroneCameraRig).GetMethod(method, PrivateInstance).Invoke(rig, args);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
