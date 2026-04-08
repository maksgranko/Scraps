using System.Data;

namespace Scraps.Data.Parsers.Internal
{
    internal static class DataTableParserHelpers
    {
        internal static void EnsureColumns(DataTable dt, int count)
        {
            while (dt.Columns.Count < count)
            {
                dt.Columns.Add($"Column{dt.Columns.Count + 1}");
            }
        }

        internal static string MakeUniqueColumnName(DataTable dt, string baseName)
        {
            if (!dt.Columns.Contains(baseName))
                return baseName;

            int i = 2;
            string candidate;
            do
            {
                candidate = baseName + i;
                i++;
            }
            while (dt.Columns.Contains(candidate));

            return candidate;
        }
    }
}
