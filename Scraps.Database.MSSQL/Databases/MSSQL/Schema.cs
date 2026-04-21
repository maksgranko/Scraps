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
        /// <exception cref="ArgumentException">Пустое название базы данных</exception>
        public static string[] GetTables(bool includeSystemTables = false, bool includeSchemaInName = false)
        {
            return GetTables(ScrapsConfig.DatabaseName, includeSystemTables, includeSchemaInName);
        }

        /// <summary>Получить список таблиц указанной базы данных.</summary>
        /// <exception cref="ArgumentException">Пустое название базы данных</exception>
        public static string[] GetTables(string databaseName, bool includeSystemTables = false, bool includeSchemaInName = false)
        {
            var db = databaseName ?? ScrapsConfig.DatabaseName;
            if (string.IsNullOrWhiteSpace(db))
                throw new ArgumentException("Название базы данных не может быть пустым.", nameof(databaseName));

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(GetDatabaseConnectionString(db)))
            {
                string query = @"
                    SELECT " +
                    (includeSchemaInName ? "[s].[name] + '.' + [t].[name]" : "[t].[name]") + @"
                    FROM [sys].[tables] [t]
                    INNER JOIN [sys].[schemas] [s] ON [s].[schema_id] = [t].[schema_id]
                    WHERE (@IncludeSystem = 1 OR ([t].[is_ms_shipped] = 0 AND [s].[name] NOT IN ('sys', 'INFORMATION_SCHEMA')))
                    ORDER BY [s].[name], [t].[name]";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@IncludeSystem", includeSystemTables ? 1 : 0);
                da.Fill(dt);
            }
            return dt.Rows.Cast<DataRow>().Select(r => r[0].ToString()).ToArray();
        }

        /// <summary>Получить список колонок таблицы.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="InvalidOperationException">Таблица не найдена</exception>
        public static string[] GetTableColumns(string tableName)
        {
            return GetTableColumns(tableName, null);
        }

        /// <summary>Получить список колонок таблицы.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="InvalidOperationException">Таблица не найдена</exception>
        public static string[] GetTableColumns(string tableName, string tableSchema)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            ResolveSchemaAndTable(tableName, tableSchema, out var resolvedSchema, out var resolvedTable);

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                      AND (@TableSchema IS NULL OR TABLE_SCHEMA = @TableSchema)
                    ORDER BY ORDINAL_POSITION";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@TableName", resolvedTable);
                da.SelectCommand.Parameters.AddWithValue("@TableSchema", (object)resolvedSchema ?? DBNull.Value);
                da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
                throw new InvalidOperationException($"Таблица '{tableName}' не найдена.");

            return dt.Rows.Cast<DataRow>().Select(r => r[0].ToString()).ToArray();
        }

        /// <summary>Получить схему таблицы (ColumnName -> DataType).</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="InvalidOperationException">Таблица не найдена</exception>
        public static Dictionary<string, string> GetTableSchema(string tableName)
        {
            return GetTableSchema(tableName, null);
        }

        /// <summary>Получить схему таблицы (ColumnName -> DataType).</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="InvalidOperationException">Таблица не найдена</exception>
        public static Dictionary<string, string> GetTableSchema(string tableName, string tableSchema)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            ResolveSchemaAndTable(tableName, tableSchema, out var resolvedSchema, out var resolvedTable);

            var schema = new Dictionary<string, string>();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT
                        COLUMN_NAME,
                        DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                      AND (@TableSchema IS NULL OR TABLE_SCHEMA = @TableSchema)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TableName", resolvedTable);
                cmd.Parameters.AddWithValue("@TableSchema", (object)resolvedSchema ?? DBNull.Value);
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

            if (schema.Count == 0)
                throw new InvalidOperationException($"Таблица '{tableName}' не найдена.");

            return schema;
        }

        /// <summary>Проверить, является ли колонка identity.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или колонки</exception>
        public static bool IsIdentityColumn(string tableName, string columnName)
        {
            return IsIdentityColumn(tableName, columnName, null);
        }

        /// <summary>Проверить, является ли колонка identity.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или колонки</exception>
        public static bool IsIdentityColumn(string tableName, string columnName, string tableSchema)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Название колонки не может быть пустым.", nameof(columnName));
            ResolveSchemaAndTable(tableName, tableSchema, out var resolvedSchema, out var resolvedTable);

            using (var conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT COLUMNPROPERTY(OBJECT_ID(@TableName), @ColumnName, 'IsIdentity') AS IsIdentity";

                var cmd = new SqlCommand(query, conn);
                var objName = string.IsNullOrWhiteSpace(resolvedSchema) ? resolvedTable : resolvedSchema + "." + resolvedTable;
                cmd.Parameters.AddWithValue("@TableName", objName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);

                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value && Convert.ToInt32(result) == 1;
            }
        }

        /// <summary>Проверить, допускает ли колонка NULL.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или колонки</exception>
        /// <exception cref="InvalidOperationException">Колонка не найдена</exception>
        public static bool IsNullableColumn(string tableName, string columnName)
        {
            return IsNullableColumn(tableName, columnName, null);
        }

        /// <summary>Проверить, допускает ли колонка NULL.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или колонки</exception>
        /// <exception cref="InvalidOperationException">Колонка не найдена</exception>
        public static bool IsNullableColumn(string tableName, string columnName, string tableSchema)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Название колонки не может быть пустым.", nameof(columnName));
            ResolveSchemaAndTable(tableName, tableSchema, out var resolvedSchema, out var resolvedTable);

            using (var connection = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName AND (@tableSchema IS NULL OR TABLE_SCHEMA = @tableSchema)",
                    connection);
                command.Parameters.AddWithValue("@tableName", resolvedTable);
                command.Parameters.AddWithValue("@columnName", columnName);
                command.Parameters.AddWithValue("@tableSchema", (object)resolvedSchema ?? DBNull.Value);

                var isNullable = command.ExecuteScalar();
                if (isNullable == null)
                    throw new InvalidOperationException($"Колонка '{columnName}' не найдена в таблице '{tableName}'.");

                return isNullable.ToString().ToLower() == "yes";
            }
        }

        private static void ResolveSchemaAndTable(string tableName, string tableSchema, out string resolvedSchema, out string resolvedTable)
        {
            resolvedSchema = string.IsNullOrWhiteSpace(tableSchema) ? null : tableSchema.Trim();
            resolvedTable = tableName.Trim();

            if (resolvedTable.Contains("."))
            {
                var parts = resolvedTable.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    resolvedSchema = string.IsNullOrWhiteSpace(resolvedSchema) ? parts[0].Trim().Trim('[', ']') : resolvedSchema;
                    resolvedTable = parts[1].Trim().Trim('[', ']');
                }
            }
        }
    }
}




