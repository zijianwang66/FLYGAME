using System;
using UnityEngine;

namespace DroneMicroClass
{
    public static class ScoreRecordStore
    {
        private const string PlayerPrefsKey = "DroneMicroClass.ScoreRecords.v1";
        private const int MaxRecords = 10;

        [Serializable]
        public sealed class ScoreRecord
        {
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

        [Serializable]
        private sealed class ScoreRecordCollection
        {
            public ScoreRecord[] records = Array.Empty<ScoreRecord>();
        }

        public static ScoreRecord[] LoadRecent()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<ScoreRecord>();
            }

            try
            {
                ScoreRecordCollection collection = JsonUtility.FromJson<ScoreRecordCollection>(json);
                return collection?.records ?? Array.Empty<ScoreRecord>();
            }
            catch (ArgumentException)
            {
                return Array.Empty<ScoreRecord>();
            }
        }

        public static void Add(ScoreRecord record)
        {
            if (record == null)
            {
                return;
            }

            ScoreRecord[] existing = LoadRecent();
            int count = Mathf.Min(existing.Length + 1, MaxRecords);
            ScoreRecord[] records = new ScoreRecord[count];
            records[0] = record;
            for (int i = 1; i < count; i++)
            {
                records[i] = existing[i - 1];
            }

            Save(records);
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        private static void Save(ScoreRecord[] records)
        {
            ScoreRecordCollection collection = new ScoreRecordCollection
            {
                records = records ?? Array.Empty<ScoreRecord>()
            };

            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(collection));
            PlayerPrefs.Save();
        }
    }
}
