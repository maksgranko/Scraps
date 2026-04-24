using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>Операторы фильтрации для SQL-запросов.</summary>
    public enum SqlFilterOperator
    {
        /// <summary>Равно (=).</summary>
        Eq,
        /// <summary>Не равно (&lt;&gt;).</summary>
        Ne,
        /// <summary>Больше (&gt;).</summary>
        Gt,
        /// <summary>Больше или равно (&gt;=).</summary>
        Ge,
        /// <summary>Меньше (&lt;).</summary>
        Lt,
        /// <summary>Меньше или равно (&lt;=).</summary>
        Le,
        /// <summary>LIKE.</summary>
        Like,
        /// <summary>IS NULL.</summary>
        IsNull,
        /// <summary>IS NOT NULL.</summary>
        IsNotNull,
        /// <summary>IN (список значений).</summary>
        In
    }

    /// <summary>Условие фильтрации SQL-запроса.</summary>
    public sealed class SqlFilterCondition
    {
        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>Оператор сравнения.</summary>
        public SqlFilterOperator Operator { get; set; } = SqlFilterOperator.Eq;
        /// <summary>Значение для сравнения.</summary>
        public object Value { get; set; }
        /// <summary>Список значений для оператора IN.</summary>
        public IEnumerable<object> Values { get; set; }
    }

    /// <summary>Условие сортировки SQL-запроса.</summary>
    public sealed class SqlSortCondition
    {
        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>Сортировка по убыванию.</summary>
        public bool Descending { get; set; }
    }
}
