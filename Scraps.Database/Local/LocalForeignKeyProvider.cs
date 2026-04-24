using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Database.LocalFiles
{
    /// <summary>Провайдер внешних ключей для файлового JSON-хранилища.</summary>
    public class LocalForeignKeyProvider : IForeignKeyProvider
    {
        private readonly LocalDatabaseSchema _schema = new LocalDatabaseSchema();
        private readonly LocalDatabaseData _data = new LocalDatabaseData();

        /// <summary>Определить внешние ключи таблицы по соглашению имен колонок (XxxID -&gt; Xxx/Xxxs/Xxxes).</summary>
        public List<ForeignKeyInfo> GetForeignKeys(string tableName)
        {
            var result = new List<ForeignKeyInfo>();
            var columns = _schema.GetTableColumns(tableName);

            foreach (var col in columns)
            {
                if (col.EndsWith("ID", StringComparison.OrdinalIgnoreCase) && col.Length > 2)
                {
                    var possibleTable = col.Substring(0, col.Length - 2);
                    string match = null;
                    if (_schema.TableExists(possibleTable))
                        match = possibleTable;
                    else if (_schema.TableExists(possibleTable + "s"))
                        match = possibleTable + "s";
                    else if (_schema.TableExists(possibleTable + "es"))
                        match = possibleTable + "es";

                    if (match != null && !match.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new ForeignKeyInfo
                        {
                            ColumnName = col,
                            ReferenceTable = match,
                            ReferenceColumn = col,
                            ReferenceIdColumn = col
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>Получить таблицу-справочник для FK-колонки.</summary>
        public DataTable GetForeignKeyLookup(string tableName, string fkColumn)
        {
            var fk = GetForeignKeys(tableName).FirstOrDefault(f => f.ColumnName.Equals(fkColumn, StringComparison.OrdinalIgnoreCase));
            if (fk == null) return new DataTable();
            return _data.GetTableData(fk.ReferenceTable);
        }

        /// <summary>Получить элементы справочника FK в формате Id/Display.</summary>
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

        /// <summary>Определить колонку отображения для таблицы-справочника.</summary>
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

        /// <summary>Получить метаданные редактирования таблицы с признаком FK для колонок.</summary>
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
                    ForeignKey = fk
                });
            }

            return meta;
        }
    }
}
