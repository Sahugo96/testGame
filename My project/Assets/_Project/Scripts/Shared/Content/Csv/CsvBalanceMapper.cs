using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Game.Shared.Content.Csv
{
    /// <summary>
    /// Balance tables (module 4, question 84): records of one content type ↔ CSV. Columns are the record's simple fields
    /// (text, numbers, true/false) and one level of nested objects ("Field.Sub"); lists (components, drops, variants) are
    /// not in the table — they are edited in the card or the wizard. The first column is the record's id; import only changes
    /// existing records and applies nothing if any cell is wrong. Numbers are written with a decimal comma; both comma and
    /// point are read.
    /// </summary>
    public static class CsvBalanceMapper
    {
        private sealed class Column
        {
            public string Name;
            public FieldInfo Outer;
            public FieldInfo Field;
        }

        public static string Export(Type recordType, IEnumerable<object> records)
        {
            List<Column> columns = Columns(recordType);
            var rows = new List<string[]> { columns.ConvertAll(c => c.Name).ToArray() };
            foreach (object record in records)
            {
                var row = new string[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    object owner = columns[i].Outer != null ? columns[i].Outer.GetValue(record) : record;
                    row[i] = owner == null ? string.Empty : Format(columns[i].Field.GetValue(owner));
                }
                rows.Add(row);
            }
            return CsvTable.Write(rows);
        }

        /// <summary>Applies the table to the records (same objects); returns the changed ones, or none when errors were found.</summary>
        public static List<object> Import(Type recordType, string csv, IList<object> records, List<string> errors)
        {
            var changed = new List<object>();
            List<string[]> rows = CsvTable.Parse(csv);
            if (rows.Count == 0)
            {
                errors.Add("csv: the table is empty");
                return changed;
            }
            List<Column> all = Columns(recordType);
            var header = new Column[rows[0].Length];
            for (int i = 0; i < header.Length; i++)
            {
                header[i] = all.Find(c => c.Name == rows[0][i].Trim());
                if (header[i] == null)
                {
                    errors.Add($"csv: unknown column '{rows[0][i]}'");
                }
            }
            if (header.Length == 0 || header[0] != all[0])
            {
                errors.Add($"csv: the first column must be '{all[0].Name}'");
            }
            if (errors.Count > 0)
            {
                return changed;
            }
            var byId = new Dictionary<string, object>();
            foreach (object record in records)
            {
                byId[Format(all[0].Field.GetValue(record))] = record;
            }
            var pending = new List<(object owner, FieldInfo field, object value, object record)>();
            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                string id = row.Length > 0 ? row[0].Trim() : string.Empty;
                if (!byId.TryGetValue(id, out object record))
                {
                    errors.Add($"csv: row {r + 1}: no record '{id}' (the table only changes existing records)");
                    continue;
                }
                for (int c = 1; c < header.Length && c < row.Length; c++)
                {
                    Column column = header[c];
                    object owner = column.Outer != null ? column.Outer.GetValue(record) : record;
                    if (owner == null)
                    {
                        if (row[c].Trim().Length > 0)
                        {
                            owner = Activator.CreateInstance(column.Outer.FieldType);
                            pending.Add((record, column.Outer, owner, record));
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (!TryParse(column.Field.FieldType, row[c], out object value))
                    {
                        errors.Add($"csv: row {r + 1} ('{id}'), column {column.Name}: cannot read '{row[c]}'");
                        continue;
                    }
                    if (!Equals(column.Field.GetValue(owner), value))
                    {
                        pending.Add((owner, column.Field, value, record));
                    }
                }
            }
            if (errors.Count > 0)
            {
                return changed;
            }
            foreach (var change in pending)
            {
                change.field.SetValue(change.owner, change.value);
                if (!changed.Contains(change.record))
                {
                    changed.Add(change.record);
                }
            }
            return changed;
        }

        private static List<Column> Columns(Type type)
        {
            var columns = new List<Column>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSimple(field.FieldType))
                {
                    columns.Add(new Column { Name = field.Name, Field = field });
                }
                else if (field.FieldType.IsClass && !field.FieldType.IsArray && field.FieldType.IsDefined(typeof(SerializableAttribute), false))
                {
                    foreach (FieldInfo inner in field.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (IsSimple(inner.FieldType))
                        {
                            columns.Add(new Column { Name = field.Name + "." + inner.Name, Outer = field, Field = inner });
                        }
                    }
                }
            }
            return columns;
        }

        private static bool IsSimple(Type type) =>
            type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(bool);

        private static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case float f:
                    return f.ToString("R", CultureInfo.InvariantCulture).Replace('.', ',');
                case double d:
                    return d.ToString("R", CultureInfo.InvariantCulture).Replace('.', ',');
                case bool b:
                    return b ? "true" : "false";
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private static bool TryParse(Type type, string text, out object value)
        {
            text = (text ?? string.Empty).Trim();
            string number = text.Replace(',', '.').Replace(" ", string.Empty).Replace(" ", string.Empty);
            value = null;
            if (type == typeof(string))
            {
                value = text;
                return true;
            }
            if (type == typeof(bool))
            {
                string lower = text.ToLowerInvariant();
                if (lower == "true" || lower == "1" || lower == "да" || lower == "истина")
                {
                    value = true;
                    return true;
                }
                if (lower == "false" || lower == "0" || lower == "нет" || lower == "ложь" || lower.Length == 0)
                {
                    value = false;
                    return true;
                }
                return false;
            }
            NumberStyles style = NumberStyles.Float;
            if (type == typeof(int) && int.TryParse(number.Length == 0 ? "0" : number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            {
                value = i;
                return true;
            }
            if (type == typeof(long) && long.TryParse(number.Length == 0 ? "0" : number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
            {
                value = l;
                return true;
            }
            if (type == typeof(float) && float.TryParse(number.Length == 0 ? "0" : number, style, CultureInfo.InvariantCulture, out float f))
            {
                value = f;
                return true;
            }
            if (type == typeof(double) && double.TryParse(number.Length == 0 ? "0" : number, style, CultureInfo.InvariantCulture, out double d))
            {
                value = d;
                return true;
            }
            return false;
        }
    }
}
