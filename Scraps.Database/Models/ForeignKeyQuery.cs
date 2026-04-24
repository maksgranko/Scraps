using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>Параметры запроса для справочника внешнего ключа.</summary>
    public sealed class ForeignKeyQueryOptions
    {
        /// <summary>Колонки для выборки.</summary>
        public string[] Columns { get; set; }
        /// <summary>Колонка отображения.</summary>
        public string DisplayColumn { get; set; }
        /// <summary>Дополнительное условие WHERE (сырой SQL).</summary>
        public string Where { get; set; }
        /// <summary>Порядок сортировки (сырой SQL).</summary>
        public string OrderBy { get; set; }
        /// <summary>Безопасные условия фильтрации.</summary>
        public List<SqlFilterCondition> Filters { get; set; } = new List<SqlFilterCondition>();
        /// <summary>Безопасные условия сортировки.</summary>
        public List<SqlSortCondition> Sorts { get; set; } = new List<SqlSortCondition>();
    }

    /// <summary>Параметры расширения данных таблицы внешними ключами.</summary>
    public sealed class ExpandForeignKeysOptions
    {
        /// <summary>Строка подключения.</summary>
        public string ConnectionString { get; set; }
        /// <summary>Колонки основной таблицы.</summary>
        public string[] BaseColumns { get; set; }
        /// <summary>Раскрывать внешние ключи.</summary>
        public bool ExpandForeignKeys { get; set; } = true;
        /// <summary>Включать все колонки справочника.</summary>
        public bool IncludeReferenceAllColumns { get; set; } = false;
        /// <summary>Включать колонку отображения справочника.</summary>
        public bool IncludeReferenceDisplayColumn { get; set; } = true;
        /// <summary>Рекурсивный обход FK.</summary>
        public bool Recursive { get; set; } = false;
        /// <summary>Максимальная глубина рекурсии.</summary>
        public int MaxDepth { get; set; } = 2;
        /// <summary>Колонки для конкретных FK (ключ: constraint/table.column/column).</summary>
        public Dictionary<string, string[]> ForeignKeyColumns { get; set; } =
            new Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase);
        /// <summary>Переопределения колонок отображения (ключ: table или constraint).</summary>
        public Dictionary<string, string> DisplayColumnOverrides { get; set; } =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        /// <summary>Дополнительные условия WHERE для FK (ключ: constraint/table.column/column).</summary>
        public Dictionary<string, string> ForeignKeyWhere { get; set; } =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        /// <summary>Порядок сортировки для FK (ключ: constraint/table.column/column).</summary>
        public Dictionary<string, string> ForeignKeyOrderBy { get; set; } =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        /// <summary>Расширенная конфигурация для конкретных FK.</summary>
        public Dictionary<string, ForeignKeyQueryOptions> ForeignKeyQuery { get; set; } =
            new Dictionary<string, ForeignKeyQueryOptions>(System.StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Автоматически определять и добавлять колонку отображения для FK (DisplayName).
        /// </summary>
        public bool AutoResolveDisplayColumn { get; set; } = true;
    }
}
