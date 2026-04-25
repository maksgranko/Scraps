using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Редактор строк для файлового хранилища.
    /// </summary>
    public class LocalRowEditor : IRowEditor
    {
        private readonly LocalDatabaseData _data = new LocalDatabaseData();

        public AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, params ChildInsert[] children)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return new AddEditResult { Success = false, Error = "Название таблицы не может быть пустым." };
            if (values == null || values.Count == 0)
                return new AddEditResult { Success = false, Error = "Значения не могут быть пустыми." };

            try
            {
                var dt = _data.GetTableData(tableName);
                var newRow = dt.NewRow();

                foreach (var kv in values)
                {
                    if (dt.Columns.Contains(kv.Key))
                    {
                        newRow[kv.Key] = kv.Value ?? DBNull.Value;
                    }
                }

                dt.Rows.Add(newRow);
                _data.ApplyTableChanges(tableName, dt);

                // Получаем ID добавленной строки (если есть колонка ID)
                object rowId = null;
                var idCol = dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase));
                if (idCol != null)
                    rowId = newRow[idCol];

                // Обработка дочерних таблиц
                if (children != null && children.Length > 0)
                {
                    foreach (var child in children)
                    {
                        if (string.IsNullOrWhiteSpace(child.TableName)) continue;

                        var childValues = new Dictionary<string, object>(child.Values);
                        // Подставляем ParentID
                        var parentFkCol = childValues.Keys.FirstOrDefault(k => k.Equals(tableName + "ID", StringComparison.OrdinalIgnoreCase));
                        if (parentFkCol != null && rowId != null)
                        {
                            childValues[parentFkCol] = rowId;
                        }

                        var childResult = AddRow(child.TableName, childValues, strictFk);
                        if (!childResult.Success)
                            return childResult;
                    }
                }

                return new AddEditResult { Success = true, RowId = rowId };
            }
            catch (Exception ex)
            {
                return new AddEditResult { Success = false, Error = ex.Message };
            }
        }

        public AddEditResult UpdateRow(string tableName, string idColumn, object idValue, Dictionary<string, object> values, bool strictFk = true)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return new AddEditResult { Success = false, Error = "Название таблицы не может быть пустым." };
            if (string.IsNullOrWhiteSpace(idColumn))
                return new AddEditResult { Success = false, Error = "ID-колонка не может быть пустой." };
            if (values == null || values.Count == 0)
                return new AddEditResult { Success = false, Error = "Значения не могут быть пустыми." };

            try
            {
                var dt = _data.GetTableData(tableName);
                if (!dt.Columns.Contains(idColumn))
                    return new AddEditResult { Success = false, Error = $"Колонка '{idColumn}' не найдена." };

                bool updated = false;
                foreach (DataRow row in dt.Rows)
                {
                    if (row[idColumn].Equals(idValue))
                    {
                        foreach (var kv in values)
                        {
                            if (dt.Columns.Contains(kv.Key))
                            {
                                row[kv.Key] = kv.Value ?? DBNull.Value;
                            }
                        }
                        updated = true;
                    }
                }

                if (!updated)
                    return new AddEditResult { Success = false, Error = $"Запись с {idColumn}={idValue} не найдена." };

                _data.ApplyTableChanges(tableName, dt);
                return new AddEditResult { Success = true };
            }
            catch (Exception ex)
            {
                return new AddEditResult { Success = false, Error = ex.Message };
            }
        }
    }
}
