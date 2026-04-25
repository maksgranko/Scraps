using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Scraps.Database.LocalFiles
{
    /// <summary>
    /// CRUD-операции с JSON-таблицами.
    /// </summary>
    public class LocalDatabaseData : IDatabaseData
    {
        private string GetPath(string tableName) => Path.Combine(ScrapsConfig.LocalDataPath, tableName + ".json");

        /// <summary>Загрузить данные таблицы из JSON-файла. Если <paramref name="columns"/> заданы — оставить только указанные колонки.</summary>
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

        /// <summary>Загрузить данные таблицы с явной строкой подключения (игнорируется, используется ScrapsConfig).</summary>
        public DataTable GetTableData(string tableName, string connectionString, params string[] columns)
        {
            return GetTableData(tableName, columns);
        }

        /// <summary>Загрузить данные с разворачиванием FK (в файловом хранилище JOIN не поддерживается — возвращает базовые данные).</summary>
        public DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            // Файловое хранилище не поддерживает JOIN — возвращаем базовые данные
            return GetTableData(tableName, baseColumns);
        }

        /// <summary>Найти строки по значению колонки.</summary>
        public DataTable FindByColumn(string tableName, string columnName, object value, SqlFilterOperator op = SqlFilterOperator.Eq)
        {
            var dt = GetTableData(tableName);
            if (!dt.Columns.Contains(columnName))
                return dt.Clone();

            var result = dt.Clone();
            foreach (DataRow row in dt.Rows)
            {
                var rowValue = row[columnName]?.ToString() ?? "";
                var searchValue = value?.ToString() ?? "";

                bool match;
                switch (op)
                {
                    case SqlFilterOperator.Like:
                        match = rowValue.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case SqlFilterOperator.IsNull:
                        match = string.IsNullOrEmpty(rowValue);
                        break;
                    case SqlFilterOperator.IsNotNull:
                        match = !string.IsNullOrEmpty(rowValue);
                        break;
                    case SqlFilterOperator.Eq:
                    default:
                        match = rowValue.Equals(searchValue, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                if (match)
                    result.ImportRow(row);
            }
            return result;
        }

        /// <summary>Сохранить изменения DataTable в JSON-файл (заменяет всё содержимое файла).</summary>
        public void ApplyTableChanges(string tableName, DataTable changes)
        {
            changes.AcceptChanges();
            var path = GetPath(tableName);
            JsonTableSerializer.Save(path, JsonTableSerializer.FromDataTable(changes));
        }

        /// <summary>Массовая вставка: добавить все строки из <paramref name="data"/> в существующую таблицу.</summary>
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

        /// <summary>Получить словарь «ключ → значение» из двух колонок таблицы.</summary>
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

        /// <summary>Получить список значений одной колонки. При <paramref name="distinct"/> дубликаты удаляются, при <paramref name="sort"/> — сортируется.</summary>
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
            var idColName = dt.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.ColumnName)
                .FirstOrDefault();

            if (idColName != null && b.Table.Columns.Contains(idColName))
                return a[idColName].Equals(b[idColName]);

            return dt.Columns.Cast<DataColumn>()
                .Where(c => b.Table.Columns.Contains(c.ColumnName))
                .All(c => a[c.ColumnName].Equals(b[c.ColumnName]));
        }
    }
}
