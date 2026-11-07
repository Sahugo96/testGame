using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// One-click Windows build (menu "Game → Build Windows"): one executable for client, host and dedicated server,
    /// plus StartServer.bat / StopServer.bat / StartHost.bat next to it. Output: &lt;project&gt;/Builds/Windows.
    /// </summary>
    public static class BuildMenu
    {
        private const string MenuPath = "Game/Build Windows (client + server)";
        private const string OpenFolderMenuPath = "Game/Open Builds Folder";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string OutputFolder = "Builds/Windows";
        private const string ExecutableName = "MyGame.exe";
        private const string ConfigPath = "Assets/_Project/Resources/Content/config.json";
        private const int WindowWidth = 1280;
        private const int WindowHeight = 720;

        [MenuItem(MenuPath)]
        public static void BuildWindows()
        {
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
            public int DefaultPort;
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
            Write(folder, "StopServer.bat",
                "@echo off\r\n"
                + "cd /d \"%~dp0\"\r\n"
                + "if not exist server.pid (echo Server is not running & pause & exit /b)\r\n"
                + "set /p PID=<server.pid\r\n"
                + "taskkill /PID %PID% /F\r\n"
                + "del server.pid\r\n"
                + "pause\r\n");
            Write(folder, "StartHost.bat",
                "@echo off\r\n"
                + "cd /d \"%~dp0\"\r\n"
                + "start \"\" \"" + exe + "\" -host -port " + port + "\r\n");
        }

        private static void Write(string folder, string name, string text) =>
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
    }
}
