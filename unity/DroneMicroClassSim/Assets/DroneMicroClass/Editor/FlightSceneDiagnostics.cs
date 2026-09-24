using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneMicroClass
{
    public static class FlightSceneDiagnostics
    {
        private const string LevelScenePath = "Assets/DroneMicroClass/Scenes/DroneFigureEightTrainingUnity.unity";

        [MenuItem("Drone MicroClass/Diagnostics/Write Flight Scene Report")]
        public static void WriteFlightSceneReport()
        {
            EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
            SimpleFlightController flight = Object.FindFirstObjectByType<SimpleFlightController>();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Drone MicroClass flight scene diagnostics");
            builder.AppendLine("Scene: " + LevelScenePath);

            if (flight == null)
            {
                builder.AppendLine("No SimpleFlightController found.");
                WriteReport(builder);
                return;
            }

            Transform droneTransform = flight.transform;
            Rigidbody body = flight.GetComponent<Rigidbody>();
            DroneController controller = flight.GetComponent<DroneController>();
            DroneWindReceiver windReceiver = flight.GetComponent<DroneWindReceiver>();
            builder.AppendLine();
            builder.AppendLine("Drone");
            builder.AppendLine($"Name: {flight.name}");
            builder.AppendLine($"Position: {droneTransform.position}");
            builder.AppendLine($"Rotation Euler: {droneTransform.eulerAngles}");
            builder.AppendLine($"Local scale: {droneTransform.localScale}");
            builder.AppendLine($"Profile: {(flight.Profile != null ? flight.Profile.name : "none")}");
            builder.AppendLine($"DroneController: {(controller != null ? "yes" : "no")}");
            builder.AppendLine($"DroneWindReceiver on scene object: {(windReceiver != null ? "yes" : "no (SimpleFlightController adds one at runtime)")}");

            if (body != null)
            {
                builder.AppendLine($"Rigidbody mass: {body.mass}");
                builder.AppendLine($"Rigidbody useGravity: {body.useGravity}");
                builder.AppendLine($"Rigidbody isKinematic: {body.isKinematic}");
                builder.AppendLine($"Rigidbody constraints: {body.constraints}");
                builder.AppendLine($"Rigidbody centerOfMass: {body.centerOfMass}");
            }

            builder.AppendLine();
            builder.AppendLine("Drone child colliders");
            Collider[] droneColliders = flight.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in droneColliders)
            {
                builder.AppendLine(FormatCollider(collider, droneTransform.position));
            }

            builder.AppendLine();
            builder.AppendLine("Enabled non-trigger colliders within 12 meters of drone start");
            Collider[] allColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (Collider collider in allColliders)
            {
                if (collider == null || !collider.enabled || collider.isTrigger || collider.transform.IsChildOf(droneTransform))
                {
                    continue;
                }

                float distance = Vector3.Distance(collider.bounds.ClosestPoint(droneTransform.position), droneTransform.position);
                if (distance <= 12f)
                {
                    builder.AppendLine(FormatCollider(collider, droneTransform.position));
                }
            }

            builder.AppendLine();
            builder.AppendLine("Runtime wind expectation");
            builder.AppendLine("No WindSystem is saved in this scene unless listed above; WindSystem creates one after scene load.");
            builder.AppendLine("For DroneFigureEightTrainingUnity, current code auto-applies Light wind unless changed at runtime.");

            WriteReport(builder);
        }

        private static string FormatCollider(Collider collider, Vector3 referencePosition)
        {
            Bounds bounds = collider.bounds;
            float distance = Vector3.Distance(bounds.ClosestPoint(referencePosition), referencePosition);
            return $"{collider.GetType().Name} object='{collider.name}' path='{GetPath(collider.transform)}' trigger={collider.isTrigger} enabled={collider.enabled} distance={distance:F3} bounds.center={bounds.center} bounds.size={bounds.size}";
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static void WriteReport(StringBuilder builder)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/flight_scene_diagnostics.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, builder.ToString());
            Debug.Log("Wrote flight scene diagnostics to " + path);
        }
    }
}
