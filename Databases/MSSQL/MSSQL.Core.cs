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
        private static string _cachedServerConnectionString;

        /// <summary>
        /// Сбросить кэшированный сервер (полезно при смене конфигурации).
        /// </summary>
        public static void ClearServerCache()
        {
            _cachedServerConnectionString = null;
        }
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
        /// Сформировать строку подключения (с автопоиском).
        /// Если databaseName не задан, берётся ScrapsConfig.DatabaseName.
        /// </summary>
        public static string ConnectionStringBuilder(string databaseName = null, bool auto = true)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = ScrapsConfig.DatabaseName;

            if (auto)
            {
                string result = ParseFirstSQLServer(databaseName);
                if (result != null) return result;
            }
            return $"Data Source={Environment.MachineName};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False;Connection Timeout=3;";
        }


        /// <summary>Попытаться найти SQL Server среди популярных вариантов (оптимизировано).</summary>
        public static string ParseFirstSQLServer(string databaseName)
        {
            if (ScrapsConfig.CacheDiscoveredServer && !string.IsNullOrWhiteSpace(_cachedServerConnectionString))
                return _cachedServerConnectionString;

            if (!string.IsNullOrWhiteSpace(ScrapsConfig.ExplicitServerName))
            {
                string explicitResult = TestServer(ScrapsConfig.ExplicitServerName, databaseName);
                if (explicitResult != null)
                {
                    if (ScrapsConfig.CacheDiscoveredServer)
                        _cachedServerConnectionString = explicitResult;
                    return explicitResult;
                }
            }

            string[] defaultServers = {
                ".\\SQLEXPRESS",
                "localhost",
                ".",
                ".\\SQLSERVER01",
                Environment.MachineName,
                $"{Environment.MachineName}\\SQLEXPRESS",
                $"{Environment.MachineName}\\SQLSERVER01",
            };

            string result = ScrapsConfig.UseParallelServerDiscovery
                ? TestServersParallel(defaultServers, databaseName)
                : TestServersSequential(defaultServers, databaseName);

            if (result != null)
            {
                if (ScrapsConfig.CacheDiscoveredServer)
                    _cachedServerConnectionString = result;
                return result;
            }

            if (ScrapsConfig.UseExtendedDiscovery)
            {
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
                        if (ScrapsConfig.CacheDiscoveredServer)
                            _cachedServerConnectionString = result;
                        return result;
                    }
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
                using (var conn = new SqlConnection($"Data Source={server};Initial Catalog=master;Integrated Security=True;Connection Timeout={timeout};"))
                {
                    conn.Open();
                    return $"Data Source={server};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False";
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
                using (var conn = new SqlConnection($"Data Source={server};Initial Catalog=master;Integrated Security=True;Connection Timeout={timeout};"))
                {
                    conn.Open();
                    return $"Data Source={server};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False";
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
                using (var conn = new SqlConnection(ConnectionStringBuilder("master")))
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
