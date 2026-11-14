using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Slot type → a part put on the humanoid figure of the preview (WorldFigures.Humanoid). Mirrored parts get a second copy at -X.</summary>
    public static class PreviewAttachments
    {
        public static readonly Dictionary<string, PreviewAttachment> BySlotType = new Dictionary<string, PreviewAttachment>
        {
            { "Head", new PreviewAttachment(PrimitiveType.Sphere, new Vector3(0f, 1.85f, -0.01f), new Vector3(0.52f, 0.4f, 0.52f), false) },
            { "Body", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), new Vector3(0.7f, 0.7f, 0.42f), false) },
            { "Hands", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0.42f, 0.86f, 0f), new Vector3(0.26f, 0.26f, 0.28f), true) },
            { "Legs", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0.15f, 0.5f, 0f), new Vector3(0.28f, 0.6f, 0.3f), true) },
            { "Feet", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0.15f, 0.08f, 0.05f), new Vector3(0.3f, 0.17f, 0.42f), true) },
            { "Necklace", new PreviewAttachment(PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0.2f), new Vector3(0.12f, 0.12f, 0.12f), false) },
            { "Ring", new PreviewAttachment(PrimitiveType.Sphere, new Vector3(0.42f, 0.8f, 0.13f), new Vector3(0.08f, 0.08f, 0.08f), false) },
            { "MainHand", new PreviewAttachment(PrimitiveType.Cylinder, new Vector3(0.6f, 1.05f, 0.2f), new Vector3(0.07f, 0.8f, 0.07f), false) },
            { "OffHand", new PreviewAttachment(PrimitiveType.Cube, new Vector3(-0.6f, 1.0f, 0.1f), new Vector3(0.08f, 0.6f, 0.5f), false) },
        };
    }
}
