using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>
    /// A graphics preset (02, 7.6: Low / Medium / High / Ultra): a project quality level by name plus URP values the client
    /// sets on a copy of that level's pipeline asset. Auto-detect on first start picks the highest preset whose
    /// MinVideoMemoryMb the video card has.
    /// </summary>
    [Serializable]
    public class GraphicsPresetDto
    {
        public string Id;
        public string NameKey;
        public string QualityLevel;
        public float RenderScale;
        public float ShadowDistance;
        public int Msaa;
        public float LodBias;
        public int MinVideoMemoryMb;
    }
}
