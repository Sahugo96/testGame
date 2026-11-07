using Game.Shared.Content;
using Game.Shared.Content.Dto;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Host
{
    /// <summary>
    /// Starts the vertical slice automatically when the configured scene (config.BootstrapScene) is played,
    /// so no scene or prefab setup is needed yet.
    /// </summary>
    public static class GameBootstrap
    {
        private const string RootName = "GameRoot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            GameConfig config = UnityContentLoader.Load<GameConfig>(ContentFileNames.Config);
            if (config == null || SceneManager.GetActiveScene().name != config.BootstrapScene)
            {
                return;
            }
            new GameObject(RootName).AddComponent<GameRoot>();
        }
    }
}
