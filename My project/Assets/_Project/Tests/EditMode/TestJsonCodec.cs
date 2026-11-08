using Game.Server.Persistence;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#else
using System.Text.Json;
#endif

namespace Game.Tests
{
    /// <summary>
    /// The save JSON codec in tests: JsonUtility inside Unity (as the game uses), System.Text.Json in .NET. In .NET the
    /// result is made to look like JsonUtility's: it has no null for strings, arrays and [Serializable] classes (they come
    /// back as "", [] and an empty object), so tests outside Unity catch code that relies on null surviving a save.
    /// </summary>
    public sealed class TestJsonCodec : IJsonCodec
    {
#if UNITY_5_3_OR_NEWER
        public string ToJson(object value) => JsonUtility.ToJson(value, true);

        public T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
#else
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };

        public string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);

        public T FromJson<T>(string json) => JsonUtilityLike.Normalize(JsonSerializer.Deserialize<T>(json, Options));
#endif
    }
}
