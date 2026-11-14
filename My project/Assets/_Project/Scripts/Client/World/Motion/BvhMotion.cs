using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Game.Client.World.Motion
{
    /// <summary>
    /// A motion capture file in the BVH format (what DeepMotion and most mocap tools export): a tree of joints with
    /// their offsets in the rest pose and, per frame, the root position and the rotation of every joint.
    /// Kept as read: BVH is right-handed, conversion to Unity's axes is done by <see cref="BvhRetarget"/>.
    /// </summary>
    public sealed class BvhMotion
    {
        private const int AxisX = 0;
        private const int AxisY = 1;
        private const int AxisZ = 2;
        private const int NoChannel = -1;

        public readonly List<string> Names = new List<string>();
        /// <summary>Index of the parent joint; -1 for the root. A parent always comes before its children.</summary>
        public readonly List<int> Parents = new List<int>();
        public readonly List<Vector3> Offsets = new List<Vector3>();
        /// <summary>Column of the joint's first channel in a frame.</summary>
        private readonly List<int> _firstChannel = new List<int>();
        /// <summary>Per joint: the axes of its rotation channels in file order (3 entries per joint).</summary>
        private readonly List<int> _rotationAxes = new List<int>();
        /// <summary>Per joint: offset of the first rotation channel from the joint's first channel.</summary>
        private readonly List<int> _rotationStart = new List<int>();
        /// <summary>Per joint: offsets of the X, Y, Z position channels, or -1 (3 entries per joint).</summary>
        private readonly List<int> _positionChannels = new List<int>();

        public int FrameCount { get; private set; }
        public float FrameTime { get; private set; }
        public int ChannelCount { get; private set; }
        /// <summary>All frames in a row: FrameCount × ChannelCount numbers.</summary>
        public float[] Frames { get; private set; }

        public int IndexOf(string name) => Names.IndexOf(name);

        /// <summary>Position of a joint that has position channels (the root) in a frame, in the file's axes and units.</summary>
        public Vector3 Position(int joint, int frame)
        {
            int row = frame * ChannelCount + _firstChannel[joint];
            int x = _positionChannels[joint * 3 + AxisX];
            int y = _positionChannels[joint * 3 + AxisY];
            int z = _positionChannels[joint * 3 + AxisZ];
            return new Vector3(x == NoChannel ? 0f : Frames[row + x], y == NoChannel ? 0f : Frames[row + y], z == NoChannel ? 0f : Frames[row + z]);
        }

        /// <summary>
        /// Rotation of a joint relative to its parent in a frame, in the file's (right-handed) axes: the channel rotations
        /// multiplied in the order they are listed, as the format defines.
        /// </summary>
        public Quaternion LocalRotation(int joint, int frame)
        {
            int row = frame * ChannelCount + _firstChannel[joint] + _rotationStart[joint];
            Quaternion rotation = Quaternion.identity;
            for (int i = 0; i < 3; i++)
            {
                rotation *= Quaternion.AngleAxis(Frames[row + i], Axis(_rotationAxes[joint * 3 + i]));
            }
            return rotation;
        }

        private static Vector3 Axis(int axis) => axis == AxisX ? Vector3.right : axis == AxisY ? Vector3.up : Vector3.forward;

        /// <summary>Reads BVH text; null (with the reason in `error`) if the text is not a usable BVH.</summary>
        public static BvhMotion Parse(string text, out string error)
        {
            var motion = new BvhMotion();
            try
            {
                error = motion.Read(text);
            }
            catch (FormatException)
            {
                error = "a number is malformed";
            }
            return error == null ? motion : null;
        }

        private string Read(string text)
        {
            string[] lines = text.Split('\n');
            var stack = new List<int>();   // joints whose block is open, innermost last; EndSite for an "End Site" block
            int line = 0;
            int current = NoChannel;
            for (; line < lines.Length; line++)
            {
                string[] words = lines[line].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0 || words[0] == "HIERARCHY")
                {
                    continue;
                }
                if (words[0] == "MOTION")
                {
                    break;
                }
                switch (words[0])
                {
                    case "ROOT":
                    case "JOINT":
                        Names.Add(words.Length > 1 ? words[1] : string.Empty);
                        Parents.Add(stack.Count > 0 ? stack[stack.Count - 1] : NoChannel);
                        Offsets.Add(Vector3.zero);
                        _firstChannel.Add(ChannelCount);
                        _rotationStart.Add(0);
                        for (int i = 0; i < 3; i++)
                        {
                            _rotationAxes.Add(i);
                            _positionChannels.Add(NoChannel);
                        }
                        current = Names.Count - 1;
                        break;
                    case "End":
                        current = EndSite;
                        break;
                    case "{":
                        stack.Add(current);
                        break;
                    case "}":
                        if (stack.Count == 0)
                        {
                            return "unbalanced braces";
                        }
                        stack.RemoveAt(stack.Count - 1);
                        current = stack.Count > 0 ? stack[stack.Count - 1] : NoChannel;
                        break;
                    case "OFFSET":
                        if (current >= 0 && words.Length >= 4)
                        {
                            Offsets[current] = new Vector3(Number(words[1]), Number(words[2]), Number(words[3]));
                        }
                        break;
                    case "CHANNELS":
                        if (current < 0 || !ReadChannels(current, words))
                        {
                            return "unsupported CHANNELS line " + (line + 1);
                        }
                        break;
                }
            }
            if (Names.Count == 0 || line >= lines.Length)
            {
                return "no HIERARCHY or MOTION section";
            }
            return ReadFrames(lines, line + 1);
        }

        private const int EndSite = -2;

        private bool ReadChannels(int joint, string[] words)
        {
            int count = int.Parse(words[1], CultureInfo.InvariantCulture);
            if (words.Length < 2 + count)
            {
                return false;
            }
            int rotations = 0;
            for (int i = 0; i < count; i++)
            {
                string channel = words[2 + i];
                int axis = channel[0] == 'X' ? AxisX : channel[0] == 'Y' ? AxisY : channel[0] == 'Z' ? AxisZ : NoChannel;
                if (axis == NoChannel)
                {
                    return false;
                }
                if (channel.EndsWith("position", StringComparison.Ordinal))
                {
                    _positionChannels[joint * 3 + axis] = i;
                }
                else if (channel.EndsWith("rotation", StringComparison.Ordinal) && rotations < 3)
                {
                    if (rotations == 0)
                    {
                        _rotationStart[joint] = i;
                    }
                    _rotationAxes[joint * 3 + rotations++] = axis;
                }
                else
                {
                    return false;
                }
            }
            ChannelCount += count;
            return rotations == 3;   // the three rotation channels are expected to go in a row
        }

        private string ReadFrames(string[] lines, int line)
        {
            int frame = 0;
            for (; line < lines.Length; line++)
            {
                string text = lines[line].Trim();   // also drops the '\r' of Windows line ends
                if (text.StartsWith("Frames:", StringComparison.Ordinal))
                {
                    FrameCount = int.Parse(text.Substring("Frames:".Length), CultureInfo.InvariantCulture);
                    Frames = new float[FrameCount * ChannelCount];
                }
                else if (text.StartsWith("Frame Time:", StringComparison.Ordinal))
                {
                    FrameTime = Number(text.Substring("Frame Time:".Length));
                }
                else if (Frames != null && frame < FrameCount)
                {
                    string[] values = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length == 0)
                    {
                        continue;
                    }
                    if (values.Length != ChannelCount)
                    {
                        return $"frame {frame} has {values.Length} numbers, expected {ChannelCount}";
                    }
                    int row = frame * ChannelCount;
                    for (int i = 0; i < values.Length; i++)
                    {
                        Frames[row + i] = Number(values[i]);
                    }
                    frame++;
                }
            }
            if (Frames == null || FrameTime <= 0f || frame != FrameCount || FrameCount == 0)
            {
                return "the MOTION section is incomplete";
            }
            return null;
        }

        private static float Number(string text) => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
