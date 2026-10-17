using System;
using UnityEngine.SceneManagement;

namespace DroneMicroClass
{
    public static class ScoreRecordStore
    {
        private const int MaxRecords = 10;

        [Serializable]
        public sealed class ScoreRecord
        {
            public string username;
            public string displayName;
            public string className;
            public string sceneName;
            public int score;
            public string grade;
            public float completionTimeSeconds;
            public int collisions;
            public int wrongCheckpointHits;
            public int outOfCourseTicks;
            public int unsafeAltitudeTicks;
            public string droneName;
            public string recordedAt;
        }

        public static ScoreRecord[] LoadRecent()
        {
            LocalUserDatabase.UserRecord user = CurrentUserSession.GetUserOrDefault();
            if (user == null)
            {
                return Array.Empty<ScoreRecord>();
            }

            LocalUserDatabase.TrainingRecord[] records = user.IsAdmin
                ? LocalUserDatabase.LoadRecentAll(MaxRecords)
                : LocalUserDatabase.LoadRecentForUser(user.Id, MaxRecords);

            return ConvertRecords(records);
        }

        public static ScoreRecord[] LoadCurrentUserRecent()
        {
            LocalUserDatabase.UserRecord user = CurrentUserSession.GetUserOrDefault();
            return user == null
                ? Array.Empty<ScoreRecord>()
                : ConvertRecords(LocalUserDatabase.LoadRecentForUser(user.Id, MaxRecords));
        }

        public static ScoreRecord[] LoadClassRecent(string className)
        {
            if (!CurrentUserSession.IsAdmin)
            {
                return Array.Empty<ScoreRecord>();
            }

            return ConvertRecords(LocalUserDatabase.LoadRecentForClass(className, MaxRecords));
        }

        public static ScoreRecord[] LoadAllRecent()
        {
            return CurrentUserSession.IsAdmin
                ? ConvertRecords(LocalUserDatabase.LoadRecentAll(MaxRecords))
                : LoadCurrentUserRecent();
        }

        public static void Add(ScoreRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(record.sceneName))
            {
                record.sceneName = SceneManager.GetActiveScene().name;
            }

            LocalUserDatabase.AddTrainingRecord(CurrentUserSession.GetUserOrDefault(), record);
        }

        public static void Clear()
        {
            LocalUserDatabase.UserRecord user = CurrentUserSession.GetUserOrDefault();
            if (user == null)
            {
                return;
            }

            if (user.IsAdmin)
            {
                LocalUserDatabase.ClearAllRecords();
            }
            else
            {
                LocalUserDatabase.ClearRecordsForUser(user.Id);
            }
        }

        public static string ExportCsv()
        {
            return LocalUserDatabase.ExportRecordsCsv(CurrentUserSession.IsAdmin);
        }

        private static ScoreRecord[] ConvertRecords(LocalUserDatabase.TrainingRecord[] records)
        {
            if (records == null || records.Length == 0)
            {
                return Array.Empty<ScoreRecord>();
            }

            ScoreRecord[] converted = new ScoreRecord[records.Length];
            for (int i = 0; i < records.Length; i++)
            {
                LocalUserDatabase.TrainingRecord record = records[i];
                converted[i] = new ScoreRecord
                {
                    username = record.Username,
                    displayName = record.DisplayName,
                    className = record.ClassName,
                    sceneName = record.SceneName,
                    score = record.Score,
                    grade = record.Grade,
                    completionTimeSeconds = record.CompletionTimeSeconds,
                    collisions = record.Collisions,
                    wrongCheckpointHits = record.WrongCheckpointHits,
                    outOfCourseTicks = record.OutOfCourseTicks,
                    unsafeAltitudeTicks = record.UnsafeAltitudeTicks,
                    droneName = record.DroneName,
                    recordedAt = record.RecordedAt
                };
            }

            return converted;
        }
    }
}
