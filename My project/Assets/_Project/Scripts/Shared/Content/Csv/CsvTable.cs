using System.Collections.Generic;
using System.Text;

namespace Game.Shared.Content.Csv
{
    /// <summary>
    /// CSV rows ↔ text (RFC 4180 quoting). The separator is ';' by default (question 84, temporarily DEFAULT): Excel with
    /// Russian regional settings opens such files in columns; numbers use a decimal comma there (see CsvBalanceMapper).
    /// </summary>
    public static class CsvTable
    {
        public const char DefaultSeparator = ';';
        private const char Quote = '"';

        public static string Write(IEnumerable<string[]> rows, char separator = DefaultSeparator)
        {
            var sb = new StringBuilder();
            foreach (string[] row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(separator);
                    }
                    string cell = row[i] ?? string.Empty;
                    bool quote = cell.IndexOf(separator) >= 0 || cell.IndexOf(Quote) >= 0 || cell.IndexOf('\n') >= 0 || cell.IndexOf('\r') >= 0;
                    sb.Append(quote ? Quote + cell.Replace("\"", "\"\"") + Quote : cell);
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        public static List<string[]> Parse(string text, char separator = DefaultSeparator)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            text = (text ?? string.Empty).TrimStart('﻿');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == Quote && i + 1 < text.Length && text[i + 1] == Quote)
                    {
                        cell.Append(Quote);
                        i++;
                    }
                    else if (c == Quote)
                    {
                        quoted = false;
                    }
                    else
                    {
                        cell.Append(c);
                    }
                    continue;
                }
                if (c == Quote)
                {
                    quoted = true;
                }
                else if (c == separator)
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    row.Add(cell.ToString());
                    cell.Clear();
                    AddRow(rows, row);
                    row = new List<string>();
                }
                else
                {
                    cell.Append(c);
                }
            }
            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                AddRow(rows, row);
            }
            return rows;
        }

        private static void AddRow(List<string[]> rows, List<string> row)
        {
            if (row.Count == 1 && row[0].Length == 0)
            {
                return;
            }
            rows.Add(row.ToArray());
        }
    }
}
