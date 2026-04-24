using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для работы с данными.
    /// </summary>
    public interface IDatabaseData
    {
        /// <summary>Получить данные таблицы.</summary>
        DataTable GetTableData(string tableName, params string[] columns);

        /// <summary>Получить данные таблицы с указанным подключением.</summary>
        DataTable GetTableData(string tableName, string connectionString, params string[] columns);

        /// <summary>Получить данные с разворачиванием внешних ключей.</summary>
        DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns);

        /// <summary>Найти записи по значению колонки.</summary>
        DataTable FindByColumn(string tableName, string columnName, object value, bool exactMatch = true);

        /// <summary>Применить изменения к таблице.</summary>
        void ApplyTableChanges(string tableName, DataTable changes);

        /// <summary>Массовая вставка.</summary>
        void BulkInsert(string tableName, DataTable data);

        /// <summary>Получить словарь Nx2.</summary>
        Dictionary<string, string> GetNx2Dictionary(string tableName, string keyColumn, string valueColumn);

        /// <summary>Получить список Nx1.</summary>
        List<string> GetNx1List(string tableName, string columnName, bool distinct = true, bool sort = true);
    }
}
