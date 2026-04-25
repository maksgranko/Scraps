using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Scraps.Database.Local
{
    /// <summary>
    /// CRUD-операции с JSON-таблицами.
    /// </summary>
    public class LocalDatabaseData : IDatabaseData
    {
        private string GetPath(string tableName) => Path.Combine(ScrapsConfig.LocalDataPath, tableName + ".json");

        public DataTable GetTableData(string tableName, params string[] columns)
        {
            var table = JsonTableSerializer.Load(GetPath(tableName));
            var dt = JsonTableSerializer.ToDataTable(table, tableName);

            if (columns != null && columns.Length > 0)
            {
                // Удаляем колонки, которых нет в списке
                for (int i = dt.Columns.Count - 1; i >= 0; i--)
                {
                    if (!columns.Contains(dt.Columns[i].ColumnName, StringComparer.OrdinalIgnoreCase))
                        dt.Columns.RemoveAt(i);
                }
            }

            return dt;
        }

        public DataTable GetTableData(string tableName, string connectionString, params string[] columns)
        {
            return GetTableData(tableName, columns);
        }

        public DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            // Файловое хранилище не поддерживает JOIN — возвращаем базовые данные
            return GetTableData(tableName, baseColumns);
        }

        public DataTable FindByColumn(string tableName, string columnName, object value, bool exactMatch = true)
        {
            var dt = GetTableData(tableName);
            if (!dt.Columns.Contains(columnName))
                return dt.Clone();

            var result = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                var rowValue = row[columnName]?.ToString() ?? "";
                var searchValue = value?.ToString() ?? "";

                bool match = exactMatch
                    ? rowValue.Equals(searchValue, StringComparison.OrdinalIgnoreCase)
                    : rowValue.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;

                if (match)
                    result.ImportRow(row);
            }
            return result;
        }

        public void ApplyTableChanges(string tableName, DataTable changes)
        {
            var path = GetPath(tableName);
            var table = JsonTableSerializer.Load(path);
            var dt = JsonTableSerializer.ToDataTable(table, tableName);

            // Применяем изменения: обновляем существующие строки по первичному ключу (если есть)
            // или просто заменяем весь DataTable
            foreach (DataRow changeRow in changes.Rows)
            {
                if (changeRow.RowState == DataRowState.Deleted)
                    continue;

                // Ищем строку для обновления
                bool updated = false;
                foreach (DataRow existingRow in dt.Rows)
                {
                    if (RowsEqualByKey(existingRow, changeRow, dt))
                    {
                        foreach (DataColumn col in dt.Columns)
                        {
                            existingRow[col] = changeRow[col];
                        }
                        updated = true;
                        break;
                    }
                }

                if (!updated)
                {
                    dt.ImportRow(changeRow);
                }
            }

            JsonTableSerializer.Save(path, JsonTableSerializer.FromDataTable(dt));
        }

        public void BulkInsert(string tableName, DataTable data)
        {
            var path = GetPath(tableName);
            var table = JsonTableSerializer.Load(path);
            var dt = JsonTableSerializer.ToDataTable(table, tableName);

            foreach (DataRow row in data.Rows)
            {
                dt.ImportRow(row);
            }

            JsonTableSerializer.Save(path, JsonTableSerializer.FromDataTable(dt));
        }

        public Dictionary<string, string> GetNx2Dictionary(string tableName, string keyColumn, string valueColumn)
        {
            var dt = GetTableData(tableName);
            var result = new Dictionary<string, string>();
            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains(keyColumn) && dt.Columns.Contains(valueColumn))
                {
                    result[row[keyColumn].ToString()] = row[valueColumn].ToString();
                }
            }
            return result;
        }

        public List<string> GetNx1List(string tableName, string columnName, bool distinct = true, bool sort = true)
        {
            var dt = GetTableData(tableName);
            var list = new List<string>();
            if (dt.Columns.Contains(columnName))
            {
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(row[columnName]?.ToString() ?? "");
                }
            }
            if (distinct)
                list = list.Distinct().ToList();
            if (sort)
                list.Sort();
            return list;
        }

        private static bool RowsEqualByKey(DataRow a, DataRow b, DataTable dt)
        {
            // Пытаемся найти колонку с "ID" для сравнения
            var idCol = dt.Columns.Cast<DataColumn>()
                .FirstOrDefault(c => c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase));

            if (idCol != null)
                return a[idCol].Equals(b[idCol]);

            // Fallback: сравниваем все колонки
            return dt.Columns.Cast<DataColumn>().All(c => a[c].Equals(b[c]));
        }
    }
}
