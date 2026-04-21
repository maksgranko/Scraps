using System.Collections.Generic;

namespace Scraps.Databases.Utilities.TableRows
{
    /// <summary>
    /// Описание связанной (дочерней) вставки при AddRow.
    /// </summary>
    public class ChildInsert
    {
        /// <summary>Имя дочерней таблицы.</summary>
        public string TableName { get; set; }

        /// <summary>Значения для вставки (FK на родителя будут подставлены автоматически).</summary>
        public Dictionary<string, object> Values { get; set; }

        /// <summary>Создать дочернюю вставку.</summary>
        public ChildInsert(string tableName, Dictionary<string, object> values)
        {
            TableName = tableName;
            Values = values ?? new Dictionary<string, object>();
        }
    }
}
