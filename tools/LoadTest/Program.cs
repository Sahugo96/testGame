using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Game.Bots;
using Game.Server;
using Game.Server.Items;
using Game.Server.Net;
using Game.Shared.Content;
using Game.Shared.Core;

namespace Game.LoadTest
{
    /// <summary>Module 3 load test: runs bots in real time and prints server metrics and traffic.</summary>
    public static class Program
    {
        private const int DefaultBots = 50;
        private const int DefaultSeconds = 60;
        private const int ReportEverySeconds = 5;
        private static readonly string[] Classes = { "mage", "warrior" };

        public static int Main(string[] args)
        {
            Log.SetSink(new QuietSink());
            var errors = new List<string>();
            ContentDatabase content = ServerContentFactory.Build(JsonContentLoader.Load(errors), errors);
            if (errors.Count > 0)
            {
                Console.Error.WriteLine(string.Join(Environment.NewLine, errors));
                return 1;
            }
            string mode = args.Length > 0 ? args[0] : "local";
            if (mode == "sizes")
            {
                return PrintSizes(content);
            }
            if (mode == "connect" && args.Length >= 3)
            {
                return RunConnect(content, args[1], int.Parse(args[2], CultureInfo.InvariantCulture), Arg(args, 3, DefaultBots));
            }
            return RunLocal(content, Arg(args, 1, DefaultBots), Arg(args, 2, DefaultSeconds));
        }

        private static int RunLocal(ContentDatabase content, int bots, int seconds)
        {
            var server = new GameServer(content, new SystemRandom(Environment.TickCount), new InMemoryAuditLog());
            using (var host = new ServerNetworkHost(server, 0))
            using (BotSwarm swarm = BotSwarm.Connect("127.0.0.1", host.Port, bots, Classes, content.Config.WorldHalfSize))
            {
                Console.WriteLine($"Local load test: {bots} bots, {seconds} s, tick {content.Config.TickRate} Hz, interest radius {content.Config.InterestRadius} m");
                double tick = 1.0 / content.Config.TickRate;
                var clock = Stopwatch.StartNew();
                double next = 0d;
                double nextReport = ReportEverySeconds;
                long lastBytes = 0;
                double lastReport = 0d;
                var perSecond = new List<double>();
                while (clock.Elapsed.TotalSeconds < seconds)
                {
                    double now = clock.Elapsed.TotalSeconds;
                    if (now < next)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    next += tick;
                    host.Poll();
                    server.Tick();
                    swarm.Update((float)tick);
                    if (now >= nextReport)
                    {
                        long bytes = host.BytesSent;
                        double rate = (bytes - lastBytes) / (now - lastReport) / Math.Max(1, host.ConnectedPlayers);
                        perSecond.Add(rate);
                        Console.WriteLine($"[{now,5:0}s] {server.Metrics.Summary()}, out {rate / 1024d:0.0} KiB/s per player, bots online {swarm.Connected}");
                        lastBytes = bytes;
                        lastReport = now;
                        nextReport += ReportEverySeconds;
                    }
                }
                double average = 0d;
                foreach (double value in perSecond)
                {
                    average += value;
                }
                average = perSecond.Count > 0 ? average / perSecond.Count : 0d;
                int skills = 0;
                foreach (BotClient bot in swarm.Bots)
                {
                    skills += bot.SkillsUsed;
                }
                Console.WriteLine("RESULT " + string.Format(CultureInfo.InvariantCulture,
                    "bots {0}; tick avg {1:0.000} ms, p99 {2:0.000} ms, max {3:0.000} ms (budget {4:0.0} ms); entities/player {5:0.0} of {6}; out {7:0.0} KiB/s per player; skills used {8}",
                    bots, server.Metrics.AverageTickMs, server.Metrics.PercentileTickMs(0.99), server.Metrics.MaxTickMs, tick * 1000d,
                    server.Metrics.EntitiesPerPlayer, server.Metrics.Entities, average / 1024d, skills));
            }
            return 0;
        }

        private static int RunConnect(ContentDatabase content, string address, int port, int bots)
        {
            using (BotSwarm swarm = BotSwarm.Connect(address, port, bots, Classes, content.Config.WorldHalfSize))
            {
                Console.WriteLine($"{bots} bots connected to {address}:{port}. Ctrl+C to stop.");
                double tick = 1.0 / content.Config.TickRate;
                var clock = Stopwatch.StartNew();
                double nextReport = ReportEverySeconds;
                while (swarm.Connected > 0)
                {
                    swarm.Update((float)tick);
                    Thread.Sleep((int)(tick * 1000d));
                    if (clock.Elapsed.TotalSeconds >= nextReport)
                    {
                        Console.WriteLine($"[{clock.Elapsed.TotalSeconds,5:0}s] bots online {swarm.Connected}, received {swarm.BytesReceived / 1024d / clock.Elapsed.TotalSeconds / Math.Max(1, swarm.Connected):0.0} KiB/s per bot");
                        nextReport += ReportEverySeconds;
                    }
                }
            }
            return 0;
        }

        /// <summary>How big one snapshot of each kind is on the wire (bytes).</summary>
        private static int PrintSizes(ContentDatabase content)
        {
            var server = new GameServer(content, new SystemRandom(1), new InMemoryAuditLog());
            EntityId player = server.AddPlayer("mage");
            server.Tick();
            var outbox = server.GetPlayer(player).Outbox;
            int world = Game.Network.ProtocolCodec.Encode(outbox.World).Length;
            int state = Game.Network.ProtocolCodec.Encode(outbox.State).Length;
            Console.WriteLine($"world snapshot {world} B for {outbox.World.Entities.Length} entities ({world / Math.Max(1, outbox.World.Entities.Length)} B each); player state {state} B");
            return 0;
        }

        private static int Arg(string[] args, int index, int fallback) =>
            args.Length > index && int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;

        private sealed class QuietSink : ILogSink
        {
            public void Info(string tag, string message)
            {
            }

            public void Warning(string tag, string message) => Console.WriteLine($"[{tag}] {message}");

            public void Error(string tag, string message) => Console.Error.WriteLine($"[{tag}] {message}");
        }
    }
}
