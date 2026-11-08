using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Packs;

namespace Game.Shared.Content.Ids
{
    /// <summary>
    /// Stable ids (module 4, question 83): content_ids.json remembers every id ever released. An id that disappears from the
    /// data is an error — saves, mail, shops and key bindings may still name it; retire records with Deprecated instead.
    /// The editor adds new ids on export (Game → Content); tests check the file lists every current id.
    /// </summary>
    public static class ContentIdLedger
    {
        /// <summary>Ledger key for world stations (shops keep their spot id).</summary>
        public const string Stations = "world.stations";

        /// <summary>Content files whose ids are not stable (level numbers are a table, not things).</summary>
        private static readonly HashSet<string> Unstable = new HashSet<string> { "levels" };

        /// <summary>Current ids per ledger key, sorted.</summary>
        public static SortedDictionary<string, SortedSet<string>> Collect(ContentFiles files)
        {
            var result = new SortedDictionary<string, SortedSet<string>>(System.StringComparer.Ordinal);
            foreach (ContentFileSpec spec in ContentFileTable.All)
            {
                if (spec.Kind != ContentFileKind.Records || Unstable.Contains(spec.Name))
                {
                    continue;
                }
                var ids = new SortedSet<string>(System.StringComparer.Ordinal);
                foreach (object record in spec.Records(spec.FilesField.GetValue(files)))
                {
                    string id = spec.IdOf(record);
                    if (!string.IsNullOrEmpty(id))
                    {
                        ids.Add(id);
                    }
                }
                result[spec.Name] = ids;
            }
            var stations = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (StationDto station in files.World?.Stations ?? new StationDto[0])
            {
                if (!string.IsNullOrEmpty(station.Id))
                {
                    stations.Add(station.Id);
                }
            }
            result[Stations] = stations;
            return result;
        }

        /// <summary>Every id of the ledger must still exist.</summary>
        public static void Check(ContentFiles files, List<string> errors)
        {
            if (files.Ids == null)
            {
                errors.Add($"content: {ContentPacks.IdLedger}.json is missing (Game → Content → Fix ids creates it)");
                return;
            }
            SortedDictionary<string, SortedSet<string>> current = Collect(files);
            foreach (ContentIdListDto list in files.Ids.Files ?? new ContentIdListDto[0])
            {
                current.TryGetValue(list.File ?? string.Empty, out SortedSet<string> ids);
                foreach (string id in list.Ids ?? new string[0])
                {
                    if (ids == null || !ids.Contains(id))
                    {
                        errors.Add($"content: {list.File} '{id}' was released ({ContentPacks.IdLedger}.json) and is gone — saves may hold it; "
                            + "restore it and mark it Deprecated (items, mobs, recipes) instead of deleting");
                    }
                }
            }
        }

        /// <summary>Ids in the data but not yet in the ledger (shown before fixing them).</summary>
        public static List<string> Unlisted(ContentFiles files)
        {
            var known = new HashSet<string>();
            foreach (ContentIdListDto list in files.Ids?.Files ?? new ContentIdListDto[0])
            {
                foreach (string id in list.Ids ?? new string[0])
                {
                    known.Add(list.File + "/" + id);
                }
            }
            var result = new List<string>();
            foreach (KeyValuePair<string, SortedSet<string>> pair in Collect(files))
            {
                foreach (string id in pair.Value)
                {
                    if (!known.Contains(pair.Key + "/" + id))
                    {
                        result.Add(pair.Key + "/" + id);
                    }
                }
            }
            return result;
        }

        /// <summary>The ledger with every current id added (nothing is ever removed here; reset by hand before release only).</summary>
        public static ContentIdLedgerDto Updated(ContentFiles files)
        {
            var merged = new SortedDictionary<string, SortedSet<string>>(System.StringComparer.Ordinal);
            foreach (ContentIdListDto list in files.Ids?.Files ?? new ContentIdListDto[0])
            {
                if (!merged.TryGetValue(list.File ?? string.Empty, out SortedSet<string> ids))
                {
                    ids = new SortedSet<string>(System.StringComparer.Ordinal);
                    merged[list.File ?? string.Empty] = ids;
                }
                ids.UnionWith(list.Ids ?? new string[0]);
            }
            foreach (KeyValuePair<string, SortedSet<string>> pair in Collect(files))
            {
                if (!merged.TryGetValue(pair.Key, out SortedSet<string> ids))
                {
                    ids = new SortedSet<string>(System.StringComparer.Ordinal);
                    merged[pair.Key] = ids;
                }
                ids.UnionWith(pair.Value);
            }
            var result = new List<ContentIdListDto>();
            foreach (KeyValuePair<string, SortedSet<string>> pair in merged)
            {
                result.Add(new ContentIdListDto { File = pair.Key, Ids = new List<string>(pair.Value).ToArray() });
            }
            return new ContentIdLedgerDto { Files = result.ToArray() };
        }
    }
}
