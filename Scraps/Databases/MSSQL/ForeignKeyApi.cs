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
        /// <summary>Информация о внешнем ключе таблицы.</summary>
        public sealed class ForeignKeyInfo
        {
            /// <summary>Колонка в основной таблице.</summary>
            public string Column { get; set; }
            /// <summary>Таблица-справочник (полное имя со схемой).</summary>
            public string RefTable { get; set; }
            /// <summary>Колонка в таблице-справочнике.</summary>
            public string RefColumn { get; set; }
            /// <summary>Имя ограничения (constraint).</summary>
            public string ConstraintName { get; set; }
            /// <summary>Может ли колонка содержать NULL.</summary>
            public bool IsNullable { get; set; }
            /// <summary>Схема основной таблицы.</summary>
            public string TableSchema { get; set; }
            /// <summary>Имя основной таблицы.</summary>
            public string TableName { get; set; }
            /// <summary>Схема таблицы-справочника.</summary>
            public string RefSchema { get; set; }
            /// <summary>Имя таблицы-справочника.</summary>
            public string RefTableName { get; set; }
        }

        /// <summary>Элемент справочника (значение и отображаемое имя).</summary>
        public sealed class LookupItem
        {
            /// <summary>Значение (обычно ID).</summary>
            public object Value { get; set; }
            /// <summary>Отображаемое имя.</summary>
            public string Display { get; set; }
        }

        /// <summary>Метаданные колонки для редактирования таблицы.</summary>
        public sealed class TableEditColumnMetadata
        {
            /// <summary>Имя колонки.</summary>
            public string Column { get; set; }
            /// <summary>SQL-тип данных.</summary>
            public string DataType { get; set; }
            /// <summary>Может ли содержать NULL.</summary>
            public bool IsNullable { get; set; }
            /// <summary>Является ли Identity-колонкой.</summary>
            public bool IsIdentity { get; set; }
            /// <summary>Информация о внешнем ключе (null если не FK).</summary>
            public ForeignKeyInfo ForeignKey { get; set; }
            /// <summary>Колонка отображения для справочника.</summary>
            public string LookupDisplayColumn { get; set; }
        }

        /// <summary>Метаданные таблицы для редактирования.</summary>
        public sealed class TableEditMetadata
        {
            /// <summary>Имя таблицы.</summary>
            public string TableName { get; set; }
            /// <summary>Схема таблицы.</summary>
            public string TableSchema { get; set; }
            /// <summary>Список метаданных колонок.</summary>
            public List<TableEditColumnMetadata> Columns { get; set; } = new List<TableEditColumnMetadata>();
        }

        /// <summary>Операторы фильтрации для SQL-запросов.</summary>
        public enum SqlFilterOperator
        {
            /// <summary>Равно (=).</summary>
            Eq,
            /// <summary>Не равно (&lt;&gt;).</summary>
            Ne,
            /// <summary>Больше (&gt;).</summary>
            Gt,
            /// <summary>Больше или равно (&gt;=).</summary>
            Ge,
            /// <summary>Меньше (&lt;).</summary>
            Lt,
            /// <summary>Меньше или равно (&lt;=).</summary>
            Le,
            /// <summary>LIKE.</summary>
            Like,
            /// <summary>IS NULL.</summary>
            IsNull,
            /// <summary>IS NOT NULL.</summary>
            IsNotNull,
            /// <summary>IN (список значений).</summary>
            In
        }

        /// <summary>Условие фильтрации SQL-запроса.</summary>
        public sealed class SqlFilterCondition
        {
            /// <summary>Имя колонки.</summary>
            public string Column { get; set; }
            /// <summary>Оператор сравнения.</summary>
            public SqlFilterOperator Operator { get; set; } = SqlFilterOperator.Eq;
            /// <summary>Значение для сравнения.</summary>
            public object Value { get; set; }
            /// <summary>Список значений для оператора IN.</summary>
            public IEnumerable<object> Values { get; set; }
        }

        /// <summary>Условие сортировки SQL-запроса.</summary>
        public sealed class SqlSortCondition
        {
            /// <summary>Имя колонки.</summary>
            public string Column { get; set; }
            /// <summary>Сортировка по убыванию.</summary>
            public bool Descending { get; set; }
        }

        /// <summary>Параметры запроса для справочника внешнего ключа.</summary>
        public sealed class ForeignKeyQueryOptions
        {
            /// <summary>Колонки для выборки.</summary>
            public string[] Columns { get; set; }
            /// <summary>Колонка отображения.</summary>
            public string DisplayColumn { get; set; }
            /// <summary>Дополнительное условие WHERE (сырой SQL).</summary>
            public string Where { get; set; }
            /// <summary>Порядок сортировки (сырой SQL).</summary>
            public string OrderBy { get; set; }
            /// <summary>Безопасные условия фильтрации.</summary>
            public List<SqlFilterCondition> Filters { get; set; } = new List<SqlFilterCondition>();
            /// <summary>Безопасные условия сортировки.</summary>
            public List<SqlSortCondition> Sorts { get; set; } = new List<SqlSortCondition>();
        }

        /// <summary>Параметры расширения данных таблицы внешними ключами.</summary>
        public sealed class ExpandForeignKeysOptions
        {
            /// <summary>Строка подключения.</summary>
            public string ConnectionString { get; set; }
            /// <summary>Колонки основной таблицы.</summary>
            public string[] BaseColumns { get; set; }
            /// <summary>Раскрывать внешние ключи.</summary>
            public bool ExpandForeignKeys { get; set; } = true;
            /// <summary>Включать все колонки справочника.</summary>
            public bool IncludeReferenceAllColumns { get; set; } = false;
            /// <summary>Включать колонку отображения справочника.</summary>
            public bool IncludeReferenceDisplayColumn { get; set; } = true;
            /// <summary>Рекурсивный обход FK.</summary>
            public bool Recursive { get; set; } = false;
            /// <summary>Максимальная глубина рекурсии.</summary>
            public int MaxDepth { get; set; } = 2;
            /// <summary>Колонки для конкретных FK (ключ: constraint/table.column/column).</summary>
            public Dictionary<string, string[]> ForeignKeyColumns { get; set; } =
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            /// <summary>Переопределения колонок отображения (ключ: table или constraint).</summary>
            public Dictionary<string, string> DisplayColumnOverrides { get; set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Дополнительные условия WHERE для FK (ключ: constraint/table.column/column).</summary>
            public Dictionary<string, string> ForeignKeyWhere { get; set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Порядок сортировки для FK (ключ: constraint/table.column/column).</summary>
            public Dictionary<string, string> ForeignKeyOrderBy { get; set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Расширенная конфигурация для конкретных FK.</summary>
            public Dictionary<string, ForeignKeyQueryOptions> ForeignKeyQuery { get; set; } =
                new Dictionary<string, ForeignKeyQueryOptions>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Автоматически определять и добавлять колонку отображения для FK (DisplayName).
            /// Если false — JOIN выполняется, но alias-колонки с DisplayName не добавляются.
            /// </summary>
            public bool AutoResolveDisplayColumn { get; set; } = true;
        }

        private static readonly object SchemaCacheSync = new object();
        private static readonly Dictionary<string, List<ForeignKeyInfo>> ForeignKeysCache =
            new Dictionary<string, List<ForeignKeyInfo>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Очистить кэш метаданных внешних ключей.</summary>
        public static void RefreshCache()
        {
            lock (SchemaCacheSync)
            {
                ForeignKeysCache.Clear();
            }
        }

        /// <summary>Получить список внешних ключей таблицы (из ScrapsConfig.ConnectionString).</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <returns>Список информации о внешних ключах.</returns>
        public static List<ForeignKeyInfo> GetForeignKeys(string tableName)
        {
            return GetForeignKeys(tableName, null, null);
        }

        /// <summary>Получить список внешних ключей таблицы с указанием схемы.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="tableSchema">Схема таблицы.</param>
        /// <returns>Список информации о внешних ключах.</returns>
        public static List<ForeignKeyInfo> GetForeignKeys(string tableName, string tableSchema)
        {
            return GetForeignKeys(tableName, tableSchema, null);
        }

        /// <summary>Получить список внешних ключей таблицы с указанной строкой подключения.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="tableSchema">Схема таблицы.</param>
        /// <param name="connectionString">Строка подключения к БД.</param>
        /// <returns>Список информации о внешних ключах.</returns>
        public static List<ForeignKeyInfo> GetForeignKeys(string tableName, string tableSchema, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

            ResolveSchemaAndTable(tableName, tableSchema, out var resolvedSchema, out var resolvedTable);
            var connStr = GetConnectionOrDefault(connectionString);
            string cacheKey = BuildSchemaCacheKey(connStr, resolvedSchema, resolvedTable);

            lock (SchemaCacheSync)
            {
                if (ForeignKeysCache.TryGetValue(cacheKey, out var cached))
                    return cached.Select(CloneForeignKey).ToList();
            }

            var result = new List<ForeignKeyInfo>();
            using (var conn = new SqlConnection(connStr))
            {
                const string query = @"
SELECT
    s.name  AS TableSchema,
    t.name  AS TableName,
    c.name  AS ColumnName,
    rs.name AS RefSchema,
    rt.name AS RefTable,
    rc.name AS RefColumn,
    fk.name AS ConstraintName,
    c.is_nullable AS IsNullable
FROM sys.foreign_key_columns fkc
INNER JOIN sys.tables t ON t.object_id = fkc.parent_object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
INNER JOIN sys.tables rt ON rt.object_id = fkc.referenced_object_id
INNER JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
WHERE t.name = @TableName
  AND (@TableSchema IS NULL OR s.name = @TableSchema)
ORDER BY fk.name, fkc.constraint_column_id";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TableName", resolvedTable);
                cmd.Parameters.AddWithValue("@TableSchema", (object)resolvedSchema ?? DBNull.Value);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var refSchema = reader["RefSchema"].ToString();
                        var refTableName = reader["RefTable"].ToString();
                        var fullRef = string.IsNullOrWhiteSpace(refSchema)
                            ? refTableName
                            : refSchema + "." + refTableName;

                        result.Add(new ForeignKeyInfo
                        {
                            Column = reader["ColumnName"].ToString(),
                            RefTable = fullRef,
                            RefColumn = reader["RefColumn"].ToString(),
                            ConstraintName = reader["ConstraintName"].ToString(),
                            IsNullable = Convert.ToBoolean(reader["IsNullable"]),
                            TableSchema = reader["TableSchema"].ToString(),
                            TableName = reader["TableName"].ToString(),
                            RefSchema = refSchema,
                            RefTableName = refTableName
                        });
                    }
                }
            }

            lock (SchemaCacheSync)
            {
                ForeignKeysCache[cacheKey] = result.Select(CloneForeignKey).ToList();
            }

return result;
        }

        /// <summary>Определить колонку отображения для справочника.</summary>
        /// <param name="refTable">Название таблицы-справочника.</param>
        /// <param name="excludeColumn">Колонка-исключение (обычно PK), которую не следует выбирать при автопоиске.</param>
        /// <param name="preferred">Предпочтительные названия колонок (по умолчанию из конфигурации).</param>
        /// <returns>Название колонки для отображения.</returns>
        public static string ResolveDisplayColumn(string refTable, string excludeColumn = null, string[] preferred = null)
        {
            if (string.IsNullOrWhiteSpace(refTable))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(refTable));

            // 1. Переопределения пользователя — всегда приоритет, даже если это PK
            if (TryResolveDisplayOverride(refTable, out var overrideColumn))
                return overrideColumn;

            var pref = (preferred != null && preferred.Length > 0)
                ? preferred
                : (ScrapsConfig.ForeignKeyDisplayColumnPreferred ?? new string[0]);

            var columns = GetTableColumns(refTable);

            // 2. Пользовательский preferred — без учета регистра, уважаем даже PK
            foreach (var p in pref)
            {
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                var match = columns.FirstOrDefault(c =>
                    string.Equals(c, p, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                    return match;
            }

            // 3. Автопоиск: исключаем PK-колонку и типичные ID-колонки
            var nonId = columns.FirstOrDefault(c =>
                !string.Equals(c, excludeColumn, StringComparison.OrdinalIgnoreCase)
                && !c.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                && !c.EndsWith("ID", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(nonId))
                return nonId;

            // 4. Fallback: любая колонка, кроме PK
            var nonPk = columns.FirstOrDefault(c =>
                !string.Equals(c, excludeColumn, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(nonPk))
                return nonPk;

            // 5. Последний шанс — первая колонка (даже если это PK)
            return columns.Length > 0 ? columns[0] : "Value";
        }

        /// <summary>Получить справочник для колонки внешнего ключа (из ScrapsConfig.ConnectionString).</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="column">Название колонки с внешним ключом.</param>
        /// <param name="displayColumn">Колонка для отображения (опционально).</param>
        /// <param name="where">Дополнительное условие WHERE.</param>
        /// <param name="orderBy">Параметры сортировки.</param>
        /// <param name="connectionString">Строка подключения (опционально).</param>
        /// <returns>DataTable с колонками Value и Display.</returns>
        public static DataTable GetForeignKeyLookup(
            string tableName,
            string column,
            string displayColumn = null,
            string where = null,
            string orderBy = null,
            string connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Название колонки не может быть пустым.", nameof(column));

            var fk = GetForeignKeys(tableName, null, connectionString)
                .FirstOrDefault(x => string.Equals(x.Column, column, StringComparison.OrdinalIgnoreCase));
            if (fk == null)
                throw new InvalidOperationException($"Колонка '{column}' не является внешним ключом в таблице '{tableName}'.");

            var display = string.IsNullOrWhiteSpace(displayColumn)
                ? ResolveDisplayColumn(fk.RefTable, fk.RefColumn)
                : displayColumn.Trim();

            var sql =
                "SELECT " + QuoteIdentifier(fk.RefColumn) + " AS [Value], " +
                QuoteIdentifier(display) + " AS [Display] " +
                "FROM " + QuoteIdentifier(fk.RefTable);

            if (!string.IsNullOrWhiteSpace(where))
                sql += " WHERE " + where;
            if (!string.IsNullOrWhiteSpace(orderBy))
                sql += " ORDER BY " + orderBy;
            else
                sql += " ORDER BY " + QuoteIdentifier(display);

return GetDataTableFromSQL(sql, GetConnectionOrDefault(connectionString));
        }

        /// <summary>Получить справочник для колонки внешнего ключа с фильтрами и сортировкой.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="column">Название колонки с внешним ключом.</param>
        /// <param name="filters">Условия фильтрации.</param>
        /// <param name="sorts">Условия сортировки.</param>
        /// <param name="displayColumn">Колонка для отображения (опционально).</param>
        /// <param name="connectionString">Строка подключения (опционально).</param>
        /// <returns>DataTable с колонками Value и Display.</returns>
        public static DataTable GetForeignKeyLookup(
            string tableName,
            string column,
            IEnumerable<SqlFilterCondition> filters,
            IEnumerable<SqlSortCondition> sorts,
            string displayColumn = null,
            string connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Название колонки не может быть пустым.", nameof(column));

            var fk = GetForeignKeys(tableName, null, connectionString)
                .FirstOrDefault(x => string.Equals(x.Column, column, StringComparison.OrdinalIgnoreCase));
            if (fk == null)
                throw new InvalidOperationException($"Колонка '{column}' не является внешним ключом в таблице '{tableName}'.");

            var refColumns = new HashSet<string>(GetTableColumns(fk.RefTable), StringComparer.OrdinalIgnoreCase);
            var display = string.IsNullOrWhiteSpace(displayColumn)
                ? ResolveDisplayColumn(fk.RefTable, fk.RefColumn)
                : displayColumn.Trim();
            if (!refColumns.Contains(display))
                throw new InvalidOperationException($"Колонка отображения '{display}' не найдена в таблице '{fk.RefTable}'.");

            string where = BuildSafeWhereClause(filters, refColumns, alias: null);
            string orderBy = BuildSafeOrderByClause(sorts, refColumns, alias: null);

            var sql =
                "SELECT " + QuoteIdentifier(fk.RefColumn) + " AS [Value], " +
                QuoteIdentifier(display) + " AS [Display] " +
                "FROM " + QuoteIdentifier(fk.RefTable);

            if (!string.IsNullOrWhiteSpace(where))
                sql += " WHERE " + where;

            if (!string.IsNullOrWhiteSpace(orderBy))
                sql += " ORDER BY " + orderBy;
            else
                sql += " ORDER BY " + QuoteIdentifier(display);

return GetDataTableFromSQL(sql, GetConnectionOrDefault(connectionString));
        }

        /// <summary>Получить список элементов справочника для колонки внешнего ключа.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="column">Название колонки с внешним ключом.</param>
        /// <param name="displayColumn">Колонка для отображения (опционально).</param>
        /// <param name="where">Дополнительное условие WHERE.</param>
        /// <param name="orderBy">Параметры сортировки.</param>
        /// <param name="connectionString">Строка подключения (опционально).</param>
        /// <returns>Список элементов справочника.</returns>
        public static List<LookupItem> GetForeignKeyLookupItems(
            string tableName,
            string column,
            string displayColumn = null,
            string where = null,
            string orderBy = null,
            string connectionString = null)
        {
            var dt = GetForeignKeyLookup(tableName, column, displayColumn, where, orderBy, connectionString);
            var result = new List<LookupItem>(dt.Rows.Count);

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new LookupItem
                {
                    Value = row["Value"],
                    Display = row["Display"]?.ToString()
                });
            }

return result;
        }

        /// <summary>Получить метаданные таблицы для редактирования (из ScrapsConfig.ConnectionString).</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <returns>Метаданные таблицы.</returns>
        public static TableEditMetadata GetTableEditMetadata(string tableName)
        {
            return GetTableEditMetadata(tableName, null);
        }

        /// <summary>Получить метаданные таблицы для редактирования с указанной строкой подключения.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="connectionString">Строка подключения.</param>
        /// <returns>Метаданные таблицы.</returns>
        public static TableEditMetadata GetTableEditMetadata(string tableName, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

            var connStr = GetConnectionOrDefault(connectionString);
            ResolveSchemaAndTable(tableName, null, out var resolvedSchema, out var resolvedTable);
            var fks = GetForeignKeys(tableName, null, connStr);
            var fkByColumn = fks.ToDictionary(x => x.Column, StringComparer.OrdinalIgnoreCase);

            var result = new TableEditMetadata
            {
                TableName = resolvedTable,
                TableSchema = resolvedSchema
            };

            using (var conn = new SqlConnection(connStr))
            {
                const string query = @"
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = @TableName
  AND (@TableSchema IS NULL OR c.TABLE_SCHEMA = @TableSchema)
ORDER BY c.ORDINAL_POSITION";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TableName", resolvedTable);
                cmd.Parameters.AddWithValue("@TableSchema", (object)resolvedSchema ?? DBNull.Value);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var col = reader["COLUMN_NAME"].ToString();
                        fkByColumn.TryGetValue(col, out var fk);

                        result.Columns.Add(new TableEditColumnMetadata
                        {
                            Column = col,
                            DataType = reader["DATA_TYPE"].ToString(),
                            IsNullable = string.Equals(reader["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase),
                            IsIdentity = reader["IsIdentity"] != DBNull.Value && Convert.ToInt32(reader["IsIdentity"]) == 1,
                            ForeignKey = fk == null ? null : CloneForeignKey(fk),
                            LookupDisplayColumn = fk == null ? null : ResolveDisplayColumn(fk.RefTable, fk.RefColumn)
                        });
                    }
                }
            }

return result;
        }

        /// <summary>Получить данные таблицы с расширенными колонками из справочников.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="options">Параметры расширения внешних ключей.</param>
        /// <returns>DataTable с расширенными данными.</returns>
        private static DataTable GetTableDataExpanded(string tableName, ExpandForeignKeysOptions options = null)
        {
            if (options == null)
                options = new ExpandForeignKeysOptions();

            return GetTableDataExpanded(tableName, options.ConnectionString, options);
        }

        /// <summary>Получить данные таблицы с расширенными колонками из справочников.</summary>
        /// <param name="tableName">Название таблицы.</param>
        /// <param name="connectionString">Строка подключения.</param>
        /// <param name="options">Параметры расширения внешних ключей.</param>
        /// <returns>DataTable с расширенными данными.</returns>
        private static DataTable GetTableDataExpanded(string tableName, string connectionString, ExpandForeignKeysOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

            options = options ?? new ExpandForeignKeysOptions();
            var connStr = GetConnectionOrDefault(connectionString ?? options.ConnectionString);

            ResolveSchemaAndTable(tableName, null, out var resolvedSchema, out var resolvedTable);
            var rootTable = string.IsNullOrWhiteSpace(resolvedSchema)
                ? resolvedTable
                : resolvedSchema + "." + resolvedTable;

            var selectParts = new List<string>();
            var joinParts = new List<string>();
            int aliasCounter = 0;
            string rootAlias = "t0";

            if (options.BaseColumns == null || options.BaseColumns.Length == 0)
            {
                selectParts.Add(rootAlias + ".*");
            }
            else
            {
                foreach (var col in options.BaseColumns)
                {
                    if (string.IsNullOrWhiteSpace(col)) continue;
                    selectParts.Add(rootAlias + "." + QuoteIdentifier(col.Trim()));
                }

                if (selectParts.Count == 0)
                    selectParts.Add(rootAlias + ".*");
            }

            if (options.ExpandForeignKeys)
            {
                var path = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootTable };
                ExpandForeignKeysRecursive(
                    currentTable: rootTable,
                    currentAlias: rootAlias,
                    depth: 0,
                    maxDepth: options.Recursive ? Math.Max(1, options.MaxDepth) : 1,
                    options: options,
                    selectParts: selectParts,
                    joinParts: joinParts,
                    aliasCounter: ref aliasCounter,
                    path: path,
                    connectionString: connStr);
            }

            string sql =
                "SELECT " + string.Join(", ", selectParts) + " " +
                "FROM " + QuoteIdentifier(rootTable) + " " + rootAlias + " " +
                string.Join(" ", joinParts);

            var dt = new DataTable();
            using (var conn = new SqlConnection(connStr))
            {
                var da = new SqlDataAdapter(sql, conn);
                da.Fill(dt);
            }

            return dt;
        }

        private static void ExpandForeignKeysRecursive(
            string currentTable,
            string currentAlias,
            int depth,
            int maxDepth,
            ExpandForeignKeysOptions options,
            List<string> selectParts,
            List<string> joinParts,
            ref int aliasCounter,
            HashSet<string> path,
            string connectionString)
        {
            if (depth >= maxDepth)
                return;

            var fks = GetForeignKeys(currentTable, null, connectionString);
            for (int i = 0; i < fks.Count; i++)
            {
                var fk = fks[i];
                if (fk == null || string.IsNullOrWhiteSpace(fk.RefTable))
                    continue;

                var refTable = fk.RefTable;
                if (path.Contains(refTable))
                    continue;

                aliasCounter++;
                string joinAlias = "j" + aliasCounter;

                var refColumns = new HashSet<string>(GetTableColumns(refTable), StringComparer.OrdinalIgnoreCase);
                var queryOptions = ResolveQueryOptions(options, currentTable, fk);
                var joinWhereRaw = queryOptions?.Where;
                if (string.IsNullOrWhiteSpace(joinWhereRaw) && options.ForeignKeyWhere != null)
                {
                    TryResolveByKey(options.ForeignKeyWhere, currentTable, fk, out joinWhereRaw);
                }

                var joinWhereSafe = BuildSafeWhereClause(queryOptions?.Filters, refColumns, joinAlias);

                var onClause =
                    currentAlias + "." + QuoteIdentifier(fk.Column) +
                    " = " + joinAlias + "." + QuoteIdentifier(fk.RefColumn);

                if (!string.IsNullOrWhiteSpace(joinWhereRaw))
                    onClause += " AND (" + joinWhereRaw + ")";
                if (!string.IsNullOrWhiteSpace(joinWhereSafe))
                    onClause += " AND (" + joinWhereSafe + ")";

                joinParts.Add(
                    "LEFT JOIN " + QuoteIdentifier(refTable) + " " + joinAlias +
                    " ON " + onClause);

                string[] requestedColumns = queryOptions?.Columns;
                if ((requestedColumns == null || requestedColumns.Length == 0) && options.ForeignKeyColumns != null)
                {
                    TryResolveByKey(options.ForeignKeyColumns, currentTable, fk, out requestedColumns);
                }

                if (options.IncludeReferenceAllColumns && (requestedColumns == null || requestedColumns.Length == 0))
                {
                    selectParts.Add(joinAlias + ".*");
                }
                else
                {
                    var columnsToSelect = new List<string>();
                    if (requestedColumns != null && requestedColumns.Length > 0)
                    {
                        columnsToSelect.AddRange(requestedColumns.Where(x => !string.IsNullOrWhiteSpace(x)));
                    }
                    else if (options.IncludeReferenceDisplayColumn && options.AutoResolveDisplayColumn)
                    {
                        string display = queryOptions?.DisplayColumn;
                        if (string.IsNullOrWhiteSpace(display) && options.DisplayColumnOverrides != null)
                        {
                            if (!options.DisplayColumnOverrides.TryGetValue(refTable, out display))
                                options.DisplayColumnOverrides.TryGetValue(fk.ConstraintName ?? string.Empty, out display);
                        }

                        if (string.IsNullOrWhiteSpace(display))
                            display = ResolveDisplayColumn(refTable, fk.RefColumn);

                        columnsToSelect.Add(display);
                    }

                    foreach (var col in columnsToSelect.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (!refColumns.Contains(col))
                            continue;

                        var aliasName = BuildSafeColumnAlias(currentAlias + "_" + fk.Column + "_" + col);
                        selectParts.Add(joinAlias + "." + QuoteIdentifier(col) + " AS " + QuoteIdentifier(aliasName));
                    }
                }

                path.Add(refTable);
                ExpandForeignKeysRecursive(
                    refTable,
                    joinAlias,
                    depth + 1,
                    maxDepth,
                    options,
                    selectParts,
                    joinParts,
                    ref aliasCounter,
                    path,
                    connectionString);
                path.Remove(refTable);
            }
        }

        private static ForeignKeyQueryOptions ResolveQueryOptions(ExpandForeignKeysOptions options, string currentTable, ForeignKeyInfo fk)
        {
            if (options?.ForeignKeyQuery == null || options.ForeignKeyQuery.Count == 0)
                return null;

            if (TryResolveByKey(options.ForeignKeyQuery, currentTable, fk, out var qo))
                return qo;

            return null;
        }

        private static bool TryResolveByKey<T>(Dictionary<string, T> source, string currentTable, ForeignKeyInfo fk, out T value)
        {
            value = default(T);
            if (source == null || fk == null)
                return false;

            var keyByConstraint = fk.ConstraintName ?? string.Empty;
            var keyByColumn = fk.Column ?? string.Empty;
            var keyByPath = currentTable + "." + keyByColumn;

            if (!string.IsNullOrWhiteSpace(keyByConstraint) && source.TryGetValue(keyByConstraint, out value))
                return true;
            if (!string.IsNullOrWhiteSpace(keyByPath) && source.TryGetValue(keyByPath, out value))
                return true;
            if (!string.IsNullOrWhiteSpace(keyByColumn) && source.TryGetValue(keyByColumn, out value))
                return true;

            return false;
        }

        /// <summary>Безопасно квотировать SQL-литерал (строка, число, дата, bool, null).</summary>
        private static string QuoteLiteral(object value)
        {
            if (value == null)
                return "NULL";

            if (value is bool b)
                return b ? "1" : "0";

            if (value is string s)
                return "'" + s.Replace("'", "''") + "'";

            if (value is DateTime dt)
                return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";

            if (value is Guid g)
                return "'" + g.ToString() + "'";

            return value.ToString();
        }

        private static string BuildSafeWhereClause(IEnumerable<SqlFilterCondition> filters, HashSet<string> allowedColumns, string alias)
        {
            if (filters == null)
                return null;

            var parts = new List<string>();
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.Column))
                    continue;

                var col = f.Column.Trim();
                if (allowedColumns != null && !allowedColumns.Contains(col))
                    throw new InvalidOperationException($"Колонка '{col}' не разрешена для фильтра.");

                var qcol = string.IsNullOrWhiteSpace(alias)
                    ? QuoteIdentifier(col)
                    : alias + "." + QuoteIdentifier(col);

                switch (f.Operator)
                {
                    case SqlFilterOperator.Eq:
                        parts.Add(qcol + " = " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.Ne:
                        parts.Add(qcol + " <> " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.Gt:
                        parts.Add(qcol + " > " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.Ge:
                        parts.Add(qcol + " >= " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.Lt:
                        parts.Add(qcol + " < " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.Le:
                        parts.Add(qcol + " <= " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.Like:
                        parts.Add(qcol + " LIKE " + QuoteLiteral(f.Value));
                        break;
                    case SqlFilterOperator.IsNull:
                        parts.Add(qcol + " IS NULL");
                        break;
                    case SqlFilterOperator.IsNotNull:
                        parts.Add(qcol + " IS NOT NULL");
                        break;
                    case SqlFilterOperator.In:
                        var values = (f.Values ?? new object[0]).Select(QuoteLiteral).ToArray();
                        if (values.Length == 0)
                            throw new InvalidOperationException("Оператор IN требует хотя бы одно значение.");
                        parts.Add(qcol + " IN (" + string.Join(", ", values) + ")");
                        break;
                }
            }

            if (parts.Count == 0)
                return null;

            return string.Join(" AND ", parts);
        }

        private static string BuildSafeOrderByClause(IEnumerable<SqlSortCondition> sorts, HashSet<string> allowedColumns, string alias)
        {
            if (sorts == null)
                return null;

            var parts = new List<string>();
            foreach (var s in sorts)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.Column))
                    continue;

                var col = s.Column.Trim();
                if (allowedColumns != null && !allowedColumns.Contains(col))
                    throw new InvalidOperationException($"Колонка '{col}' не разрешена для сортировки.");

                var qcol = string.IsNullOrWhiteSpace(alias)
                    ? QuoteIdentifier(col)
                    : alias + "." + QuoteIdentifier(col);

                parts.Add(qcol + (s.Descending ? " DESC" : " ASC"));
            }

            if (parts.Count == 0)
                return null;

            return string.Join(", ", parts);
        }

        private static bool TryResolveDisplayOverride(string refTable, out string displayColumn)
        {
            displayColumn = null;
            var map = ScrapsConfig.ForeignKeyDisplayColumnOverrides;
            if (map == null || map.Count == 0)
                return false;

            if (map.TryGetValue(refTable, out displayColumn) && !string.IsNullOrWhiteSpace(displayColumn))
                return true;

            ResolveSchemaAndTable(refTable, null, out var schema, out var table);
            if (!string.IsNullOrWhiteSpace(table))
            {
                if (map.TryGetValue(table, out displayColumn) && !string.IsNullOrWhiteSpace(displayColumn))
                    return true;

                var full = string.IsNullOrWhiteSpace(schema) ? table : schema + "." + table;
                if (map.TryGetValue(full, out displayColumn) && !string.IsNullOrWhiteSpace(displayColumn))
                    return true;
            }

            return false;
        }

        private static string GetConnectionOrDefault(string connectionString)
        {
            var conn = string.IsNullOrWhiteSpace(connectionString)
                ? ScrapsConfig.ConnectionString
                : connectionString;

            if (string.IsNullOrWhiteSpace(conn))
                throw new ArgumentException("Строка подключения не может быть пустой.", nameof(connectionString));

            return conn;
        }

        private static string BuildSchemaCacheKey(string connectionString, string schema, string table)
        {
            return (connectionString ?? string.Empty) + "|" + (schema ?? string.Empty) + "|" + (table ?? string.Empty);
        }

        private static ForeignKeyInfo CloneForeignKey(ForeignKeyInfo source)
        {
            if (source == null) return null;
            return new ForeignKeyInfo
            {
                Column = source.Column,
                RefTable = source.RefTable,
                RefColumn = source.RefColumn,
                ConstraintName = source.ConstraintName,
                IsNullable = source.IsNullable,
                TableSchema = source.TableSchema,
                TableName = source.TableName,
                RefSchema = source.RefSchema,
                RefTableName = source.RefTableName
            };
        }
    }
}
