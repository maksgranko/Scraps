using Scraps.Databases;
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
        /// Получить таблицу из БД и перевести названия колонок.
        /// </summary>
        public static DataTable GetTableTranslated(string tableName)
        {
            var dt = MSSQL.GetTableData(tableName);
            return TranslationManager.TranslateDataTable(dt, tableName);
        }

        /// <summary>
        /// Получить DataTable по SQL и (опционально) перевести колонки.
        /// </summary>
        public static DataTable GetBySql(string sql, string tableNameForTranslations = null)
        {
            var dt = MSSQL.GetDataTableFromSQL(sql);
            if (!string.IsNullOrWhiteSpace(tableNameForTranslations))
            {
                TranslationManager.TranslateDataTable(dt, tableNameForTranslations);
            }
            return dt;
        }
    }
}




