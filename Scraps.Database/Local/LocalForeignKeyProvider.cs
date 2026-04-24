using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Провайдер внешних ключей для файлового хранилища.
    /// FK определяются по именам колонок (оканчивающимся на ID и совпадающим с именем таблицы).
    /// </summary>
    internal class LocalForeignKeyProvider : IForeignKeyProvider
    {
        private readonly LocalDatabaseSchema _schema = new LocalDatabaseSchema();
        private readonly LocalDatabaseData _data = new LocalDatabaseData();

        public List<ForeignKeyInfo> GetForeignKeys(string tableName)
        {
            var result = new List<ForeignKeyInfo>();
            var columns = _schema.GetTableColumns(tableName);
            var tables = _schema.GetTables();

            foreach (var col in columns)
            {
                if (col.EndsWith("ID", StringComparison.OrdinalIgnoreCase) && col.Length > 2)
                {
                    var possibleTable = col.Substring(0, col.Length - 2);
                    if (tables.Any(t => t.Equals(possibleTable, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(new ForeignKeyInfo
                        {
                            ColumnName = col,
                            ReferenceTable = possibleTable,
                            ReferenceColumn = col,
                            ReferenceIdColumn = col
                        });
                    }
                }
            }

            return result;
        }

        public DataTable GetForeignKeyLookup(string tableName, string fkColumn)
        {
            var fk = GetForeignKeys(tableName).FirstOrDefault(f => f.ColumnName.Equals(fkColumn, StringComparison.OrdinalIgnoreCase));
            if (fk == null) return new DataTable();
            return _data.GetTableData(fk.ReferenceTable);
        }

        public List<LookupItem> GetForeignKeyLookupItems(string tableName, string fkColumn)
        {
            var dt = GetForeignKeyLookup(tableName, fkColumn);
            var result = new List<LookupItem>();
            var displayCol = ResolveDisplayColumn(dt.TableName);
            var idCol = dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase));

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new LookupItem
                {
                    Id = idCol != null ? row[idCol] : null,
                    Display = row[displayCol]?.ToString()
                });
            }

            return result;
        }

        public string ResolveDisplayColumn(string tableName, string idColumn = "ID")
        {
            var columns = _schema.GetTableColumns(tableName);

            // Сначала проверяем override из ScrapsConfig
            if (ScrapsConfig.ForeignKeyDisplayColumnOverrides.TryGetValue(tableName, out var overrideCol))
                return overrideCol;

            // Проверяем preferred columns
            foreach (var preferred in ScrapsConfig.ForeignKeyDisplayColumnPreferred)
            {
                var match = columns.FirstOrDefault(c => c.Equals(preferred, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            // Fallback: первая колонка, не являющаяся ID
            var fallback = columns.FirstOrDefault(c => !c.Equals(idColumn, StringComparison.OrdinalIgnoreCase) && !c.EndsWith("ID", StringComparison.OrdinalIgnoreCase));
            return fallback ?? columns.FirstOrDefault() ?? idColumn;
        }

        public TableEditMetadata GetTableEditMetadata(string tableName)
        {
            var columns = _schema.GetTableColumns(tableName);
            var fks = GetForeignKeys(tableName);
            var meta = new TableEditMetadata { TableName = tableName };

            foreach (var col in columns)
            {
                var fk = fks.FirstOrDefault(f => f.ColumnName.Equals(col, StringComparison.OrdinalIgnoreCase));
                meta.Columns.Add(new TableEditColumnMetadata
                {
                    ColumnName = col,
                    IsForeignKey = fk != null,
                    ReferenceTable = fk?.ReferenceTable
                });
            }

            return meta;
        }
    }
}
