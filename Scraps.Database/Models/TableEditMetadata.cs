using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>Метаданные колонки для редактирования таблицы.</summary>
    public sealed class TableEditColumnMetadata
    {
        /// <summary>Имя колонки (алиас для Column).</summary>
        public string ColumnName { get => Column; set => Column = value; }
        /// <summary>Является ли внешним ключом.</summary>
        public bool IsForeignKey { get => ForeignKey != null; set { /* read-only computed */ } }
        /// <summary>Таблица-справочник (алиас для ForeignKey.RefTable).</summary>
        public string ReferenceTable { get => ForeignKey?.RefTable; set { if (ForeignKey != null) ForeignKey.RefTable = value; } }

        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>SQL-тип данных.</summary>
        public string DataType { get; set; }
        /// <summary>Может ли содержать NULL.</summary>
        public bool IsNullable { get; set; }
        /// <summary>Является ли Identity-колонкой.</summary>
        public bool IsIdentity { get; set; }
        /// <summary>Информация о внешнем ключе (null если не FK).</summary>
        public ForeignKeyInfo ForeignKey { get; set; }
        /// <summary>Колонка отображения для справочника.</summary>
        public string LookupDisplayColumn { get; set; }
    }

    /// <summary>Метаданные таблицы для редактирования.</summary>
    public sealed class TableEditMetadata
    {
        /// <summary>Имя таблицы.</summary>
        public string TableName { get; set; }
        /// <summary>Схема таблицы.</summary>
        public string TableSchema { get; set; }
        /// <summary>Список метаданных колонок.</summary>
        public List<TableEditColumnMetadata> Columns { get; set; } = new List<TableEditColumnMetadata>();
    }
}
