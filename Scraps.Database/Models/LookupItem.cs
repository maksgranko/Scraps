namespace Scraps.Database
{
    /// <summary>Элемент справочника (значение и отображаемое имя).</summary>
    public sealed class LookupItem
    {
        /// <summary>Значение (обычно ID).</summary>
        public object Value { get; set; }
        /// <summary>Отображаемое имя.</summary>
        public string Display { get; set; }
    }
}
