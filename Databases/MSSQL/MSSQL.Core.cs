using Scraps.Configs;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Scraps.Databases
{
    /// <summary>
    /// Утилиты работы с Microsoft SQL Server.
    /// </summary>
    public static partial class MSSQL
    {
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

        /// <summary>
        /// Сформировать строку подключения (с автопоиском) используя ScrapsConfig.DatabaseName.
        /// </summary>
        public static string ConnectionStringBuilder(bool auto = true)
        {
            return ConnectionStringBuilder(ScrapsConfig.DatabaseName, auto);
        }

        /// <summary>Попытаться найти SQL Server среди популярных вариантов.</summary>
        public static string ParseFirstSQLServer(string databaseName)
        {
            string[] defaultServers = {
                ".\\SQLEXPRESS",
                "localhost",
                ".",
                ".\\SQLSERVER01",
                Environment.MachineName,
                $"{Environment.MachineName}\\SQLEXPRESS",
                $"{Environment.MachineName}\\SQLSERVER01",
            };

            foreach (var server in defaultServers)
            {
                try
                {
                    using (var conn = new SqlConnection($"Data Source={server};Initial Catalog=master;Integrated Security=True;Connection Timeout=3;"))
                    {
                        conn.Open();
                        return $"Data Source={server};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False";
                    }
                }
                catch { }
            }

            var instances = TryGetSqlDataSources();
            if (instances != null)
            {
                foreach (DataRow row in instances.Rows)
                {
                    string serverName = row["ServerName"].ToString();
                    string instanceName = row["InstanceName"].ToString();
                    string fullServerName = string.IsNullOrEmpty(instanceName)
                        ? serverName
                        : $"{serverName}\\{instanceName}";

                    try
                    {
                        using (var conn = new SqlConnection($"Data Source={fullServerName};Initial Catalog=master;Integrated Security=True;Connection Timeout=3;"))
                        {
                            conn.Open();
                            return $"Data Source={fullServerName};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False";
                        }
                    }
                    catch { }
                }
            }

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
