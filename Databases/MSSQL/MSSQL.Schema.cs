using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>Получить список таблиц базы данных (из ScrapsConfig.DatabaseName).</summary>
        public static string[] GetTables(bool includeSystemTables = false)
        {
            return GetTables(ScrapsConfig.DatabaseName, includeSystemTables);
        }

        /// <summary>Получить список таблиц указанной базы данных.</summary>
        public static string[] GetTables(string databaseName, bool includeSystemTables = false)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnectionStringBuilder("master")))
            {
                string query = @"SELECT TABLE_NAME
                                FROM INFORMATION_SCHEMA.TABLES
                                WHERE TABLE_TYPE = 'BASE TABLE'";

                if (!includeSystemTables)
                {
                    query += " AND TABLE_CATALOG = @DatabaseName";
                }

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@DatabaseName", databaseName ?? ScrapsConfig.DatabaseName);
                da.Fill(dt);
            }
            return dt.Rows.Cast<DataRow>().Select(r => r[0].ToString()).ToArray();
        }

        /// <summary>Получить список колонок таблицы.</summary>
        public static string[] GetTableColumns(string tableName)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@TableName", tableName);
                da.Fill(dt);
            }
            return dt.Rows.Cast<DataRow>().Select(r => r[0].ToString()).ToArray();
        }

        /// <summary>Получить схему таблицы (ColumnName -> DataType).</summary>
        public static Dictionary<string, string> GetTableSchema(string tableName)
        {
            var schema = new Dictionary<string, string>();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT
                        COLUMN_NAME,
                        DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        schema[reader["COLUMN_NAME"].ToString()] =
                            reader["DATA_TYPE"].ToString();
                    }
                }
            }
            return schema;
        }

        /// <summary>Проверить, является ли колонка identity.</summary>
        public static bool IsIdentityColumn(string tableName, string columnName)
        {
            try
            {
                using (var conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = @"
                        SELECT COLUMNPROPERTY(OBJECT_ID(@TableName), @ColumnName, 'IsIdentity') AS IsIdentity";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@ColumnName", columnName);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result != DBNull.Value && Convert.ToInt32(result) == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Проверить, допускает ли колонка NULL.</summary>
        public static bool IsNullableColumn(string tableName, string columnName)
        {
            using (var connection = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName", connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@columnName", columnName);

                var isNullable = command.ExecuteScalar();
                return isNullable != null && isNullable.ToString().ToLower() == "yes";
            }
        }
    }
}
