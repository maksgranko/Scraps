using Scraps.Configs;
using Scraps.Data.DataTables;
using Scraps.Localization;
using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>
        /// Описание JOIN по внешнему ключу для выборки данных.
        /// </summary>
        public sealed class ForeignKeyJoin
        {
            /// <summary>Колонка в основной таблице (например, RoleId).</summary>
            public string BaseColumn { get; set; }

            /// <summary>Таблица-справочник (например, Roles).</summary>
            public string ReferenceTable { get; set; }

            /// <summary>Колонка в таблице-справочнике, по которой выполняется JOIN (например, RoleID).</summary>
            public string ReferenceColumn { get; set; }

            /// <summary>
            /// Колонки из таблицы-справочника для выборки.
            /// Если пусто/null, выбираются все колонки (`*`) из справочника.
            /// </summary>
            public string[] ReferenceColumns { get; set; }

            /// <summary>
            /// Префикс алиаса для выбранных колонок справочника.
            /// По умолчанию используется имя таблицы-справочника.
            /// </summary>
            public string AliasPrefix { get; set; }

            public ForeignKeyJoin() { }

            public ForeignKeyJoin(string baseColumn, string referenceTable, string referenceColumn, params string[] referenceColumns)
            {
                BaseColumn = baseColumn;
                ReferenceTable = referenceTable;
                ReferenceColumn = referenceColumn;
                ReferenceColumns = referenceColumns;
            }
        }

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

        /// <summary>
        /// Получить данные таблицы с LEFT JOIN по указанным внешним ключам.
        /// Можно ограничить колонки основной таблицы через <paramref name="baseColumns"/>.
        /// </summary>
        /// <param name="tableName">Основная таблица.</param>
        /// <param name="foreignKeys">Список описаний FK JOIN.</param>
        /// <param name="baseColumns">Колонки основной таблицы (null/пусто = все колонки).</param>
        public static DataTable GetTableData(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            return GetTableData(tableName, ScrapsConfig.ConnectionString, foreignKeys, baseColumns);
        }

        /// <summary>
        /// Получить данные таблицы с LEFT JOIN по указанным внешним ключам и явной строкой подключения.
        /// Можно ограничить колонки основной таблицы через <paramref name="baseColumns"/>.
        /// </summary>
        /// <param name="tableName">Основная таблица.</param>
        /// <param name="connectionString">Строка подключения.</param>
        /// <param name="foreignKeys">Список описаний FK JOIN.</param>
        /// <param name="baseColumns">Колонки основной таблицы (null/пусто = все колонки).</param>
        public static DataTable GetTableData(string tableName, string connectionString, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Строка подключения не может быть пустой.", nameof(connectionString));

            var fkList = foreignKeys == null
                ? new List<ForeignKeyJoin>()
                : foreignKeys.Where(f => f != null).ToList();

            var selectParts = new List<string>();
            if (baseColumns == null || baseColumns.Length == 0)
            {
                selectParts.Add("t.*");
            }
            else
            {
                foreach (var col in baseColumns)
                {
                    if (string.IsNullOrWhiteSpace(col)) continue;
                    var cleanCol = col.Trim();
                    selectParts.Add($"t.{QuoteIdentifier(cleanCol)}");
                }

                if (selectParts.Count == 0)
                    selectParts.Add("t.*");
            }

            var joinParts = new List<string>();
            for (int i = 0; i < fkList.Count; i++)
            {
                var fk = fkList[i];
                if (string.IsNullOrWhiteSpace(fk.BaseColumn))
                    throw new ArgumentException("Для FK JOIN не задан BaseColumn.", nameof(foreignKeys));
                if (string.IsNullOrWhiteSpace(fk.ReferenceTable))
                    throw new ArgumentException("Для FK JOIN не задан ReferenceTable.", nameof(foreignKeys));
                if (string.IsNullOrWhiteSpace(fk.ReferenceColumn))
                    throw new ArgumentException("Для FK JOIN не задан ReferenceColumn.", nameof(foreignKeys));

                string alias = "fk" + i;
                string prefix = string.IsNullOrWhiteSpace(fk.AliasPrefix)
                    ? fk.ReferenceTable.Trim().Trim('[', ']')
                    : fk.AliasPrefix.Trim();

                joinParts.Add(
                    $"LEFT JOIN {QuoteIdentifier(fk.ReferenceTable)} {alias} ON t.{QuoteIdentifier(fk.BaseColumn)} = {alias}.{QuoteIdentifier(fk.ReferenceColumn)}");

                if (fk.ReferenceColumns == null || fk.ReferenceColumns.Length == 0)
                {
                    selectParts.Add($"{alias}.*");
                    continue;
                }

                foreach (var refCol in fk.ReferenceColumns)
                {
                    if (string.IsNullOrWhiteSpace(refCol)) continue;
                    string cleanRefCol = refCol.Trim();
                    string outAlias = BuildSafeColumnAlias(prefix + "_" + cleanRefCol);
                    selectParts.Add($"{alias}.{QuoteIdentifier(cleanRefCol)} AS {QuoteIdentifier(outAlias)}");
                }
            }

            string query =
                "SELECT " + string.Join(", ", selectParts) + " " +
                "FROM " + QuoteIdentifier(tableName) + " t " +
                string.Join(" ", joinParts);

            var dt = new DataTable();
            using (var conn = new SqlConnection(connectionString))
            {
                var da = new SqlDataAdapter(query, conn);
                da.Fill(dt);
            }

            if (dt.Columns.Count == 0)
                throw new InvalidOperationException($"Таблица '{tableName}' не найдена или вернула пустую схему.");

            return dt;
        }

        /// <summary>
        /// Получить данные таблицы с FK JOIN и проверкой прав.
        /// </summary>
        public static DataTable GetTableData(string tableName, string roleName, PermissionFlags required, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            if (!RoleManager.CheckAccess(roleName, tableName, required))
                throw new UnauthorizedAccessException($"Нет доступа для роли '{roleName}' к таблице '{tableName}'.");

            return GetTableData(tableName, ScrapsConfig.ConnectionString, foreignKeys, baseColumns);
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
            TranslationManager.Untranslate(importData, tableName);

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

        /// <summary>
        /// Прочитать таблицу формата Nx2 (ключ/значение) из БД в словарь int/string.
        /// </summary>
        public static Dictionary<int, string> GetNx2Dictionary(string tableName, string keyColumn, string valueColumn)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(keyColumn))
                throw new ArgumentException("Название колонки ключа не может быть пустым.", nameof(keyColumn));
            if (string.IsNullOrWhiteSpace(valueColumn))
                throw new ArgumentException("Название колонки значения не может быть пустым.", nameof(valueColumn));

            string query =
                $"SELECT {QuoteIdentifier(keyColumn)} AS [KeyColumn], {QuoteIdentifier(valueColumn)} AS [ValueColumn] " +
                $"FROM {QuoteIdentifier(tableName)}";

            var dt = GetDataTableFromSQL(query);
            if (dt == null)
                throw new InvalidOperationException("Не удалось получить данные Nx2 из БД.");

            return Parser.ParseNx2ToDictionary(dt);
        }

        /// <summary>
        /// Прочитать таблицу формата Nx1 (одна колонка) из БД в список строк.
        /// </summary>
        public static List<string> GetNx1List(string tableName, string valueColumn, bool trim = true, bool skipEmpty = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(valueColumn))
                throw new ArgumentException("Название колонки значения не может быть пустым.", nameof(valueColumn));

            string query =
                $"SELECT {QuoteIdentifier(valueColumn)} AS [ValueColumn] " +
                $"FROM {QuoteIdentifier(tableName)}";

            var dt = GetDataTableFromSQL(query);
            if (dt == null)
                throw new InvalidOperationException("Не удалось получить данные Nx1 из БД.");

            return Parser.ParseNx1ToList(dt, valueColumnIndex: 0, trim: trim, skipEmpty: skipEmpty);
        }

        /// <summary>
        /// Прочитать таблицу формата Nx2 (ключ/значение) из БД в словарь с пользовательскими преобразователями.
        /// </summary>
        public static Dictionary<TKey, TValue> GetNx2Dictionary<TKey, TValue>(
            string tableName,
            string keyColumn,
            string valueColumn,
            Func<object, TKey> keyParser,
            Func<object, TValue> valueParser,
            bool skipInvalidRows = false)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(keyColumn))
                throw new ArgumentException("Название колонки ключа не может быть пустым.", nameof(keyColumn));
            if (string.IsNullOrWhiteSpace(valueColumn))
                throw new ArgumentException("Название колонки значения не может быть пустым.", nameof(valueColumn));

            string query =
                $"SELECT {QuoteIdentifier(keyColumn)} AS [KeyColumn], {QuoteIdentifier(valueColumn)} AS [ValueColumn] " +
                $"FROM {QuoteIdentifier(tableName)}";

            var dt = GetDataTableFromSQL(query);
            if (dt == null)
                throw new InvalidOperationException("Не удалось получить данные Nx2 из БД.");

            return Parser.ParseNx2ToDictionary(dt, keyParser, valueParser, keyColumnIndex: 0, valueColumnIndex: 1, skipInvalidRows: skipInvalidRows);
        }
        private static string BuildSafeColumnAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                return "Column";

            var chars = alias.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }

            var result = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "Column" : result;
        }
    }
}



