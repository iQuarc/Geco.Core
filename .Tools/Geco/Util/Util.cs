using System;
using System.Text;
using System.Text.RegularExpressions;
using Geco.Common.SimpleMetadata;

namespace Geco
{
    public static class Util
    {
        public static bool TableNameMatchesRegex(Table table, string tablesRegex, bool onNull)
        {
            if (string.IsNullOrWhiteSpace(tablesRegex))
                return onNull;
            return
                Regex.IsMatch(table.Name, tablesRegex) ||
                Regex.IsMatch($"[{table.Name}]", tablesRegex) ||
                Regex.IsMatch($"{table.Schema.Name}.{table.Name}", tablesRegex) ||
                Regex.IsMatch($"[{table.Schema.Name}].[{table.Name}]", tablesRegex);
        }

        public static bool TableNameMatches(Table table, string name)
        {
            return string.Equals(name, table.Name, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"[{table.Name}]", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"{table.Schema.Name}.{table.Name}", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"[{table.Schema.Name}].[{table.Name}]", StringComparison.OrdinalIgnoreCase);
        }

        public static bool ColumnNameMatches(Column column, string name)
        {
            return string.Equals(name, column.Name, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"[{column.Name}]", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"{column.Table.Name}.{column.Name}", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"[{column.Table.Name}].[{column.Name}]", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"{column.Table.Schema.Name}.{column.Table.Name}.{column.Name}", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, $"[{column.Table.Schema.Name}].[{column.Table.Name}].[{column.Name}]", StringComparison.OrdinalIgnoreCase);
        }

        public static StringBuilder Append(this StringBuilder builder, string? value, bool? append)
        {
            if (append == true)
                builder.Append(value);
            return builder;
        }
    }
}