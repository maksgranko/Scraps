namespace Scraps.Database
{
    /// <summary>
    /// Описание JOIN по внешнему ключу для выборки данных.
    /// </summary>
    public sealed class ForeignKeyJoin
    {
        /// <summary>Колонка в основной таблице (например, RoleId).</summary>
        public string BaseColumn { get; set; }

        /// <summary>Таблица-справочник (например, Roles).</summary>
        public string ReferenceTable { get; set; }

        /// <summary>Колонка в таблице-справочнике, по которой выполняется JOIN (например, RoleID).</summary>
        public string ReferenceColumn { get; set; }

        /// <summary>
        /// Колонки из таблицы-справочника для выборки.
        /// Если пусто/null, выбираются все колонки (`*`) из справочника.
        /// </summary>
        public string[] ReferenceColumns { get; set; }

        /// <summary>
        /// Префикс алиаса для выбранных колонок справочника.
        /// По умолчанию используется имя таблицы-справочника.
        /// </summary>
        public string AliasPrefix { get; set; }

        /// <summary>
        /// Создать пустое описание JOIN по внешнему ключу.
        /// </summary>
        public ForeignKeyJoin() { }

        /// <summary>
        /// Создать описание JOIN по внешнему ключу.
        /// </summary>
        public ForeignKeyJoin(string baseColumn, string referenceTable, string referenceColumn, params string[] referenceColumns)
        {
            BaseColumn = baseColumn;
            ReferenceTable = referenceTable;
            ReferenceColumn = referenceColumn;
            ReferenceColumns = referenceColumns;
        }
    }
}
