using System;
using System.Reflection;

namespace Game.Shared.Content.Packs
{
    /// <summary>How a content file is laid out and merged across packs (module 4, question 82).</summary>
    public enum ContentFileKind
    {
        /// <summary>One array of records with stable ids (items, mobs, …): packs only add new ids.</summary>
        Records,
        /// <summary>One object only the base pack has (config, balance limits).</summary>
        BaseOnly,
        /// <summary>One object from the base pack; other packs may add to its arrays (world: spawns, stations).</summary>
        Extendable,
    }

    /// <summary>One content file type: its name, file and record types, where it goes in <see cref="ContentFiles"/>.</summary>
    public sealed class ContentFileSpec
    {
        public ContentFileSpec(string name, Type fileType, ContentFileKind kind, string arrayField, Func<object, string> idOf)
        {
            Name = name;
            FileType = fileType;
            Kind = kind;
            if (arrayField != null)
            {
                ArrayField = fileType.GetField(arrayField) ?? throw new ArgumentException($"{fileType.Name} has no field {arrayField}");
                RecordType = ArrayField.FieldType.GetElementType();
            }
            IdOf = idOf;
            FilesField = typeof(ContentFiles).GetField(FieldName(fileType)) ?? throw new ArgumentException($"ContentFiles has no field for {fileType.Name}");
        }

        /// <summary>File name without extension inside a pack folder (also the key in content_ids.json).</summary>
        public string Name { get; }

        public Type FileType { get; }

        public ContentFileKind Kind { get; }

        /// <summary>The records array of a Records file; null otherwise.</summary>
        public FieldInfo ArrayField { get; }

        public Type RecordType { get; }

        /// <summary>Stable id of a record (Records files).</summary>
        public Func<object, string> IdOf { get; }

        /// <summary>The field of <see cref="ContentFiles"/> holding the merged file.</summary>
        public FieldInfo FilesField { get; }

        public object[] Records(object file)
        {
            if (file == null || ArrayField == null)
            {
                return new object[0];
            }
            var array = (Array)ArrayField.GetValue(file);
            var result = new object[array?.Length ?? 0];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = array.GetValue(i);
            }
            return result;
        }

        /// <summary>A new file object holding exactly these records.</summary>
        public object WithRecords(object[] records)
        {
            object file = Activator.CreateInstance(FileType);
            Array array = Array.CreateInstance(RecordType, records.Length);
            for (int i = 0; i < records.Length; i++)
            {
                array.SetValue(records[i], i);
            }
            ArrayField.SetValue(file, array);
            return file;
        }

        private static string FieldName(Type fileType)
        {
            foreach (FieldInfo field in typeof(ContentFiles).GetFields())
            {
                if (field.FieldType == fileType)
                {
                    return field.Name;
                }
            }
            return null;
        }
    }
}
