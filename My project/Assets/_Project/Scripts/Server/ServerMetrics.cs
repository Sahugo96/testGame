using System;
using System.Globalization;

namespace Game.Server
{
    /// <summary>
    /// Server metrics (module 3): simulation time per tick over a sliding window (average, 99th percentile, maximum),
    /// players, entities and how many entities each player receives per snapshot (interest management at work).
    /// Traffic is counted by the transport (ServerNetworkHost).
    /// </summary>
    public sealed class ServerMetrics
    {
        private readonly double[] _ticks;
        private readonly double[] _sorted;
        private int _count;
        private int _next;

        public ServerMetrics(int windowTicks)
        {
            _ticks = new double[Math.Max(1, windowTicks)];
            _sorted = new double[_ticks.Length];
        }

        public int Players { get; private set; }
        public int Entities { get; private set; }
        public double EntitiesPerPlayer { get; private set; }
        public long TotalTicks { get; private set; }

        public void RecordTick(double milliseconds, int players, int entities, int entitiesSent)
        {
            _ticks[_next] = milliseconds;
            _next = (_next + 1) % _ticks.Length;
            _count = Math.Min(_count + 1, _ticks.Length);
            Players = players;
            Entities = entities;
            EntitiesPerPlayer = players > 0 ? (double)entitiesSent / players : 0d;
            TotalTicks++;
        }

        public double AverageTickMs
        {
            get
            {
                double sum = 0d;
                for (int i = 0; i < _count; i++)
                {
                    sum += _ticks[i];
                }
                return _count > 0 ? sum / _count : 0d;
            }
        }

        public double MaxTickMs
        {
            get
            {
                double max = 0d;
                for (int i = 0; i < _count; i++)
                {
                    max = Math.Max(max, _ticks[i]);
                }
                return max;
            }
        }

        public double PercentileTickMs(double percentile)
        {
            if (_count == 0)
            {
                return 0d;
            }
            Array.Copy(_ticks, _sorted, _count);
            Array.Sort(_sorted, 0, _count);
            int index = (int)Math.Min(_count - 1, Math.Ceiling(percentile * _count) - 1);
            return _sorted[Math.Max(0, index)];
        }

        public string Summary() => string.Format(CultureInfo.InvariantCulture,
            "players {0}, entities {1}, entities/player {2:0.0}, tick avg {3:0.000} ms, p99 {4:0.000} ms, max {5:0.000} ms",
            Players, Entities, EntitiesPerPlayer, AverageTickMs, PercentileTickMs(0.99), MaxTickMs);
    }
}
