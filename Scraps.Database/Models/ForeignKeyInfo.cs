namespace Scraps.Database
{
    /// <summary>Информация о внешнем ключе таблицы.</summary>
    public sealed class ForeignKeyInfo
    {
        /// <summary>Колонка в основной таблице (алиас для Column).</summary>
        public string ColumnName { get => Column; set => Column = value; }
        /// <summary>Таблица-справочник (алиас для RefTable).</summary>
        public string ReferenceTable { get => RefTable; set => RefTable = value; }
        /// <summary>Колонка в таблице-справочнике (алиас для RefColumn).</summary>
        public string ReferenceColumn { get => RefColumn; set => RefColumn = value; }
        /// <summary>ID-колонка в таблице-справочнике (алиас для RefTableName).</summary>
        public string ReferenceIdColumn { get => RefTableName; set => RefTableName = value; }

        /// <summary>Колонка в основной таблице.</summary>
        public string Column { get; set; }
        /// <summary>Таблица-справочник (полное имя со схемой).</summary>
        public string RefTable { get; set; }
        /// <summary>Колонка в таблице-справочнике.</summary>
        public string RefColumn { get; set; }
        /// <summary>Имя ограничения (constraint).</summary>
        public string ConstraintName { get; set; }
        /// <summary>Может ли колонка содержать NULL.</summary>
        public bool IsNullable { get; set; }
        /// <summary>Схема основной таблицы.</summary>
        public string TableSchema { get; set; }
        /// <summary>Имя основной таблицы.</summary>
        public string TableName { get; set; }
        /// <summary>Схема таблицы-справочника.</summary>
        public string RefSchema { get; set; }
        /// <summary>Имя таблицы-справочника.</summary>
        public string RefTableName { get; set; }
    }
}
