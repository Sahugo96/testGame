using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// One-click Windows build (menu "Game → Build Windows"): one executable for client, host and dedicated server,
    /// plus StartServer.bat / StopServer.bat / StartHost.bat / StartBots.bat next to it. Output: &lt;project&gt;/Builds/Windows.
    /// </summary>
    public static class BuildMenu
    {
        private const string MenuPath = "Game/Build Windows (client + server)";
        private const string OpenFolderMenuPath = "Game/Open Builds Folder";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string OutputFolder = "Builds/Windows";
        private const string ExecutableName = "MyGame.exe";
        private const string ConfigPath = "Assets/_Project/Resources/Content/base/config.json";
        private const int WindowWidth = 1280;
        private const int WindowHeight = 720;
        /// <summary>How long StopServer.bat waits for the graceful stop before killing the process.</summary>
        private const int StopWaitSeconds = 30;

        [MenuItem(MenuPath)]
        public static void BuildWindows()
        {
            if (!WorldMaterialAsset.Ensure())
            {
                return;
            }
            // Windowed and running in background: two copies can be tested on one computer.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = WindowWidth;
            PlayerSettings.defaultScreenHeight = WindowHeight;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(OutputFolder, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Build] Failed: {report.summary.result}, errors: {report.summary.totalErrors}. See the Console above.");
                return;
            }

            WriteScripts(OutputFolder);
            Debug.Log($"[Build] Done: {Path.GetFullPath(OutputFolder)} ({report.summary.totalSize / (1024 * 1024)} MB). "
                + "Zip the whole folder and send it.");
            EditorUtility.RevealInFinder(Path.Combine(OutputFolder, ExecutableName));
        }

        [MenuItem(OpenFolderMenuPath)]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorUtility.RevealInFinder(OutputFolder);
        }

        /// <summary>Only the field the scripts need; the port comes from content (config.DefaultPort), not from code.</summary>
        [System.Serializable]
        private sealed class PortConfig
        {
#pragma warning disable 0649 // assigned by JsonUtility
            public int DefaultPort;
#pragma warning restore 0649
        }

        private static void WriteScripts(string folder)
        {
            string exe = ExecutableName;
            int port = JsonUtility.FromJson<PortConfig>(File.ReadAllText(ConfigPath)).DefaultPort;
            Write(folder, "StartServer.bat",
                "@echo off\r\n"
                + "cd /d \"%~dp0\"\r\n"
                + "echo Dedicated server, port " + port + " (TCP). Log: server.log. Stop: StopServer.bat\r\n"
                + "start \"\" \"" + exe + "\" -batchmode -nographics -server -port " + port + " -logFile server.log\r\n"
                + "timeout /t 3 /nobreak >nul\r\n"
                + "powershell -NoProfile -Command \"Get-Content -Path 'server.log' -Wait -Tail 30\"\r\n");
            // Graceful stop: server.stop asks the server to save everyone and quit; a forced kill only if it hangs.
            Write(folder, "StopServer.bat",
                "@echo off\r\n"
                + "cd /d \"%~dp0\"\r\n"
                + "if not exist server.pid (echo Server is not running & pause & exit /b)\r\n"
                + "set /p PID=<server.pid\r\n"
                + "echo stop> server.stop\r\n"
                + "echo Saving and stopping the server...\r\n"
                + "for /l %%i in (1,1," + StopWaitSeconds + ") do (\r\n"
                + "  tasklist /FI \"PID eq %PID%\" | find \"%PID%\" >nul || goto stopped\r\n"
                + "  timeout /t 1 /nobreak >nul\r\n"
                + ")\r\n"
                + "echo The server did not stop in time: forced stop (progress since the last autosave may be lost).\r\n"
                + "taskkill /PID %PID% /F\r\n"
                + ":stopped\r\n"
                + "if exist server.pid del server.pid\r\n"
                + "if exist server.stop del server.stop\r\n"
                + "echo Stopped.\r\n"
                + "pause\r\n");
            Write(folder, "StartBots.bat",
                "@echo off\r\n"
                + "cd /d \"%~dp0\"\r\n"
                + "set /p ADDR=Server address (Enter = 127.0.0.1): \r\n"
                + "if \"%ADDR%\"==\"\" set ADDR=127.0.0.1\r\n"
                + "set /p COUNT=How many bots (Enter = 20): \r\n"
                + "if \"%COUNT%\"==\"\" set COUNT=20\r\n"
                + "start \"\" \"" + exe + "\" -batchmode -nographics -bots %COUNT% -connect %ADDR% -port " + port + " -logFile bots.log\r\n");
            Write(folder, "StartHost.bat",
                "@echo off\r\n"
                + "cd /d \"%~dp0\"\r\n"
                + "start \"\" \"" + exe + "\" -host -port " + port + "\r\n");
        }

        private static void Write(string folder, string name, string text) =>
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
    }
}
