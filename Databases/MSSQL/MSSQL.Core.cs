using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Scraps.Databases
{
    /// <summary>
    /// Утилиты работы с Microsoft SQL Server.
    /// </summary>
    public static partial class MSSQL
    {
        
        /// <summary>
        /// Безопасно обернуть имя таблицы/колонки в [].
        /// </summary>
        public static string QuoteIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var trimmed = name.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                return trimmed;
            if (trimmed.Contains("."))
            {
                var parts = trimmed.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    parts[i] = "[" + part.Replace("]", "]]") + "]";
                }
                return string.Join(".", parts);
            }
            return "[" + trimmed.Replace("]", "]]") + "]";
        }

        /// <summary>
        /// Строка подключения к master на базе ScrapsConfig.ConnectionString.
        /// </summary>
        private static string GetMasterConnectionString()
        {
            if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                throw new InvalidOperationException("ScrapsConfig.ConnectionString не задан.");

            var builder = new SqlConnectionStringBuilder(ScrapsConfig.ConnectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ToString();
        }

        /// <summary>
        /// Строка подключения к указанной БД на базе ScrapsConfig.ConnectionString.
        /// </summary>
        private static string GetDatabaseConnectionString(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                throw new InvalidOperationException("ScrapsConfig.ConnectionString не задан.");
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Название базы данных не может быть пустым.", nameof(databaseName));

            var builder = new SqlConnectionStringBuilder(ScrapsConfig.ConnectionString)
            {
                InitialCatalog = databaseName
            };
            return builder.ToString();
        }
        /// <summary>
        /// Сформировать строку подключения (с автопоиском).
        /// Если databaseName не задан, берётся ScrapsConfig.DatabaseName.
        /// Можно передать готовую строку подключения напрямую.
        /// </summary>
        public static string ConnectionStringBuilder(string databaseName = null, bool auto = true)
        {
            if (LooksLikeConnectionString(databaseName))
                return databaseName;

            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = ScrapsConfig.DatabaseName;

            if (auto)
            {
                string result = ParseFirstSQLServer(databaseName);
                if (result != null) return result;
            }
            return BuildDefaultConnectionString(Environment.MachineName, databaseName, timeout: 3);
        }

        /// <summary>
        /// Сформировать строку подключения по серверу и базе данных.
        /// </summary>
        public static string ConnectionStringBuilder(string serverName, string databaseName)
        {
            if (LooksLikeConnectionString(serverName))
                return serverName;
            if (string.IsNullOrWhiteSpace(serverName))
                serverName = Environment.MachineName;
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = ScrapsConfig.DatabaseName;
            return BuildDefaultConnectionString(serverName, databaseName, timeout: 3);
        }

        private static bool LooksLikeConnectionString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildDefaultConnectionString(string server, string databaseName, int timeout)
        {
            return $"Data Source={server};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout={timeout};";
        }


        /// <summary>Попытаться найти SQL Server среди популярных вариантов (оптимизировано).</summary>
        public static string ParseFirstSQLServer(string databaseName)
        {
            if (!string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                return ScrapsConfig.ConnectionString;

            if (!string.IsNullOrWhiteSpace(ScrapsConfig.ExplicitServerName))
            {
                string explicitResult = TestServer(ScrapsConfig.ExplicitServerName, databaseName);
                if (explicitResult != null)
                {
                    if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                        ScrapsConfig.ConnectionString = explicitResult;
                    return explicitResult;
                }
            }

            string[] defaultServers = {
                ".\\SQLEXPRESS",
                "localhost",
                ".",
                ".\\SQLSERVER01",
                ".\\MSSQLSERVER01",
                Environment.MachineName,
                $"{Environment.MachineName}\\SQLEXPRESS",
                $"{Environment.MachineName}\\SQLSERVER01",
                $"{Environment.MachineName}\\MSSQLSERVER01",
            };

            string result = ScrapsConfig.UseParallelServerDiscovery
                ? TestServersParallel(defaultServers, databaseName)
                : TestServersSequential(defaultServers, databaseName);

            if (result != null)
            {
                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                    ScrapsConfig.ConnectionString = result;
                return result;
            }

            var instances = TryGetSqlDataSources();
            if (instances != null)
            {
                var extendedServers = new List<string>();
                foreach (DataRow row in instances.Rows)
                {
                    string serverName = row["ServerName"].ToString();
                    string instanceName = row["InstanceName"].ToString();
                    string fullServerName = string.IsNullOrEmpty(instanceName)
                        ? serverName
                        : $"{serverName}\\{instanceName}";
                    extendedServers.Add(fullServerName);
                }

                result = ScrapsConfig.UseParallelServerDiscovery
                    ? TestServersParallel(extendedServers.ToArray(), databaseName)
                    : TestServersSequential(extendedServers.ToArray(), databaseName);

                if (result != null)
                {
                    if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                        ScrapsConfig.ConnectionString = result;
                    return result;
                }
            }

            return null;
        }

        /// <summary>Параллельный тест серверов (возвращает первый успешный).</summary>
        private static string TestServersParallel(string[] servers, string databaseName)
        {
            if (servers == null || servers.Length == 0) return null;

            int timeout = ScrapsConfig.ServerDiscoveryTimeout > 0 ? ScrapsConfig.ServerDiscoveryTimeout : 1;
            var cts = new CancellationTokenSource();
            var tasks = new List<Task<string>>();

            foreach (var server in servers)
            {
                tasks.Add(Task.Run(() => TestServerAsync(server, databaseName, timeout, cts.Token), cts.Token));
            }

            try
            {
                Task<string> firstCompleted = Task.WhenAny(tasks).Result;
                cts.Cancel();
                return firstCompleted.Result;
            }
            catch
            {
                return null;
            }
            finally
            {
                cts.Dispose();
            }
        }

        /// <summary>Последовательный тест серверов.</summary>
        private static string TestServersSequential(string[] servers, string databaseName)
        {
            if (servers == null || servers.Length == 0) return null;

            foreach (var server in servers)
            {
                string result = TestServer(server, databaseName);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>Асинхронный тест сервера с поддержкой отмены.</summary>
        private static string TestServerAsync(string server, string databaseName, int timeout, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var conn = new SqlConnection(BuildDefaultConnectionString(server, "master", timeout)))
                {
                    conn.Open();
                    return BuildDefaultConnectionString(server, databaseName, timeout);
                }
            }
            catch { }
            return null;
        }

        /// <summary>Проверить соединение с сервером (с настраиваемым таймаутом).</summary>
        private static string TestServer(string server, string databaseName)
        {
            try
            {
                int timeout = ScrapsConfig.ServerDiscoveryTimeout > 0 ? ScrapsConfig.ServerDiscoveryTimeout : 1;
                using (var conn = new SqlConnection(BuildDefaultConnectionString(server, "master", timeout)))
                {
                    conn.Open();
                    return BuildDefaultConnectionString(server, databaseName, timeout);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Попробовать получить источники данных SQL Server через reflection (совместимо с netstandard).
        /// </summary>
        private static DataTable TryGetSqlDataSources()
        {
            try
            {
                var type = Type.GetType("System.Data.Sql.SqlDataSourceEnumerator, System.Data");
                if (type == null) return null;

                var instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var instance = instanceProp?.GetValue(null);
                if (instance == null) return null;

                var method = type.GetMethod("GetDataSources", BindingFlags.Public | BindingFlags.Instance);
                return method?.Invoke(instance, null) as DataTable;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Проверить соединение с SQL Server.</summary>
        public static bool CheckConnection()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                    return false;
                using (var conn = new SqlConnection(GetMasterConnectionString()))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Выполнить SQL-команду без возврата данных.</summary>
        public static int ExecuteNonQuery(string query)
        {
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Выполнить SQL-команду и вернуть скалярный результат.</summary>
        public static object ExecuteScalar(string query)
        {
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>Получить DataTable из SQL-запроса.</summary>
        public static DataTable GetDataTableFromSQL(string sqlRequest)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    new SqlDataAdapter(sqlRequest, conn).Fill(dt);
                }
                return dt;
            }
            catch
            {
                return null;
            }
        }
    }
}
