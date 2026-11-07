using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Slot type → placeholder primitive on the preview model. Mirrored parts get a second copy at -X.</summary>
    public static class PreviewAttachments
    {
        public static readonly Dictionary<string, PreviewAttachment> BySlotType = new Dictionary<string, PreviewAttachment>
        {
            { "Head", new PreviewAttachment(PrimitiveType.Sphere, new Vector3(0f, 1.8f, 0f), new Vector3(0.6f, 0.45f, 0.6f), false) },
            { "Body", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f), new Vector3(0.95f, 0.7f, 0.6f), false) },
            { "Hands", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0.55f, 0.9f, 0f), new Vector3(0.25f, 0.25f, 0.25f), true) },
            { "Legs", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.5f, 0.5f), false) },
            { "Feet", new PreviewAttachment(PrimitiveType.Cube, new Vector3(0.2f, 0.08f, 0.08f), new Vector3(0.28f, 0.16f, 0.45f), true) },
            { "Necklace", new PreviewAttachment(PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0.3f), new Vector3(0.18f, 0.18f, 0.18f), false) },
            { "Ring", new PreviewAttachment(PrimitiveType.Sphere, new Vector3(0.58f, 0.78f, 0.08f), new Vector3(0.1f, 0.1f, 0.1f), false) },
            { "MainHand", new PreviewAttachment(PrimitiveType.Cylinder, new Vector3(0.7f, 1.05f, 0.2f), new Vector3(0.08f, 0.8f, 0.08f), false) },
            { "OffHand", new PreviewAttachment(PrimitiveType.Cube, new Vector3(-0.7f, 1.0f, 0.1f), new Vector3(0.08f, 0.65f, 0.5f), false) },
        };
    }
}
