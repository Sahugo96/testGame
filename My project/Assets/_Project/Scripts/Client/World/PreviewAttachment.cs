using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Placeholder look of one equipment slot type on the preview model (until real models arrive).</summary>
    public struct PreviewAttachment
    {
        public PrimitiveType Shape;
        public Vector3 Position;
        public Vector3 Scale;
        public bool Mirrored;

        public PreviewAttachment(PrimitiveType shape, Vector3 position, Vector3 scale, bool mirrored)
        {
            Shape = shape;
            Position = position;
            Scale = scale;
            Mirrored = mirrored;
        }
    }
}
