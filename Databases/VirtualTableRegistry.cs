using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Databases
{
    /// <summary>
    /// Реестр виртуальных таблиц (имя -> SQL запрос).
    /// </summary>
    public static class VirtualTableRegistry
    {
        private static readonly Dictionary<string, string> Queries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Зарегистрировать виртуальную таблицу.
        /// </summary>
        public static void Register(string name, string sql)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            Queries[name] = sql;
        }

        /// <summary>
        /// Зарегистрировать несколько виртуальных таблиц.
        /// </summary>
        public static void RegisterMany(IDictionary<string, string> queries)
        {
            if (queries == null) throw new ArgumentNullException(nameof(queries));
            foreach (var kv in queries)
            {
                Register(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Удалить виртуальную таблицу по имени.
        /// </summary>
        public static bool Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Queries.Remove(name);
        }

        /// <summary>
        /// Очистить все виртуальные таблицы.
        /// </summary>
        public static void Clear()
        {
            Queries.Clear();
        }

        /// <summary>
        /// Получить список имён виртуальных таблиц.
        /// </summary>
        public static string[] GetNames()
        {
            var result = new string[Queries.Count];
            Queries.Keys.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Получить SQL запроса по имени.
        /// </summary>
        public static bool TryGetQuery(string name, out string sql)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                sql = null;
                return false;
            }
            return Queries.TryGetValue(name, out sql);
        }

        /// <summary>
        /// Выполнить SQL виртуальной таблицы и вернуть DataTable.
        /// </summary>
        public static DataTable GetData(string name)
        {
            if (!TryGetQuery(name, out var sql))
                throw new KeyNotFoundException($"Виртуальная таблица '{name}' не зарегистрирована.");

            return MSSQL.GetDataTableFromSQL(sql);
        }
    }
}
