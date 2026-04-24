using System;
using System.Data;
using System.Linq;

namespace Scraps.Database.LocalFiles.Sql
{
    /// <summary>Вычислитель предикатов WHERE для строки DataTable.</summary>
    public static class WhereEvaluator
    {
        /// <summary>Проверить, удовлетворяет ли строка заданному предикату.</summary>
        public static bool Evaluate(DataRow row, Predicate predicate)
        {
            if (predicate == null) return true;

            switch (predicate)
            {
                case ComparisonPredicate cmp:
                    return EvaluateComparison(row, cmp);
                case LikePredicate like:
                    return EvaluateLike(row, like);
                case IsNullPredicate isn:
                    return EvaluateIsNull(row, isn);
                case AndPredicate and:
                    return Evaluate(row, and.Left) && Evaluate(row, and.Right);
                case OrPredicate or:
                    return Evaluate(row, or.Left) || Evaluate(row, or.Right);
                case NotPredicate not:
                    return !Evaluate(row, not.Inner);
                default:
                    return true;
            }
        }

        private static bool EvaluateComparison(DataRow row, ComparisonPredicate cmp)
        {
            var colValue = row[cmp.Column];
            if (colValue == DBNull.Value) colValue = null;

            if (cmp.Value == null)
            {
                return cmp.Operator == "=" ? colValue == null : colValue != null;
            }

            var left = ConvertToComparable(colValue);
            var right = ConvertToComparable(cmp.Value);

            int comparison = Compare(left, right);

            switch (cmp.Operator)
            {
                case "=": case "==": return comparison == 0;
                case "!=": case "<>": return comparison != 0;
                case ">": return comparison > 0;
                case "<": return comparison < 0;
                case ">=": return comparison >= 0;
                case "<=": return comparison <= 0;
                default: throw new InvalidOperationException($"Unknown operator: {cmp.Operator}");
            }
        }

        private static bool EvaluateLike(DataRow row, LikePredicate like)
        {
            var value = row[like.Column]?.ToString() ?? "";
            var pattern = like.Pattern ?? "";
            // Convert SQL LIKE pattern to regex
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("%", ".*")
                .Replace("_", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(value, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static bool EvaluateIsNull(DataRow row, IsNullPredicate isn)
        {
            var value = row[isn.Column];
            bool isNull = value == DBNull.Value || value == null;
            return isn.IsNot ? !isNull : isNull;
        }

        private static IComparable ConvertToComparable(object value)
        {
            if (value == null) return null;
            if (value is IComparable cmp) return cmp;
            return value.ToString();
        }

        private static int Compare(IComparable a, IComparable b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.CompareTo(b);
        }
    }
}
