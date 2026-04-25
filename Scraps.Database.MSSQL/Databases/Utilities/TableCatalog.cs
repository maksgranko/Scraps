using System;
using System.Collections.Generic;
using System.Linq;

namespace Scraps.Databases.Utilities
{
    /// <summary>
    /// Каталог таблиц и утилиты инициализации списка.
    /// </summary>
    public static class TableCatalog
    {
        /// <summary>
        /// Инициализировать список таблиц с возможностью автодетекта, виртуальных таблиц и фильтрации.
        /// </summary>
        public static string[] InitializeTables(
            bool autodetect,
            string[] manualTables,
            string[] removeOnStart = null,
            string[] removeOnAutodetect = null,
            string[] virtualTables = null)
        {
            List<string> tablesTemp;
            List<string> tempDelete = new List<string>();

            if (removeOnStart != null) tempDelete.AddRange(removeOnStart);

            if (autodetect)
            {
                tablesTemp = global::Scraps.Database.Database.GetTables().ToList();
                if (removeOnAutodetect != null) tempDelete.AddRange(removeOnAutodetect);
            }
            else
            {
                tablesTemp = manualTables?.ToList() ?? new List<string>();
            }

            if (virtualTables != null && virtualTables.Length > 0)
            {
                tablesTemp.AddRange(virtualTables);
            }

            tablesTemp.RemoveAll(x => tempDelete.Contains(x));
            return tablesTemp.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

    }
}










