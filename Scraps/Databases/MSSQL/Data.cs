using Scraps.Configs;
using Scraps.Localization;
using Scraps.Security;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>Получить все записи из таблицы (из ScrapsConfig.ConnectionString).</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="InvalidOperationException">Таблица не найдена</exception>
        public static DataTable GetTableData(string tableName)
        {
            return GetTableData(tableName, ScrapsConfig.ConnectionString);
        }

        /// <summary>Получить все записи из таблицы с указанной строкой подключения.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="InvalidOperationException">Таблица не найдена</exception>
        public static DataTable GetTableData(string tableName, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter($"SELECT * FROM {QuoteIdentifier(tableName)}", conn);
                da.Fill(dt);
            }

            if (dt.Columns.Count == 0)
                throw new InvalidOperationException($"Таблица '{tableName}' не найдена.");

            return dt;
        }

        /// <summary>Получить все записи из таблицы с проверкой прав.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы</exception>
        /// <exception cref="UnauthorizedAccessException">Нет прав доступа</exception>
        public static DataTable GetTableData(string tableName, string roleName, PermissionFlags required)
        {
            if (!RoleManager.CheckAccess(roleName, tableName, required))
                throw new UnauthorizedAccessException($"Нет доступа для роли '{roleName}' к таблице '{tableName}'.");

            return GetTableData(tableName);
        }

        /// <summary>Получить все записи из таблицы с переводом названий колонок.</summary>
        public static DataTable GetTableDataTranslated(string tableName)
        {
            var dt = GetTableData(tableName);
            return TranslationManager.TranslateDataTable(dt, tableName);
        }

        /// <summary>Найти записи по значению колонки (из ScrapsConfig.ConnectionString).</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или колонки</exception>
        public static DataTable FindByColumn(string tableName, string columnName, object value, bool useLike = true)
        {
            return FindByColumn(tableName, columnName, value, ScrapsConfig.ConnectionString, useLike);
        }

        /// <summary>Найти записи по значению колонки с указанной строкой подключения.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или колонки</exception>
        public static DataTable FindByColumn(string tableName, string columnName, object value, string connectionString, bool useLike = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Название колонки не может быть пустым.", nameof(columnName));

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                if (value == null)
                {
                    string queryNull = $"SELECT * FROM {QuoteIdentifier(tableName)} WHERE {QuoteIdentifier(columnName)} IS NULL";
                    SqlDataAdapter daNull = new SqlDataAdapter(queryNull, conn);
                    daNull.Fill(dt);
                    return dt;
                }

                bool isString = value is string;
                string op = (useLike && isString) ? "LIKE" : "=";

                string query = $"SELECT * FROM {QuoteIdentifier(tableName)} WHERE {QuoteIdentifier(columnName)} {op} @Value";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                object paramValue = (useLike && isString) ? $"%{value}%" : value;
                da.SelectCommand.Parameters.AddWithValue("@Value", paramValue);

                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>Применить изменения DataTable в БД (из ScrapsConfig.ConnectionString).</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или null данные</exception>
        public static int ApplyTableChanges(string tableName, DataTable data)
        {
            return ApplyTableChanges(tableName, data, ScrapsConfig.ConnectionString);
        }

        /// <summary>Применить изменения DataTable в БД с указанной строкой подключения.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или null данные</exception>
        public static int ApplyTableChanges(string tableName, DataTable data, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (data == null)
                throw new ArgumentException("Данные не могут быть null.", nameof(data));

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter($"SELECT * FROM {QuoteIdentifier(tableName)}", conn);
                SqlCommandBuilder cb = new SqlCommandBuilder(da);
                da.UpdateCommand = cb.GetUpdateCommand();
                da.InsertCommand = cb.GetInsertCommand();
                da.DeleteCommand = cb.GetDeleteCommand();

                conn.Open();
                return da.Update(data);
            }
        }

        /// <summary>Массовая вставка (из ScrapsConfig.ConnectionString).</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или null данные</exception>
        public static int BulkInsert(string tableName, DataTable data)
        {
            return BulkInsert(tableName, data, ScrapsConfig.ConnectionString);
        }

        /// <summary>Массовая вставка с указанной строкой подключения.</summary>
        /// <exception cref="ArgumentException">Пустое название таблицы или null данные</exception>
        public static int BulkInsert(string tableName, DataTable data, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (data == null)
                throw new ArgumentException("Данные не могут быть null.", nameof(data));

            DataTable importData = data.Copy();
            TranslationManager.UntranslateDataTable(importData, tableName);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = QuoteIdentifier(tableName);

                    foreach (DataColumn column in importData.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.WriteToServer(importData);
                    return importData.Rows.Count;
                }
            }
        }
    }
}




