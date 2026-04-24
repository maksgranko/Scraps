using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для редактирования строк.
    /// </summary>
    public interface IRowEditor
    {
        /// <summary>Добавить строку.</summary>
        AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, params ChildInsert[] children);

        /// <summary>Обновить строку.</summary>
        AddEditResult UpdateRow(string tableName, string idColumn, object idValue, Dictionary<string, object> values, bool strictFk = true);
    }
}
