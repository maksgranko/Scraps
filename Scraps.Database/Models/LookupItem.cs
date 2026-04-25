namespace Scraps.Database
{
    /// <summary>Элемент справочника (значение и отображаемое имя).</summary>
    public sealed class LookupItem
    {
        /// <summary>Идентификатор (алиас для Value).</summary>
        public object Id { get => Value; set => Value = value; }
        /// <summary>Значение (обычно ID).</summary>
        public object Value { get; set; }
        /// <summary>Отображаемое имя.</summary>
        public string Display { get; set; }
    }
}
