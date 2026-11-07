using Game.Server.Persistence;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#else
using System.Text.Json;
#endif

namespace Game.Tests
{
    /// <summary>The save JSON codec in tests: JsonUtility inside Unity (as the game uses), System.Text.Json in .NET.</summary>
    public sealed class TestJsonCodec : IJsonCodec
    {
#if UNITY_5_3_OR_NEWER
        public string ToJson(object value) => JsonUtility.ToJson(value, true);

        public T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
#else
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };

        public string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);

        public T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
#endif
    }
}
