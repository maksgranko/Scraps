using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для работы с внешними ключами.
    /// </summary>
    public interface IForeignKeyProvider
    {
        /// <summary>Получить список внешних ключей таблицы.</summary>
        List<ForeignKeyInfo> GetForeignKeys(string tableName);

        /// <summary>Получить данные справочника для внешнего ключа.</summary>
        DataTable GetForeignKeyLookup(string tableName, string fkColumn);

        /// <summary>Получить элементы справочника.</summary>
        List<LookupItem> GetForeignKeyLookupItems(string tableName, string fkColumn);

        /// <summary>Определить колонку отображения.</summary>
        string ResolveDisplayColumn(string tableName, string idColumn = "ID");

        /// <summary>Получить метаданные для редактирования таблицы.</summary>
        TableEditMetadata GetTableEditMetadata(string tableName);
    }
}
