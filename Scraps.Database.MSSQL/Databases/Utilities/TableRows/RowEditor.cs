using Scraps.Configs;
using Scraps.Database.MSSQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using AddEditResult = Scraps.Database.AddEditResult;
using ChildInsert = Scraps.Database.ChildInsert;
using ForeignKeyInfo = Scraps.Database.ForeignKeyInfo;
using TableEditMetadata = Scraps.Database.TableEditMetadata;

namespace Scraps.Database.MSSQL.Utilities.TableRows
{
    /// <summary>
    /// Универсальные операции Add/Edit с автоматическим разрешением Foreign Key.
    /// </summary>
    public static class RowEditor
    {
        #region --- Public API ---

        /// <summary>
        /// Добавить строку в таблицу с автоматическим разрешением FK.
        /// </summary>
        public static AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, ChildInsert[] children = null)
        {
            return AddRow(tableName, values, ScrapsConfig.ConnectionString, strictFk, children);
        }

        /// <summary>
        /// Добавить строку в таблицу с указанной строкой подключения.
        /// </summary>
        public static AddEditResult AddRow(string tableName, Dictionary<string, object> values, string connectionString, bool strictFk = true, ChildInsert[] children = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return Fail("Название таблицы не может быть пустым.");
            if (values == null || values.Count == 0)
                return Fail("Значения не могут быть пустыми.");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var result = InsertRow(tableName, values, conn, tx, strictFk, out var resolvedRowId);
                        if (!result.Success)
                        {
                            tx.Rollback();
                            return result;
                        }

                        // Обработка дочерних таблиц
                        if (children != null && children.Length > 0)
                        {
                            var fkMeta = GetParentFkMetadata(tableName, children, conn, tx);
                            foreach (var child in children)
                            {
                                if (string.IsNullOrWhiteSpace(child.TableName)) continue;

                                // Подставляем ParentID в дочерние значения
                                var childValues = new Dictionary<string, object>(child.Values);
                                if (fkMeta.TryGetValue(child.TableName, out var parentCol))
                                {
                                    childValues[parentCol] = resolvedRowId;
                                }

                                var childResult = InsertRow(child.TableName, childValues, conn, tx, strictFk, out _);
                                if (!childResult.Success)
                                {
                                    tx.Rollback();
                                    return childResult;
                                }
                            }
                        }

                        tx.Commit();
                        result.RowId = resolvedRowId;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка AddRow: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновить строку в таблице с автоматическим разрешением FK.
        /// </summary>
        public static AddEditResult UpdateRow(string tableName, string keyColumn, object keyValue, Dictionary<string, object> values, bool strictFk = true)
        {
            return UpdateRow(tableName, keyColumn, keyValue, values, ScrapsConfig.ConnectionString, strictFk);
        }

        /// <summary>
        /// Обновить строку в таблице с указанной строкой подключения.
        /// </summary>
        public static AddEditResult UpdateRow(string tableName, string keyColumn, object keyValue, Dictionary<string, object> values, string connectionString, bool strictFk = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return Fail("Название таблицы не может быть пустым.");
            if (string.IsNullOrWhiteSpace(keyColumn))
                return Fail("Ключевая колонка не может быть пустой.");
            if (values == null || values.Count == 0)
                return Fail("Значения не могут быть пустыми.");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var result = UpdateRowInternal(tableName, keyColumn, keyValue, values, conn, tx, strictFk);
                        if (result.Success)
                            tx.Commit();
                        else
                            tx.Rollback();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                return Fail($"Ошибка UpdateRow: {ex.Message}");
            }
        }

        #endregion

        #region --- Insert Logic ---

        private static AddEditResult InsertRow(string tableName, Dictionary<string, object> values, SqlConnection conn, SqlTransaction tx, bool strictFk, out object rowId)
        {
            rowId = null;

            var meta = GetTableMetadata(tableName, conn, tx);
            if (meta == null)
                return Fail($"Не удалось получить метаданные таблицы '{tableName}'.");

            var resolved = ResolveValues(tableName, values, meta, conn, tx, strictFk, out var error);
            if (resolved == null)
                return Fail(error);

            // Определяем Identity-колонку (исключаем из INSERT если значение не передано)
            var identityCol = meta.Columns.FirstOrDefault(c => c.IsIdentity)?.Column;
            var insertCols = new List<string>();
            var insertParams = new List<string>();
            var parameters = new List<SqlParameter>();
            int paramIdx = 0;

            foreach (var kv in resolved)
            {
                // Если Identity и значение не передано — пропускаем
                if (kv.Key == identityCol && kv.Value == null)
                    continue;

                insertCols.Add(MSSQL.QuoteIdentifier(kv.Key));
                insertParams.Add($"@p{paramIdx}");
                parameters.Add(new SqlParameter($"@p{paramIdx}", kv.Value ?? (object)DBNull.Value));
                paramIdx++;
            }

            if (insertCols.Count == 0)
                return Fail("Нет колонок для вставки.");

            var sql = $"INSERT INTO {MSSQL.QuoteIdentifier(tableName)} ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertParams)})";
            if (identityCol != null)
                sql += "; SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                var result = cmd.ExecuteScalar();
                rowId = result == null || result == DBNull.Value ? null : result;
            }

            return new AddEditResult { Success = true };
        }

        #endregion

        #region --- Update Logic ---

        private static AddEditResult UpdateRowInternal(string tableName, string keyColumn, object keyValue, Dictionary<string, object> values, SqlConnection conn, SqlTransaction tx, bool strictFk)
        {
            var meta = GetTableMetadata(tableName, conn, tx);
            if (meta == null)
                return Fail($"Не удалось получить метаданные таблицы '{tableName}'.");

            var resolved = ResolveValues(tableName, values, meta, conn, tx, strictFk, out var error);
            if (resolved == null)
                return Fail(error);

            var setParts = new List<string>();
            var parameters = new List<SqlParameter>();
            int paramIdx = 0;

            foreach (var kv in resolved)
            {
                setParts.Add($"{MSSQL.QuoteIdentifier(kv.Key)} = @p{paramIdx}");
                parameters.Add(new SqlParameter($"@p{paramIdx}", kv.Value ?? (object)DBNull.Value));
                paramIdx++;
            }

            var sql = $"UPDATE {MSSQL.QuoteIdentifier(tableName)} SET {string.Join(", ", setParts)} WHERE {MSSQL.QuoteIdentifier(keyColumn)} = @key";
            parameters.Add(new SqlParameter("@key", keyValue ?? (object)DBNull.Value));

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.ExecuteNonQuery();
            }

            return new AddEditResult { Success = true, RowId = keyValue };
        }

        #endregion

        #region --- FK Resolution ---

        private static Dictionary<string, object> ResolveValues(string tableName, Dictionary<string, object> values, TableEditMetadata meta, SqlConnection conn, SqlTransaction tx, bool strictFk, out string error)
        {
            error = null;
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var fks = meta.Columns.Where(c => c.ForeignKey != null).ToDictionary(c => c.Column, c => c.ForeignKey, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in values)
            {
                var colName = kv.Key;
                var rawValue = kv.Value;

                // null / DBNull → NULL
                if (rawValue == null || rawValue == DBNull.Value)
                {
                    result[colName] = null;
                    continue;
                }

                // Если не FK → использовать как есть
                if (!fks.TryGetValue(colName, out var fk))
                {
                    result[colName] = rawValue;
                    continue;
                }

                // FK-разрешение
                var resolved = ResolveFkValue(fk, rawValue, conn, tx, strictFk, out error);
                if (error != null)
                    return null;

                result[colName] = resolved;
            }

            return result;
        }

        private static object ResolveFkValue(ForeignKeyInfo fk, object rawValue, SqlConnection conn, SqlTransaction tx, bool strictFk, out string error)
        {
            error = null;
            var refTable = fk.RefTable;
            var refCol = fk.RefColumn;

            // Если int → проверить существование
            if (rawValue is int || rawValue is long || rawValue is short || rawValue is byte)
            {
                var intVal = Convert.ToInt32(rawValue);
                if (!LookupExists(refTable, refCol, intVal, conn, tx))
                    error = $"Справочник '{refTable}': значение {intVal} не найдено.";
                return intVal;
            }

            // Если string → искать по DisplayColumn
            if (rawValue is string strValue)
            {
                if (string.IsNullOrWhiteSpace(strValue))
                    return null; // пустая строка = NULL

                var displayCol = GetDisplayColumn(refTable, conn, tx);
                if (displayCol == null)
                {
                    error = $"Не удалось определить колонку отображения для справочника '{refTable}'.";
                    return null;
                }

                // Проверить совпадение типов
                if (!IsStringType(refTable, displayCol, conn, tx))
                {
                    error = $"Несовпадение типов: входное значение '{strValue}' (string) несовместимо с колонкой '{displayCol}' в '{refTable}'.";
                    return null;
                }

                var foundId = FindByDisplayValue(refTable, refCol, displayCol, strValue, conn, tx);
                if (foundId != null)
                    return foundId;

                if (strictFk)
                {
                    error = $"Справочник '{refTable}': значение '{strValue}' не найдено (strict режим).";
                    return null;
                }

                // Авто-создание в справочнике
                return AutoCreateLookup(refTable, displayCol, strValue, conn, tx, out error);
            }

            // Другие типы — пока не поддерживаем
            error = $"Неподдерживаемый тип значения для FK: {rawValue.GetType().Name}";
            return null;
        }

        #endregion

        #region --- Lookup Helpers ---

        private static string GetDisplayColumn(string refTable, SqlConnection conn, SqlTransaction tx)
        {
            // Пытаемся использовать ResolveDisplayColumn
            try
            {
                return MSSQL.ResolveDisplayColumn(refTable);
            }
            catch
            {
                // Fallback: первая не-ID колонка
                var cols = GetTableColumns(refTable, conn, tx);
                return cols.FirstOrDefault(c => !c.Equals("ID", StringComparison.OrdinalIgnoreCase)
                    && !c.EndsWith("ID", StringComparison.OrdinalIgnoreCase));
            }
        }

        private static bool IsStringType(string tableName, string columnName, SqlConnection conn, SqlTransaction tx)
        {
            var sql = $@"
                SELECT DATA_TYPE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = {QuoteLiteral(ExtractTableName(tableName))} 
                AND COLUMN_NAME = {QuoteLiteral(columnName)}";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                var result = cmd.ExecuteScalar();
                if (result == null) return false;
                var type = result.ToString().ToLower();
                return type.Contains("char") || type.Contains("text") || type.Contains("varchar") || type.Contains("nvarchar");
            }
        }

        private static object FindByDisplayValue(string refTable, string refCol, string displayCol, string value, SqlConnection conn, SqlTransaction tx)
        {
            var sql = $@"
                SELECT {MSSQL.QuoteIdentifier(refCol)} 
                FROM {MSSQL.QuoteIdentifier(refTable)} 
                WHERE {MSSQL.QuoteIdentifier(displayCol)} = @val";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@val", value);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : result;
            }
        }

        private static bool LookupExists(string refTable, string refCol, object value, SqlConnection conn, SqlTransaction tx)
        {
            var sql = $@"
                SELECT COUNT(1) 
                FROM {MSSQL.QuoteIdentifier(refTable)} 
                WHERE {MSSQL.QuoteIdentifier(refCol)} = @val";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@val", value ?? (object)DBNull.Value);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static object AutoCreateLookup(string refTable, string displayCol, string value, SqlConnection conn, SqlTransaction tx, out string error)
        {
            error = null;

            // Проверяем что таблица Nx1 или Nx2
            var cols = GetTableColumns(refTable, conn, tx);
            if (cols.Length > 2)
            {
                error = $"Справочник '{refTable}' имеет {cols.Length} колонок. Авто-создание возможно только для Nx1/Nx2 таблиц.";
                return null;
            }

            var sql = $"INSERT INTO {MSSQL.QuoteIdentifier(refTable)} ({MSSQL.QuoteIdentifier(displayCol)}) VALUES (@val); SELECT SCOPE_IDENTITY();";
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@val", value);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : result;
            }
        }

        #endregion

        #region --- Metadata & Child Helpers ---

        private static TableEditMetadata GetTableMetadata(string tableName, SqlConnection conn, SqlTransaction tx)
        {
            try
            {
                return MSSQL.GetTableEditMetadata(tableName);
            }
            catch
            {
                return null;
            }
        }

        private static string[] GetTableColumns(string tableName, SqlConnection conn, SqlTransaction tx)
        {
            var sql = $@"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = {QuoteLiteral(ExtractTableName(tableName))}
                ORDER BY ORDINAL_POSITION";

            var cols = new List<string>();
            using (var cmd = new SqlCommand(sql, conn, tx))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    cols.Add(reader[0].ToString());
            }
            return cols.ToArray();
        }

        private static Dictionary<string, string> GetParentFkMetadata(string parentTable, ChildInsert[] children, SqlConnection conn, SqlTransaction tx)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parentKey = GetPrimaryKeyColumn(parentTable, conn, tx);
            if (string.IsNullOrWhiteSpace(parentKey)) return result;

            foreach (var child in children)
            {
                if (string.IsNullOrWhiteSpace(child.TableName)) continue;
                var fks = MSSQL.GetForeignKeys(child.TableName);
                var match = fks.FirstOrDefault(fk =>
                    fk.RefTable.Equals(parentTable, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    result[child.TableName] = match.Column;
            }
            return result;
        }

        private static string GetPrimaryKeyColumn(string tableName, SqlConnection conn, SqlTransaction tx)
        {
            var sql = $@"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 
                AND TABLE_NAME = {QuoteLiteral(tableName)}";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }

        #endregion

        #region --- Utilities ---

        private static AddEditResult Fail(string error)
        {
            return new AddEditResult { Success = false, Error = error };
        }

        private static string QuoteLiteral(string value)
        {
            if (string.IsNullOrEmpty(value)) return "''";
            return "'" + value.Replace("'", "''") + "'";
        }

        /// <summary>
        /// Извлечь имя таблицы без схемы (dbo.Roles → Roles).
        /// </summary>
        private static string ExtractTableName(string refTable)
        {
            if (string.IsNullOrWhiteSpace(refTable))
                return refTable;
            var dot = refTable.LastIndexOf('.');
            return dot >= 0 ? refTable.Substring(dot + 1) : refTable;
        }

        #endregion
    }
}
