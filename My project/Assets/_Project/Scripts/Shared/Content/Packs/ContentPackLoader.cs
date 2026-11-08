using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Shared.Content.Dto;

namespace Game.Shared.Content.Packs
{
    /// <summary>
    /// Loads content packs (module 4, question 82): Content/packs.json lists packs in load order, "base" first; each pack is a
    /// folder with pack.json and any content files. Records from all packs are merged; a pack may only add new ids (redefining
    /// another pack's record is an error); config and balance limits come from base only; other packs may add spawns and
    /// stations to the world. Everything read goes into <see cref="ContentFiles.Hash"/>.
    /// </summary>
    public static class ContentPackLoader
    {
        /// <param name="parse">JSON text → object of the given type (JsonUtility in Unity).</param>
        public static ContentFiles Load(IContentSource source, Func<string, Type, object> parse, List<string> errors)
        {
            var files = new ContentFiles();
            var hash = new ContentHash();
            List<string> packs = ReadPackList(source, parse, hash, errors);
            files.Packs = packs.ToArray();
            foreach (ContentFileSpec spec in ContentFileTable.All)
            {
                LoadFile(spec, packs, source, parse, hash, files, errors);
            }
            string ledger = source.ReadText(ContentPacks.IdLedger);
            if (ledger != null)
            {
                files.Ids = (ContentIdLedgerDto)parse(ledger, typeof(ContentIdLedgerDto));
            }
            files.Hash = hash.Finish();
            return files;
        }

        /// <summary>Only the base config (GameBootstrap decides whether to start before anything else is loaded).</summary>
        public static GameConfig LoadConfig(IContentSource source, Func<string, Type, object> parse)
        {
            string text = source.ReadText(ContentPacks.PathOf(ContentPacks.Base, ContentFileTable.Config));
            return text != null ? (GameConfig)parse(text, typeof(GameConfig)) : null;
        }

        private static List<string> ReadPackList(IContentSource source, Func<string, Type, object> parse, ContentHash hash, List<string> errors)
        {
            var packs = new List<string>();
            string text = source.ReadText(ContentPacks.PackList);
            if (text == null)
            {
                errors.Add($"content: {ContentPacks.Folder}/{ContentPacks.PackList}.json is missing");
                return packs;
            }
            hash.Add(ContentPacks.PackList, text);
            var list = (PackListDto)parse(text, typeof(PackListDto));
            foreach (string pack in list?.Packs ?? new string[0])
            {
                if (string.IsNullOrEmpty(pack) || packs.Contains(pack))
                {
                    errors.Add($"content: packs.json lists '{pack}' twice or empty");
                    continue;
                }
                string manifestPath = ContentPacks.PathOf(pack, ContentPacks.Manifest);
                string manifestText = source.ReadText(manifestPath);
                if (manifestText == null)
                {
                    errors.Add($"content: pack '{pack}' has no {manifestPath}.json");
                    continue;
                }
                hash.Add(manifestPath, manifestText);
                var manifest = (PackManifestDto)parse(manifestText, typeof(PackManifestDto));
                if (manifest?.Id != pack)
                {
                    errors.Add($"content: {manifestPath}.json has Id '{manifest?.Id}', expected '{pack}'");
                }
                foreach (string dependency in manifest?.Dependencies ?? new string[0])
                {
                    if (!packs.Contains(dependency))
                    {
                        errors.Add($"content: pack '{pack}' needs '{dependency}' loaded before it (packs.json order)");
                    }
                }
                packs.Add(pack);
            }
            if (packs.Count == 0 || packs[0] != ContentPacks.Base)
            {
                errors.Add($"content: packs.json must start with '{ContentPacks.Base}'");
            }
            return packs;
        }

        private static void LoadFile(ContentFileSpec spec, List<string> packs, IContentSource source, Func<string, Type, object> parse, ContentHash hash, ContentFiles files, List<string> errors)
        {
            object merged = null;
            var records = new List<object>();
            var owners = new Dictionary<string, string>();
            foreach (string pack in packs)
            {
                string path = ContentPacks.PathOf(pack, spec.Name);
                string text = source.ReadText(path);
                if (text == null)
                {
                    continue;
                }
                hash.Add(path, text);
                object file = parse(text, spec.FileType);
                switch (spec.Kind)
                {
                    case ContentFileKind.BaseOnly:
                        if (pack != ContentPacks.Base)
                        {
                            errors.Add($"content: {path}.json — only the '{ContentPacks.Base}' pack may have '{spec.Name}'");
                            break;
                        }
                        merged = file;
                        break;
                    case ContentFileKind.Extendable:
                        merged = merged == null ? file : AppendArrays(spec.FileType, merged, file);
                        break;
                    default:
                        foreach (object record in spec.Records(file))
                        {
                            string id = spec.IdOf(record) ?? string.Empty;
                            if (owners.TryGetValue(id, out string owner) && owner != pack)
                            {
                                errors.Add($"content: pack '{pack}' redefines {spec.Name} '{id}' of pack '{owner}' — packs only add new ids");
                                continue;
                            }
                            owners[id] = pack;
                            records.Add(record);
                        }
                        merged = merged ?? file;
                        break;
                }
            }
            if (spec.Kind == ContentFileKind.Records && merged != null)
            {
                merged = spec.WithRecords(records.ToArray());
            }
            spec.FilesField.SetValue(files, merged);
        }

        /// <summary>Array fields of the extra file are appended to the main one; other fields of the extra file are ignored.</summary>
        private static object AppendArrays(Type type, object main, object extra)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!field.FieldType.IsArray)
                {
                    continue;
                }
                var a = (Array)field.GetValue(main) ?? Array.CreateInstance(field.FieldType.GetElementType(), 0);
                var b = (Array)field.GetValue(extra) ?? Array.CreateInstance(field.FieldType.GetElementType(), 0);
                Array joined = Array.CreateInstance(field.FieldType.GetElementType(), a.Length + b.Length);
                Array.Copy(a, joined, a.Length);
                Array.Copy(b, 0, joined, a.Length, b.Length);
                field.SetValue(main, joined);
            }
            return main;
        }
    }
}
