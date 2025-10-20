using UnityEngine;
using UnityEngine.SceneManagement;

public static class SnakeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSnakeGameExists()
    {
        // Only spawn the Snake game controller inside the Snake scene
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Snake")
        {
            return;
        }

        var existing = Object.FindObjectOfType<SnakeGame>();
        if (existing != null)
        {
            return;
        }

        var controller = new GameObject("SnakeGame");
        controller.AddComponent<SnakeGame>();
    }
}
