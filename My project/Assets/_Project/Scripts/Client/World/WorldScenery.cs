using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// The scenery around the play field: textured grass, a sandy square at the spawn point, a fence along the border
    /// of the field and trees, rocks and flowers. Everything except the ground plane has no collider, so it never
    /// catches a click; trees and rocks stand only outside the field, because the server knows no obstacles.
    /// The layout is generated from a fixed seed: the same on every run and for every player.
    /// </summary>
    public static class WorldScenery
    {
        private const int Seed = 20240917;
        private const float PlaneSizeUnits = 10f;

        private const int GroundTextureSize = 256;
        /// <summary>The grass texture repeats every this many metres.</summary>
        private const float GroundTileMetres = 10f;
        private const int NoiseCoarseCells = 6;
        private const int NoiseMediumCells = 24;
        private const int NoiseFineCells = 96;
        private const float NoiseCoarseWeight = 0.5f;
        private const float NoiseMediumWeight = 0.3f;
        private const float NoiseFineWeight = 0.2f;
        private const int GroundAnisoLevel = 4;

        private const float OutskirtsScale = 25f;
        private const float OutskirtsDepth = 0.05f;
        private const float SquareRadius = 7f;
        private const float SquareHeight = 0.01f;

        private const float FencePostStep = 4f;
        private const float FencePostHeight = 1.1f;
        private const float FenceRailLow = 0.45f;
        private const float FenceRailHigh = 0.85f;

        private const int TreeCount = 110;
        private const int RockCount = 30;
        private const float GroveInner = 3f;
        private const float GroveOuter = 30f;
        private const int FlowerCount = 140;
        private const int PebbleCount = 40;
        /// <summary>Flowers do not grow closer than this to the middle of the spawn square.</summary>
        private const float ClearRadius = 8f;

        /// <summary>Builds the ground (with the collider for clicks) and the scenery for a field of ±halfSize metres.</summary>
        public static void Build(float halfSize)
        {
            var root = new GameObject("Scenery").transform;
            var random = new System.Random(Seed);
            Texture2D grass = GrassTexture();
            float size = halfSize * 2f;

            GameObject ground = WorldPrimitives.Create(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(size / PlaneSizeUnits, 1f, size / PlaneSizeUnits);
            Material groundMaterial = WorldPrimitives.Textured(grass, size / GroundTileMetres, Color.white);
            if (groundMaterial != null)
            {
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            }

            // The land beyond the field, down to the horizon: one even colour (a repeating texture shows as a grid far away).
            float far = size * OutskirtsScale;
            Flat(WorldPrimitives.Part("Outskirts", root, PrimitiveType.Plane, new Vector3(0f, -OutskirtsDepth, 0f),
                new Vector3(far / PlaneSizeUnits, 1f, far / PlaneSizeUnits), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Outskirts)));

            Flat(WorldPrimitives.Part("Square", root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f),
                new Vector3(SquareRadius * 2f, SquareHeight, SquareRadius * 2f), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Sand)));

            BuildFence(root, halfSize);
            BuildGrove(root, random, halfSize);
            BuildMeadow(root, random, halfSize);
        }

        private static void BuildFence(Transform root, float halfSize)
        {
            Material wood = WorldPrimitives.Colored(WorldPalette.Wood);
            Material dark = WorldPrimitives.Colored(WorldPalette.WoodDark);
            float length = halfSize * 2f;
            for (int side = 0; side < 4; side++)
            {
                // Sides in turn: +Z, +X, -Z, -X; `along` runs along the side.
                bool alongX = side % 2 == 0;
                float sign = side < 2 ? 1f : -1f;
                for (float along = -halfSize; along < halfSize; along += FencePostStep)
                {
                    Vector3 position = alongX ? new Vector3(along, FencePostHeight * 0.5f, halfSize * sign) : new Vector3(halfSize * sign, FencePostHeight * 0.5f, -along);
                    WorldPrimitives.Part("Post", root, PrimitiveType.Cube, position, new Vector3(0.18f, FencePostHeight, 0.18f), Vector3.zero, dark);
                }
                Vector3 railSize = alongX ? new Vector3(length, 0.1f, 0.07f) : new Vector3(0.07f, 0.1f, length);
                Vector3 railAt = alongX ? new Vector3(0f, 0f, halfSize * sign) : new Vector3(halfSize * sign, 0f, 0f);
                WorldPrimitives.Part("Rail", root, PrimitiveType.Cube, railAt + Vector3.up * FenceRailLow, railSize, Vector3.zero, wood);
                WorldPrimitives.Part("Rail", root, PrimitiveType.Cube, railAt + Vector3.up * FenceRailHigh, railSize, Vector3.zero, wood);
            }
        }

        /// <summary>Trees and rocks in a belt around the field.</summary>
        private static void BuildGrove(Transform root, System.Random random, float halfSize)
        {
            for (int i = 0; i < TreeCount; i++)
            {
                Vector3 position = BeltPoint(random, halfSize);
                float scale = Range(random, 0.8f, 1.5f);
                if (random.Next(3) == 0)
                {
                    Pine(root, position, scale);
                }
                else
                {
                    LeafTree(root, random, position, scale);
                }
            }
            Material rock = WorldPrimitives.Colored(WorldPalette.Rock);
            Material rockDark = WorldPrimitives.Colored(WorldPalette.Tone(WorldPalette.Rock, 0.8f));
            for (int i = 0; i < RockCount; i++)
            {
                Vector3 position = BeltPoint(random, halfSize);
                float width = Range(random, 0.8f, 2.2f);
                float height = width * Range(random, 0.45f, 0.8f);
                WorldPrimitives.Part("Rock", root, PrimitiveType.Sphere, position + Vector3.up * height * 0.3f,
                    new Vector3(width, height, width * Range(random, 0.7f, 1.1f)), new Vector3(0f, Range(random, 0f, 180f), 0f), i % 2 == 0 ? rock : rockDark);
            }
        }

        private static void LeafTree(Transform root, System.Random random, Vector3 at, float scale)
        {
            float trunk = 2.2f * scale;
            WorldPrimitives.Part("Trunk", root, PrimitiveType.Cylinder, at + Vector3.up * trunk * 0.5f,
                new Vector3(0.35f * scale, trunk * 0.5f, 0.35f * scale), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Trunk));
            Material dark = WorldPrimitives.Colored(WorldPalette.LeafDark);
            Material light = WorldPrimitives.Colored(WorldPalette.LeafLight);
            WorldPrimitives.Part("Crown", root, PrimitiveType.Sphere, at + Vector3.up * (trunk + 0.7f * scale), Vector3.one * 2.6f * scale, Vector3.zero, dark);
            WorldPrimitives.Part("Crown", root, PrimitiveType.Sphere,
                at + new Vector3(Range(random, -0.8f, 0.8f) * scale, trunk + 1.5f * scale, Range(random, -0.8f, 0.8f) * scale), Vector3.one * 1.9f * scale, Vector3.zero, light);
            WorldPrimitives.Part("Crown", root, PrimitiveType.Sphere,
                at + new Vector3(Range(random, -1f, 1f) * scale, trunk + 0.5f * scale, Range(random, -1f, 1f) * scale), Vector3.one * 1.7f * scale, Vector3.zero, light);
        }

        private static void Pine(Transform root, Vector3 at, float scale)
        {
            const int tiers = 4;
            float trunk = 1.2f * scale;
            WorldPrimitives.Part("Trunk", root, PrimitiveType.Cylinder, at + Vector3.up * trunk * 0.5f,
                new Vector3(0.3f * scale, trunk * 0.5f, 0.3f * scale), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Trunk));
            Material needles = WorldPrimitives.Colored(WorldPalette.Pine);
            for (int tier = 0; tier < tiers; tier++)
            {
                // Tiers get narrower towards the top.
                float width = (2.4f - tier * 0.5f) * scale;
                WorldPrimitives.Part("Tier", root, PrimitiveType.Sphere, at + Vector3.up * (trunk + (0.3f + tier * 0.75f) * scale),
                    new Vector3(width, 1.1f * scale, width), Vector3.zero, needles);
            }
        }

        /// <summary>Flowers and pebbles on the field itself: too small to be an obstacle.</summary>
        private static void BuildMeadow(Transform root, System.Random random, float halfSize)
        {
            Color[] petals = { WorldPalette.FlowerYellow, WorldPalette.FlowerWhite, WorldPalette.FlowerPink };
            Material stem = WorldPrimitives.Colored(WorldPalette.LeafLight);
            for (int i = 0; i < FlowerCount; i++)
            {
                Vector3 at = FieldPoint(random, halfSize);
                if (at.sqrMagnitude < ClearRadius * ClearRadius)
                {
                    continue;
                }
                Flat(WorldPrimitives.Part("Stem", root, PrimitiveType.Cube, at + Vector3.up * 0.11f, new Vector3(0.03f, 0.22f, 0.03f), Vector3.zero, stem));
                Flat(WorldPrimitives.Part("Flower", root, PrimitiveType.Sphere, at + Vector3.up * 0.24f, new Vector3(0.13f, 0.08f, 0.13f), Vector3.zero,
                    WorldPrimitives.Colored(petals[random.Next(petals.Length)])));
            }
            Material pebble = WorldPrimitives.Colored(WorldPalette.Rock);
            for (int i = 0; i < PebbleCount; i++)
            {
                Vector3 at = FieldPoint(random, halfSize);
                float width = Range(random, 0.15f, 0.4f);
                Flat(WorldPrimitives.Part("Pebble", root, PrimitiveType.Sphere, at + Vector3.up * 0.02f, new Vector3(width, width * 0.5f, width * 0.8f),
                    new Vector3(0f, Range(random, 0f, 180f), 0f), pebble));
            }
        }

        /// <summary>Small and flat things cast no shadow: it would cost more than it shows.</summary>
        private static void Flat(Transform part) => part.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        private static Vector3 FieldPoint(System.Random random, float halfSize) =>
            new Vector3(Range(random, -halfSize, halfSize), 0f, Range(random, -halfSize, halfSize));

        /// <summary>A point in the belt between the fence and the edge of the grove.</summary>
        private static Vector3 BeltPoint(System.Random random, float halfSize)
        {
            float outer = halfSize + GroveOuter;
            float inner = halfSize + GroveInner;
            while (true)
            {
                var point = new Vector3(Range(random, -outer, outer), 0f, Range(random, -outer, outer));
                if (Mathf.Abs(point.x) > inner || Mathf.Abs(point.z) > inner)
                {
                    return point;
                }
            }
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // ---------- grass texture ----------

        /// <summary>A grass texture made of repeating noise (no seams when tiled): large spots, medium tufts, fine grain.</summary>
        private static Texture2D GrassTexture()
        {
            var pixels = new Color32[GroundTextureSize * GroundTextureSize];
            for (int y = 0; y < GroundTextureSize; y++)
            {
                for (int x = 0; x < GroundTextureSize; x++)
                {
                    float u = (float)x / GroundTextureSize;
                    float v = (float)y / GroundTextureSize;
                    float noise = TileNoise(u, v, NoiseCoarseCells, 1) * NoiseCoarseWeight
                        + TileNoise(u, v, NoiseMediumCells, 2) * NoiseMediumWeight
                        + TileNoise(u, v, NoiseFineCells, 3) * NoiseFineWeight;
                    pixels[y * GroundTextureSize + x] = Color.Lerp(WorldPalette.GrassDark, WorldPalette.GrassLight, noise);
                }
            }
            var texture = new Texture2D(GroundTextureSize, GroundTextureSize, TextureFormat.RGB24, true)
            {
                name = "Grass",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = GroundAnisoLevel,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        /// <summary>Smooth noise 0..1 with `cells` random values per side that wraps around at the edges of the texture.</summary>
        private static float TileNoise(float u, float v, int cells, int layer)
        {
            float x = u * cells;
            float y = v * cells;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = Mathf.SmoothStep(0f, 1f, x - x0);
            float fy = Mathf.SmoothStep(0f, 1f, y - y0);
            float bottom = Mathf.Lerp(Lattice(x0, y0, cells, layer), Lattice(x0 + 1, y0, cells, layer), fx);
            float top = Mathf.Lerp(Lattice(x0, y0 + 1, cells, layer), Lattice(x0 + 1, y0 + 1, cells, layer), fx);
            return Mathf.Lerp(bottom, top, fy);
        }

        /// <summary>A fixed pseudo-random value 0..1 of a lattice point; the lattice repeats every `cells` points.</summary>
        private static float Lattice(int x, int y, int cells, int layer)
        {
            unchecked
            {
                uint hash = (uint)(x % cells) * 374761393u + (uint)(y % cells) * 668265263u + (uint)layer * 2246822519u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0xFFFFu) / 65535f;
            }
        }
    }
}
