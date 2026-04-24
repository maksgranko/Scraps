using System;
using System.Data;
using System.IO;
using System.Linq;
using Scraps.Configs;

namespace Scraps.Database.LocalFiles.Sql
{
    /// <summary>Исполнитель SELECT-операторов.</summary>
    public static class SelectExecutor
    {
        /// <summary>Выполнить SELECT и вернуть результат в DataTable.</summary>
        public static DataTable Execute(SelectStatement stmt)
        {
            var dt = LoadTable(stmt.TableName);
            var result = dt.Clone();

            foreach (DataRow row in dt.Rows)
            {
                if (WhereEvaluator.Evaluate(row, stmt.Where?.Predicate))
                {
                    var newRow = result.NewRow();
                if (stmt.IsCountAll)
                {
                    // handled after loop
                    continue;
                }
                if (stmt.Columns.Count == 1 && stmt.Columns[0] == "*")
                {
                    foreach (DataColumn col in dt.Columns)
                        newRow[col.ColumnName] = row[col.ColumnName];
                }
                else
                {
                    foreach (var col in stmt.Columns)
                    {
                        if (dt.Columns.Contains(col))
                            newRow[col] = row[col];
                    }
                }
                    result.Rows.Add(newRow);
                }
            }

            if (stmt.IsCountAll)
            {
                result = new DataTable();
                result.Columns.Add("Count", typeof(int));
                var countRow = result.NewRow();
                countRow[0] = dt.Rows.Cast<DataRow>().Count(r => WhereEvaluator.Evaluate(r, stmt.Where?.Predicate));
                result.Rows.Add(countRow);
            }

            return result;
        }

        private static DataTable LoadTable(string tableName)
        {
            var path = Path.Combine(ScrapsConfig.LocalDataPath, tableName + ".json");
            var table = JsonTableSerializer.Load(path);
            return JsonTableSerializer.ToDataTable(table, tableName);
        }
    }

    /// <summary>Исполнитель INSERT-операторов.</summary>
    public static class InsertExecutor
    {
        /// <summary>Выполнить INSERT и вернуть количество добавленных строк.</summary>
        public static int Execute(InsertStatement stmt)
        {
            var path = Path.Combine(ScrapsConfig.LocalDataPath, stmt.TableName + ".json");
            var table = JsonTableSerializer.Load(path);
            var dt = JsonTableSerializer.ToDataTable(table, stmt.TableName);

            var row = dt.NewRow();
            for (int i = 0; i < stmt.Columns.Count && i < stmt.Values.Count; i++)
            {
                var col = stmt.Columns[i];
                var val = stmt.Values[i];
                if (dt.Columns.Contains(col))
                {
                    row[col] = val ?? DBNull.Value;
                }
            }

            // Handle IDENTITY columns not specified in INSERT
            foreach (DataColumn col in dt.Columns)
            {
                if (!stmt.Columns.Contains(col.ColumnName))
                {
                    var schemaEntry = table.Schema.FirstOrDefault(s => s.Name == col.ColumnName);
                    if (schemaEntry != null && schemaEntry.Type.StartsWith("Int32") || col.DataType == typeof(int))
                    {
                        // Auto-increment
                        int maxId = 0;
                        foreach (DataRow existing in dt.Rows)
                        {
                            if (int.TryParse(existing[col.ColumnName]?.ToString(), out var id) && id > maxId)
                                maxId = id;
                        }
                        row[col.ColumnName] = maxId + 1;
                    }
                }
            }

            dt.Rows.Add(row);
            JsonTableSerializer.Save(path, JsonTableSerializer.FromDataTable(dt));
            return 1;
        }
    }

    /// <summary>Исполнитель UPDATE-операторов.</summary>
    public static class UpdateExecutor
    {
        /// <summary>Выполнить UPDATE и вернуть количество обновленных строк.</summary>
        public static int Execute(UpdateStatement stmt)
        {
            var path = Path.Combine(ScrapsConfig.LocalDataPath, stmt.TableName + ".json");
            var table = JsonTableSerializer.Load(path);
            var dt = JsonTableSerializer.ToDataTable(table, stmt.TableName);

            int count = 0;
            foreach (DataRow row in dt.Rows)
            {
                if (WhereEvaluator.Evaluate(row, stmt.Where?.Predicate))
                {
                    foreach (var assign in stmt.Assignments)
                    {
                        if (dt.Columns.Contains(assign.Column))
                            row[assign.Column] = assign.Value ?? DBNull.Value;
                    }
                    count++;
                }
            }

            if (count > 0)
                JsonTableSerializer.Save(path, JsonTableSerializer.FromDataTable(dt));
            return count;
        }
    }

    /// <summary>Исполнитель DELETE-операторов.</summary>
    public static class DeleteExecutor
    {
        /// <summary>Выполнить DELETE и вернуть количество удаленных строк.</summary>
        public static int Execute(DeleteStatement stmt)
        {
            var path = Path.Combine(ScrapsConfig.LocalDataPath, stmt.TableName + ".json");
            var table = JsonTableSerializer.Load(path);
            var dt = JsonTableSerializer.ToDataTable(table, stmt.TableName);

            int count = 0;
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (WhereEvaluator.Evaluate(dt.Rows[i], stmt.Where?.Predicate))
                {
                    dt.Rows[i].Delete();
                    count++;
                }
            }

            if (count > 0)
            {
                dt.AcceptChanges();
                JsonTableSerializer.Save(path, JsonTableSerializer.FromDataTable(dt));
            }
            return count;
        }
    }

    /// <summary>Исполнитель CREATE TABLE-операторов.</summary>
    public static class CreateTableExecutor
    {
        /// <summary>Создать таблицу и вернуть 0 при успехе.</summary>
        public static int Execute(CreateTableStatement stmt)
        {
            var path = Path.Combine(ScrapsConfig.LocalDataPath, stmt.TableName + ".json");
            if (File.Exists(path))
                throw new InvalidOperationException($"Table '{stmt.TableName}' already exists.");

            var table = new JsonTable();
            foreach (var col in stmt.Columns)
            {
                string netType = MapSqlTypeToNetType(col.Type);
                table.Schema.Add(new SchemaEntry { Name = col.Name, Type = netType });
            }
            JsonTableSerializer.Save(path, table);
            return 0;
        }

        private static string MapSqlTypeToNetType(string sqlType)
        {
            var baseType = sqlType?.Split('(')[0]?.Trim().ToLowerInvariant() ?? "string";
            switch (baseType)
            {
                case "int": return "Int32";
                case "bigint": return "Int64";
                case "smallint": return "Int16";
                case "tinyint": return "Byte";
                case "bit": return "Boolean";
                case "datetime": case "datetime2": case "date": case "smalldatetime": return "DateTime";
                case "float": case "real": return "Double";
                case "decimal": case "numeric": case "money": case "smallmoney": return "Decimal";
                case "uniqueidentifier": return "Guid";
                default: return "String";
            }
        }
    }

    /// <summary>Исполнитель DROP TABLE-операторов.</summary>
    public static class DropTableExecutor
    {
        /// <summary>Удалить таблицу и вернуть 0 при успехе.</summary>
        public static int Execute(DropTableStatement stmt)
        {
            var path = Path.Combine(ScrapsConfig.LocalDataPath, stmt.TableName + ".json");
            if (File.Exists(path))
                File.Delete(path);
            return 0;
        }
    }
}
