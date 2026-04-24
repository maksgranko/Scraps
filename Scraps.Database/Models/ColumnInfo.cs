namespace Scraps.Database
{
    /// <summary>Информация о колонке таблицы.</summary>
    public class ColumnInfo
    {
        /// <summary>Имя колонки.</summary>
        public string Name { get; set; }
        /// <summary>Тип данных.</summary>
        public string DataType { get; set; }
        /// <summary>Может ли содержать NULL.</summary>
        public bool IsNullable { get; set; }
        /// <summary>Является ли Identity-колонкой.</summary>
        public bool IsIdentity { get; set; }
        /// <summary>Максимальная длина (для строковых типов).</summary>
        public int? MaxLength { get; set; }
    }
}
