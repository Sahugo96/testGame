using System;

namespace Game.Host
{
    /// <summary>
    /// Command-line options of a build: -server (headless dedicated server), -host, -connect &lt;address&gt;,
    /// -port &lt;n&gt;, -name &lt;text&gt;, -class &lt;id&gt;, -bots &lt;n&gt; (with -connect: headless load-test bots). Without -server/-host/-connect the start menu is shown.
    /// </summary>
    public sealed class LaunchOptions
    {
        public bool DedicatedServer;
        public bool Host;
        /// <summary>-bots N with -connect: N headless bots instead of a player (module 3 load tests).</summary>
        public int Bots;
        public string ConnectAddress;
        public int Port;
        public string PlayerName;
        public string ClassId;

        public static LaunchOptions Parse(string[] args)
        {
            var options = new LaunchOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i].ToLowerInvariant())
                {
                    case "-server":
                        options.DedicatedServer = true;
                        break;
                    case "-bots":
                        int.TryParse(next, out options.Bots);
                        i++;
                        break;
                    case "-host":
                        options.Host = true;
                        break;
                    case "-connect":
                        options.ConnectAddress = next;
                        i++;
                        break;
                    case "-port":
                        int.TryParse(next, out options.Port);
                        i++;
                        break;
                    case "-name":
                        options.PlayerName = next;
                        i++;
                        break;
                    case "-class":
                        options.ClassId = next;
                        i++;
                        break;
                }
            }
            return options;
        }

        public bool AutoStart => Host || !string.IsNullOrEmpty(ConnectAddress);
    }
}
