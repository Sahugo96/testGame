using Game.Shared.Core;
using UnityEngine;

namespace Game.Host
{
    /// <summary>Routes <see cref="Log"/> to the Unity console.</summary>
    public sealed class UnityLogSink : ILogSink
    {
        public void Info(string tag, string message) => Debug.Log($"[{tag}] {message}");

        public void Warning(string tag, string message) => Debug.LogWarning($"[{tag}] {message}");

        public void Error(string tag, string message) => Debug.LogError($"[{tag}] {message}");
    }
}
