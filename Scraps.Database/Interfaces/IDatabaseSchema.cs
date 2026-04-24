using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для работы со схемой базы данных.
    /// </summary>
    public interface IDatabaseSchema
    {
        /// <summary>Получить список таблиц.</summary>
        List<string> GetTables(bool includeSystem = false);

        /// <summary>Получить колонки таблицы.</summary>
        List<string> GetTableColumns(string tableName);

        /// <summary>Получить схему таблицы (DataTable).</summary>
        DataTable GetTableSchema(string tableName);

        /// <summary>Проверить, является ли колонка identity.</summary>
        bool IsIdentityColumn(string tableName, string columnName);

        /// <summary>Проверить, является ли колонка nullable.</summary>
        bool IsNullableColumn(string tableName, string columnName);
    }
}
