using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// The look of everything that stands in the world, chosen by a key from the content (`VisualShape` of a mob or a
    /// station) — the same "handler by key" approach as for effects. A figure is assembled from primitive shapes and is
    /// tinted by the content's colour; forward is +Z, the origin is the point on the ground. The numbers below are the
    /// figures themselves (sizes in metres), kept as data tables. Unknown keys fall back to a plain capsule, so new
    /// content is visible before its figure is drawn. Temporary (DEFAULT): until real models arrive.
    /// </summary>
    public static partial class WorldFigures
    {
        /// <summary>Keys that are not written in the content: players by class, drops.</summary>
        public const string Humanoid = "Humanoid";
        public const string Caster = "Caster";
        public const string Fighter = "Fighter";
        public const string Loot = "Loot";
        private const string Fallback = "Capsule";

        private delegate void Recipe(FigureBuilder b, Color tint);

        private static readonly Dictionary<string, Recipe> Recipes = new Dictionary<string, Recipe>(StringComparer.Ordinal)
        {
            { Humanoid, PlainHumanoid },
            { Caster, CasterHumanoid },
            { Fighter, FighterHumanoid },
            { "Merchant", Merchant },
            { "Dummy", Dummy },
            { "Wolf", Wolf },
            { "Golem", Golem },
            { "Cube", CubeCreature },
            { Loot, LootSack },
            { "Whetstone", Whetstone },
            { "Anvil", Anvil },
            { "Market", MarketStall },
            { "Mailbox", Mailbox },
            { "ShopSpot", ShopSpot },
            { Fallback, PlainCapsule },
            { "Cylinder", PlainDrum },
        };

        public static bool Knows(string shape) => !string.IsNullOrEmpty(shape) && Recipes.ContainsKey(shape);

        /// <summary>Assembles the figure of a key (the fallback for an unknown one); the model is not parented yet.</summary>
        public static Figure Build(string shape, Color tint)
        {
            if (string.IsNullOrEmpty(shape) || !Recipes.TryGetValue(shape, out Recipe recipe))
            {
                shape = Fallback;
                recipe = PlainCapsule;
            }
            var builder = new FigureBuilder(shape);
            recipe(builder, tint);
            return builder.Figure;
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static Vector3 Cube(float side) => new Vector3(side, side, side);

        // ---------- fallbacks ----------

        private static void PlainCapsule(FigureBuilder b, Color tint)
        {
            b.Pill(V(0f, 1f, 0f), V(1f, 2f, 1f), Vector3.zero, tint);
            b.Figure.Size = V(1f, 2f, 1f);
            b.Figure.LieHeight = 0.5f;
        }

        private static void PlainDrum(FigureBuilder b, Color tint)
        {
            b.Tube(V(0f, 0.225f, 0f), V(1.2f, 0.45f, 1.2f), tint);
            b.Figure.Size = V(1.2f, 0.45f, 1.2f);
        }

        // ---------- drops ----------

        private static void LootSack(FigureBuilder b, Color tint)
        {
            b.GlowDisc(V(0f, 0.012f, 0f), 0.8f, 0.012f, Vector3.zero, WorldPalette.Tone(tint, 0.85f));
            b.Ball(V(0f, 0.2f, 0f), V(0.46f, 0.4f, 0.46f), WorldPalette.Leather);
            b.Tube(V(0f, 0.42f, 0f), V(0.16f, 0.12f, 0.16f), WorldPalette.Boots);
            b.Tube(V(0f, 0.39f, 0f), V(0.21f, 0.03f, 0.21f), WorldPalette.Gold);
            Transform gem = b.GlowBox(V(0f, 0.85f, 0f), Cube(0.17f), V(45f, 0f, 45f), tint);
            b.Spin(gem, V(0f, 140f, 0f));
            b.Bob(gem, 0.07f, 0.7f);
            b.Figure.Size = V(0.8f, 1.1f, 0.8f);
        }
    }
}
