using System.IO;
using UnityEditor;
using UnityEngine;

namespace DroneMicroClass
{
    public static class LocalDatabaseDiagnostics
    {
        [MenuItem("Drone MicroClass/Diagnostics/Write Local Database Report")]
        public static void WriteLocalDatabaseReport()
        {
            LocalUserDatabase.EnsureInitialized();
            bool pilotLogin = LocalUserDatabase.Authenticate("pilot", "123456", out LocalUserDatabase.UserRecord pilot);
            bool adminLogin = LocalUserDatabase.Authenticate("admin", "admin123", out LocalUserDatabase.UserRecord admin);
            bool trainingRecordInserted = false;
            string latestTrainingRecord = "null";

            if (pilotLogin && pilot != null)
            {
                string sceneName = "DiagnosticsTrainingWrite";
                LocalUserDatabase.AddTrainingRecord(pilot, new ScoreRecordStore.ScoreRecord
                {
                    sceneName = sceneName,
                    score = 96,
                    grade = "S",
                    completionTimeSeconds = 78.5f,
                    collisions = 1,
                    wrongCheckpointHits = 0,
                    outOfCourseTicks = 2,
                    unsafeAltitudeTicks = 3,
                    droneName = "Diagnostics Drone",
                    recordedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                LocalUserDatabase.TrainingRecord[] records = LocalUserDatabase.LoadRecentForUser(pilot.Id, 1);
                if (records.Length > 0)
                {
                    LocalUserDatabase.TrainingRecord record = records[0];
                    trainingRecordInserted = record.SceneName == sceneName && record.UserId == pilot.Id && record.Score == 96;
                    latestTrainingRecord = record.Id + " / " + record.Username + " / " + record.SceneName + " / " + record.Score + " / " + record.Grade;
                }
            }

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/local_database_diagnostics.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                "Drone MicroClass local database diagnostics\n" +
                "Database: " + LocalUserDatabase.DatabasePath + "\n" +
                "Pilot login: " + pilotLogin + " / " + (pilot != null ? pilot.Username + " / " + pilot.Role : "null") + "\n" +
                "Admin login: " + adminLogin + " / " + (admin != null ? admin.Username + " / " + admin.Role : "null") + "\n" +
                "Training record insert/readback: " + trainingRecordInserted + "\n" +
                "Latest pilot training record: " + latestTrainingRecord + "\n");

            Debug.Log("Wrote local database diagnostics to " + path);
        }
    }
}
