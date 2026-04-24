using Scraps.Database;
using static Scraps.Database.Current;
using Scraps.Localization;
using System.Data;

namespace Scraps.Export
{
    /// <summary>
    /// Получение данных для отчётов.
    /// </summary>
    public static class ReportDataBuilder
    {
        /// <summary>
        /// Получить DataTable по SQL и (опционально) перевести колонки.
        /// </summary>
        public static DataTable GetBySql(string sql, string tableNameForTranslations = null)
        {
            var dt = GetDataTable(sql);
            if (!string.IsNullOrWhiteSpace(tableNameForTranslations))
            {
                TranslationManager.Translate(dt, tableNameForTranslations);
            }
            return dt;
        }
    }
}





