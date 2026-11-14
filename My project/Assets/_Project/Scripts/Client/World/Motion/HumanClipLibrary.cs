using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World.Motion
{
    /// <summary>
    /// Motion clips of the humanoid figure by name: `Resources/Animations/&lt;name&gt;.txt` holds the BVH text (the file as
    /// exported, with the extension changed so that Unity ships it as a text asset). Read and converted on first use,
    /// then cached; a missing or unusable file gives null (the emote then plays without motion) and one warning.
    /// </summary>
    public sealed class HumanClipLibrary
    {
        private const string Folder = "Animations/";

        private readonly Dictionary<string, HumanClip> _clips = new Dictionary<string, HumanClip>();

        public HumanClip Get(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            if (!_clips.TryGetValue(name, out HumanClip clip))
            {
                clip = Load(name);
                _clips[name] = clip;
            }
            return clip;
        }

        private static HumanClip Load(string name)
        {
            TextAsset asset = Resources.Load<TextAsset>(Folder + name);
            if (asset == null)
            {
                Debug.LogWarning($"[Motion] Resources/{Folder}{name}.txt is missing.");
                return null;
            }
            BvhMotion motion = BvhMotion.Parse(asset.text, out string error);
            HumanClip clip = motion == null ? null : BvhRetarget.ToHumanClip(motion, out error);
            if (clip == null)
            {
                Debug.LogWarning($"[Motion] Resources/{Folder}{name}.txt cannot be played: {error}.");
            }
            return clip;
        }
    }
}
