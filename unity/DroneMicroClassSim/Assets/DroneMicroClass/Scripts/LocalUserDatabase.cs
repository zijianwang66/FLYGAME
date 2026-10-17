using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DroneMicroClass
{
    public static class LocalUserDatabase
    {
        private const int PasswordIterations = 100000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private static bool initialized;

        public sealed class UserRecord
        {
            public int Id;
            public string Username;
            public string DisplayName;
            public string ClassName;
            public string Role;
            public string CreatedAt;

            public bool IsAdmin => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public sealed class TrainingRecord
        {
            public int Id;
            public int UserId;
            public string Username;
            public string DisplayName;
            public string ClassName;
            public string SceneName;
            public int Score;
            public string Grade;
            public float CompletionTimeSeconds;
            public int Collisions;
            public int WrongCheckpointHits;
            public int OutOfCourseTicks;
            public int UnsafeAltitudeTicks;
            public string DroneName;
            public string RecordedAt;
        }

        public static string DatabasePath => Path.Combine(Application.persistentDataPath, "DroneMicroClass.db");

        public static bool Authenticate(string username, string password, out UserRecord user)
        {
            EnsureInitialized();
            user = null;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            using (MiniSqlite.Database database = OpenDatabase())
            {
                List<MiniSqlite.Row> rows = database.Query(
                    "SELECT id, username, password_hash, password_salt, display_name, class_name, role, created_at FROM users WHERE username = @username LIMIT 1;",
                    MiniSqlite.Parameters("@username", username.Trim()));
                if (rows.Count == 0)
                {
                    return false;
                }

                MiniSqlite.Row row = rows[0];
                string expectedHash = row.GetString("password_hash");
                string salt = row.GetString("password_salt");
                if (!VerifyPassword(password, salt, expectedHash))
                {
                    return false;
                }

                user = ReadUser(row);
                database.Execute("UPDATE users SET last_login_at = @last_login_at WHERE id = @id;",
                    MiniSqlite.Parameters("@last_login_at", NowText(), "@id", user.Id));
                database.Execute("INSERT INTO sessions (user_id, device_name, login_at) VALUES (@user_id, @device_name, @login_at);",
                    MiniSqlite.Parameters("@user_id", user.Id, "@device_name", SystemInfo.deviceName, "@login_at", NowText()));
            }

            return true;
        }

        public static bool RegisterStudent(string username, string password, string displayName, string className, out UserRecord user, out string message)
        {
            EnsureInitialized();
            user = null;
            message = string.Empty;

            username = (username ?? string.Empty).Trim();
            password = password ?? string.Empty;
            displayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim();
            className = string.IsNullOrWhiteSpace(className) ? "默认班级" : className.Trim();

            if (username.Length < 3)
            {
                message = "账号至少需要 3 个字符。";
                return false;
            }

            if (username.Length > 32)
            {
                message = "账号不能超过 32 个字符。";
                return false;
            }

            if (username.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
            {
                message = "账号不能包含空格。";
                return false;
            }

            if (password.Length < 6)
            {
                message = "密码至少需要 6 位。";
                return false;
            }

            using (MiniSqlite.Database database = OpenDatabase())
            {
                long existing = database.ScalarLong(
                    "SELECT COUNT(1) FROM users WHERE username = @username;",
                    MiniSqlite.Parameters("@username", username));
                if (existing > 0)
                {
                    message = "账号已存在，请换一个账号。";
                    return false;
                }

                CreatePasswordHash(password, out string salt, out string hash);
                database.Execute(
                    "INSERT INTO users (username, password_hash, password_salt, display_name, class_name, role, created_at, last_login_at) " +
                    "VALUES (@username, @password_hash, @password_salt, @display_name, @class_name, @role, @created_at, @last_login_at);",
                    MiniSqlite.Parameters(
                        "@username", username,
                        "@password_hash", hash,
                        "@password_salt", salt,
                        "@display_name", displayName,
                        "@class_name", className,
                        "@role", "student",
                        "@created_at", NowText(),
                        "@last_login_at", NowText()));

                List<MiniSqlite.Row> rows = database.Query(
                    "SELECT id, username, password_hash, password_salt, display_name, class_name, role, created_at FROM users WHERE username = @username LIMIT 1;",
                    MiniSqlite.Parameters("@username", username));
                if (rows.Count == 0)
                {
                    message = "账号创建后读取失败，请重试。";
                    return false;
                }

                user = ReadUser(rows[0]);
                database.Execute("INSERT INTO sessions (user_id, device_name, login_at) VALUES (@user_id, @device_name, @login_at);",
                    MiniSqlite.Parameters("@user_id", user.Id, "@device_name", SystemInfo.deviceName, "@login_at", NowText()));
            }

            message = "注册成功。";
            return true;
        }

        public static UserRecord GetDefaultPilotUser()
        {
            EnsureInitialized();
            UserRecord user = FindUser("pilot");
            return user ?? FindUser("admin");
        }

        public static void AddTrainingRecord(UserRecord user, ScoreRecordStore.ScoreRecord record)
        {
            EnsureInitialized();
            UserRecord resolvedUser = user ?? GetDefaultPilotUser();
            if (resolvedUser == null || record == null)
            {
                return;
            }

            using (MiniSqlite.Database database = OpenDatabase())
            {
                database.Execute(
                    "INSERT INTO training_records " +
                    "(user_id, scene_name, score, grade, completion_time_seconds, collisions, wrong_checkpoint_hits, out_of_course_ticks, unsafe_altitude_ticks, drone_name, recorded_at) " +
                    "VALUES (@user_id, @scene_name, @score, @grade, @completion_time_seconds, @collisions, @wrong_checkpoint_hits, @out_of_course_ticks, @unsafe_altitude_ticks, @drone_name, @recorded_at);",
                    MiniSqlite.Parameters(
                        "@user_id", resolvedUser.Id,
                        "@scene_name", string.IsNullOrWhiteSpace(record.sceneName) ? "Unknown" : record.sceneName,
                        "@score", record.score,
                        "@grade", record.grade ?? string.Empty,
                        "@completion_time_seconds", record.completionTimeSeconds,
                        "@collisions", record.collisions,
                        "@wrong_checkpoint_hits", record.wrongCheckpointHits,
                        "@out_of_course_ticks", record.outOfCourseTicks,
                        "@unsafe_altitude_ticks", record.unsafeAltitudeTicks,
                        "@drone_name", record.droneName ?? string.Empty,
                        "@recorded_at", string.IsNullOrWhiteSpace(record.recordedAt) ? NowText() : record.recordedAt));
            }
        }

        public static TrainingRecord[] LoadRecentForUser(int userId, int limit)
        {
            EnsureInitialized();
            return QueryTrainingRecords("WHERE r.user_id = @user_id", MiniSqlite.Parameters("@user_id", userId), limit);
        }

        public static TrainingRecord[] LoadRecentForClass(string className, int limit)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(className))
            {
                return Array.Empty<TrainingRecord>();
            }

            return QueryTrainingRecords("WHERE u.class_name = @class_name", MiniSqlite.Parameters("@class_name", className), limit);
        }

        public static TrainingRecord[] LoadRecentAll(int limit)
        {
            EnsureInitialized();
            return QueryTrainingRecords(string.Empty, null, limit);
        }

        public static void ClearRecordsForUser(int userId)
        {
            EnsureInitialized();
            using (MiniSqlite.Database database = OpenDatabase())
            {
                database.Execute("DELETE FROM training_records WHERE user_id = @user_id;", MiniSqlite.Parameters("@user_id", userId));
            }
        }

        public static void ClearAllRecords()
        {
            EnsureInitialized();
            using (MiniSqlite.Database database = OpenDatabase())
            {
                database.Execute("DELETE FROM training_records;");
            }
        }

        public static string ExportRecordsCsv(bool includeAllUsers)
        {
            EnsureInitialized();
            UserRecord user = CurrentUserSession.GetUserOrDefault();
            TrainingRecord[] records = includeAllUsers && user != null && user.IsAdmin
                ? LoadRecentAll(10000)
                : LoadRecentForUser(user.Id, 10000);

            string fileName = includeAllUsers && user != null && user.IsAdmin
                ? "training_records_all.csv"
                : "training_records_" + SanitizeFileName(user != null ? user.Username : "user") + ".csv";
            string path = Path.Combine(Application.persistentDataPath, fileName);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("id,user_id,username,display_name,class_name,scene_name,score,grade,completion_time_seconds,collisions,wrong_checkpoint_hits,out_of_course_ticks,unsafe_altitude_ticks,drone_name,recorded_at");
            foreach (TrainingRecord record in records)
            {
                builder.Append(record.Id).Append(',');
                builder.Append(record.UserId).Append(',');
                AppendCsv(builder, record.Username).Append(',');
                AppendCsv(builder, record.DisplayName).Append(',');
                AppendCsv(builder, record.ClassName).Append(',');
                AppendCsv(builder, record.SceneName).Append(',');
                builder.Append(record.Score).Append(',');
                AppendCsv(builder, record.Grade).Append(',');
                builder.Append(record.CompletionTimeSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(',');
                builder.Append(record.Collisions).Append(',');
                builder.Append(record.WrongCheckpointHits).Append(',');
                builder.Append(record.OutOfCourseTicks).Append(',');
                builder.Append(record.UnsafeAltitudeTicks).Append(',');
                AppendCsv(builder, record.DroneName).Append(',');
                AppendCsv(builder, record.RecordedAt).AppendLine();
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
            return path;
        }

        public static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            Directory.CreateDirectory(Application.persistentDataPath);
            using (MiniSqlite.Database database = OpenDatabase())
            {
                CreateTables(database);
                EnsureDefaultUser(database, "admin", "admin123", "系统管理员", "管理组", "admin");
                EnsureDefaultUser(database, "pilot", "123456", "演示飞手", "默认班级", "student");
            }

            initialized = true;
        }

        private static UserRecord FindUser(string username)
        {
            using (MiniSqlite.Database database = OpenDatabase())
            {
                List<MiniSqlite.Row> rows = database.Query(
                    "SELECT id, username, password_hash, password_salt, display_name, class_name, role, created_at FROM users WHERE username = @username LIMIT 1;",
                    MiniSqlite.Parameters("@username", username));
                return rows.Count > 0 ? ReadUser(rows[0]) : null;
            }
        }

        private static TrainingRecord[] QueryTrainingRecords(string whereClause, Dictionary<string, object> parameters, int limit)
        {
            using (MiniSqlite.Database database = OpenDatabase())
            {
                Dictionary<string, object> queryParameters = parameters ?? new Dictionary<string, object>();
                queryParameters["@limit"] = Mathf.Clamp(limit, 1, 10000);

                List<MiniSqlite.Row> rows = database.Query(
                    "SELECT r.id, r.user_id, u.username, u.display_name, u.class_name, r.scene_name, r.score, r.grade, " +
                    "r.completion_time_seconds, r.collisions, r.wrong_checkpoint_hits, r.out_of_course_ticks, " +
                    "r.unsafe_altitude_ticks, r.drone_name, r.recorded_at " +
                    "FROM training_records r JOIN users u ON u.id = r.user_id " +
                    whereClause + " ORDER BY r.recorded_at DESC, r.id DESC LIMIT @limit;",
                    queryParameters);

                TrainingRecord[] records = new TrainingRecord[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    MiniSqlite.Row row = rows[i];
                    records[i] = new TrainingRecord
                    {
                        Id = row.GetInt("id"),
                        UserId = row.GetInt("user_id"),
                        Username = row.GetString("username"),
                        DisplayName = row.GetString("display_name"),
                        ClassName = row.GetString("class_name"),
                        SceneName = row.GetString("scene_name"),
                        Score = row.GetInt("score"),
                        Grade = row.GetString("grade"),
                        CompletionTimeSeconds = Convert.ToSingle(row.GetDouble("completion_time_seconds")),
                        Collisions = row.GetInt("collisions"),
                        WrongCheckpointHits = row.GetInt("wrong_checkpoint_hits"),
                        OutOfCourseTicks = row.GetInt("out_of_course_ticks"),
                        UnsafeAltitudeTicks = row.GetInt("unsafe_altitude_ticks"),
                        DroneName = row.GetString("drone_name"),
                        RecordedAt = row.GetString("recorded_at")
                    };
                }

                return records;
            }
        }

        private static void CreateTables(MiniSqlite.Database database)
        {
            database.Execute(
                "CREATE TABLE IF NOT EXISTS users (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "username TEXT NOT NULL UNIQUE, " +
                "password_hash TEXT NOT NULL, " +
                "password_salt TEXT NOT NULL, " +
                "display_name TEXT NOT NULL, " +
                "class_name TEXT NOT NULL, " +
                "role TEXT NOT NULL, " +
                "created_at TEXT NOT NULL, " +
                "last_login_at TEXT);");

            database.Execute(
                "CREATE TABLE IF NOT EXISTS training_records (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "user_id INTEGER NOT NULL, " +
                "scene_name TEXT NOT NULL, " +
                "score INTEGER NOT NULL, " +
                "grade TEXT NOT NULL, " +
                "completion_time_seconds REAL NOT NULL, " +
                "collisions INTEGER NOT NULL, " +
                "wrong_checkpoint_hits INTEGER NOT NULL, " +
                "out_of_course_ticks INTEGER NOT NULL, " +
                "unsafe_altitude_ticks INTEGER NOT NULL, " +
                "drone_name TEXT, " +
                "recorded_at TEXT NOT NULL, " +
                "FOREIGN KEY(user_id) REFERENCES users(id));");

            database.Execute(
                "CREATE TABLE IF NOT EXISTS sessions (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "user_id INTEGER NOT NULL, " +
                "device_name TEXT, " +
                "login_at TEXT NOT NULL, " +
                "FOREIGN KEY(user_id) REFERENCES users(id));");

            database.Execute(
                "CREATE TABLE IF NOT EXISTS settings (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "user_id INTEGER NOT NULL UNIQUE, " +
                "gamepad_stick_mode TEXT, " +
                "training_config_json TEXT, " +
                "updated_at TEXT NOT NULL, " +
                "FOREIGN KEY(user_id) REFERENCES users(id));");

            database.Execute("CREATE INDEX IF NOT EXISTS idx_training_records_user_time ON training_records(user_id, recorded_at DESC);");
            database.Execute("CREATE INDEX IF NOT EXISTS idx_training_records_time ON training_records(recorded_at DESC);");
            database.Execute("CREATE INDEX IF NOT EXISTS idx_users_class ON users(class_name);");
        }

        private static void EnsureDefaultUser(MiniSqlite.Database database, string username, string password, string displayName, string className, string role)
        {
            long existing = database.ScalarLong(
                "SELECT COUNT(1) FROM users WHERE username = @username;",
                MiniSqlite.Parameters("@username", username));
            if (existing > 0)
            {
                return;
            }

            CreatePasswordHash(password, out string salt, out string hash);
            database.Execute(
                "INSERT INTO users (username, password_hash, password_salt, display_name, class_name, role, created_at) " +
                "VALUES (@username, @password_hash, @password_salt, @display_name, @class_name, @role, @created_at);",
                MiniSqlite.Parameters(
                    "@username", username,
                    "@password_hash", hash,
                    "@password_salt", salt,
                    "@display_name", displayName,
                    "@class_name", className,
                    "@role", role,
                    "@created_at", NowText()));
        }

        private static MiniSqlite.Database OpenDatabase()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            return new MiniSqlite.Database(DatabasePath);
        }

        private static UserRecord ReadUser(MiniSqlite.Row row)
        {
            return new UserRecord
            {
                Id = row.GetInt("id"),
                Username = row.GetString("username"),
                DisplayName = row.GetString("display_name"),
                ClassName = row.GetString("class_name"),
                Role = row.GetString("role"),
                CreatedAt = row.GetString("created_at")
            };
        }

        private static void CreatePasswordHash(string password, out string saltBase64, out string hashBase64)
        {
            byte[] salt = new byte[SaltBytes];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, PasswordIterations))
            {
                saltBase64 = Convert.ToBase64String(salt);
                hashBase64 = Convert.ToBase64String(pbkdf2.GetBytes(HashBytes));
            }
        }

        private static bool VerifyPassword(string password, string saltBase64, string expectedHashBase64)
        {
            try
            {
                byte[] salt = Convert.FromBase64String(saltBase64);
                byte[] expected = Convert.FromBase64String(expectedHashBase64);
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, PasswordIterations))
                {
                    return FixedTimeEquals(pbkdf2.GetBytes(expected.Length), expected);
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        private static string NowText()
        {
            return DateTime.Now.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        private static StringBuilder AppendCsv(StringBuilder builder, string value)
        {
            value = value ?? string.Empty;
            bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!mustQuote)
            {
                return builder.Append(value);
            }

            return builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "records" : value;
        }

        private static class MiniSqlite
        {
            private const int Ok = 0;
            private const int ResultRow = 100;
            private const int ResultDone = 101;
            private const int OpenReadWrite = 0x00000002;
            private const int OpenCreate = 0x00000004;
            private const int Static = 0;
            private static readonly IntPtr Transient = new IntPtr(-1);

            public static Dictionary<string, object> Parameters(params object[] values)
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                for (int i = 0; i + 1 < values.Length; i += 2)
                {
                    parameters[(string)values[i]] = values[i + 1];
                }

                return parameters;
            }

            public sealed class Row
            {
                private readonly Dictionary<string, object> values;

                public Row(Dictionary<string, object> values)
                {
                    this.values = values;
                }

                public string GetString(string name)
                {
                    return values.TryGetValue(name, out object value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : string.Empty;
                }

                public int GetInt(string name)
                {
                    return values.TryGetValue(name, out object value) && value != null ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : 0;
                }

                public double GetDouble(string name)
                {
                    return values.TryGetValue(name, out object value) && value != null ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : 0d;
                }
            }

            public sealed class Database : IDisposable
            {
                private IntPtr handle;

                public Database(string path)
                {
                    byte[] utf8Path = NullTerminatedUtf8(path);
                    int result = sqlite3_open_v2(utf8Path, out handle, OpenReadWrite | OpenCreate, IntPtr.Zero);
                    if (result != Ok)
                    {
                        string message = handle != IntPtr.Zero ? GetErrorMessage(handle) : "Unable to open SQLite database.";
                        Dispose();
                        throw new InvalidOperationException(message);
                    }
                }

                public void Execute(string sql, Dictionary<string, object> parameters = null)
                {
                    using (Statement statement = Prepare(sql, parameters))
                    {
                        while (true)
                        {
                            int result = sqlite3_step(statement.Handle);
                            if (result == ResultDone)
                            {
                                return;
                            }

                            if (result != ResultRow)
                            {
                                throw new InvalidOperationException(GetErrorMessage(handle));
                            }
                        }
                    }
                }

                public long ScalarLong(string sql, Dictionary<string, object> parameters = null)
                {
                    using (Statement statement = Prepare(sql, parameters))
                    {
                        int result = sqlite3_step(statement.Handle);
                        if (result == ResultRow)
                        {
                            return sqlite3_column_int64(statement.Handle, 0);
                        }

                        if (result == ResultDone)
                        {
                            return 0;
                        }

                        throw new InvalidOperationException(GetErrorMessage(handle));
                    }
                }

                public List<Row> Query(string sql, Dictionary<string, object> parameters = null)
                {
                    List<Row> rows = new List<Row>();
                    using (Statement statement = Prepare(sql, parameters))
                    {
                        while (true)
                        {
                            int result = sqlite3_step(statement.Handle);
                            if (result == ResultDone)
                            {
                                return rows;
                            }

                            if (result != ResultRow)
                            {
                                throw new InvalidOperationException(GetErrorMessage(handle));
                            }

                            int columnCount = sqlite3_column_count(statement.Handle);
                            Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < columnCount; i++)
                            {
                                string name = PointerToString(sqlite3_column_name(statement.Handle, i));
                                values[name] = ReadColumn(statement.Handle, i);
                            }

                            rows.Add(new Row(values));
                        }
                    }
                }

                public void Dispose()
                {
                    if (handle != IntPtr.Zero)
                    {
                        sqlite3_close(handle);
                        handle = IntPtr.Zero;
                    }
                }

                private Statement Prepare(string sql, Dictionary<string, object> parameters)
                {
                    IntPtr statement;
                    byte[] utf8Sql = NullTerminatedUtf8(sql);
                    int result = sqlite3_prepare_v2(handle, utf8Sql, utf8Sql.Length, out statement, IntPtr.Zero);
                    if (result != Ok)
                    {
                        throw new InvalidOperationException(GetErrorMessage(handle));
                    }

                    Statement wrapper = new Statement(statement);
                    try
                    {
                        if (parameters != null)
                        {
                            foreach (KeyValuePair<string, object> parameter in parameters)
                            {
                                int index = sqlite3_bind_parameter_index(statement, parameter.Key);
                                if (index <= 0)
                                {
                                    continue;
                                }

                                Bind(statement, index, parameter.Value);
                            }
                        }

                        return wrapper;
                    }
                    catch
                    {
                        wrapper.Dispose();
                        throw;
                    }
                }

                private string GetErrorMessage(IntPtr database)
                {
                    return PointerToString(sqlite3_errmsg(database));
                }
            }

            private sealed class Statement : IDisposable
            {
                public Statement(IntPtr handle)
                {
                    Handle = handle;
                }

                public IntPtr Handle { get; private set; }

                public void Dispose()
                {
                    if (Handle != IntPtr.Zero)
                    {
                        sqlite3_finalize(Handle);
                        Handle = IntPtr.Zero;
                    }
                }
            }

            private static object ReadColumn(IntPtr statement, int index)
            {
                switch (sqlite3_column_type(statement, index))
                {
                    case 1:
                        return sqlite3_column_int64(statement, index);
                    case 2:
                        return sqlite3_column_double(statement, index);
                    case 3:
                        return PointerToString(sqlite3_column_text(statement, index));
                    case 5:
                        return null;
                    default:
                        return PointerToString(sqlite3_column_text(statement, index));
                }
            }

            private static void Bind(IntPtr statement, int index, object value)
            {
                if (value == null)
                {
                    Check(sqlite3_bind_null(statement, index), statement);
                    return;
                }

                if (value is int intValue)
                {
                    Check(sqlite3_bind_int(statement, index, intValue), statement);
                    return;
                }

                if (value is long longValue)
                {
                    Check(sqlite3_bind_int64(statement, index, longValue), statement);
                    return;
                }

                if (value is float floatValue)
                {
                    Check(sqlite3_bind_double(statement, index, floatValue), statement);
                    return;
                }

                if (value is double doubleValue)
                {
                    Check(sqlite3_bind_double(statement, index, doubleValue), statement);
                    return;
                }

                byte[] text = Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture));
                Check(sqlite3_bind_text(statement, index, text, text.Length, Transient), statement);
            }

            private static void Check(int result, IntPtr statement)
            {
                if (result != Ok)
                {
                    throw new InvalidOperationException("SQLite bind failed: " + result);
                }
            }

            private static byte[] NullTerminatedUtf8(string value)
            {
                byte[] text = Encoding.UTF8.GetBytes(value ?? string.Empty);
                byte[] buffer = new byte[text.Length + 1];
                Buffer.BlockCopy(text, 0, buffer, 0, text.Length);
                return buffer;
            }

            private static string PointerToString(IntPtr pointer)
            {
                if (pointer == IntPtr.Zero)
                {
                    return string.Empty;
                }

                int length = 0;
                while (Marshal.ReadByte(pointer, length) != 0)
                {
                    length++;
                }

                byte[] buffer = new byte[length];
                Marshal.Copy(pointer, buffer, 0, length);
                return Encoding.UTF8.GetString(buffer);
            }

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_open_v2(byte[] filename, out IntPtr database, int flags, IntPtr vfs);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_close(IntPtr database);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr sqlite3_errmsg(IntPtr database);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_prepare_v2(IntPtr database, byte[] sql, int sqlBytes, out IntPtr statement, IntPtr tail);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_step(IntPtr statement);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_finalize(IntPtr statement);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_bind_parameter_index(IntPtr statement, string name);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_bind_null(IntPtr statement, int index);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_bind_int(IntPtr statement, int index, int value);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_bind_int64(IntPtr statement, int index, long value);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_bind_double(IntPtr statement, int index, double value);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_bind_text(IntPtr statement, int index, byte[] value, int bytes, IntPtr destructor);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_column_count(IntPtr statement);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr sqlite3_column_name(IntPtr statement, int index);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern int sqlite3_column_type(IntPtr statement, int index);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern long sqlite3_column_int64(IntPtr statement, int index);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern double sqlite3_column_double(IntPtr statement, int index);

            [DllImport("drone_sqlite3", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr sqlite3_column_text(IntPtr statement, int index);
        }
    }
}
