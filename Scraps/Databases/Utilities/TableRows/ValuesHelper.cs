using System.Collections.Generic;

namespace Scraps.Databases.Utilities.TableRows
{
    /// <summary>
    /// Упрощённое создание словаря значений для операций Add/Edit.
    /// </summary>
    public static class Values
    {
        /// <summary>
        /// Создать Dictionary&lt;string, object&gt; из пар ключ/значение.
        /// </summary>
        /// <example>Values.Create("Name", "Иван", "Age", 25)</example>
        public static Dictionary<string, object> Create(params object[] pairs)
        {
            var dict = new Dictionary<string, object>();
            if (pairs == null || pairs.Length == 0)
                return dict;

            for (int i = 0; i < pairs.Length; i += 2)
            {
                var key = pairs[i]?.ToString();
                var value = i + 1 < pairs.Length ? pairs[i + 1] : null;
                if (!string.IsNullOrWhiteSpace(key))
                    dict[key] = value;
            }
            return dict;
        }
    }
}
