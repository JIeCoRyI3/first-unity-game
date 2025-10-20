using UnityEngine;

public static class SnakeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSnakeGameExists()
    {
        var existing = Object.FindObjectOfType<SnakeGame>();
        if (existing != null)
        {
            return;
        }

        var controller = new GameObject("SnakeGame");
        controller.AddComponent<SnakeGame>();
    }
}
