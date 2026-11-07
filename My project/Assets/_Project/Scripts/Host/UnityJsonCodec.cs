using Game.Server.Persistence;
using UnityEngine;

namespace Game.Host
{
    /// <summary>Saves as readable JSON through JsonUtility (the same format as the content files).</summary>
    public sealed class UnityJsonCodec : IJsonCodec
    {
        public string ToJson(object value) => JsonUtility.ToJson(value, true);

        public T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    }
}
