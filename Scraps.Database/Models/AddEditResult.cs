using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>
    /// Результат операции AddRow или UpdateRow.
    /// </summary>
    public class AddEditResult
    {
        /// <summary>Успешно ли выполнена операция.</summary>
        public bool Success { get; set; }

        /// <summary>ID добавленной или изменённой записи (null при ошибке).</summary>
        public object RowId { get; set; }

        /// <summary>Сообщение об ошибке (null при успехе).</summary>
        public string Error { get; set; }

        /// <summary>Словарь созданных справочников: [имя_таблицы] = созданный_id.</summary>
        public Dictionary<string, object> CreatedLookups { get; set; } = new Dictionary<string, object>();
    }
}
